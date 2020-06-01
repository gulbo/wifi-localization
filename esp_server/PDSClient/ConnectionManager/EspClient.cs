using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Windows.Controls;
using System.Net;
using PDSClient.ConnectionManager.ConnException;
using System.Runtime.InteropServices;

namespace PDSClient.ConnectionManager
{
    class EspClient : IPositionSender
    {

        private TcpListener server;
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

        public int NBoards { get; private set; }

        public EspClient(int nBoards, DBConnect DBConnection, Action ConnectionErrorAction)
        {
            
            this.NBoards = nBoards;
            this.server = new TcpListener(IPAddress.Any, 7999);
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

            this.server.Start();

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
                s = server.AcceptSocket();
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
                    server.Stop();
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
            List<Packet> pktLst = new List<Packet>();
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
                    Packet pkt = Packet.ReceivePacket(s, idBoard, ct);
                    pktLst.Add(pkt);
                }
                mre2.Set();
                System.Diagnostics.Debug.WriteLine("Sleeping for timestamp sync...");
                ManualResetEvent.WaitAll(mres2);
                System.Diagnostics.Debug.WriteLine("Woke up from timestamp sync...");
                netTimestamp = GetUnixTimestampNet();
                System.Diagnostics.Debug.WriteLine(index + ") Timestamp retrieved: " + netTimestamp + "... sending to board");
                s.Send(BitConverter.GetBytes(netTimestamp), 4, SocketFlags.None);

                DBConnection.Insert(pktLst);
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

            ControlledRecv(s, receiveBuffer, 4, ct);
            idBoard = BitConverter.ToInt32(receiveBuffer, 0);
            idBoard = IPAddress.NetworkToHostOrder(idBoard);
            System.Diagnostics.Debug.WriteLine(index + ": idboard = " + idBoard);
            msg = msg + " " + idBoard;

            ControlledRecv(s, receiveBuffer, 6, ct);
            Array.Copy(receiveBuffer, 0, mac_addr, 0, 6);
            Array.Copy(mac_addr, mac_addresses[index], 6);
            PhysicalAddress espMac = new PhysicalAddress(mac_addr);
            System.Diagnostics.Debug.WriteLine(index + ": mac received = " + espMac.ToString());
            msg = msg + " " + espMac.ToString();
 
            //attendo che tutti i thread abbiano inserito il mac address alla posizione relativa e resetto l'evento
            System.Diagnostics.Debug.WriteLine(index + ": setting mre and waiting for the other boards to receive their mac");
            mre1.Set();
            ManualResetEvent.WaitAll(mres1);
            mre2.Reset();
            System.Diagnostics.Debug.WriteLine(index + ": woken up from WaitAll. All macs have been received");

            //response OK
            msg = "OK";
            //TODO rimpiazzare
            int n = 0;
            //int n = NBoards - 1;
            //n = IPAddress.HostToNetworkOrder(n);
            Encoding.ASCII.GetBytes(msg, 0, 2, receiveBuffer, 0);
            Array.Copy(BitConverter.GetBytes(n), 0, receiveBuffer, 2, 4);
            System.Diagnostics.Debug.WriteLine(index + ": sending OK to my board");
            s.Send(receiveBuffer, 6, SocketFlags.None);
            int offset = 0;
            if(n != 0)
            {
                for (int i = 0; i < NBoards; i++)
                {
                    if (i != index)
                    {
                        Array.Copy(mac_addresses[i], 0, receiveBuffer, offset, 6);
                        offset += 6;
                    }
                }
                System.Diagnostics.Debug.WriteLine(index + ": sending list of macs to my board");
                s.Send(receiveBuffer, offset, SocketFlags.None);
            }
            
            //parte del DE
            /*ControlledRecv(s, receiveBuffer, 6);

            msg = Encoding.ASCII.GetString(receiveBuffer, 0, 2);
            System.Diagnostics.Debug.WriteLine("DE part, msg received: " + msg);
            n = BitConverter.ToInt32(receiveBuffer, 2);
            n = IPAddress.NetworkToHostOrder(n);*/

            //TODO da implementare riconoscimento e verifica dei MAC
            mre1.Reset();
            if(n != 0)
            {
                System.Diagnostics.Debug.WriteLine(index + ") Entering sniffing phase");
                sniffingPhase(s, mre2, index, ref verifiedBoards);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(index + ") skipping sniffing phase");
                //receive DE
                ControlledRecv(s, receiveBuffer, 2, ct);
                msg = Encoding.ASCII.GetString(receiveBuffer, 0, 2);
                System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): received " + msg);

