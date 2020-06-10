using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Net;
using PDSClient.ConnectionManager.ConnException;
using System.Runtime.InteropServices;

namespace PDSClient.ConnectionManager
{
    public class EspServer
    {

        private TcpListener _tcp_listener;
        private List<Thread> threads;
        private ManualResetEvent[] mres1, mres2, countMRE;
        private AutoResetEvent[] phoneInfo_are;
        private byte[][] mac_addresses;
        private DBConnect DBConnection;
        private Action ConnectionErrorAction { get; set; }
        private int verifiedBoards;
        private CancellationTokenSource cts;
        private CancellationToken[] ctArray;
        public bool Running { get; private set; }

        public int BoardsNumber { get; private set; }

        public EspServer(int nBoards, DBConnect DBConnection, Action ConnectionErrorAction)
        {
            
            BoardsNumber = nBoards;
            _tcp_listener = new TcpListener(IPAddress.Any, 7999);
            this.DBConnection = DBConnection;
            this.ConnectionErrorAction = ConnectionErrorAction;
            threads = new List<Thread>();
            mres1 = new ManualResetEvent[nBoards];
            mres2 = new ManualResetEvent[nBoards];
            countMRE = new ManualResetEvent[nBoards];
            phoneInfo_are = new AutoResetEvent[nBoards];
            mac_addresses = new byte[nBoards][];
            ctArray = new CancellationToken[nBoards];
            cts = new CancellationTokenSource();
            Running = false;

            for (int i = 0; i < nBoards; i++)
            {
                mac_addresses[i] = new byte[6];
            }

            this._tcp_listener.Start();

            for (int i = 0; i < nBoards; i++)
            {
                var t = new Thread(new ParameterizedThreadStart(this.ControlledReceivePkt));
                
                mres1[i] = new ManualResetEvent(false);
                mres2[i] = new ManualResetEvent(false);
                countMRE[i] = new ManualResetEvent(false);
                phoneInfo_are[i] = new AutoResetEvent(false);
                threads.Add(t);
                t.Start(new Tuple<int, CancellationToken>(i, cts.Token));
            }

            Running = true;
        }

        public void ControlledReceivePkt(object t)
        {
            int index = ((Tuple<int, CancellationToken>) t).Item1;
            CancellationToken ct = ((Tuple<int, CancellationToken>)t).Item2;
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            var errMsg = "";
            Socket s = null;
            ctArray[index] = ct;
            ct.Register(new Action(() => CancelDelegate()));

            try
            {
                System.Diagnostics.Debug.WriteLine((int)index + ") Before acceptSocket");
                s = _tcp_listener.AcceptSocket();
                SetKeepAlive(s, true, 3000, 500);
                System.Diagnostics.Debug.WriteLine((int)index + ") After acceptSocket");
                ReceivePkt(s, index);
            }
            catch(MyThreadExitException e)
            {
                System.Diagnostics.Debug.WriteLine((int)index + ") MyThreadExitException caught");
                if (s != null)
                    s.Close();
                return;
            }
            catch(ThreadInterruptedException e)
            {
                System.Diagnostics.Debug.WriteLine((int)index + ") ThreadInterruptedException catched... terminating");
                if(s != null)
                    s.Close();
                return;
            }
            catch(ThreadAbortException e)
            {
                System.Diagnostics.Debug.WriteLine((int)index + ") ThreadAbortException catched... terminating");
                if(s != null)
                    s.Close();
                return;
            }
            catch(MySocketException e) when (e.Status != MySocketException.INVALID_SSID_LEN)
            {
                System.Diagnostics.Debug.WriteLine((int)index + ") MySocketException catched... terminating");
                OrderedExit(s, dispatcher);
                return;
            }
            catch(MySocketException e) when (e.Status == MySocketException.INVALID_SSID_LEN)
            {
                System.Diagnostics.Debug.WriteLine((int)index + ") critical error found in ssidLen... terminating");
                System.Windows.MessageBox.Show("Critical error. Reboot the system.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                OrderedExit(s, dispatcher);
                return;
            }
            catch (SocketException e)
            {
                System.Diagnostics.Debug.WriteLine((int)index + ") SocketException catched... terminating");
                OrderedExit(s, dispatcher);
                return;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception caught: " + e.ToString());
                errMsg = e.ToString();
                if (s != null)
                    s.Close();

                dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    System.Windows.MessageBox.Show(errMsg, "Alert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    _tcp_listener.Stop();
                    Environment.Exit(0);
                }));
            }
        }

