using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using PDSClient.ConnectionManager;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace PDSClient.StatModule
{

    class StaticChart
    {
        CartesianChart _movement;
        CartesianChart _temporalDistribution;
        ListBox _listBox;
        ChartValues<DatiDispositivo> scatter;
        ChartValues<int>[] occurenceCounter;
        StatCalc _statCalc;
        ObservableCollection<DatiDispositivo> macList;
        Dictionary<string, List<int>> map;
        Dictionary<string, int> macToIndex;
        public long animationCurrTimestamp { get; set; }
        public long animationStartTimestamp { get; set; }
        double minX, maxX, minY, maxY;
        List<SolidColorBrush> barsColor;

        public StaticChart(DBConnect dbC, ListBox lb, CartesianChart mv, CartesianChart td)
        {
            _listBox = lb;
            _movement = mv;
            _temporalDistribution = td;
            _statCalc = new StatCalc(dbC);
            macList = new ObservableCollection<DatiDispositivo>();
            scatter = new ChartValues<DatiDispositivo>();
            occurenceCounter = new ChartValues<int>[3];
            map = new Dictionary<string, List<int>>();
            macToIndex = new Dictionary<string, int>();
            animationStartTimestamp = animationCurrTimestamp;
            minX = minY = 0;
            maxX = maxY = 0;
            //colori delle barre
            barsColor = new List<SolidColorBrush>();
            barsColor.Add(Utils.orange);
            barsColor.Add(Utils.purple);
            barsColor.Add(Utils.green);

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
            animationStartTimestamp = start;
            animationCurrTimestamp = end;

            List<DatiDispositivo> phoneInfo = _statCalc.PhonesInRange(start, end);

            if (phoneInfo == null)
            {
                //TODO messaggio di errore
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

            foreach (DatiDispositivo ph in phoneInfo)
                macList.Add(ph);

            var macListDistinct = macList.Distinct(new PhoneInfoComparer());

            Application.Current.Dispatcher.Invoke(() => _listBox.ItemsSource = macListDistinct);
        }

        //aggiunge una serie per il chart di movimento dei MAC
        public void AddSeries(string mac)
        {
            scatter.Clear();

            foreach (DatiDispositivo ph in macList)
            {
                if (ph.MacAddr == mac)
                {

                    scatter.Add(ph);
                    if (ph.Position.Ascissa < minX)
                        minX = ph.Position.Ascissa;
                    if (ph.Position.Ascissa > maxX)
                        maxX = ph.Position.Ascissa;
                    if (ph.Position.Ordinata < minY)
                        minY = ph.Position.Ordinata;
                    if (ph.Position.Ordinata > maxY)
                        maxY = ph.Position.Ordinata;

                }
            }
            _movement.Series.Add(new LineSeries()
            {
                Title = Utils.FormatMACAddr(mac),
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 10,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
            });

            if (!macToIndex.ContainsKey(mac))
                macToIndex.Add(mac, _movement.Series.Count - 1); //associa un indice ad un determinato mac per poi recuperare la serie corrispondente 

            _movement.Series[macToIndex[mac]].LabelPoint = point => string.Format("Timestamp:{0} \n X:{1}  Y:{2}",
                new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(((DatiDispositivo)point.Instance).Timestamp),
                ((DatiDispositivo)point.Instance).Position.Ascissa, ((DatiDispositivo)point.Instance).Position.Ordinata);

            _movement.Series[macToIndex[mac]].Configuration = Mappers.Xy<DatiDispositivo>().X(b => b.Position.Ascissa).Y(b => b.Position.Ordinata);
            _movement.Series[macToIndex[mac]].Values = new ChartValues<DatiDispositivo>(scatter);
            _movement.AxisX[0].MinValue = minX - 0.1;
            _movement.AxisX[0].MaxValue = maxX + 0.1;
            _movement.AxisY[0].MinValue = minY - 0.1;
            _movement.AxisY[0].MaxValue = maxY + 0.1;
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
                    if (ph.MacAddr == mac)
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
                Title =Utils.FormatMACAddr(mac),
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
            map.Clear();

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
                if (map.ContainsKey(ph.MacAddr))
                {
                    map[ph.MacAddr].Add(ph.Timestamp);
                }
                else
                {
                    map.Add(ph.MacAddr, new List<int> { ph.Timestamp });
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
                    //se l'intervallo è di un'ora spaziamo di 5 minuti
                    if (TimeSpan.FromSeconds(size).TotalHours < 1)
                    {

                        //int nIntervals = (int)(size / TimeSpan.FromMinutes(10).TotalSeconds) + 1;
                        int nIntervals = 12;

                        SetIntervals(5, start, end);
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
                    //se l'intervallo è di 4 ore spaziamo di 20 minuti
                    else if (TimeSpan.FromSeconds(size).TotalHours < 4)
                    {

                        int nIntervals = (int)(size / TimeSpan.FromMinutes(10).TotalSeconds) + 1;
                        SetIntervals(20, start, end);
                        yAxis.Title = "Time[hh:mm]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddMinutes(i * 20);
                            DateTime fin = dStart.AddMinutes(i * 20 + 20);
                            yAxisLabels.Add(init.Hour.ToString("00") + ":" +
                                            init.Minute.ToString("00") + " - " +
                                            fin.Hour.ToString("00") + ":" +
                                            fin.Minute.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;
                    }
                    //se l'intervallo è di 12 ore spaziamo di 1 ora
                    else if (TimeSpan.FromSeconds(size).TotalHours < 12)
                    {
                        SetIntervals(60, start, end);
                        int nIntervals = (int)((end - start) / TimeSpan.FromHours(1).TotalSeconds) + 1;
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
                    //se l'intervallo è di 24 ore spaziamo di 2 ora
                    else if (TimeSpan.FromSeconds(size).TotalHours < 24)
                    {
                        SetIntervals(120, start, end);
                        int nIntervals = (int)((end - start) / TimeSpan.FromHours(2).TotalSeconds) + 1;
                        yAxis.Title = "Time[hh:mm]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddHours(i);
                            DateTime fin = dStart.AddHours(i + 2);
                            yAxisLabels.Add(init.Hour.ToString("00") + ":" +
                                            init.Minute.ToString("00") + " - " +
                                            fin.Hour.ToString("00") + ":" +
                                            fin.Minute.ToString("00"));
                        }
                        yAxis.Labels = yAxisLabels;

                    }
                    //se l'intervallo è di 3 giorni spaziamo di 6 ore
                    else if (TimeSpan.FromSeconds(size).TotalDays < 3)
                    {
                        SetIntervals(360, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromHours(6).TotalSeconds) + 1;
                        yAxis.Title = "Time[dd/mm hh:mm]";
                        yAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddHours(i * 5);
                            DateTime fin = dStart.AddHours(i * 5 + 5);
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
                    //se l'intervallo è di una settimana spaziamo di un giorno
                    else if (TimeSpan.FromSeconds(size).TotalDays < 7)
                    {
                        SetIntervals(1440, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromDays(1).TotalSeconds) + 1;
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
                        SetIntervals(10080, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromDays(7).TotalSeconds) + 1;
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
                        SetIntervals(43200, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromDays(30).TotalSeconds) + 1;
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
                        SetIntervals(525600, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromDays(365).TotalSeconds) + 1;
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
                }
            });
        }
        //crea gli intervalli
        private void SetIntervals(int minutes, int start, int end)
        {
            //trovo il numero di intervalli dividendo il mio intervallo di tempo per l'intervallo sceltoo +1 perchè abbiamo almeno 1 intervallo
            int nIntervals = (int)((end - start) / TimeSpan.FromMinutes(minutes).TotalSeconds) + 1;

            int[] intervals = new int[nIntervals];

            int n = 0; //indice colonna (mac)
            foreach (string s in map.Keys)
            {

                for (int i = 0; i < nIntervals; i++)
                    intervals[i] = 0;
                //faccio il conteggio delle occorrenze del mac in quell'intervallo
                foreach (int t in map[s])
                {
                    //incremento il contatore all'interno dell'intervallo opportuno
                    intervals[(int)((t - start) / TimeSpan.FromMinutes(minutes).TotalSeconds)]++;

                }
                //carichiamo la lista delle occorrenze per ogni MAC
                for (int i = 0; i < nIntervals; i++)
                {
                    occurenceCounter[n].Add(intervals[i]);
                }
                // se è la prima volta che cerchiamo i MAC creo le series
                if (_temporalDistribution.Series.Count < 3)
                {
                    AddTemporalSeries(s, barsColor[n], occurenceCounter[n]);
                }
                else
                {
                    _temporalDistribution.Series[n].Values = occurenceCounter[n];
                }
               
                n++;

            }

        }

    }
}
