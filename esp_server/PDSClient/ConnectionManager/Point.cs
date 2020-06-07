﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDSClient.ConnectionManager
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }


        public Point()
        {

        }

        public Point(double x, double y) {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return "X: " + X + " - " + "Y: " + Y;
        }
    }
}
