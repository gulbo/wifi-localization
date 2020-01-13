using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PDSClient.StatModule
{
    class Utils
    {
        public static DateTime UnixTimestampToDateTime(long unixTimestamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            dtDateTime = dtDateTime.AddSeconds(unixTimestamp).ToLocalTime();
            return dtDateTime;
        }

        public static DateTime UnixTimestampToDateTime(int unixTimestamp)
        {
            return UnixTimestampToDateTime((long)unixTimestamp);
        }

        public static String FormatMACAddr(String macAddr)
        {
            var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
            var replace = "$1:$2:$3:$4:$5:$6";
            var newformat = Regex.Replace(macAddr, regex, replace);

            return newformat;
        }
    }
}
