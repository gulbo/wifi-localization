using System.Collections.Generic;


namespace PDSClient.ConnectionManager
{
    class PhoneInfoComparer : IEqualityComparer<DatiDispositivo>
    {
        public bool Equals(DatiDispositivo x, DatiDispositivo y)
        {
            return (x.MacAddr.CompareTo(y.MacAddr)==0);
        }

        public int GetHashCode(DatiDispositivo obj)
        {
            return obj.MacAddr.GetHashCode();
        }
    }
}
