using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using WifiLocalization.Utilities;

namespace WifiLocalization.ConnectionManager
{
    public class EspBoard
    {
        private Socket socket_;
        private int board_id_;
        private PhysicalAddress py_address_;
        private CancellationToken cancellation_token_;
        private ManualResetEvent[] time_sync_events_;

        public EspBoard(Socket socket, CancellationToken token, ManualResetEvent[] time_sync_events)
        {
            socket_ = socket;
            cancellation_token_ = token;
            time_sync_events_ = time_sync_events;
            board_id_ = -1;
        }
        
        ~EspBoard()
        {
            if (socket_ != null || socket_.Connected)
                socket_.Close();
        }

        public int getBoardID()
        {
            return board_id_;
        }

        public Socket getSocket()
        {
            return socket_;
        }

        public CancellationToken GetCancellationToken()
        {
            return cancellation_token_;
        }

        /** Initializza la board
         * Client: <HI BoardID>
         * Server: <HI>
         * Client: aspetta di ricevere il timestamp
         * Server: aspetta che tutte le boards si connettano
         * Server: <GO  timestamp>
         */
        public bool initialize()
        {
            const int PROTOCOL_MESSAGE_LENGTH = 2;

            if (socket_ == null) // || !socket_.Connected)
                return false;

            byte[] buffer = new byte[512 * 1024];

            // ricevo HI
            receiveBytes(buffer, PROTOCOL_MESSAGE_LENGTH);
            writeDebugLine_("Aspetto HI");
            String msg = Encoding.ASCII.GetString(buffer, 0, PROTOCOL_MESSAGE_LENGTH);
            if (msg != "HI")
            {
                writeDebugLine_("ERRORE, messaggio inaspettato: " + msg);
                socket_.Close();
                return false;
            }

            // ricevo BoardID 4 bytes
            receiveBytes(buffer, 4);
            board_id_ = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
            writeDebugLine_("ID board ottenuto");

            // ricevo MAC 6 bytes
            receiveBytes(buffer, 6);

            Byte[] mac_address = new Byte[6];
            Array.Copy(buffer, 0, mac_address, 0, 6);
            py_address_ = new PhysicalAddress(mac_address);
            writeDebugLine_("MAC ottenuto " + py_address_);

            // rispondo HI
            msg = "HI";
            Encoding.ASCII.GetBytes(msg, 0, PROTOCOL_MESSAGE_LENGTH, buffer, 0);
            writeDebugLine_("Invio HI");
            socket_.Send(buffer, PROTOCOL_MESSAGE_LENGTH, SocketFlags.None);

            // mi sincronizzo con tutte le altre boards
            waitForTimeSync_();

            // invia GO timestamp
            msg = "GO";
            Encoding.ASCII.GetBytes(msg, 0, PROTOCOL_MESSAGE_LENGTH, buffer, 0);
            writeDebugLine_("Invio GO");
            socket_.Send(buffer, PROTOCOL_MESSAGE_LENGTH, SocketFlags.None);
            int network_epoch = IPAddress.HostToNetworkOrder((int) EspServer.getUnixEpoch());
            socket_.Send(BitConverter.GetBytes(network_epoch), 4, SocketFlags.None);

            return true;
        }
        
        /**
         * Legge bytes e li scrive in un buffer, 3 secondi di timeout (return false se il timeout finisce)
         */
        public bool receiveBytes(byte[] buff, int bytes)
        {
            int result = 0;
            // 3 secondi di timeout
            if (socket_.Poll(3000000, SelectMode.SelectRead))
            {
                result = socket_.Receive(buff, bytes, SocketFlags.None);
            }
            else if (cancellation_token_.IsCancellationRequested || !socket_.Connected || socket_.Poll(100000, SelectMode.SelectError))
            {
                    throw new Exception("ReceiveBytes interrotto");
            }
            return result == bytes;
        }

        /**
         * Riceve ogni secondo un numero da 1 fino a <seconds>
         * Ritorna false in caso di errore
         */
        public bool pingFor(int seconds)
        {
            byte[] buffer = new byte[4];
            for (int ping = 1; ping <= seconds; ping++)
            {
                bool result = receiveBytes(buffer, 4);
                if (!result)
                    return false;
                int ping_read = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
                if (ping_read != ping)
                {
                    writeDebugLine_("Expected ping: " + ping + " Obtained: " + ping_read);
                    return false;
                }
            }
            return true;
        }

        /**
         * Ricevi i pacchetti inviati dalle boards, e sincronizza tutte le boards sul timestamp corrente
         */
        public List<Pacchetto> receivePackets()
        {
            List<Pacchetto> packet_list = new List<Pacchetto>();
            byte[] buffer = new byte[1024];
            writeDebugLine_("In attesa di ricevere dati");

            // ricevo #pacchetti
            receiveBytes(buffer, 4);
            int packet_number = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
            writeDebugLine_("Numero di pacchetti da ricevere: " + packet_number);

            // ricevo i pacchetti
            for (int i = 0; i < packet_number; i++)
            {
                Pacchetto packet = Pacchetto.RiceviPacchetto(this);
                packet_list.Add(packet);
            }
            writeDebugLine_("Pacchetti ricevuti");

            // mi sincronizzo con le altre boards
            waitForTimeSync_();

            // invio timestamp
            int epoch = EspServer.getUnixEpoch();
            int network_epoch = IPAddress.HostToNetworkOrder(epoch);
            socket_.Send(BitConverter.GetBytes(network_epoch), 4, SocketFlags.None);

            writeDebugLine_("Timestamp inviato: " + epoch);
            return packet_list;
        }

        private void writeDebugLine_(String str)
        {
            if (board_id_ > 0)
                System.Diagnostics.Debug.WriteLine("Board" + board_id_ + ": " + str);
            else
                System.Diagnostics.Debug.WriteLine("Board non initializzata: " + str);
        }

        /**
         * Entra in attesa fino a quando tutti gli eventi in time_sync_events_ risultano seegnalati (set)
         */
        private void waitForTimeSync_()
        {
            writeDebugLine_("Attendo di sincronizzarmi con le altre boards");
            time_sync_events_[board_id_ - 1].Set();
            ManualResetEvent.WaitAll(time_sync_events_);
            Thread.Sleep(1000); // sleep per 1 secondo, aspetto che gli altri threads si sveglino prima di fare reset
            time_sync_events_[board_id_ - 1].Reset();
            writeDebugLine_("Risvegliato dall'attesa");
        }
    }
}
