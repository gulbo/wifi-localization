using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using PDSClient.ConnectionManager.ConnException;

namespace PDSClient.ConnectionManager
{
    public class Pacchetto
    {
        public const int LUNGHEZZA_CHECKSUM = 4;

        //Proprietà

        public PhysicalAddress MAC_address {get; private set;}
        public int RSSI {get; private set;}
        public string SSID {get; private set;}
        public int Timestamp {get; private set;}
        public string Checksum { get; private set;}
        public int ID_scheda {get; private set;}
        public bool Global {get; private set;}

        public Pacchetto()
        {

        }

        public Pacchetto(string mac, int rssi, string ssid, int timestamp, string checksum, int id_scheda, bool global)
        {
            this.MAC_address = PhysicalAddress.Parse(mac);
            this.RSSI = rssi;
            this.SSID = ssid;
            this.Timestamp = timestamp;
            this.Checksum = checksum;
            this.ID_scheda = id_scheda;
            this.Global = global;
        }
        
        public Pacchetto(byte[] mac, int rssi, String ssid, int timestamp, byte[] checksum, int id_scheda, bool global)
        {
            if (mac.Length != 6)
            {
                throw new Exception("Lunghezza MAC address non valida");
            }
            this.MAC_address = new PhysicalAddress(mac);
            this.RSSI = rssi;
            this.SSID = ssid;
            this.Timestamp = timestamp;
            this.Checksum = BitConverter.ToString(checksum, 0, LUNGHEZZA_CHECKSUM).Replace("-", "");
            this.ID_scheda = id_scheda;
            this.Global = global;
        }

        public Pacchetto(PhysicalAddress mac, int rssi, string ssid, int timestamp, string checksum, int id_scheda, bool global)
        {
            this.MAC_address = mac;
            this.RSSI = rssi;
            this.SSID = ssid;
            this.Timestamp = timestamp;
            this.Checksum = checksum;
            this.ID_scheda = id_scheda;
            this.Global = global;
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

        public static Pacchetto RiceviPacchetto(Socket socket, int id_scheda, CancellationToken canc_token)
        {
            if (!socket.Connected)
            {
                throw new Exception("Il socket non è connesso");
            }

            byte[] mac_byte = new byte[6];
            byte[] byte_ricevuti = new byte[512];
            bool global = false;

            EspClient.ControlledRecv(socket, mac_byte, 6, canc_token);
           
            //Controlla che sia un mac reale

            if (CntrlGlobal(mac_byte))
            {
                global = true;
            }

            PhysicalAddress mac = new PhysicalAddress(mac_byte);

            EspClient.ControlledRecv(socket, byte_ricevuti, 2, canc_token);
            short rssi = BitConverter.ToInt16(byte_ricevuti, 0);
            rssi = IPAddress.NetworkToHostOrder(rssi);

            EspClient.ControlledRecv(socket, byte_ricevuti, 2, canc_token);
            short lunghezza_ssid = BitConverter.ToInt16(byte_ricevuti, 0);
            lunghezza_ssid = IPAddress.NetworkToHostOrder(lunghezza_ssid);
            if (lunghezza_ssid < 0 || lunghezza_ssid > 32)
            {
                System.Diagnostics.Debug.WriteLine("Errore critico: la lunghezza del ssid è errata");
                socket.Close();
                throw new MySocketException(MySocketException.INVALID_SSID_LEN);
            }
            string ssid = "";

            Debug.Assert(lunghezza_ssid >= 0);
            if (lunghezza_ssid > 0)
            {
                EspClient.ControlledRecv(socket, byte_ricevuti, lunghezza_ssid, canc_token);
                ssid = System.Text.Encoding.ASCII.GetString(byte_ricevuti, 0, lunghezza_ssid > 0 ? lunghezza_ssid : 0);
            }

            EspClient.ControlledRecv(socket, byte_ricevuti, 4, canc_token);
            int timestamp = BitConverter.ToInt32(byte_ricevuti, 0);
            timestamp = IPAddress.NetworkToHostOrder(timestamp);

            EspClient.ControlledRecv(socket, byte_ricevuti, 4, canc_token);
            string checksum = BitConverter.ToString(byte_ricevuti, 0, LUNGHEZZA_CHECKSUM).Replace("-", "");

            Pacchetto pacchetto = new Pacchetto(mac, rssi, ssid, timestamp, checksum, id_scheda, global);
            return pacchetto;
        }

        public override string ToString()
        {
            StringBuilder builderStringa = new StringBuilder();
            builderStringa.Append("MAC: ").Append(this.MAC_address.ToString()).Append(" - ");
            builderStringa.Append("RSSI: ").Append(this.RSSI).Append(" - ");
            builderStringa.Append("SSID: ").Append(this.SSID).Append(" - ");
            builderStringa.Append("Timestamp: ").Append(this.Timestamp).Append(" - ");
            builderStringa.Append("Checksum: ").Append(this.Checksum).Append(" - ");
            builderStringa.Append("Id_scheda: ").Append(this.ID_scheda).Append(" - ");
            builderStringa.Append("Global: ").Append(this.Global);
            return builderStringa.ToString();
        }
    }
}
