using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using PDSClient.ConnectionManager;
using PDSClient.ConnectionManager.ConnException;

namespace PDSClient.StatModule
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

        public List<PhoneInfo> GetLastMinutePositions(int nBoards, int threshold = 0)
        {
            List<PhoneInfo> list = DBConnection.GetLastMinuteData(nBoards, threshold);

            if(list == null)
            {
                //return new List<PhoneInfo>();
                return null;
            }

            return list.GroupBy(pi => new
            {
                MacAddr = pi.MacAddr,
            })
                            .Select(item => new PhoneInfo(item.Key.MacAddr, item.Select(it => it.Timestamp).First(),
                                                        new Punto(item.Select(it => it.Position.Ascissa).Average(),
                                                                item.Select(it => it.Position.Ordinata).Average()),
                                                        item.Select(it => it.Global).First())).ToList();

            //return list;
            //return list.GroupBy(pi => pi.MacAddr)
            //        .Select(item => item.OrderByDescending(it => it.Timestamp).First()).ToList();
        }

        public List<Scheda> GetBoardsPosition() {
            return DBConnection.SelezionaSchede();
        }

        public List<PhoneInfo> MostFrequentPhones(int n, int min, int max, int threshold = 0)
        {
            List<PhoneInfo> list = DBConnection.MostFrequentPhones(n, min, max, threshold);

            return list;
        }

        public int FiveMinuteHiddenPhones(List<PhoneInfo> hiddenPhones)
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

            foreach (PhoneInfo p in hiddenPhones)
            {
                if (nMacHidden.ContainsKey(p.MacAddr))
                {
                    nMacHidden[p.MacAddr].Modified = true;
                    if ((++nMacHidden[p.MacAddr].Count) >= 5)
                    {
                        count++;
                    }
                }
                else
                {
                    nMacHidden.Add(p.MacAddr, new MyTuple());
                }
            }

            return count;
        }

        public int FiveMinuteVisiblePhones(List<PhoneInfo> phoneInfos)
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

            foreach (PhoneInfo p in phoneInfos)
            {
                if (nMacVisible.ContainsKey(p.MacAddr))
                {
                    nMacVisible[p.MacAddr].Modified = true;
                    if ((++nMacVisible[p.MacAddr].Count) >= 5)
                    {
                        count++;
                    }
                }
                else
                {
                    nMacVisible.Add(p.MacAddr, new MyTuple());
                }
            }

            return count;
        }

        public List<PhoneInfo> PhonesInRange(int min, int max, int threshold = 0) {

            return DBConnection.PhonesInRange(min, max);
        }

    }
}
