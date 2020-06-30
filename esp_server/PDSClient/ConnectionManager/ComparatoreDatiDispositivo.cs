﻿using System.Collections.Generic;

namespace PDSClient.ConnectionManager
{
    class ComparatoreDatiDispositivo : IEqualityComparer<DatiDispositivo>
    {
        public ComparatoreDatiDispositivo()
        {

        }

        public bool Equals(DatiDispositivo a, DatiDispositivo b)
        {
            return (a.MAC_Address.CompareTo(b.MAC_Address) == 0);
        }

        public int GetHashCode(DatiDispositivo oggetto)
        {
            return oggetto.MAC_Address.GetHashCode();
        }
    }
}
