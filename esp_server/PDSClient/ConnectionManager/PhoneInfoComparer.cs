using System.Collections.Generic;


namespace PDSClient.ConnectionManager
{
    class PhoneInfoComparer : IEqualityComparer<DatiDispositivo>
    {
        public bool Equals(DatiDispositivo x, DatiDispositivo y)
        {
            return (x.MAC_Address.CompareTo(y.MAC_Address) ==0);
        }

        public int GetHashCode(DatiDispositivo obj)
        {
            return obj.MAC_Address.GetHashCode();
        }
    }
}
