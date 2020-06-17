using System;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Net;

namespace PDSClient.ConnectionManager
{
    public class EspServer
    {
        private const int ESP_SERVER_PORT = 7999;
        private TcpListener tcp_listener_;                              /** listener per nuove connessioni tcp in ingresso */
        private Thread connection_handler_;                             /** thread che gestisce le connessioni in ingresso */
        private ManualResetEvent[] time_sync_events_;                   /** evento per sincronizzare il tempo su tutte le boards */    
        private AutoResetEvent[] packets_ready_events;                  /** evento per segnalare l'avvenuto inserimento dei pacchetti nel DB */
        private DBConnect db_connect;                                   /** connessione con il DB */
        private Action ConnectionErrorAction { get; set; }              /** Action per segnalare un errore di connessione **/
        private CancellationTokenSource cancellation_token_source_;     /** generatore di Tokens per fermare i threads in esecuzione */
        private int boards_number_;                                     /** numbero di boards presenti */
        private List<Thread> board_handlers_;                           /** lista dei threads che gestiscono le connessioni con le boards */
        private bool is_running_;                                      

        public EspServer(int boards_number, DBConnect DBConnection, Action ConnectionErrorAction)
        {
            boards_number_ = boards_number;
            tcp_listener_ = new TcpListener(IPAddress.Any, ESP_SERVER_PORT);
            board_handlers_ = new List<Thread>();
            is_running_ = false;
            this.db_connect = DBConnection;
            this.ConnectionErrorAction = ConnectionErrorAction;
            cancellation_token_source_ = new CancellationTokenSource();

            time_sync_events_ = new ManualResetEvent[boards_number];
            packets_ready_events = new AutoResetEvent[boards_number];
            for (int i = 0; i < boards_number; i++)
            {
                time_sync_events_[i] = new ManualResetEvent(false);
                packets_ready_events[i] = new AutoResetEvent(false);
            }

            connection_handler_ = new Thread(this.connectionHandler);
            connection_handler_.Name = "ConnectionHandler";
            connection_handler_.Start();
        }
        
        ~EspServer()
        {
            if (is_running_)
                stop();
        }

        /** 
         * Gestisce le nuove richieste di connessione, su un thread a sé stante
         */
        public void connectionHandler()
        {
            tcp_listener_.Start();
            writeDebugLine_("Handler delle connessioni in ingresso avviato");
            is_running_ = true;

            CancellationToken token = cancellation_token_source_.Token;
            while (!token.IsCancellationRequested)
            {
                Socket socket = null;
                var dispatcher = System.Windows.Application.Current.Dispatcher;

                try
                {
                    socket = tcp_listener_.AcceptSocket();
                }
                catch (Exception e)
                {
                    writeDebugLine_(e.ToString());
                    if (socket != null)
                        socket.Close();

                    dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        System.Windows.MessageBox.Show(e.ToString(), "Alert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        tcp_listener_.Stop();
                        Environment.Exit(0);
                    }));

                }

                keepConnectionAlive_(socket, true, 4000, 500);

