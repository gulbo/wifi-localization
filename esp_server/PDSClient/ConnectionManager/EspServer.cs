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
        private TcpListener tcp_listener_;
        private ManualResetEvent[] time_sync_events_;
        private AutoResetEvent[] packets_ready_events;
        private DBConnect DBConnection;
        private Action ConnectionErrorAction { get; set; }
        private CancellationTokenSource cancellation_token_source_;
        private int boards_number_;
        private bool is_running_;
        private EspBoard[] esp_boards_;
        private List<Thread> board_handlers_;
        private Thread connection_handler_;

        public EspServer(int boards_number, DBConnect DBConnection, Action ConnectionErrorAction)
        {
            boards_number_ = boards_number;
            esp_boards_ = new EspBoard[boards_number];
            tcp_listener_ = new TcpListener(IPAddress.Any, ESP_SERVER_PORT);
            board_handlers_ = new List<Thread>();
            is_running_ = false;
            this.DBConnection = DBConnection;
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
            stop();
        }

        public void connectionHandler()
        {
            tcp_listener_.Start();
            writeDebugLine_("Handler delle connessioni in ingresso avviato");
            is_running_ = true;

            while (true)
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

                // creo un thread per gestire questa connessione
                Thread board_handler = new Thread(new ParameterizedThreadStart(this.boardHandler));
                board_handler.Name = "ConnectionHandler";
                board_handler.Start(socket);
                board_handlers_.Add(board_handler);
            }
        }

        public void boardHandler(object arg)
        {
            Socket socket = (Socket) arg;
            CancellationToken token = cancellation_token_source_.Token;
            EspBoard board = new EspBoard(socket, token, time_sync_events_);
            if (board.initialize())
            {
                writeDebugLine_("Nuova board inizializzata");
                esp_boards_[board.getBoardID() - 1] = board; //sovrascrivere la board vecchia?

                while (!token.IsCancellationRequested)
                {
                    // ricevo i pacchetti
                    List<Pacchetto> packet_list = board.receivePackets();

                    // inserisco i pacchetti nel DB
                    DBConnection.InsertPacchetto(packet_list);
                    packets_ready_events[board.getBoardID() -1].Set();
                }
            }
        }
  
        private void OrderedExit(Socket s, Dispatcher dispatcher)
        {
            if (s != null)
            {
                s.Close();
            }

            if (is_running_)
            {
                if (s != null && s.Connected)
                {
                    s.Close();
                }
                dispatcher.BeginInvoke(ConnectionErrorAction, DispatcherPriority.Send);
            }
        }

        public static Int32 getUnixEpoch()
        {
            return (Int32)(DateTime.Now.ToLocalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        private static void keepConnectionAlive_(Socket socket, bool on, uint keepAliveTime, uint keepAliveInterval)
        {
            int size = Marshal.SizeOf(new uint());

            var inOptionValues = new byte[size * 3];

            BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)keepAliveTime).CopyTo(inOptionValues, size);
            BitConverter.GetBytes((uint)keepAliveInterval).CopyTo(inOptionValues, size * 2);

            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }

        public void stop()
        {
            writeDebugLine_("Fermando ESP Server...");
            is_running_ = false;
            cancellation_token_source_.Cancel();

            // interrompo il connection handler
            if (connection_handler_.IsAlive)
                connection_handler_.Interrupt();
            var result = connection_handler_.Join(1000);
            if (!result)
                connection_handler_.Abort();

            tcp_listener_.Stop();

            // interrompo i board handlers
            for (int i = 0; i < boards_number_; i++)
            foreach (Thread thread in board_handlers_)
            {
                if (thread.IsAlive)
                    thread.Interrupt();
            }

            foreach (Thread thread in board_handlers_)
            {
                result = thread.Join(1000);
                if (!result)
                    thread.Abort();
            }
        }

        public void waitAllBoardsData()
        {
            AutoResetEvent.WaitAll(packets_ready_events);
        }

        private void writeDebugLine_(String str)
        {
            System.Diagnostics.Debug.WriteLine("EspServer: " + str);
        }
    }
}
