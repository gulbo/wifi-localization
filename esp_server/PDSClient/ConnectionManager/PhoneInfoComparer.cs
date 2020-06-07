using System.Collections.Generic;


namespace PDSClient.ConnectionManager
{
    class PhoneInfoComparer : IEqualityComparer<PhoneInfo>
    {
        public bool Equals(PhoneInfo x, PhoneInfo y)
        {
            return (x.MacAddr.CompareTo(y.MacAddr)==0);
        }

        public int GetHashCode(PhoneInfo obj)
        {
            return obj.MacAddr.GetHashCode();
        }
    }
}
