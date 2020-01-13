using PDSClient.StatModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDSClient.ConnectionManager
{
    public class PhoneInfo 
    {
        //attributes
        private String macAddr;
        private int timestamp;
        private Point position;
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

        public Point Position
        {
            get { return position; }
        }

        public bool Global
        {
            get { return global; }
        }

        public PhoneInfo(String macAddr, int timestamp, Point position, bool global)
        {
            this.macAddr = macAddr;
            this.timestamp = timestamp;
            this.position = position;
            this.global = global;
        }

        public PhoneInfo(String macAddr, int timestamp, Point position)
        {
            this.macAddr = macAddr;
            this.timestamp = timestamp;
            this.position = position;
        }

        public PhoneInfo(String macAddr, int timestamp, double x, double y)
        {
            this.macAddr = macAddr;
            this.timestamp = timestamp;
            this.position = new Point(x, y);
        }

        public override string ToString()
        {
            return macAddr + " " + timestamp + " " + "(" + position.X + ", " + position.Y + ")";
        }
    }
}
