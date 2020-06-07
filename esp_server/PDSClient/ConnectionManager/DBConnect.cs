using System;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using MySql.Data.MySqlClient;

namespace PDSClient.ConnectionManager
{
    public sealed class DBConnect
    {
        private enum ReduceType { Mean, Random };
        private ConcurrentDictionary<int, Scheda> schede;

        //Proprietà

        public String Server {get; private set;}
        public String Uid {get; private set;}
        public String Password {get; private set;}
        public String Database {get; private set;}
        public bool Connesso {get; private set;}

        public DBConnect(string server, string uid, string password)
        {
            this.Server = server;
            this.Uid = uid;
            this.Password = password;
            this.Database = "db_wifi-localization";
            this.Connesso = false;
            schede = new ConcurrentDictionary<int, Scheda>();
        }
        public Scheda GetScheda(int id_scheda)
        {
            return schede[id_scheda];
        }

        public void AggiungiScheda(Scheda scheda)
        {
            schede.TryAdd(scheda.ID_scheda, scheda);
        }

        //Inserisci schede nella tabella 'schede' del DB e nel local dictionary

        public bool InsertScheda(Scheda scheda)
        {
            try
            {
                String query = String.Format("INSERT INTO schede (ID_scheda, x, y) VALUES({0}, {1}, {2})",
                    scheda.ID_scheda, scheda.Punto.Ascissa, scheda.Punto.Ordinata);

                AggiungiScheda(scheda);

                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire le schede nel database");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool InsertScheda(IEnumerable<Scheda> schede)
        {
            try
            {
                StringBuilder builderQuery = new StringBuilder();
                builderQuery.Append("INSERT INTO schede (ID_scheda, x, y) VALUES");
                foreach (Scheda scheda in schede)
                {
                    builderQuery.Append(string.Format("({0},{1},{2})",
                        scheda.ID_scheda, scheda.Punto.Ascissa.ToString(CultureInfo.InvariantCulture), scheda.Punto.Ordinata.ToString(CultureInfo.InvariantCulture)));
                    if (scheda.Equals(schede.Last<Scheda>()))
                    {
                        builderQuery.Append(";");
                    }
                    else
                    {
                        builderQuery.Append(", ");
                    }
                    AggiungiScheda(scheda);
                }
                string query = builderQuery.ToString();

                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire le schede nel database");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool InsertScheda(int id_scheda, double x, double y)
        {
            try
            {
                String query = String.Format("INSERT INTO schede (ID_scheda, x, y) VALUES({0}, {1}, {2})", id_scheda, x, y);

                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                AggiungiScheda(new Scheda(id_scheda, x, y));
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire le schede nel database");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        //Rimuovi scheda (selezionando l'ID_scheda) nella tabella 'schede' del DB

        public bool RemoveScheda(int id_scheda)
        {
            try
            {
                string query = String.Format("DELETE FROM schede WHERE ID_scheda='{0}'", id_scheda);

                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Errore durante la rimozione della scheda");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool RemoveSchede()
        {
            try
            {
                string query = String.Format("DELETE FROM schede");

                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Errore durante la rimozione delle schede");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        //Seleziona l'elenco delle schede (tutte) dalla tabella 'schede' del DB

        public List<Scheda> SelezionaSchede()
        {

            //Creo una lista di schede per memorizzare i risultati

            List<Scheda> list = new List<Scheda>();

            using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = connessione.CreateCommand())
            {
                try
                {
                    connessione.Open();
                    cmd.CommandText = "SELECT * FROM schede";
                    using (var leggiDati = cmd.ExecuteReader())
                    {
                        while (leggiDati.Read())
                        {
                            Scheda scheda = new Scheda(leggiDati.GetInt32(0), leggiDati.GetDouble(1), leggiDati.GetDouble(2));
                            list.Add(scheda);
                        }
                        this.Connesso = true;
                        return list;
                    }
                }
                catch (MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Impossibile recuperare le informazioni sulle schede dal database");
                    this.Connesso = false;
                    return null;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }

        //Inserisci pacchetti nella tabella 'pacchetti' del DB

        public bool InsertPacchetto(Pacchetto pacchetto)
        {
            try
            {
                string query = String.Format("INSERT INTO  pacchetti(MAC, RSSI, SSID, timestamp, hash, ID_scheda, global) VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                    pacchetto.MAC_address, pacchetto.RSSI, pacchetto.SSID, pacchetto.Timestamp, pacchetto.Checksum, pacchetto.ID_scheda, pacchetto.Global);

                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire i pacchetti nel database");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool InsertPacchetto(List<Pacchetto> list)
        {
            try
            {
                StringBuilder builderQuery = new StringBuilder();
                builderQuery.Append("INSERT INTO  pacchetti(MAC, RSSI, SSID, timestamp, hash, ID_scheda, global) VALUES");
                foreach (Pacchetto pacchetto in list)
                {
                    builderQuery.Append(string.Format("('{0}','{1}','{2}','{3}','{4}','{5}',{6})",
                        pacchetto.MAC_address, pacchetto.RSSI, Escape_stringa(pacchetto.SSID), pacchetto.Timestamp, pacchetto.Checksum, pacchetto.ID_scheda, pacchetto.Global));
                    if (pacchetto.Equals(list.Last<Pacchetto>()))
                    {
                        builderQuery.Append(";");
                    }
                    else
                    {
                        builderQuery.Append(", ");
                    }
                }
                string query = builderQuery.ToString();

                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire i pacchetti nel database");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool InsertPacchetto(string mac, int rssi, string ssid, int timestamp, string checksum, string id_scheda, bool global)
        {
            try
            {
                string query = String.Format("INSERT INTO  pacchetti(MAC, RSSI, SSID, timestamp, hash, ID_scheda, global) VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                    mac, rssi, ssid, timestamp, checksum, id_scheda, global);
               
                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire i pacchetti nel database");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static String Escape_stringa(String stringa)
        {
            return MySql.Data.MySqlClient.MySqlHelper.EscapeString(stringa);
        }

        //Rimuovi pacchetti (selezionando l'ID_pacchetto) nella tabella 'pacchetti' del DB

        public bool RemovePacchetto(int id_pacchetto)
        {
            try
            {
                string query = String.Format("DELETE FROM pacchetti WHERE ID_pacchetto='{0}'", id_pacchetto);

                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Errore durante la rimozione dei pacchetti");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool RemovePacchetti()
        {
            try
            {
                string query = String.Format("DELETE FROM pacchetti");

                using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (var cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                this.Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Errore durante la rimozione dei pacchetti");
                this.Connesso = false;
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        //Seleziona l'elenco dei pacchetti dalla tabella 'pacchetti' del DB

        public List<Pacchetto> SelezionaPacchetti(string query)
        {
            //Creo una lista di pacchetti per memorizzare i risultati

            List<Pacchetto> list = new List<Pacchetto>();

            using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = connessione.CreateCommand())
            {
                try
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    using (var leggiDati = cmd.ExecuteReader())
                    {
                        while (leggiDati.Read())
                        {
                            Pacchetto pacchetto = new Pacchetto(leggiDati.GetString(1), leggiDati.GetInt32(2), leggiDati.GetString(3),
                                leggiDati.GetInt32(4), leggiDati.GetString(5), leggiDati.GetInt32(6), leggiDati.GetBoolean(7));
                            list.Add(pacchetto);
                        }
                        this.Connesso = true;
                        return list;
                    }
                }
                catch (MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Impossibile recuperare le informazioni sui pacchetti dal database");
                    this.Connesso = false;
                    return null;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }
 
        public List<string>[] SelezionaColonnePacchetti(string query)
        {
            //Creo una lista per ogni colonna della tabella 'pacchetti' per memorizzare i risultati

            List<string>[] list = new List<string>[8];   
            list[0] = new List<string>();
            list[1] = new List<string>();
            list[2] = new List<string>();
            list[3] = new List<string>();
            list[4] = new List<string>();
            list[5] = new List<string>();
            list[6] = new List<string>();
            list[7] = new List<string>();

            using (var connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (var cmd = connessione.CreateCommand())
            {
                try
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    using (var leggiDati = cmd.ExecuteReader())
                    {
                        while (leggiDati.Read())
                        {
                            list[0].Add(leggiDati["ID_pacchetto"] + "");
                            list[1].Add(leggiDati["MAC"] + "");
                            list[2].Add(leggiDati["RSSI"] + "");
                            list[3].Add(leggiDati["SSID"] + "");
                            list[4].Add(leggiDati["Timestamp"] + "");
                            list[5].Add(leggiDati["Checksum"] + "");
                            list[6].Add(leggiDati["ID_scheda"] + "");
                            list[7].Add(leggiDati["Global"] + "");
                        }
                        this.Connesso = true;
                        return list;
                    }
                }
                catch(MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Impossibile recuperare le informazioni sui pacchetti dal database");
                    this.Connesso = false;
                    return null;
                }
                catch (Exception e)
                {
                    return null;
                }
            }

        }


       //CONTINUO DA QUI ... DANIELE


       


        //insert phones in the table 'posizioni'
        private void InsertPhones(IEnumerable<PhoneInfo> phones, MySqlConnection conn)
        {
            if (phones.Count() == 0)
            {
                return;
            }
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            StringBuilder builder = new StringBuilder();
            builder.Append("INSERT INTO posizioni (MAC, x, y, timestamp, global) VALUES ");
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
            catch (MySqlException e)
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
                    builder.Append("SELECT DISTINCT P1.MAC, P1.timestamp, P1.global, P1.hash");

                    for (int i = 0; i < nBoards; i++)
                        builder.Append(", P").Append(i + 1).Append(".ID_scheda, P").Append(i + 1).Append(".RSSI");

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
                            //board signal area for each board
                            for (int i = 0; i < nBoards; i++)
                            {
                                int id = dataReader.GetInt32(4 + i * 2);
                                int rssi = dataReader.GetInt32(5 + i * 2);

                                circles.Add(new Circle(GetBoard(id).P, rssi));
                            }
                            Point point = Circle.Intersection(circles);
                            if (!(Double.IsNaN(point.X) || Double.IsNaN(point.Y)))
                            {
                                PhoneInfo p = new PhoneInfo(mac, timestamp, point, global);
                                list.Add(p);
                            }
                        }
                    }

                    InsertPhones(list, conn);
                }
                this.Connected = true;
                return list;
            }
            catch (KeyNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("idBoard not found...");
                throw e;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("MySqlException catched." + e.ToString());
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
            String query = " SELECT COUNT(DISTINCT P1.MAC)" + VarQuery(nBoards, time, threshold);

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
                catch (MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("MySqlException catched.");
                    this.Connected = false;
                    return -100;
                }
            }
        }


        private String VarQuery(int nBoards, int timestamp, int threshold = 0)
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
                builder.Append(" P").Append(i).Append(".ID_scheda > P").Append(i + 1).Append(".ID_scheda");
            }
            //TIMESTAMP
            for (int i = 1; i < nBoards; i++)
                builder.Append(" AND ABS(P1.timestamp - P").Append(i + 1).Append(".timestamp) <= ").Append(threshold);
            //MAC
            for (int i = 1; i < nBoards; i++)
                builder.Append(" AND P").Append(i).Append(".MAC = P").Append(i + 1).Append(".MAC");
            //HASH 
            builder.Append(" AND P1.hash = P2.hash AND P1.hash <> 00000000");
            //MAC
            for (int i = 1; i < nBoards; i++)
                builder.Append(" AND P").Append(i).Append(".MAC = P").Append(i + 1).Append(".MAC");
            builder.Append(" AND P1.timestamp > ").Append(timestamp);
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
            String query = "SELECT MAC, timestamp, x, y FROM posizioni AS p1 WHERE p1.MAC IN " +
                "(SELECT * FROM " +
                "(SELECT MAC " +
                "FROM posizioni AS p2 WHERE p2.timestamp < " + max + " AND p2.timestamp > " + min +
                " GROUP BY p2.MAC ORDER BY COUNT(*) DESC LIMIT " + n+") AS p3) AND p1.timestamp < " + max + " AND p1.timestamp > " + min + ";";

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
                catch (MySqlException e)
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
            String query = "SELECT MAC, timestamp, x, y " +
                "FROM posizioni WHERE timestamp < " + max + " AND timestamp > " + min+" ORDER BY timestamp";

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
                catch (MySqlException e)
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
               "FROM posizioni WHERE global = 1 AND timestamp > " + time +
               " AND ABS(x - " + p.Position.X.ToString(CultureInfo.InvariantCulture) + ") < " + threshold.ToString(CultureInfo.InvariantCulture) +
               " AND ABS(y - " + p.Position.Y.ToString(CultureInfo.InvariantCulture) + ") < " + threshold.ToString(CultureInfo.InvariantCulture) + 
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