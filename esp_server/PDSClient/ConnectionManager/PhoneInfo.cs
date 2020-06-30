using System;
using PDSClient.StatModule;


namespace PDSClient.ConnectionManager
{
    public class DatiDispositivo
    {
        private readonly String MAC_address;
        private readonly Punto posizione;
        private readonly int timestamp;
        private readonly bool global;


        public String MAC_Address
        {
            get { return MAC_address; }
        }

        public Punto Posizione
        {
            get { return posizione; }
        }

        public int Timestamp
        {
            get { return timestamp; }
        }

        public bool Global
        {
            get { return global; }
        }

        public DatiDispositivo(String macAddr, int timestamp, Punto position, bool global)
        {
            this.MAC_address = macAddr;
            this.timestamp = timestamp;
            this.posizione = position;
            this.global = global;
        }

        public DatiDispositivo(String macAddr, int timestamp, Punto position)
        {
            this.MAC_address = macAddr;
            this.timestamp = timestamp;
            this.posizione = position;
        }

        public DatiDispositivo(String macAddr, int timestamp, double x, double y)
        {
            this.MAC_address = macAddr;
            this.timestamp = timestamp;
            this.posizione = new Punto(x, y);
        }

        public override string ToString()
        {
            return MAC_address + " " + timestamp + " " + "(" + posizione.Ascissa + ", " + posizione.Ordinata + ")";
        }
    }
}
