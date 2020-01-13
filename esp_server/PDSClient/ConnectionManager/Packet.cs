using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;
using PDSClient.ConnectionManager.ConnException;

namespace PDSClient.ConnectionManager
{
    public class Packet
    {
        public const int CHECKSUM_LENGTH = 4;

        //proprietà
        public int IdBoard { get; private set; }
        public PhysicalAddress MacAddr { get; private set; }
        public int Rssi { get; private set; }
        public String Ssid { get; private set; }
        public int Timestamp { get; private set; }
        public String Checksum { get; private set; }
        public bool Global { get; private set; }

        public Packet(int idBoard, PhysicalAddress macaddr, int rssi, String ssid, int timestamp, string checksum, bool global)
        {
            this.IdBoard = idBoard;
            this.MacAddr = macaddr;
            this.Rssi = rssi;
            this.Ssid = ssid;
            this.Timestamp = timestamp;
            this.Checksum = checksum;
            this.Global = global;
        }

        public Packet(int idBoard, byte[] macaddr, int rssi, String ssid, int timestamp, byte[] checksum, bool global)
        {
            if(macaddr.Length != 6)
            {
                throw new Exception("Invalid MacAddr length");
            }
            this.IdBoard = idBoard;
            this.MacAddr = new PhysicalAddress(macaddr);
            this.Rssi = rssi;
            this.Ssid = ssid;
            this.Timestamp = timestamp;
            this.Checksum = BitConverter.ToString(checksum, 0, CHECKSUM_LENGTH).Replace("-", "");
            this.Global = global;
        }

        public Packet(int idBoard, String macaddr, int rssi, String ssid, int timestamp, String checksum, bool global)
        {
            this.IdBoard = idBoard;
            this.MacAddr = PhysicalAddress.Parse(macaddr);
            this.Rssi = rssi;
            this.Ssid = ssid;
            this.Timestamp = timestamp;
            this.Checksum = checksum;
            this.Global = global;
        }

        public static Packet ReceivePacket(Socket s, int idBoard, CancellationToken ct)
        {
            if (!s.Connected)
            {
                throw new Exception("Socket not connected");
            }

            byte[] receiveBuffer = new byte[512];
            byte[] mac_addr = new byte[6];
            bool global = false;

            EspClient.ControlledRecv(s, mac_addr, 6, ct);
            PhysicalAddress macaddr = new PhysicalAddress(mac_addr);

            EspClient.ControlledRecv(s, receiveBuffer, 2, ct);
            short rssi = BitConverter.ToInt16(receiveBuffer, 0);
            rssi = IPAddress.NetworkToHostOrder(rssi);

            EspClient.ControlledRecv(s, receiveBuffer, 2, ct);
            short ssidLen = BitConverter.ToInt16(receiveBuffer, 0);
            ssidLen = IPAddress.NetworkToHostOrder(ssidLen);
            if (ssidLen < 0 || ssidLen > 32)
            {
                System.Diagnostics.Debug.WriteLine("Critical error: wrong value of ssidLen");
                s.Close();
                throw new MySocketException(MySocketException.INVALID_SSID_LEN);
            }
            String ssid = "";

            Debug.Assert(ssidLen >= 0);
            if(ssidLen > 0)
            {
                EspClient.ControlledRecv(s, receiveBuffer, ssidLen, ct);

                ssid = System.Text.Encoding.ASCII.GetString(receiveBuffer, 0, ssidLen > 0 ? ssidLen : 0);

            }

            EspClient.ControlledRecv(s, receiveBuffer, 4, ct);
            int timestamp = BitConverter.ToInt32(receiveBuffer, 0);
            timestamp = IPAddress.NetworkToHostOrder(timestamp);

            EspClient.ControlledRecv(s, receiveBuffer, 4, ct);
            string checksum = BitConverter.ToString(receiveBuffer, 0, CHECKSUM_LENGTH).Replace("-", "");

            //check if it is a real mac
            if (IsGlobal(mac_addr))
                global = true;

            Packet pkt = new Packet(idBoard, macaddr, rssi, ssid, timestamp, checksum, global);

            return pkt;
        }

        public static bool IsGlobal(byte[] mac)
        {
            if ((mac[0] & 0x02) == 0)
                return true;
            else
                return false;
        }

        public static bool IsGlobal(String mac)
        {
            byte[] arr = mac.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
            return IsGlobal(arr);
        }

        public override String ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("IdBoard: ").Append(this.IdBoard).Append(" - ");
            builder.Append("MAC: ").Append(this.MacAddr.ToString()).Append(" - ");
            builder.Append("RSSI: ").Append(this.Rssi).Append(" - ");
            builder.Append("SSID: ").Append(this.Ssid).Append(" - ");
            builder.Append("Timestamp: ").Append(this.Timestamp).Append(" - ");
            builder.Append("Checksum: ").Append(this.Checksum);
            builder.Append("Global: ").Append(this.Global);
            return builder.ToString();
        }
    }
}
