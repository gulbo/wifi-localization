using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace WifiLocalization.ConnectionManager
{
    public class Pacchetto
    {
        public const int LUNGHEZZA_CHECKSUM = 4;

        public PhysicalAddress MAC_Address { get; private set; }
        public int RSSI { get; private set; }
        public string SSID { get; private set; }
        public int Timestamp { get; private set; }
        public string Checksum { get; private set; }
        public int ID_scheda { get; private set; }
        public bool Global { get; private set; }

        public Pacchetto()
        {

        }
        public Pacchetto(string mac, int rssi, string ssid, int timestamp, string checksum, int id_scheda, bool global)
        {
            MAC_Address = PhysicalAddress.Parse(mac);
            RSSI = rssi;
            SSID = ssid;
            Timestamp = timestamp;
            Checksum = checksum;
            ID_scheda = id_scheda;
            Global = global;
        }
        
        public Pacchetto(byte[] mac, int rssi, string ssid, int timestamp, byte[] checksum, int id_scheda, bool global)
        {
            if (mac.Length != 6)
            {
                throw new Exception("Lunghezza MAC address non valida");
            }
            MAC_Address = new PhysicalAddress(mac);
            RSSI = rssi;
            SSID = ssid;
            Timestamp = timestamp;
            Checksum = BitConverter.ToString(checksum, 0, LUNGHEZZA_CHECKSUM).Replace("-", "");
            ID_scheda = id_scheda;
            Global = global;
        }

        public Pacchetto(PhysicalAddress mac, int rssi, string ssid, int timestamp, string checksum, int id_scheda, bool global)
        {
            MAC_Address = mac;
            RSSI = rssi;
            SSID = ssid;
            Timestamp = timestamp;
            Checksum = checksum;
            ID_scheda = id_scheda;
            Global = global;
        }

        public static bool CntrlGlobal(byte[] mac)
        {
            if ((mac[0] & 0x02) == 0)
                return true;
            else
                return false;
        }

        public static bool CntrlGlobal(string mac)
        {
            byte[] array = mac.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
            return CntrlGlobal(array);
        }

        public static Pacchetto RiceviPacchetto(EspBoard board)
        {
            Socket socket = board.getSocket();
            if (!socket.Connected)
            {
                throw new Exception("Il socket non è connesso");
            }

            byte[] mac_byte = new byte[6];
            byte[] byte_ricevuti = new byte[512];
            bool global = false;

            board.receiveBytes(mac_byte, 6);
           
            //Controlla che sia un mac reale

            if (CntrlGlobal(mac_byte))
            {
                global = true;
            }

            PhysicalAddress mac = new PhysicalAddress(mac_byte);

            board.receiveBytes(byte_ricevuti, 2);
            short rssi = BitConverter.ToInt16(byte_ricevuti, 0);
            rssi = IPAddress.NetworkToHostOrder(rssi);

            board.receiveBytes(byte_ricevuti, 2);
            short lunghezza_ssid = BitConverter.ToInt16(byte_ricevuti, 0);
            lunghezza_ssid = IPAddress.NetworkToHostOrder(lunghezza_ssid);
            if (lunghezza_ssid < 0 || lunghezza_ssid > 32)
            {
                System.Diagnostics.Debug.WriteLine("Errore critico: la lunghezza del ssid è errata");
                socket.Close();
                throw new Exception("Lunghezza SSID non valida: " + lunghezza_ssid);
            }
            string ssid = "";

            Debug.Assert(lunghezza_ssid >= 0);
            if (lunghezza_ssid > 0)
            {
                board.receiveBytes(byte_ricevuti, lunghezza_ssid);
                ssid = System.Text.Encoding.ASCII.GetString(byte_ricevuti, 0, lunghezza_ssid > 0 ? lunghezza_ssid : 0);
            }

            board.receiveBytes(byte_ricevuti, 4);
            int timestamp = BitConverter.ToInt32(byte_ricevuti, 0);
            timestamp = IPAddress.NetworkToHostOrder(timestamp);

            board.receiveBytes(byte_ricevuti, 4);
            string checksum = BitConverter.ToString(byte_ricevuti, 0, LUNGHEZZA_CHECKSUM).Replace("-", "");

            Pacchetto pacchetto = new Pacchetto(mac, rssi, ssid, timestamp, checksum, board.getBoardID(), global);
            return pacchetto;
        }

        public override string ToString()
        {
            StringBuilder builderStringa = new StringBuilder();
            builderStringa.Append("MAC: ").Append(MAC_Address.ToString()).Append(" - ");
            builderStringa.Append("RSSI: ").Append(RSSI).Append(" - ");
            builderStringa.Append("SSID: ").Append(SSID).Append(" - ");
            builderStringa.Append("Timestamp: ").Append(Timestamp).Append(" - ");
            builderStringa.Append("Checksum: ").Append(Checksum).Append(" - ");
            builderStringa.Append("Id_scheda: ").Append(ID_scheda).Append(" - ");
            builderStringa.Append("Global: ").Append(Global);
            return builderStringa.ToString();
        }
    }
}
