﻿using System;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Concurrent;
using MySql.Data.MySqlClient;
using WifiLocalization.Utilities;

namespace WifiLocalization.ConnectionManager
{
    public sealed class DBConnect
    {
        private enum ReduceType { Mean, Random };
        private ConcurrentDictionary<int, Scheda> schede;

        public string Server { get; private set; }
        public string Uid { get; private set; }
        public string Password { get; private set; }
        public string Database { get; private set; }
        public bool Connesso { get; private set; }

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

        public bool InserisciScheda(Scheda scheda)
        {
            try
            {
                String query = String.Format("INSERT INTO schede (ID_scheda, x, y) VALUES({0}, {1}, {2})",
                    scheda.ID_scheda, scheda.Punto.Ascissa, scheda.Punto.Ordinata);

                AggiungiScheda(scheda);

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire le schede nel database" + e.ToString());
                Connesso = false;
                return false;
            }
        }

        public bool InserisciScheda(IEnumerable<Scheda> schede)
        {
            if (schede.Count() == 0)
            {
                return false;
            }
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

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire le schede nel database" + e.ToString());
                Connesso = false;
                return false;
            }
        }

        public bool InserisciScheda(int id_scheda, double x, double y)
        {
            try
            {
                String query = String.Format("INSERT INTO schede (ID_scheda, x, y) VALUES({0}, {1}, {2})", id_scheda, x, y);

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                AggiungiScheda(new Scheda(id_scheda, x, y));
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire le schede nel database" + e.ToString());
                Connesso = false;
                return false;
            }
        }

        //Rimuovi scheda (selezionando l'ID_scheda) nella tabella 'schede' del DB

        public bool RimuoviScheda(int id_scheda)
        {
            try
            {
                string query = String.Format("DELETE FROM schede WHERE ID_scheda='{0}'", id_scheda);

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Errore durante la rimozione della scheda" + e.ToString());
                Connesso = false;
                return false;
            }
        }

        //Rimuovi schede nella tabella 'schede' del DB

        public bool RimuoviSchede()
        {
            try
            {
                string query = String.Format("DELETE FROM schede");

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Errore durante la rimozione delle schede" + e.ToString());
                Connesso = false;
                return false;
            }
        }

        //Seleziona l'elenco delle schede (tutte) dalla tabella 'schede' del DB