                // creo un thread per gestire questa connessione con la board
                Thread board_handler = new Thread(new ParameterizedThreadStart(this.boardHandler));
                board_handler.Start(new Tuple<Socket, Thread>(socket, board_handler));
                Monitor.Enter(board_handlers_);
                board_handlers_.Add(board_handler);
                Monitor.Exit(board_handlers_);
            }
            writeDebugLine_("ConnectionHandler fermato");
        }

        /** 
         * Gestisce la connessione con una singola board
         */
        public void boardHandler(object arg)
        {
            Socket socket = ((Tuple<Socket,Thread>) arg).Item1;
            Thread thread = ((Tuple<Socket, Thread>)arg).Item2;
            CancellationToken token = cancellation_token_source_.Token;
            EspBoard board = new EspBoard(socket, token, time_sync_events_);
            
            try
            {
                if (board.initialize())
                {
                    writeDebugLine_("Nuova board inizializzata");
                    thread.Name = "Board" + board.getBoardID();
                    signalBoardConnected_(board.getBoardID());

                    while (!token.IsCancellationRequested)
                    {
                        writeDebugLine_(getBoardsConnected() + " boards connesse");
                        // ricevo i pacchetti
                        List<Pacchetto> packet_list = board.receivePackets();

                        // inserisco i pacchetti nel DB
                        db_connect.InsertPacchetto(packet_list);
                        packets_ready_events[board.getBoardID() -1].Set();
                    }
                }
            }
            catch (Exception ex)
            {
                if (socket != null || socket.Connected)
                {
                    socket.Close();
                    if (board.getBoardID() != -1)
                    {
                        writeDebugLine_("Board" + board.getBoardID() + " connessione persa");
                        signalBoardDisconnected_(board.getBoardID());
                    }
                }
                Monitor.Enter(board_handlers_);
                board_handlers_.Remove(thread);
                Monitor.Exit(board_handlers_);
            }
        }

        private void signalBoardDisconnected_(int board_id)
        {
            //var dispatcher = System.Windows.Application.Current.Dispatcher;
            //dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
            //{
            //    System.Windows.MessageBox.Show("Board" + board_id + " disconnessa. Riconnettere!", "Alert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            //}));

            new Thread(() => System.Windows.MessageBox.Show(
                                                    "Board" + board_id + " disconnessa. Riconnettere!",
                                                    "Alert",
                                                    System.Windows.MessageBoxButton.OK,
                                                    System.Windows.MessageBoxImage.Error)
                                                ).Start();
        }

        private void signalBoardConnected_(int board_id)
        {
            //var dispatcher = System.Windows.Application.Current.Dispatcher;
            //dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
            //{
            //    System.Windows.MessageBox.Show("Board" + board_id + " connessa.", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            //}));

            new Thread(() => System.Windows.MessageBox.Show(
                                                   "Board" + board_id + " connessa.",
                                                   "Info",
                                                   System.Windows.MessageBoxButton.OK,
                                                   System.Windows.MessageBoxImage.Information)
                                               ).Start();
        }

        /**
         * Ottieni la Unix epoch in secondi
         */
        public static Int32 getUnixEpoch()
        {
            return (Int32)(DateTime.Now.ToLocalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        /**
         * Imposta il keepAliveTime e keepAliveInterval su una connessione
         */
        private static void keepConnectionAlive_(Socket socket, bool on, uint keepAliveTime, uint keepAliveInterval)
        {
            int size = Marshal.SizeOf(new uint());

            var inOptionValues = new byte[size * 3];

            BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)keepAliveTime).CopyTo(inOptionValues, size);
            BitConverter.GetBytes((uint)keepAliveInterval).CopyTo(inOptionValues, size * 2);

            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        /**
         * Ferma il server
         */
        public void stop()
        {
            writeDebugLine_("Fermando ESP Server...");
            is_running_ = false;
            cancellation_token_source_.Cancel();

            // interrompo il connection handler
            tcp_listener_.Stop();
            if (connection_handler_.IsAlive)
                connection_handler_.Interrupt();
            var result = connection_handler_.Join(100);
            if (!result)
                connection_handler_.Abort();

            // interrompo i board handlers
            Monitor.Enter(board_handlers_);
            foreach (Thread thread in board_handlers_)
            {
                if (thread.IsAlive)
                    thread.Interrupt();
            }

            foreach (Thread thread in board_handlers_)
            {
                result = thread.Join(100);
                if (!result)
                    thread.Abort();
            }
            Monitor.Exit(board_handlers_);
        }

        /**
         * Attenti che tutte le board abbiano inserito i pacchetti nel DB
         */
        public void waitAllBoardsData()
        {
            AutoResetEvent.WaitAll(packets_ready_events);
        }

        private void writeDebugLine_(String str)
        {
            System.Diagnostics.Debug.WriteLine("EspServer: " + str);
        }

        public int getBoardsConnected()
        {
            Monitor.Enter(board_handlers_);
            int connected_boards_count = board_handlers_.Count;
            Monitor.Exit(board_handlers_);
            return connected_boards_count;
        }
    }
}