        private void CancelDelegate()
        {
            System.Diagnostics.Debug.WriteLine("CancelDelegate invoked");
        }

        private void OrderedExit(Socket s, Dispatcher dispatcher)
        {
            if (s != null)
            {
                s.Close();
            }

            if (Running)
            {
                if (s != null && s.Connected)
                {
                    s.Close();
                }
                dispatcher.BeginInvoke(ConnectionErrorAction, DispatcherPriority.Send);
            }
        }

        public void ReceivePkt(Socket s, Object index)
        {
            int idBoard = -1;
            byte[] receiveBuffer = new byte[512];
            List<Pacchetto> pktLst = new List<Pacchetto>();
            ManualResetEvent mre1 = mres1[(int)index];
            ManualResetEvent mre2 = mres2[(int)index];
            AutoResetEvent are = phoneInfo_are[(int)index];
            CancellationToken ct = ctArray[(int)index];
            int n, netTimestamp;

            init_proto(s, (int)index, mre1, mre2, are);

            while (true)
            {
                System.Diagnostics.Debug.WriteLine("Performing receive...");
                ControlledRecv(s, receiveBuffer, 4, ct);
                idBoard = BitConverter.ToInt32(receiveBuffer, 0);
                idBoard = IPAddress.NetworkToHostOrder(idBoard);

                ControlledRecv(s, receiveBuffer, 4, ct);
                n = BitConverter.ToInt32(receiveBuffer, 0);
                n = IPAddress.NetworkToHostOrder(n);
                String result = "Numero di entry: " + n;
                System.Diagnostics.Debug.WriteLine("Number of entries: " + n + " from idBoard: " + idBoard);
                mre2.Reset();
                for (int i = 0; i < n; i++)
                {
                    Pacchetto pkt = Pacchetto.RiceviPacchetto(s, idBoard, ct);
                    pktLst.Add(pkt);
                }
                mre2.Set();
                System.Diagnostics.Debug.WriteLine("Sleeping for timestamp sync...");
                ManualResetEvent.WaitAll(mres2);
                System.Diagnostics.Debug.WriteLine("Woke up from timestamp sync...");
                netTimestamp = GetUnixTimestampNet();
                System.Diagnostics.Debug.WriteLine(index + ") Timestamp retrieved: " + netTimestamp + "... sending to board");
                s.Send(BitConverter.GetBytes(netTimestamp), 4, SocketFlags.None);

                DBConnection.InsertPacchetto(pktLst);
                System.Diagnostics.Debug.WriteLine(index + ") pktList inserted into db... Setting the AutoResetEvent");
                are.Set();

                pktLst.Clear();
            }
               System.Diagnostics.Debug.WriteLine("End receiving");
        }

        public static bool MyRecv(Socket s, byte[] receiveBuffer, int nBytes, CancellationToken ct)
        {
            int result = 0;
            while(true)
            {
                if(s.Poll(1000000, SelectMode.SelectRead))
                {
                    result = s.Receive(receiveBuffer, nBytes, SocketFlags.None);
                    break;
                }
                else
                {
                    if (ct.IsCancellationRequested)
                    {
                        throw new MyThreadExitException();
                    }
                }
            }
            return result == nBytes;
        }

        public static void ControlledRecv(Socket s, byte[] receiveBuffer, int nBytes, CancellationToken ct)
        {
            if (!MyRecv(s, receiveBuffer, nBytes, ct) || !s.Connected)
                throw new MySocketException("Error on Receive");
        }

