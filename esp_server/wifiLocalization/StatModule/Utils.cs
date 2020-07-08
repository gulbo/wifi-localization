using System;
using System.Windows.Media;
using System.Text.RegularExpressions;

namespace WifiLocalization.StatModule
{
    class Utils
    {
        public static SolidColorBrush orange  = Brushes.Orange;
        public static SolidColorBrush purple  = Brushes.MediumSlateBlue;
        public static SolidColorBrush green  = Brushes.Teal;
        public static SolidColorBrush red = Brushes.IndianRed;
        public static SolidColorBrush lightOrange = Brushes.LightSalmon;
        

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

        public static string Formatta_MAC_Address(String macAddr)
        {
            var regex = "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})";
            var replace = "$1:$2:$3:$4:$5:$6";
            var newformat = Regex.Replace(macAddr, regex, replace);

            return newformat;
        }
    }
}
