using System;
using PDSClient.StatModule;


namespace PDSClient.ConnectionManager
{
    public class DatiDispositivo
    {
        //attributes
        private String macAddr;
        private int timestamp;
        private Punto position;
        private bool global;

        //properties
        public String MacAddr 
        {
            get { return macAddr; }
        }

        public String FormattedMacAddr
        {
            get { return Utils.FormatMACAddr(MacAddr); }
        }

        public int Timestamp
        {
            get { return timestamp; }
        }

        public Punto Position
        {
            get { return position; }
        }

        public bool Global
        {
            get { return global; }
        }

        public DatiDispositivo(String macAddr, int timestamp, Punto position, bool global)
        {
            this.macAddr = macAddr;
            this.timestamp = timestamp;
            this.position = position;
            this.global = global;
        }

        public DatiDispositivo(String macAddr, int timestamp, Punto position)
        {
            this.macAddr = macAddr;
            this.timestamp = timestamp;
            this.position = position;
        }

        public DatiDispositivo(String macAddr, int timestamp, double x, double y)
        {
            this.macAddr = macAddr;
            this.timestamp = timestamp;
            this.position = new Punto(x, y);
        }

        public override string ToString()
        {
            return macAddr + " " + timestamp + " " + "(" + position.Ascissa + ", " + position.Ordinata + ")";
        }
    }
}
