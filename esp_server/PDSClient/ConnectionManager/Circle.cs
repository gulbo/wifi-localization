
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
        public Point Center { get; set; }


        public Circle(Point center, int rssi)
        {
            Center = center;
            Radius = RSSIConverter(rssi);
        }

        public static Point Intersection(ICollection<Circle> collection)
        {
            Point intersection = new Point(0, 0);
            int n = 0;
            List<Point> points = new List<Point>();
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

            foreach (Point point in points)
            {
                intersection.X += point.X;
                intersection.Y += point.Y;
                n++;
            }
            intersection.X /= n;
            intersection.Y /= n;
            return intersection;
        }

        private Point WeightedAverage(Circle c)
        {
            double x, y;
            double r = this.Radius + c.Radius;
            double w1, w2;

            w1 = this.Radius / r;
            w2 = c.Radius / r;

            System.Diagnostics.Debug.Assert(w1 >= 0 && w1 <= 1);
            System.Diagnostics.Debug.Assert(w2 >= 0 && w2 <= 1);

            x = this.Center.X * w2 + c.Center.X * w1;
            y = this.Center.Y * w2 + c.Center.Y * w1;

            return new Point(x, y);
        }

        static double RSSIConverter(int rssi)
        {
            const double measurePower = -60;
            const double n = 3.3;

            return Math.Pow(10, (measurePower - rssi) / (10 * n));
        }


        public bool DoesIntersect(Circle c)
        {
            double distSq = (Center.X - c.Center.X) * (Center.X - c.Center.X) + (Center.Y - c.Center.Y) * (Center.Y - c.Center.Y);
            double radSumSq = (Radius + c.Radius) * (Radius + c.Radius);
            if (distSq <= radSumSq)
                return true;
            else
                return false;
        }

        


        public Point Intersect(Circle c)
        {            
            Point intersection;
           
            double cx0 = Center.X;
            double cy0 = Center.Y;
            double radius0 = Radius;
            double cx1 = c.Center.X;
            double cy1 = c.Center.Y;
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

            intersection = new Point(cx, cy);
            return intersection;

        }
        
    }
}
