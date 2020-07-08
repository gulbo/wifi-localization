using System;
using System.Collections.Generic;
using System.Linq;
using WifiLocalization.Utilities;
using WifiLocalization.ConnectionManager;

namespace WifiLocalization.ChartManager
{
    class MyTuple {
       public int Count { get; set; }
       public bool Modified { get; set;}
        public MyTuple() {
            Count = 1;
            Modified = true;
        }
    };


    public class StatCalc
    {
        private Dictionary<string, MyTuple> nMacVisible;
        private Dictionary<string, MyTuple> nMacHidden;
        private DBConnect DBConnection;

        public DBConnect Connection { get { return DBConnection; } }

        public StatCalc(DBConnect connection)
        {
            DBConnection = connection;
            nMacVisible = new Dictionary<string, MyTuple>();
            nMacHidden = new Dictionary<string, MyTuple>();
        }

        public int Counter(int nBoards)
        {
            return DBConnection.CountLast5MinutesPhones(nBoards, 1);
        }

        public List<DatiDispositivo> GetLastMinutePositions(int nBoards, int threshold = 0)
        {
            List<DatiDispositivo> list = DBConnection.GetLastMinuteData(nBoards, threshold);

            if(list == null)
            {
                //return new List<PhoneInfo>();
                return null;
            }

            return list.GroupBy(pi => new
            {
                MacAddr = pi.MAC_Address,
            })
                            .Select(item => new DatiDispositivo(item.Key.MacAddr, item.Select(it => it.Timestamp).First(),
                                                        new Punto(item.Select(it => it.Posizione.Ascissa).Average(),
                                                                item.Select(it => it.Posizione.Ordinata).Average()),
                                                        item.Select(it => it.Global).First())).ToList();

        }

        public List<Scheda> GetBoardsPosition() {
            return DBConnection.SelezionaSchede();
        }
        //interfaccia per contattare il DB
        public List<DatiDispositivo> MostFrequentPhones(int numberOfMACs, int min, int max, int threshold = 0)
        {
            List<DatiDispositivo> list = DBConnection.MostFrequentPhones(numberOfMACs, min, max, threshold);

            return list;
        }

        public int FiveMinuteHiddenPhones(List<DatiDispositivo> hiddenPhones)
        {
            int count = 0;
            HashSet<String> removeKeys = new HashSet<String>();

            foreach (String s in nMacHidden.Keys)
            {
                if (!nMacHidden[s].Modified)
                {
                    removeKeys.Add(s);
                }
                else
                {
                    nMacHidden[s].Modified = false;
                }
            }

            foreach (String s in removeKeys)
            {
                nMacHidden.Remove(s);
            }

            foreach (DatiDispositivo p in hiddenPhones)
            {
                if (nMacHidden.ContainsKey(p.MAC_Address))
                {
                    nMacHidden[p.MAC_Address].Modified = true;

                        count++;
                    
                }
                else
                {
                    nMacHidden.Add(p.MAC_Address, new MyTuple());
                }
            }

            return count;
        }

        public int FiveMinuteVisiblePhones(List<DatiDispositivo> phoneInfos)
        {
            int count = 0;
            HashSet<String> removeKeys = new HashSet<String>();

            foreach (String s in nMacVisible.Keys)
            {
                if (!nMacVisible[s].Modified)
                {
                    removeKeys.Add(s);
                }
                else
                {
                    nMacVisible[s].Modified = false;
                }
            }

            foreach (String s in removeKeys)
            {
                nMacVisible.Remove(s);
            }

            foreach (DatiDispositivo p in phoneInfos)
            {
                if (nMacVisible.ContainsKey(p.MAC_Address))
                {
                    nMacVisible[p.MAC_Address].Modified = true;                 
                        count++;
                }
                else
                {
                    nMacVisible.Add(p.MAC_Address, new MyTuple());
                }
            }

            return count;
        }
        //interfaccia per prelevare dal DB le posizioni all'interno dell'intervallo temporale 
        public List<DatiDispositivo> PhonesInRange(long min, long max, int threshold = 0) {

            return DBConnection.PhonesInRange(Convert.ToInt32(min), Convert.ToInt32(max));
        }


    }
}
