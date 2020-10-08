using System;
using System.Collections.Concurrent;

namespace WifiLocalization.Utilities
{
    public class Punto
    {

        public double Ascissa { get; set; }
        public double Ordinata { get; set; }

        public Punto()
        {

        }

        public Punto(double x, double y)
        {
            Ascissa = x;
            Ordinata = y;
        }

        public bool isInside(ConcurrentDictionary<int, Scheda> schede)
        {
            double x_max = Double.MinValue;
            double y_max = Double.MinValue;
            double x_min = Double.MaxValue;
            double y_min = Double.MaxValue;
            foreach (Scheda scheda in schede.Values)
            {
                if (scheda.Punto.Ascissa > x_max)
                    x_max = scheda.Punto.Ascissa;
                if (scheda.Punto.Ascissa < x_min)
                    x_min = scheda.Punto.Ascissa;
                if (scheda.Punto.Ordinata > y_max)
                    y_max = scheda.Punto.Ordinata;
                if (scheda.Punto.Ordinata < y_min)
                    y_min = scheda.Punto.Ordinata;
            }

            if (Ascissa < x_min)
                return false;
            if (Ascissa > x_max)
                return false;
            if (Ordinata < y_min)
                return false;
            if (Ordinata > y_max)
                return false;
            return true;
        }

        public static bool operator == (Punto  a, Punto b)
        {
            return Equals(a, b);
        }

        public static bool operator != (Punto a, Punto b)
        {
            return !Equals(a, b);
        }

        public override bool Equals(object oggetto)
        {
            if ((oggetto == null) || !this.GetType().Equals(oggetto.GetType()))
            {
                return false;
            }
            else
            {
                Punto punto = (Punto)oggetto;
                return (Ordinata == punto.Ordinata) && (Ascissa == punto.Ascissa);
            }
        }

        public override string ToString()
        {
            return "Ascissa (X): " + Ascissa + " - " + "Ordinata (Y): " + Ordinata;
        }
    }
}
