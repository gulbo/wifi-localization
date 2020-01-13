using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using PDSClient.ConnectionManager;
using System.Collections.Concurrent;
using System.Globalization;
using PDSClient.ConnectionManager.ConnException;

namespace PDSClient.ConnectionManager
{
    public sealed class DBConnect
    {
        private enum ReduceType { Mean, Random };

        private ConcurrentDictionary<int, Board> boards;

        public String Server { get; private set; }
        public String Uid { get; private set; }
        public String Password { get; private set; }
        public String Database { get; private set; }
        public bool Connected { get; private set; }

        public DBConnect(string server, string uid, string password)
        {
            this.Server = server;
            this.Uid = uid;
            this.Password = password;
            this.Database = "mydb";
            this.Connected = false;
            boards = new ConcurrentDictionary<int, Board>();
        }

        public void AddBoard(Board b)
        {
            boards.TryAdd(b.Id, b);
        }

        public Board GetBoard(int id)
        {
            return boards[id];
        }


        //insert packet in the table 'pacchetti'
        public bool Insert(String idBoard, String mac, int rssi, String ssid, String timestamp, String hash)
        {
            try
            {
                string query = String.Format("INSERT INTO  Pacchetti(MAC, RSSI, SSID, TIMESTAMP, HASH, IDSCHEDA) VALUES('{0}','{1}','{2}','{3}','{4}','{5}')",
                                                mac, rssi, ssid, timestamp, hash, idBoard);
               
                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connected = true;
                return true;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to insert packet into db.");
                this.Connected = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Insert(Packet p)
        {
            try
            {
                string query = String.Format("INSERT INTO  Pacchetti(MAC, RSSI, SSID, TIMESTAMP, HASH, IDSCHEDA, GLOBAL) VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                                               p.MacAddr, p.Rssi, p.Ssid, p.Timestamp, p.Checksum, p.IdBoard,p.Global);
                
                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connected = true;
                return true;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to insert packet into db.");
                this.Connected = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool Insert(List<Packet> list)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("INSERT INTO  Pacchetti(MAC, RSSI, SSID, TIMESTAMP, HASH, IDSCHEDA, GLOBAL) VALUES");
                
                foreach (Packet p in list)
                {
                    builder.Append(String.Format("('{0}','{1}','{2}','{3}','{4}','{5}',{6})", p.MacAddr, p.Rssi, Sanitize(p.Ssid), p.Timestamp, p.Checksum, p.IdBoard,p.Global));
                    if (p.Equals(list.Last<Packet>()))
                    {
                        builder.Append(";");
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                }

                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = builder.ToString();
                    cmd.ExecuteNonQuery();
                }
                this.Connected = true;
                return true;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to insert packet list into db");
                this.Connected = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static String Sanitize(String txt)
        {
            return MySql.Data.MySqlClient.MySqlHelper.EscapeString(txt);
        }

        //delete packet with selected id from the table 'pacchetti'
        public bool Delete(int id)
        {
            try
            {
                string query = String.Format("DELETE FROM Pacchetti WHERE id='{0}'", id);

                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connected = true;
                return true;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to delete packet from db");
                this.Connected = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool DeleteBoards()
        {
            try
            {
                string query = String.Format("DELETE FROM Boards");

                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connected = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to delete boards from db");
                this.Connected = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        public List<string>[] Select(string query)
        {
            //Create a list to store the result
            List<string>[] list = new List<string>[7];   
            list[0] = new List<string>();
            list[1] = new List<string>();
            list[2] = new List<string>();
            list[3] = new List<string>();
            list[4] = new List<string>();
            list[5] = new List<string>();
            list[6] = new List<string>();

            using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = conn.CreateCommand())
            {
                try
                {
                    conn.Open();
                    cmd.CommandText = query;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list[0].Add(reader["ID"] + "");
                            list[1].Add(reader["MAC"] + "");
                            list[2].Add(reader["RSSI"] + "");
                            list[3].Add(reader["SSID"] + "");
                            list[4].Add(reader["TIMESTAMP"] + "");
                            list[5].Add(reader["HASH"] + "");
                            list[6].Add(reader["IDSCHEDA"] + "");
                        }
                        this.Connected = true;
                        return list;
                    }
                }
                catch(MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Unable to retrieve packets informations.");
                    this.Connected = false;
                    return null;
                }
                
            }

        }


        public List<Packet> SelectPackets(string query)
        {
            //Create a list to store the result
            List<Packet> list = new List<Packet>();
         
            using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = conn.CreateCommand())
            {
                try
                {
                    conn.Open();
                    cmd.CommandText = query;
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            Packet p = new Packet(dataReader.GetInt32(6), dataReader.GetString(1), dataReader.GetInt32(2),
                            dataReader.GetString(3), dataReader.GetInt32(4), dataReader.GetString(5), dataReader.GetBoolean(7));
                            list.Add(p);
                        }
                        this.Connected = true;
                        return list;
                    }
                }
                catch(MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Unable to retrieve packets information");
                    this.Connected = false;
                    return null;
                }
            }
        }


        //insert boards in the table 'boards' in DB and in the local dictionary
        public bool InsertBoard(Board board)
        {
            try
            {
                String query = String.Format("INSERT INTO boards (idBoard, x, y) VALUES({0}, {1}, {2})",
                                        board.Id, board.P.X, board.P.Y);

                AddBoard(board);

                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connected = true;
                return true;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to insert boards into db.");
                this.Connected = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool InsertBoard(int id, float x, float y)
        {
            try
            {
                String query = String.Format("INSERT INTO boards (idBoard, x, y) VALUES({0}, {1}, {2})", id, x, y);

                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                AddBoard(new Board(id,x,y));
                this.Connected = true;
                return true;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to insert boards into the db");
                this.Connected = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool InsertBoard(IEnumerable<Board> boards)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("INSERT INTO boards (idBoard, x, y) VALUES");
                foreach (Board b in boards)
                {
                    builder.Append(String.Format("({0},{1},{2})", b.Id, b.P.X.ToString(CultureInfo.InvariantCulture), b.P.Y.ToString(CultureInfo.InvariantCulture)));
                    if (b.Equals(boards.Last<Board>()))
                    {
                        builder.Append(";");
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                    AddBoard(b);
                }
                String query = builder.ToString();

                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connected = true;
                return true;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to insert boards into db");
                this.Connected = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        //get a list of the boards in the table
        public List<Board> GetBoards()
        {
            List<Board> list = new List<Board>();

            using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = conn.CreateCommand())
            {
                try
                {
                    conn.Open();
                    cmd.CommandText = "SELECT * FROM BOARDS";
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            Board b = new Board(dataReader.GetInt32(0), dataReader.GetFloat(1), dataReader.GetFloat(2));
                            list.Add(b);
                        }
                        this.Connected = true;
                        return list;
                    }
                }
                catch(MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Unable to retrieve boards information from db");
                    this.Connected = false;
                    return null;
                }
                
            }
        }

        //remove board from the table
        public bool RemoveBoard(int id)
        {
            try
            {
                string query = String.Format("DELETE FROM Boards WHERE idBoard='{0}'", id);

                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connected = true;
                return true;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Error when removing board");
                this.Connected = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }


        //insert phones in the table 'posizioni'
        private void InsertPhones(IEnumerable<PhoneInfo> phones, MySqlConnection conn)
        {
            if(phones.Count() == 0)
            {
                return;
            }
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            StringBuilder builder = new StringBuilder();
            builder.Append("INSERT INTO posizioni (mac, x, y, timestamp, global) VALUES ");
            foreach (PhoneInfo phone in phones)
            {
                builder.Append(String.Format("('{0}',{1},{2},{3},{4})", 
                                            phone.MacAddr, 
                                            Math.Round(phone.Position.X, 2).ToString(culture),
                                            Math.Round(phone.Position.Y, 2).ToString(culture),
                                            phone.Timestamp,
                                            phone.Global));
                if (phone.Equals(phones.Last<PhoneInfo>()))
                {
                    builder.Append(";");
                }
                else
                {
                    builder.Append(", ");
                }
            }
            String query = builder.ToString();
            try
            {
                using (var cmd = conn.CreateCommand())
                {
                    //Create Command
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connected = true;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to insert phones in database");
                this.Connected = false;
                throw;
            }
        }


        //calculate position of the phones received by all boards in the last minute and insert them in the table
        public List<PhoneInfo> GetLastMinuteData(int nBoards, int threshold = 0)
        {
            try
            {
                if (nBoards < 2)
                    return null;

                int time = EspClient.GetUnixTimestamp() - 60;

                //Create a list to store the result
                List<PhoneInfo> list = new List<PhoneInfo>();

                using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    //Create Query
                    StringBuilder builder = new StringBuilder();
                    builder.Append("SELECT P1.MAC, P1.TIMESTAMP, P1.GLOBAL");

                    for (int i = 0; i < nBoards; i++)
                        builder.Append(", P").Append(i + 1).Append(".IDSCHEDA, P").Append(i + 1).Append(".RSSI");

                    builder.Append(VarQuery(nBoards, time, threshold));

                    //Create Command
                    cmd.CommandText = builder.ToString();
                    cmd.ExecuteNonQuery();
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            String mac = dataReader.GetString(0);  
                            int timestamp = dataReader.GetInt32(1);
                            bool global = dataReader.GetBoolean(2);

                            List<Circle> circles = new List<Circle>();
                            for (int i = 0; i < nBoards; i++)
                            {
                                int id = dataReader.GetInt32(3 + i * 2);
                                int rssi = dataReader.GetInt32(4 + i * 2);

                                circles.Add(new Circle(GetBoard(id).P, rssi));  
                            }
                            Point point = Circle.Intersection(circles);
                            if(!(Double.IsNaN(point.X) || Double.IsNaN(point.Y)))
                            {
                                PhoneInfo p = new PhoneInfo(mac, timestamp, point, global);
                                list.Add(p);
                            }
                        }
                    }
                    list = ReduceList(list, ReduceType.Mean);

                    InsertPhones(list, conn);
                }
                this.Connected = true;
                return list;
            }
            catch(KeyNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("idBoard not found...");
                throw e;
            }
            catch(MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("MySqlException catched.");
                this.Connected = false;
                return null;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Caught exception: " + e.ToString());
                return null;
            }
        }


        public int CountLast5MinutesPhones(int nBoards, int threshold = 0)
        {
            if (nBoards < 2)
                return -1;
            int time = EspClient.GetUnixTimestamp() - 300;
            String query = " SELECT COUNT(DISTINCT P1.MAC)" + VarQuery(nBoards,time,threshold);

            using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = conn.CreateCommand())
            {
                try
                {
                    conn.Open();
                    cmd.CommandText = query;
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        this.Connected = true;
                        if (dataReader.Read())
                        {
                            return dataReader.GetInt32(0);
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }
                catch(MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("MySqlException catched.");
                    this.Connected = false;
                    return -100;
                }
            }
        }   


        private String VarQuery(int nBoards, int timestamp, int threshold=0)
        {
            StringBuilder builder = new StringBuilder();

            //FROM
            builder.Append(" FROM pacchetti P1");
            for (int i = 1; i < nBoards; i++)
                builder.Append(", pacchetti P").Append(i + 1);
            
            builder.Append(" WHERE");
            //ID
            for (int i = 1; i < nBoards; i++)
            {
                if (i > 1)
                    builder.Append(" AND");
                builder.Append(" P").Append(i).Append(".IDSCHEDA > P").Append(i + 1).Append(".IDSCHEDA");
            }
            //TIMESTAMP
            for (int i = 1; i < nBoards; i++)
                builder.Append(" AND ABS(P1.TIMESTAMP - P").Append(i + 1).Append(".TIMESTAMP) <= ").Append(threshold);
            //MAC
            for (int i = 1; i < nBoards; i++)
                builder.Append(" AND P").Append(i).Append(".MAC = P").Append(i + 1).Append(".MAC");
            builder.Append(" AND P1.TIMESTAMP > ").Append(timestamp);
            return builder.ToString();

        }

        private List<PhoneInfo> ReduceList(List<PhoneInfo> list, ReduceType reduceType)
        {
            List<PhoneInfo> result = new List<PhoneInfo>();
            switch (reduceType)
            {
                case ReduceType.Mean:
                    result = list.GroupBy(pi => new
                            {
                                MacAddr = pi.MacAddr,
                                Timestamp = pi.Timestamp
                            })
                            .Select(item => new PhoneInfo(item.Key.MacAddr,
                                                        item.Key.Timestamp,
                                                        new Point(item.Select(it => it.Position.X).Average(),
                                                                item.Select(it => it.Position.Y).Average()),
                                                        item.Select(it => it.Global).First())).ToList();
                    break;
                case ReduceType.Random:
                    result = list.GroupBy(pi => new
                            {
                                MacAddr = pi.MacAddr,
                                Timestamp = pi.Timestamp
                            })
                            .Select(item =>
                            {
                                var rand = new Random();
                                return item.ElementAt(rand.Next(item.Count()));
                            }).ToList();
                    break;
                default:
                    return null;
            }
            return result;
        }


        public List<PhoneInfo> MostFrequentPhones(int n, int min, int max, int threshold = 0)
        {
            List<PhoneInfo> list = new List<PhoneInfo>();
            String query = "SELECT MAC, TIMESTAMP, X, Y FROM posizioni AS p1 WHERE p1.MAC IN " +
                "(SELECT * FROM " +
                "(SELECT MAC " +
                "FROM posizioni AS p2 WHERE p2.TIMESTAMP < " + max + " AND p2.TIMESTAMP > " + min +
                " GROUP BY p2.MAC ORDER BY COUNT(*) DESC LIMIT " + n+") AS p3) AND p1.TIMESTAMP < " + max + " AND p1.TIMESTAMP > " + min + ";";

            using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = conn.CreateCommand())
            {
                try
                {
                    conn.Open();
                    cmd.CommandText = query;
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            PhoneInfo pi = new PhoneInfo(dataReader.GetString(0), dataReader.GetInt32(1), dataReader.GetDouble(2), dataReader.GetDouble(3));
                            list.Add(pi);
                        }
                        this.Connected = true;
                        return list;
                    }
                }
                catch(MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("MySqlException catched");
                    this.Connected = false;
                    return null;
                }
            }
        }

        public List<PhoneInfo> PhonesInRange(int min, int max, int threshold = 0)
        {
            List<PhoneInfo> list = new List<PhoneInfo>();
            String query = "SELECT MAC, TIMESTAMP, X, Y " +
                "FROM posizioni WHERE TIMESTAMP < " + max + " AND TIMESTAMP > " + min+" ORDER BY TIMESTAMP";

            using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = conn.CreateCommand())
            {
                try
                {
                    conn.Open();
                    cmd.CommandText = query;
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            PhoneInfo pi = new PhoneInfo(dataReader.GetString(0), dataReader.GetInt32(1), dataReader.GetDouble(2), dataReader.GetDouble(3));
                            list.Add(pi);
                        }
                        this.Connected = true;
                        return list;
                    }
                }
                catch(MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("MySqlException catched");
                    this.Connected = false;
                    return null;
                }
            }
        }

        public List<String> CountHiddenPhones(PhoneInfo p, double threshold)
        {
            int time = EspClient.GetUnixTimestamp() - 60;
            List<String> list = new List<String>();

            String query = "SELECT MAC " +
               "FROM posizioni WHERE GLOBAL = 1 AND TIMESTAMP > " + time +
               " AND ABS(X - " + p.Position.X.ToString(CultureInfo.InvariantCulture) + ") < " + threshold.ToString(CultureInfo.InvariantCulture) +
               " AND ABS(Y - " + p.Position.Y.ToString(CultureInfo.InvariantCulture) + ") < " + threshold.ToString(CultureInfo.InvariantCulture) + 
               " AND MAC <> '" + p.MacAddr + "'";


            using (var conn = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = conn.CreateCommand())
            {
                try
                {
                    conn.Open();
                    cmd.CommandText = query;
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            list.Add(dataReader.GetString(0));
                        }
                        this.Connected = true;
                        return list;
                    }
                }
                catch (MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("MySqlException catched.");
                    this.Connected = false;
                    return null;
                }
            }
        }
    }
}