                //receive 0
                ControlledRecv(s, receiveBuffer, 4, ct);
                n = BitConverter.ToInt32(receiveBuffer, 0);
                n = IPAddress.NetworkToHostOrder(n);
                System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): nBoards identified = " + n);

                //send GO
                msg = "GO";
                Encoding.ASCII.GetBytes(msg, 0, 2, receiveBuffer, 0);
                System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): synch on GO send");
                s.Send(receiveBuffer, 2, SocketFlags.None);
                System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): GO sent");
            }

            //OK/RT
            //questa fase è preceduta dalla sincronizzazione tra tutti i thread. Attendo che tutte le schede siano a questo punto e parto
            mre1.Set();
            ManualResetEvent.WaitAll(mres1);
            mre2.Reset();

            int netTimestamp = GetUnixTimestampNet();
            s.Send(BitConverter.GetBytes(netTimestamp), 4, SocketFlags.None);

            return idBoard;
        }

        public void sniffingPhase(Socket s, ManualResetEvent startMRE, int index, ref int verifiedBoards)
        {
            //startMRE serve per sincronizzare tutti i thread all'avvio della sniffing phase
            //endMRE serve per attendere che tutti i thread hanno finito la sniffing phase, controllano il numero di
            //board identificate da ogni board. Se pari a NBoards - 1, procedi
            byte[] buffer = new byte[1024];
            ManualResetEvent endMRE = countMRE[index];
            CancellationToken ct = ctArray[index];
            System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): starting sniffing phase");
            System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): synchronizing at start...");
            startMRE.Set();
            ManualResetEvent.WaitAll(mres2);
            System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): woke up from sync");
            while (true)
            {
                System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): waiting on recv...");
                ControlledRecv(s, buffer, 2, ct);
                String msg = Encoding.ASCII.GetString(buffer, 0, 2);
                System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): received " + msg);
                if (msg.Equals("PN"))
                {
                    System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "):PN message received");
                    continue;
                }
                if (msg.Equals("DE"))
                {
                    //ultimo ping dalle schede, verifico l'intero ricevuto
                    ControlledRecv(s, buffer, 4, ct);
                    int n = BitConverter.ToInt32(buffer, 0);
                    n = IPAddress.NetworkToHostOrder(n);
                    System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): nBoards identified = " + n);
                    System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): NBoards variable is = " + NBoards);

                    if(n == NBoards - 1)
                    {
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): number is fine, all boards identified");
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): before is " + verifiedBoards);
                        Interlocked.Increment(ref verifiedBoards);
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): after is " + verifiedBoards);
                    }
                    System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): going to sleep at endMRE");

                    endMRE.Set();
                    ManualResetEvent.WaitAll(countMRE);
                    startMRE.Reset();
                    System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): woken up from endMRE sleep");
                    System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): value of count is: " + verifiedBoards);
                    if (verifiedBoards == NBoards)
                    {
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): number is correct... Ending sniffing phase");
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): sending GO");
                        msg = "GO";
                        Encoding.ASCII.GetBytes(msg, 0, 2, buffer, 0);
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): synch on GO send");
                        startMRE.Set();
                        ManualResetEvent.WaitAll(mres2);
                        endMRE.Reset();
                        s.Send(buffer, 2, SocketFlags.None);
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): GO sent");

                        break;
                    }
                    else
                    {
                        if(n == NBoards - 1)
                        {
                            Interlocked.Decrement(ref verifiedBoards);
                        }
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): number is incorrect... Sending RT");
                        msg = "RT";
                        Encoding.ASCII.GetBytes(msg, 0, 2, buffer, 0);
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): sync on RT send");
                        startMRE.Set();
                        ManualResetEvent.WaitAll(mres2);
                        endMRE.Reset();
                        s.Send(buffer, 2, SocketFlags.None);
                        System.Diagnostics.Debug.WriteLine(index + "(" + Thread.CurrentThread.ManagedThreadId + "): RT sent");
                    }
                }
            }
        }

        public void Shutdown()
        {
            System.Diagnostics.Debug.WriteLine("Shutting down operation on EspClient starting...");
            Running = false;
            cts.Cancel();

            for (int i = 0; i < NBoards; i++)
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

            server.Stop();

            for (int i = 0; i < NBoards; i++)
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
