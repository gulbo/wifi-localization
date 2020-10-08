using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using WifiLocalization.ConnectionManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WifiLocalization.Utilities;

namespace WifiLocalization.ChartManager
{

    class chartsManager
    {
        CartesianChart _movement;
        CartesianChart _temporalDistribution;
        ListBox _listBox;
        ChartValues<DatiDispositivo> scatter;
        ChartValues<int>[] occurenceCounter;
        StatCalc _statCalc;
        ObservableCollection<DatiDispositivo> macList;
        Dictionary<string, List<int>> MacOccurenceMap;
        Dictionary<string, int> macToIndex;
        public long animationCurrTimestamp { get; set; }
        public long animationStartTimestamp { get; set; }
        double minX, maxX, minY, maxY;
        List<SolidColorBrush> barsColor;

        public chartsManager(DBConnect dbC, ListBox lb, CartesianChart mv, CartesianChart td)
        {
            _listBox = lb;
            _movement = mv;
            _temporalDistribution = td;
            _statCalc = new StatCalc(dbC);
            macList = new ObservableCollection<DatiDispositivo>();
            scatter = new ChartValues<DatiDispositivo>();
            occurenceCounter = new ChartValues<int>[3];
            MacOccurenceMap = new Dictionary<string, List<int>>();
            macToIndex = new Dictionary<string, int>();
            animationStartTimestamp = animationCurrTimestamp;
            minX = minY = 0;
            maxX = maxY = 0;
            //colori delle barre
            barsColor = new List<SolidColorBrush>();
            barsColor.Add(Utilities.Utils.orange);
            barsColor.Add(Utilities.Utils.purple);
            barsColor.Add(Utilities.Utils.green);


            for (int i = 0; i < 3; i++)
            {
                occurenceCounter[i] = new ChartValues<int>();
            }
            _movement.AxisX.Add(new Axis
            {
                MinValue = -2,
                MaxValue = 2
            });
            _movement.AxisY.Add(new Axis
            {
                MinValue = -2,
                MaxValue = 2
            });
        }
        //metodo per la creazione della lista di MAC per quell'intervallo temporale
        public void CreateListBox(long start, long end)
        {
            macList.Clear();
            ClearSearchResults();
            animationStartTimestamp = start;
            animationCurrTimestamp = end;

            List<DatiDispositivo> phoneInfo = _statCalc.PhonesInRange(start, end);

            if (phoneInfo == null)
            {
                System.Windows.MessageBox.Show("Errore con la connessione al database. Controllare che il database sia online.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (phoneInfo.Count == 0)
            {
                
                System.Windows.MessageBox.Show("Nessuna posizione rilevata nell'intervallo specificato",
                    "No data found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            //solo a scopo visivo si crea una lista di mac da visualizzare
            foreach (DatiDispositivo ph in phoneInfo)
                macList.Add(ph);

            var macListDistinct = macList.Distinct(new ComparatoreDatiDispositivo());

            Application.Current.Dispatcher.Invoke(() => _listBox.ItemsSource = macListDistinct);
        }
        //pulisce Series e lista mac ricerca precedente
        public void ClearSearchResults()
        {
            _movement.Series.Clear();
            macToIndex.Clear();
        }

        //aggiunge una serie per il chart di movimento dei MAC
        public void AddSeries(string mac)
        {
            scatter.Clear();

            foreach (DatiDispositivo ph in macList)
            {
                if (ph.MAC_Address == mac)
                {

                    scatter.Add(ph);
                    if (ph.Posizione.Ascissa < minX)
                        minX = ph.Posizione.Ascissa;
                    if (ph.Posizione.Ascissa > maxX)
                        maxX = ph.Posizione.Ascissa;
                    if (ph.Posizione.Ordinata < minY)
                        minY = ph.Posizione.Ordinata;
                    if (ph.Posizione.Ordinata > maxY)
                        maxY = ph.Posizione.Ordinata;

                }
            }
            _movement.Series.Add(new LineSeries()
            {
                Title = Utilities.Utils.Formatta_MAC_Address(mac),
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 10,
                StrokeThickness = 4,
                Fill = Brushes.Transparent,
            });

            if (!macToIndex.ContainsKey(mac))
                macToIndex.Add(mac, _movement.Series.Count - 1); //associa un indice ad un determinato mac per poi recuperare la serie corrispondente 

            _movement.Series[macToIndex[mac]].LabelPoint = point => string.Format("Timestamp:{0} \n X:{1}  Y:{2}",
                new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(((DatiDispositivo)point.Instance).Timestamp),
                ((DatiDispositivo)point.Instance).Posizione.Ascissa, ((DatiDispositivo)point.Instance).Posizione.Ordinata);

            _movement.Series[macToIndex[mac]].Configuration = Mappers.Xy<DatiDispositivo>().X(b => b.Posizione.Ascissa).Y(b => b.Posizione.Ordinata);
            _movement.Series[macToIndex[mac]].Values = new ChartValues<DatiDispositivo>(scatter);
            _movement.AxisX[0].MinValue = minX - 5;
            _movement.AxisX[0].MaxValue = maxX + 5;
            _movement.AxisY[0].MinValue = minY - 5;
            _movement.AxisY[0].MaxValue = maxY + 5;
        }

        //rimuove la serie del mac passato dal chart di movimento dei MAC
        public void RemoveSeries(string rmac)
        {
            //trovo il massimo timestamp
            long tmp = animationStartTimestamp;
            bool found = false;
            int count = _movement.Series.Count - 1;


            for (int i = 0; i < _movement.Series.Count; i++)
            {
                if (i == count)
                    break;

                if (macToIndex[rmac] == i && found == false)
                    found = true;

                if (found)
                    _movement.Series[i].Values = _movement.Series[i + 1].Values;
            }

            for (int i = 0; i < _movement.Series.Count; i++)
                if (macToIndex[macToIndex.ElementAt(i).Key] > macToIndex[rmac])
                    macToIndex[macToIndex.ElementAt(i).Key]--;


            macToIndex.Remove(rmac);
            _movement.Series.RemoveAt(_movement.Series.Count - 1);

            foreach (string mac in macToIndex.Keys)
            {
                foreach (DatiDispositivo ph in _movement.Series[macToIndex[mac]].Values)
                {
                    if (ph.MAC_Address == mac)
                    {
                        if (ph.Timestamp > tmp)
                        {
                            tmp = ph.Timestamp;
                        }
                    }
                }
            }
            animationCurrTimestamp = tmp;
        }
       
        //metodo per aggiungere Serie a chart _temporalDistribuition
        public void AddTemporalSeries(string mac, SolidColorBrush color, ChartValues<int> values)
        {
            _temporalDistribution.Series.Add(new StackedRowSeries()
            {
                Title = Utilities.Utils.Formatta_MAC_Address(mac),
                Values = values,
                StackMode = StackMode.Values,
                DataLabels = true,
                LabelPoint = p => p.X.ToString(),
                Fill = color
            });
        }
        //metodo per la creazione del grafico delle ricorrenze dei MAC (_temporalDistribution)
        public void CreatePercentualChart(int start, int end)
        {
            DateTime dStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(start);
            Double d;
            int nIntervals = 0;

            for (int i = 0; i < 3; i++)
            {
                occurenceCounter[i].Clear();
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _temporalDistribution.AxisX.Clear();
                _temporalDistribution.AxisY.Clear();
            });
            //se le date sono valide cerco i 3 MAC più ricorrenti
            List<DatiDispositivo> mostFreq = (start == -1 && end == -1) ? new List<DatiDispositivo>() : _statCalc.MostFrequentPhones(3, start, end);
            MacOccurenceMap.Clear();

            if (start == -1 && end == -1)
            {
                return;
            }
            if (mostFreq == null)
            {
                System.Windows.MessageBox.Show("Errore con la connessione al Database, controllare che sia online", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (mostFreq.Count == 0)
            {
                System.Windows.MessageBox.Show("Nessun dato presente nell'intervallo fornito",
                    "No data found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            //conta le occorrenze per MAC
            foreach (DatiDispositivo ph in mostFreq)
            {
                if (MacOccurenceMap.ContainsKey(ph.MAC_Address))
                {
                    MacOccurenceMap[ph.MAC_Address].Add(ph.Timestamp);
                }
                else
                {
                    MacOccurenceMap.Add(ph.MAC_Address, new List<int> { ph.Timestamp });
                }
            }

                Application.Current.Dispatcher.Invoke(() =>
            {
                Axis xAxis = new Axis();
                Axis yAxis = new Axis();
                if (_temporalDistribution.AxisX.Count != 0)
                {
                    _temporalDistribution.AxisX.RemoveAt(0);
                }
                if (_temporalDistribution.AxisY.Count != 0)
                {
                    _temporalDistribution.AxisY.RemoveAt(0);
                }

                _temporalDistribution.AxisX.Add(xAxis);
                _temporalDistribution.AxisY.Add(yAxis);
                List<string> yAxisLabels = new List<string>();

                double size = (double)end - start;
                if (size > 0)
                {
                    //se intervallo è di max 3 min spazio di 10 s
                    if (TimeSpan.FromSeconds(size).TotalMinutes < 3)
                    {
                        d = (Double)1 / (Double)6;
                        nIntervals = (int)(size / TimeSpan.FromSeconds(10).TotalSeconds);
                        if ((size % TimeSpan.FromSeconds(10).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Time[mm::ss]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddSeconds(i * 10);
                            DateTime fin = dStart.AddSeconds(i*10 + 10);
                            yAxisLabels.Add(init.Minute.ToString("00") + ":" +
                                            init.Second.ToString("00") + " - " +
                                            fin.Minute.ToString("00") + ":" +
                                            fin.Second.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    //se intervallo è di max 5 min spazio di 20 s
                    else if (TimeSpan.FromSeconds(size).TotalMinutes < 5)
                    {
                        d = (Double)1 / (Double)3;
                        nIntervals = (int)(size / TimeSpan.FromSeconds(20).TotalSeconds) ;
                        if ((size % TimeSpan.FromSeconds(20).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Time[mm::ss]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddSeconds(i * 20);
                            DateTime fin = dStart.AddSeconds(i * 20 + 20);
                            yAxisLabels.Add(init.Minute.ToString("00") + ":" +
                                            init.Second.ToString("00") + " - " +
                                            fin.Minute.ToString("00") + ":" +
                                            fin.Second.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    //se intervallo è di max 20 min spazio di 1 minuto
                    else if (TimeSpan.FromSeconds(size).TotalMinutes < 20)
                    {
                        d = 1;
                        nIntervals = (int)(size / TimeSpan.FromMinutes(1).TotalSeconds);
                        if ((size % TimeSpan.FromMinutes(1).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Timehh:mm]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddMinutes(i );
                            DateTime fin = dStart.AddMinutes(i + 1);
                            yAxisLabels.Add(init.Minute.ToString("00") + ":" +
                                             init.Second.ToString("00") + " - " +
                                             fin.Minute.ToString("00") + ":" +
                                             fin.Second.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    //se l'intervallo è di max 2 ora spaziamo di 5 minuti
                    else if (TimeSpan.FromSeconds(size).TotalHours < 2)
                    {
                        d = 5;
                        nIntervals = (int)(size / TimeSpan.FromMinutes(5).TotalSeconds);
                        if ((size % TimeSpan.FromMinutes(5).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Time[hh:mm]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddMinutes(i * 5);
                            DateTime fin = dStart.AddMinutes(i * 5 + 5);
                            yAxisLabels.Add(init.Hour.ToString("00") + ":" +
                                            init.Minute.ToString("00") + " - " +
                                            fin.Hour.ToString("00") + ":" +
                                            fin.Minute.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    //se l'intervallo è di max 4 ore spaziamo di 10 minuti
                    else if (TimeSpan.FromSeconds(size).TotalHours < 4)
                    {
                        d = 10;
                        nIntervals = (int)(size / TimeSpan.FromMinutes(10).TotalSeconds);
                        if ((size % TimeSpan.FromMinutes(10).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Time[hh:mm]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddMinutes(i * 10);
                            DateTime fin = dStart.AddMinutes(i * 10 + 10);
                            yAxisLabels.Add(init.Hour.ToString("00") + ":" +
                                            init.Minute.ToString("00") + " - " +
                                            fin.Hour.ToString("00") + ":" +
                                            fin.Minute.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    //se l'intervallo è di max 24 ore spaziamo di 1 ora
                    else if (TimeSpan.FromSeconds(size).TotalDays < 1)
                    {
                        d = 60;
                        nIntervals = (int)((end - start) / TimeSpan.FromHours(1).TotalSeconds);
                        if ((size % TimeSpan.FromHours(1).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Time[hh:mm]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddHours(i);
                            DateTime fin = dStart.AddHours(i + 1);
                            yAxisLabels.Add(init.Hour.ToString("00") + ":" +
                                            init.Minute.ToString("00") + " - " +
                                            fin.Hour.ToString("00") + ":" +
                                            fin.Minute.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;

                    }
                    //se l'intervallo è di max 3 giorni spaziamo di 6 ore
                    else if (TimeSpan.FromSeconds(size).TotalDays < 6)
                    {
                        d = 360;
                        nIntervals = (int)((end - start) / TimeSpan.FromHours(6).TotalSeconds);
                        if ((size % TimeSpan.FromHours(6).TotalSeconds) > 0 ) nIntervals++;
                        yAxis.Title = "Time[dd/mm hh:mm]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddHours(i * 6);
                            DateTime fin = dStart.AddHours(i * 6 + 6);
                            yAxisLabels.Add(init.Day.ToString("00") + "/" +
                                            init.Month.ToString("00") + "  " +
                                            init.Hour.ToString("00") + ":" +
                                            init.Minute.ToString("00") + " - " +
                                            fin.Day.ToString("00") + "/" +
                                            fin.Month.ToString("00") + "  " +
                                            fin.Hour.ToString("00") + ":" +
                                            fin.Minute.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    //se l'intervallo è di max una settimana spaziamo di un giorno
                    else if (TimeSpan.FromSeconds(size).TotalDays <= 7)
                    {
                        d = 1440;
                        nIntervals = (int)((end - start) / TimeSpan.FromDays(1).TotalSeconds);
                        if ((size % TimeSpan.FromDays(1).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Time[dd/mm/yyyy]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddDays(i);
                            DateTime fin = dStart.AddDays(i + 1);
                            yAxisLabels.Add(init.Day.ToString("00") + "/" +
                                            init.Month.ToString("00") + "/" +
                                            init.Year.ToString("00") + " - " +
                                            fin.Day.ToString("00") + "/" +
                                            fin.Month.ToString("00") + "/" +
                                            fin.Year.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;

                    }
                    //se l'intervallo è di un mese spaziamo di una settimana
                    else if (TimeSpan.FromSeconds(size).TotalDays < 30)
                    {
                        d = 10080;
                        nIntervals = (int)((end - start) / TimeSpan.FromDays(7).TotalSeconds);
                        if ((size % TimeSpan.FromDays(7).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Time[dd/mm/yyyy]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddDays(i * 7);
                            DateTime fin = dStart.AddDays(i * 7 + 7);
                            yAxisLabels.Add(init.Day.ToString("00") + "/" +
                                            init.Month.ToString("00") + "/" +
                                            init.Year.ToString("00") + " - " +
                                            fin.Day.ToString("00") + "/" +
                                            fin.Month.ToString("00") + "/" +
                                            fin.Year.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    //se l'intervallo è di un anno spaziamo di un mese
                    else if (TimeSpan.FromSeconds(size).TotalDays < 365)
                    {
                        d = 43200;
                        nIntervals = (int)((end - start) / TimeSpan.FromDays(30).TotalSeconds);
                        if ((size % TimeSpan.FromDays(30).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Time[dd/mm/yyyy]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddDays(i * 30);
                            DateTime fin = dStart.AddDays(i * 30 + 30);
                            yAxisLabels.Add(init.Day.ToString("00") + "/" +
                                            init.Month.ToString("00") + "/" +
                                            init.Year.ToString("00") + " - " +
                                            fin.Day.ToString("00") + "/" +
                                            fin.Month.ToString("00") + "/" +
                                            fin.Year.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    //altrimenti spaziamo di un anno
                    else
                    {
                        //  SetIntervals(525600, start, end);

                        d = 525600;
                        nIntervals = (int)((end - start) / TimeSpan.FromDays(365).TotalSeconds);
                        if ((size % TimeSpan.FromDays(365).TotalSeconds) > 0) nIntervals++;
                        yAxis.Title = "Time[mm/yyyy]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddDays(i * 365);
                            DateTime fin = dStart.AddDays(i * 365 + 365);
                            yAxisLabels.Add(init.Month.ToString("00") + "/" +
                                            init.Year.ToString("00") + " - " +
                                            fin.Month.ToString("00") + "/" +
                                            fin.Year.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    SetIntervals(nIntervals, d, start, end);
                }
            });
        }
        //crea gli intervalli
        private void SetIntervals(int nIntervals, Double timeUnit, int start, int end)
        {
            int[] intervals = new int[nIntervals];

            int n = 0; //indice colonna (mac)
            foreach (string mac in MacOccurenceMap.Keys)
            {

                for (int i = 0; i < nIntervals; i++)
                    intervals[i] = 0;
                //faccio il conteggio delle occorrenze del mac in quell'intervallo controllando il timestamp
                foreach (int time in MacOccurenceMap[mac])
                {
                    //incremento il contatore all'interno dell'intervallo opportuno
                    intervals[(int)((time - start) / TimeSpan.FromMinutes(timeUnit).TotalSeconds)]++;

                }
                //carichiamo la lista delle occorrenze per ogni MAC
                for (int i = 0; i < nIntervals; i++)
                {
                    occurenceCounter[n].Add(intervals[i]);
                }
                // se è la prima volta che cerchiamo i MAC creo le series
                if (_temporalDistribution.Series.Count < 3)
                {
                    AddTemporalSeries(mac, barsColor[n], occurenceCounter[n]);
                }
                //altrimenti aggiorno il valore
                else
                {
                    _temporalDistribution.Series[n].Values = occurenceCounter[n];
                }
               
                n++;

            }

        }

    }
}
