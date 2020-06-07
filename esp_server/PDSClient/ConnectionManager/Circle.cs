using System;
using System.Linq;
using System.Collections.Generic;

namespace PDSClient.ConnectionManager
{
    public class Cerchio
    {

        //Proprietà

        public Punto Centro { get; set; }
        public double Raggio { get; set; }
        
        static double Converti_RSSI(int rssi)
        {
            const double n = 3.3;
            const double measurePower = -60;
            
            return Math.Pow(10, (measurePower - rssi) / (10 * n));
        }

        public Cerchio(Punto centro, int rssi)
        {
            Centro = centro;
            Raggio = Converti_RSSI(rssi);
        }

        public static Punto Intersezione(ICollection<Cerchio> collection)
        {
            Punto intersezione = new Punto(0, 0);
            int n = 0;
            List<Punto> punti = new List<Punto>();
            for (int i = 0; i < collection.Count - 1; i++)
            {
                for (int j = i + 1; j < collection.Count; j++)
                {
                    if (collection.ElementAt(i).NonInterseca(collection.ElementAt(j)))
                        punti.Add(collection.ElementAt(i).Interseca(collection.ElementAt(j)));
                    else
                    {
                        punti.Add(collection.ElementAt(i).WeightedAverage(collection.ElementAt(j)));
                    }
                }
            }

            foreach (Punto point in punti)
            {
                intersezione.Ascissa += point.Ascissa;
                intersezione.Ordinata += point.Ordinata;
                n++;
            }
            intersezione.Ascissa /= n;
            intersezione.Ordinata /= n;
            return intersezione;
        }

        private Punto WeightedAverage(Cerchio cerchio)
        {
            double x, y;
            double raggio = Raggio + cerchio.Raggio;
            double w1, w2;

            w1 = Raggio / raggio;
            w2 = cerchio.Raggio / raggio;

            System.Diagnostics.Debug.Assert(w1 >= 0 && w1 <= 1);
            System.Diagnostics.Debug.Assert(w2 >= 0 && w2 <= 1);

            x = this.Centro.Ascissa * w2 + cerchio.Centro.Ascissa * w1;
            y = this.Centro.Ordinata * w2 + cerchio.Centro.Ordinata * w1;

            return new Punto(x, y);
        }

       


        public bool NonInterseca(Cerchio cerchio)
        {
            double distSq = (Centro.Ascissa - cerchio.Centro.Ascissa) * (Centro.Ascissa - cerchio.Centro.Ascissa) + (Centro.Ordinata - cerchio.Centro.Ordinata) * (Centro.Ordinata - cerchio.Centro.Ordinata);
            double radSumSq = (Raggio + cerchio.Raggio) * (Raggio + cerchio.Raggio);
            if (distSq <= radSumSq)
                return true;
            else
                return false;
        }

        


        public Punto Interseca(Cerchio cerchio)
        {            
            Punto intersection;
           
            double cx0 = Centro.Ascissa;
            double cy0 = Centro.Ordinata;
            double radius0 = Raggio;
            double cx1 = cerchio.Centro.Ascissa;
            double cy1 = cerchio.Centro.Ordinata;
            double radius1 = cerchio.Raggio;

            // Find the distance between the centers.
            double dx = cx0 - cx1;
            double dy = cy0 - cy1;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // Find a and h.
            double a = (radius0 * radius0 -
                radius1 * radius1 + dist * dist) / (2 * dist);
            double h = Math.Sqrt(radius0 * radius0 - a * a);

            // Find median point.
            double cx = cx0 + a * (cx1 - cx0) / dist;
            double cy = cy0 + a * (cy1 - cy0) / dist;

            intersection = new Punto(cx, cy);
            return intersection;

        }
        
    }
}