        public static Int32 GetUnixTimestamp()
        {
            return (Int32)(DateTime.Now.ToLocalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        private int GetUnixTimestampNet()
        {
            Int32 timestamp = GetUnixTimestamp();
            return IPAddress.HostToNetworkOrder((int)timestamp);
        }

        public void WaitAll()
        {
            AutoResetEvent.WaitAll(phoneInfo_are);
        }

        public void WaitAny()
        {
            AutoResetEvent.WaitAny(phoneInfo_are);
        }

        public int init_proto(Socket s, int index, ManualResetEvent mre1, ManualResetEvent mre2, AutoResetEvent are)
        {
            int idBoard;
            byte[] receiveBuffer = new byte[256 * 1024];
            byte[] mac_addr = new byte[6];
            CancellationToken ct = ctArray[index];

            // ricevo HI
            ControlledRecv(s, receiveBuffer, 2, ct);
            System.Diagnostics.Debug.WriteLine(index + ": new connection opened");
            String msg = Encoding.ASCII.GetString(receiveBuffer, 0, 2);
            if (msg != "HI")
            {
                System.Diagnostics.Debug.WriteLine(index + ": unexpected msg received " + msg);
                msg = "ERROR, closed connection";
                s.Close();
                return -1;
            }

            // ricevo BoardID 4 bytes
            ControlledRecv(s, receiveBuffer, 4, ct);
            idBoard = BitConverter.ToInt32(receiveBuffer, 0);
            idBoard = IPAddress.NetworkToHostOrder(idBoard);
            System.Diagnostics.Debug.WriteLine(index + ": idboard = " + idBoard);
            msg = msg + " " + idBoard;

            // ricevo MAC 6 bytes
            ControlledRecv(s, receiveBuffer, 6, ct);
            Array.Copy(receiveBuffer, 0, mac_addr, 0, 6);
            Array.Copy(mac_addr, mac_addresses[index], 6);
            PhysicalAddress espMac = new PhysicalAddress(mac_addr);
            System.Diagnostics.Debug.WriteLine(index + ": mac received = " + espMac.ToString());
            msg = msg + " " + espMac.ToString();

            //rispondo HI
            msg = "HI";
            Encoding.ASCII.GetBytes(msg, 0, 2, receiveBuffer, 0);
            System.Diagnostics.Debug.WriteLine(index + ": sending HI to my board");
            s.Send(receiveBuffer, 2, SocketFlags.None);

            // attendo che tutti i thread abbiano inserito il mac address alla posizione relativa e resetto l'evento
            System.Diagnostics.Debug.WriteLine(index + ": setting mre and waiting for the other boards to receive their mac");
            mre1.Set();
            ManualResetEvent.WaitAll(mres1);
            mre2.Reset();
            System.Diagnostics.Debug.WriteLine(index + ": woken up from WaitAll. All macs have been received");

            mre1.Reset();
     
            //invia GO timestamp
            msg = "GO";
            Encoding.ASCII.GetBytes(msg, 0, 2, receiveBuffer, 0);
            System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): synch on GO send");
            s.Send(receiveBuffer, 2, SocketFlags.None);
            System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): GO sent");


            //questa fase è preceduta dalla sincronizzazione tra tutti i thread. Attendo che tutte le schede siano a questo punto e parto
            mre1.Set();
            ManualResetEvent.WaitAll(mres1);
            mre2.Reset();

            int netTimestamp = GetUnixTimestampNet();
            s.Send(BitConverter.GetBytes(netTimestamp), 4, SocketFlags.None);

            return idBoard;
        }

        public void Shutdown()
        {
            System.Diagnostics.Debug.WriteLine("Shutting down operation on EspClient starting...");
            Running = false;
            cts.Cancel();

            for (int i = 0; i < BoardsNumber; i++)
            {
                if (threads[i].IsAlive)
                {
                    System.Diagnostics.Debug.WriteLine("Interrupting " + i + " EspClient's thread");
                    threads[i].Interrupt();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Thread " + i + " already stopped");
                }

            }

            _tcp_listener.Stop();

            for (int i = 0; i < BoardsNumber; i++)
            {
                System.Diagnostics.Debug.WriteLine("Joining " + i + " EspClient's thread");
                var result = threads[i].Join(1000);
                if (!result)
                {
                    System.Diagnostics.Debug.WriteLine("Thread " + i + " did not terminate... aborting...");
                    threads[i].Abort();
                }
            }

            System.Diagnostics.Debug.WriteLine("Terminating TcpListener");
        }

        private static void SetKeepAlive(Socket socket, bool on, uint keepAliveTime, uint keepAliveInterval)
        {
            int size = Marshal.SizeOf(new uint());

            var inOptionValues = new byte[size * 3];

            BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)keepAliveTime).CopyTo(inOptionValues, size);
            BitConverter.GetBytes((uint)keepAliveInterval).CopyTo(inOptionValues, size * 2);

            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }
    }
}