        public List<Scheda> SelezionaSchede()
        {

            //Creo una lista di schede per memorizzare i risultati

            List<Scheda> lista_schede = new List<Scheda>();

            using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (MySqlCommand cmd = connessione.CreateCommand())
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
                            lista_schede.Add(scheda);
                        }
                        Connesso = true;
                        return lista_schede;
                    }
                }
                catch (MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Impossibile recuperare le informazioni sulle schede dal database" + e.ToString());
                    Connesso = false;
                    return null;
                }
            }
        }

        //Inserisci pacchetti nella tabella 'pacchetti' del DB

        public bool InserisciPacchetto(Pacchetto pacchetto)
        {
            try
            {
                string query = String.Format("INSERT INTO  pacchetti(MAC, RSSI, SSID, timestamp, hash, ID_scheda, global) VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                    pacchetto.MAC_Address, pacchetto.RSSI, pacchetto.SSID, pacchetto.Timestamp, pacchetto.Checksum, pacchetto.ID_scheda, pacchetto.Global);

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire i pacchetti nel database" + e.ToString());
                Connesso = false;
                return false;
            }
        }

        public bool InserisciPacchetto(List<Pacchetto> pacchetti)
        {
            if (pacchetti.Count() == 0)
            {
                return false;
            }
            try
            {
                StringBuilder builderQuery = new StringBuilder();
                builderQuery.Append("INSERT INTO  pacchetti(MAC, RSSI, SSID, timestamp, hash, ID_scheda, global) VALUES");
                foreach (Pacchetto pacchetto in pacchetti)
                {
                    builderQuery.Append(string.Format("('{0}','{1}','{2}','{3}','{4}','{5}',{6})",
                        pacchetto.MAC_Address, pacchetto.RSSI, MySqlHelper.EscapeString(pacchetto.SSID), pacchetto.Timestamp, pacchetto.Checksum, pacchetto.ID_scheda, pacchetto.Global));
                    if (pacchetto.Equals(pacchetti.Last<Pacchetto>()))
                    {
                        builderQuery.Append(";");
                    }
                    else
                    {
                        builderQuery.Append(", ");
                    }
                }
                string query = builderQuery.ToString();

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire i pacchetti nel database" + e.ToString());
                Connesso = false;
                return false;
            }
        }

        public bool InserisciPacchetto(string mac, int rssi, string ssid, int timestamp, string checksum, string id_scheda, bool global)
        {
            try
            {
                string query = String.Format("INSERT INTO  pacchetti(MAC, RSSI, SSID, timestamp, hash, ID_scheda, global) VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}')",
                    mac, rssi, ssid, timestamp, checksum, id_scheda, global);
               
                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Impossibile inserire i pacchetti nel database" + e.ToString());
                Connesso = false;
                return false;
            }
        }

        //Rimuovi pacchetto (selezionando l'ID_pacchetto) nella tabella 'pacchetti' del DB

        public bool RimuoviPacchetto(int id_pacchetto)
        {
            try
            {
                string query = String.Format("DELETE FROM pacchetti WHERE ID_pacchetto='{0}'", id_pacchetto);

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Errore durante la rimozione dei pacchetti" + e.ToString());
                Connesso = false;
                return false;
            }
        }

        //Rimuovi pacchetti nella tabella 'pacchetti' del DB

        public bool RimuoviPacchetti()
        {
            try
            {
                string query = String.Format("DELETE FROM pacchetti");

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Errore durante la rimozione dei pacchetti" + e.ToString());
                Connesso = false;
                return false;
            }
        }
        
        //Seleziona l'elenco dei pacchetti dalla tabella 'pacchetti' del DB

        public List<Pacchetto> SelezionaPacchetti(string query)
        {
            //Creo una lista di pacchetti per memorizzare i risultati

            List<Pacchetto> lista_pacchetti = new List<Pacchetto>();

            using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (MySqlCommand cmd = connessione.CreateCommand())
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
                            lista_pacchetti.Add(pacchetto);
                        }
                        Connesso = true;
                        return lista_pacchetti;
                    }
                }
                catch (MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Impossibile recuperare le informazioni sui pacchetti dal database" + e.ToString());
                    Connesso = false;
                    return null;
                }
            }
        }
 
        public List<string>[] SelezionaColonnePacchetti(string query)
        {
            //Creo una lista per ogni colonna della tabella 'pacchetti' per memorizzare i risultati

            List<string>[] lista = new List<string>[8];   
            lista[0] = new List<string>();
            lista[1] = new List<string>();
            lista[2] = new List<string>();
            lista[3] = new List<string>();
            lista[4] = new List<string>();
            lista[5] = new List<string>();
            lista[6] = new List<string>();
            lista[7] = new List<string>();

            using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (MySqlCommand cmd = connessione.CreateCommand())
            {
                try
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    using (var leggiDati = cmd.ExecuteReader())
                    {
                        while (leggiDati.Read())
                        {
                            lista[0].Add(leggiDati["ID_pacchetto"] + "");
                            lista[1].Add(leggiDati["MAC"] + "");
                            lista[2].Add(leggiDati["RSSI"] + "");
                            lista[3].Add(leggiDati["SSID"] + "");
                            lista[4].Add(leggiDati["Timestamp"] + "");
                            lista[5].Add(leggiDati["Checksum"] + "");
                            lista[6].Add(leggiDati["ID_scheda"] + "");
                            lista[7].Add(leggiDati["Global"] + "");
                        }
                        Connesso = true;
                        return lista;
                    }
                }
                catch(MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("Impossibile recuperare le informazioni sui pacchetti dal database" + e.ToString());
                    Connesso = false;
                    return null;
                }
            }

        }

        //Inserisci posizioni (dispositivi) nella tabella 'posizioni' del DB

        private void InserisciPosizioni(IEnumerable<DatiDispositivo> dispositivi, MySqlConnection connessione)
        {
            if (dispositivi.Count() == 0)
            {
                return;
            }
            StringBuilder builderQuery = new StringBuilder();
            builderQuery.Append("INSERT INTO posizioni (MAC, x, y, timestamp, global) VALUES ");
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            foreach (DatiDispositivo telefono in dispositivi)
            {
                builderQuery.Append(string.Format("('{0}',{1},{2},{3},{4})",
                    telefono.MAC_Address, Math.Round(telefono.Posizione.Ascissa, 2).ToString(culture),
                    Math.Round(telefono.Posizione.Ordinata, 2).ToString(culture), telefono.Timestamp, telefono.Global));
                if (telefono.Equals(dispositivi.Last<DatiDispositivo>()))
                {
                    builderQuery.Append(";");
                }
                else
                {
                    builderQuery.Append(", ");
                }
            }
            string query = builderQuery.ToString();
            try
            {
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    //Create Command
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Unable to insert phones in database" + e.ToString());
                Connesso = false;
                throw;
            }
        }







        //Rimuovi posizioni (dispositivi) nella tabella 'posizioni' del DB

        public bool RimuoviPosizioni()
        {
            try
            {
                string query = String.Format("DELETE FROM posizioni");

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                Connesso = true;
                return true;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("Errore durante la cancellazione della storia delle posizioni" + e.ToString());
                Connesso = false;
                return false;
            }
        }








        /// <summary>
        /// se abbiamo almeno 2 schede, ricerca nel database la tupla(Distinct) MAC,timestamp,global,hash, ID scheda i,RSSI scheda i 
        /// all'interno di pacchetti in cui prendendo in considerazione dati di schede diverse riguardanti lo stesso MAC (i-esimo)
        /// la cui differenza di timestamp (tra dati diversi) sia entro una certa soglia (si riferisca alla stessa lettura) e il cui
        /// timestamp sia entro il minuto di ricerca , prese queste tuple calcola i punti di intersezione tramite i metodi dei cerchi
        /// ed infine li aggiunge ad una lista 
        /// </summary>
        /// <param name="nBoards">numero di schede presenti</param>
        /// <param name="threshold">soglia del timestamp tra dati diversi</param>
        /// <returns>lista di punti di intersezione</returns>
        public List<DatiDispositivo> GetLastMinuteData(int nBoards, int threshold = 0)
        {
            try
            {
                if (nBoards < 2)
                    return null;

                int time = EspServer.getUnixEpoch() - 60;

                //Create a list to store the result
                List<DatiDispositivo> list = new List<DatiDispositivo>();

                using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
                using (MySqlCommand cmd = connessione.CreateCommand())
                {
                    connessione.Open();
                    //Create Query
                    StringBuilder builder = new StringBuilder();
                    builder.Append("SELECT DISTINCT P1.MAC, P1.timestamp, P1.global, P1.hash");

                    for (int i = 0; i < nBoards; i++)
                        builder.Append(", P").Append(i + 1).Append(".ID_scheda, P").Append(i + 1).Append(".RSSI");

                    builder.Append(VarQuery(nBoards, time, threshold));

                    //Create Command
                    cmd.CommandText = builder.ToString();
                    cmd.ExecuteNonQuery();
                    using (MySqlDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            string mac = dataReader.GetString(0);  
                            int timestamp = dataReader.GetInt32(1);
                            bool global = dataReader.GetBoolean(2);

                            List<Cerchio> cerchi = new List<Cerchio>();
                            for (int i = 0; i < nBoards; i++)
                            {
                                int id = dataReader.GetInt32(4 + i * 2);
                                int rssi = dataReader.GetInt32(5 + i * 2);

                                cerchi.Add(new Cerchio(GetScheda(id).Punto, rssi));  
                            }
                            Punto point = Cerchio.Intersezione(cerchi);
                            if(!(Double.IsNaN(point.Ascissa) || Double.IsNaN(point.Ordinata)))
                            {
                                if (point.isInside(schede))
                                {
                                    DatiDispositivo p = new DatiDispositivo(mac, timestamp, point, global);
                                    list.Add(p);
                                }
                                else 
                                {
                                    System.Diagnostics.Debug.WriteLine("Geofence discarded: (" + point.Ascissa + "; " + point.Ordinata + ")");
                                }
                            }
                        }
                    }

                    InserisciPosizioni(list, connessione);
                }
                Connesso = true;
                return list;
            }
            catch (KeyNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("idBoard not found..." + e.ToString());
                throw e;
            }
            catch (MySqlException e)
            {
                System.Diagnostics.Debug.WriteLine("MySqlException catched." + e.ToString());
                Connesso = false;
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
            int time = EspServer.getUnixEpoch() - 300;
            string query = " SELECT COUNT(DISTINCT P1.MAC)" + VarQuery(nBoards,time,threshold);

            using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (MySqlCommand cmd = connessione.CreateCommand())
            {
                try
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        Connesso = true;
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
                    System.Diagnostics.Debug.WriteLine("MySqlException catched." + e.ToString());
                    Connesso = false;
                    return -100;
                }
            }
        }


        private string VarQuery(int nBoards, int timestamp, int threshold=0)
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
            for (int i = 1; i< nBoards; i++)
                builder.Append(" AND P").Append(i).Append(".hash = P").Append(i + 1).Append(".hash AND P1.hash <> 00000000");         
            builder.Append(" AND P1.timestamp > ").Append(timestamp);
            return builder.ToString();

        }

        public List<DatiDispositivo> MostFrequentPhones(int n, int min, int max, int threshold = 0)
        {
            List<DatiDispositivo> MACList = new List<DatiDispositivo>();
            string query = "SELECT DISTINCT MAC, timestamp " +
                "FROM posizioni AS p1" +
                " WHERE p1.MAC IN (SELECT * " +
                                    "FROM (SELECT MAC " +
                                          "FROM posizioni AS p2 " +
                                          "WHERE p2.timestamp < " + max + " AND p2.timestamp > " + min +
                                          " GROUP BY p2.MAC" +
                                          " ORDER BY COUNT(*) DESC" +
                                          " LIMIT " + n+")" +
                                   " AS p3)" +
               " AND p1.timestamp < " + max + " AND p1.timestamp > " + min + ";";

            using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (MySqlCommand cmd = connessione.CreateCommand())
            {
                try
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            DatiDispositivo pi = new DatiDispositivo(dataReader.GetString(0), dataReader.GetInt32(1), 0, 0);
                            MACList.Add(pi);
                        }
                        Connesso = true;
                        return MACList;
                    }
                }
                catch (MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("MySqlException catched" + e.ToString());
                    Connesso = false;
                    return null;
                }
            }
        }

        public List<DatiDispositivo> PhonesInRange(int min, int max, int threshold = 0)
        {
            List<DatiDispositivo> list = new List<DatiDispositivo>();
            string query = "SELECT MAC, timestamp, x, y " +
                "FROM posizioni WHERE timestamp < " + max + " AND timestamp > " + min+" ORDER BY timestamp";

            using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (MySqlCommand cmd = connessione.CreateCommand())
            {
                try
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            DatiDispositivo pi = new DatiDispositivo(dataReader.GetString(0), dataReader.GetInt32(1), dataReader.GetDouble(2), dataReader.GetDouble(3));
                            list.Add(pi);
                        }
                        Connesso = true;
                        return list;
                    }
                }
                catch (MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("MySqlException catched" + e.ToString());
                    Connesso = false;
                    return null;
                }
            }
        }

        public List<string> CountHiddenPhones(DatiDispositivo p, double threshold)
        {
            int time = EspServer.getUnixEpoch() - 60;
            List<string> list = new List<string>();

            string query = "SELECT MAC " +
               "FROM posizioni WHERE global = 1 AND timestamp > " + time +
               " AND ABS(x - " + p.Posizione.Ascissa.ToString(CultureInfo.InvariantCulture) + ") < " + threshold.ToString(CultureInfo.InvariantCulture) +
               " AND ABS(y - " + p.Posizione.Ordinata.ToString(CultureInfo.InvariantCulture) + ") < " + threshold.ToString(CultureInfo.InvariantCulture) + 
               " AND MAC <> '" + p.MAC_Address + "'";

            using (MySqlConnection connessione = new MySqlConnection("Database=" + Database + ";" + "Server=" + Server + ";" + "Port=3306;" + "UID=" + Uid + ";" + "Password=" + Password + ";"))
            using (MySqlCommand cmd = connessione.CreateCommand())
            {
                try
                {
                    connessione.Open();
                    cmd.CommandText = query;
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            list.Add(dataReader.GetString(0));
                        }
                        Connesso = true;
                        return list;
                    }
                }
                catch (MySqlException e)
                {
                    System.Diagnostics.Debug.WriteLine("MySqlException catched." + e.ToString());
                    Connesso = false;
                    return null;
                }
            }
        }
    }
}