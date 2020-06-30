using System;
using PDSClient.StatModule;

namespace PDSClient.ConnectionManager
{
    public class DatiDispositivo
    {
        private readonly String MAC;
        private readonly Punto posizione;
        private readonly int timestamp;
        private readonly bool global;

        public DatiDispositivo()
        {

        }

        public string MAC_Address
        {
            get { return MAC; }
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

        public string FormattedMacAddr
        {
            get { return Utils.Formatta_MAC_Address(MAC_Address); }
        }

        public DatiDispositivo(String mac, int timestamp, Punto posizione)
        {
            this.MAC = mac;
            this.timestamp = timestamp;
            this.posizione = posizione;
        }

        public DatiDispositivo(String mac, int timestamp, double x, double y)
        {
            this.MAC = mac;
            this.timestamp = timestamp;
            this.posizione = new Punto(x, y);
        }

        public DatiDispositivo(String mac, int timestamp, Punto posizione,  bool global)
        {
            this.MAC = mac;
            this.posizione = posizione;
            this.timestamp = timestamp;
            this.global = global;
        }
        public DatiDispositivo(String mac, int timestamp, double x, double y, bool global)
        {
            this.MAC = mac;
            this.posizione = new Punto(x, y);
            this.timestamp = timestamp;
            this.global = global;
        }

        public override string ToString()
        {
            return MAC + " - " + timestamp + " - " + "(" + posizione.Ascissa + ", " + posizione.Ordinata + ")";
        }
    }
}
