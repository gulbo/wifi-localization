
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDSClient.ConnectionManager
{
    public class Circle
    {
        
        //properties
        public double Radius { get; set; }
        public Punto Center { get; set; }


        public Circle(Punto center, int rssi)
        {
            Center = center;
            Radius = RSSIConverter(rssi);
        }

        public static Punto Intersection(ICollection<Circle> collection)
        {
            Punto intersection = new Punto(0, 0);
            int n = 0;
            List<Punto> points = new List<Punto>();
            for (int i = 0; i < collection.Count - 1; i++)
            {
                for (int j = i + 1; j < collection.Count; j++)
                {
                    if (collection.ElementAt(i).DoesIntersect(collection.ElementAt(j)))
                        points.Add(collection.ElementAt(i).Intersect(collection.ElementAt(j)));
                    else
                    {
                        points.Add(collection.ElementAt(i).WeightedAverage(collection.ElementAt(j)));
                    }
                }
            }

            foreach (Punto point in points)
            {
                intersection.Ascissa += point.Ascissa;
                intersection.Ordinata += point.Ordinata;
                n++;
            }
            intersection.Ascissa /= n;
            intersection.Ordinata /= n;
            return intersection;
        }

        private Punto WeightedAverage(Circle c)
        {
            double x, y;
            double r = this.Radius + c.Radius;
            double w1, w2;

            w1 = this.Radius / r;
            w2 = c.Radius / r;

            System.Diagnostics.Debug.Assert(w1 >= 0 && w1 <= 1);
            System.Diagnostics.Debug.Assert(w2 >= 0 && w2 <= 1);

            x = this.Center.Ascissa * w2 + c.Center.Ascissa * w1;
            y = this.Center.Ordinata * w2 + c.Center.Ordinata * w1;

            return new Punto(x, y);
        }

        static double RSSIConverter(int rssi)
        {
            const double measurePower = -60;
            const double n = 3.3;

            return Math.Pow(10, (measurePower - rssi) / (10 * n));
        }


        public bool DoesIntersect(Circle c)
        {
            double distSq = (Center.Ascissa - c.Center.Ascissa) * (Center.Ascissa - c.Center.Ascissa) + (Center.Ordinata - c.Center.Ordinata) * (Center.Ordinata - c.Center.Ordinata);
            double radSumSq = (Radius + c.Radius) * (Radius + c.Radius);
            if (distSq <= radSumSq)
                return true;
            else
                return false;
        }

        


        public Punto Intersect(Circle c)
        {            
            Punto intersection;
           
            double cx0 = Center.Ascissa;
            double cy0 = Center.Ordinata;
            double radius0 = Radius;
            double cx1 = c.Center.Ascissa;
            double cy1 = c.Center.Ordinata;
            double radius1 = c.Radius;

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
