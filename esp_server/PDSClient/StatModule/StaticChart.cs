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
        ChartValues<PhoneInfo> scatter;
        ChartValues<HeatPoint> heatPoints;
        StatCalc _statCalc;
        ObservableCollection<PhoneInfo> macList;
        Dictionary<string, List<int>> map;
        Dictionary<string, int> macToIndex;
        public long animationCurrTimestamp { get; set; }
        long animationStartTimestamp;
        long animationTimestampEnd;
        double minX, maxX, minY, maxY;
        
        public StaticChart(DBConnect dbC, ListBox lb, CartesianChart mv, CartesianChart td) {
            _listBox = lb;
            _movement = mv;
            _temporalDistribution = td;
            _statCalc = new StatCalc(dbC);
            macList = new ObservableCollection<PhoneInfo>();
            scatter = new ChartValues<PhoneInfo>();
            heatPoints = new ChartValues<HeatPoint>();
            map = new Dictionary<string, List<int>>();
            macToIndex = new Dictionary<string, int>();
            animationStartTimestamp = animationCurrTimestamp;
            minX = minY = 0;
            maxX = maxY = 0;

            _temporalDistribution.Series.Add(new HeatSeries()
            {
                DataLabels = true,
                GradientStopCollection = new GradientStopCollection
                {
                new GradientStop((Color)ColorConverter.ConvertFromString("#a5d4f9"), 0),
                new GradientStop((Color)ColorConverter.ConvertFromString("#66b6f6"), 0.5),
                new GradientStop((Color)ColorConverter.ConvertFromString("#2295f2"), 1)
                },
            });

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

        public void CreateListBox(int start, int end)
        {
       
            macList.Clear();
            animationCurrTimestamp = start;
            animationStartTimestamp = start;
            animationTimestampEnd = end;

            List<PhoneInfo> phoneInfo = _statCalc.PhonesInRange(start, end);

            if (phoneInfo == null)
            {
                //TODO messaggio di errore
                System.Windows.MessageBox.Show("Error with the database connection. Please check that the database is online.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if(phoneInfo.Count == 0)
            {
                System.Windows.MessageBox.Show("No data found within the given time interval.",
                    "No data found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            foreach (PhoneInfo ph in phoneInfo)
                macList.Add(ph);

            var macListDistinct = macList.Distinct(new PhoneInfoComparer());

            Application.Current.Dispatcher.Invoke(() => _listBox.ItemsSource = macListDistinct);
        }

        public void AddSeries(string mac) {
            scatter.Clear();

            foreach (PhoneInfo ph in macList)
            {
                if (ph.MacAddr == mac)
                {
                    if (animationCurrTimestamp >= ph.Timestamp)
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
            }
            _movement.Series.Add(new LineSeries() {
                Title = Utils.FormatMACAddr(mac),
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 10,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent, 
            });

            if (!macToIndex.ContainsKey(mac))
                macToIndex.Add(mac, _movement.Series.Count - 1); //associa un indice ad un determinato mac per poi recuperare la serie corrispondente 

            _movement.Series[macToIndex[mac]].LabelPoint = point => string.Format("Timestamp:{0} \n X:{1}  Y:{2}", 
                new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(((PhoneInfo)point.Instance).Timestamp), 
                ((PhoneInfo)point.Instance).Position.Ascissa, ((PhoneInfo)point.Instance).Position.Ascissa);
           
            //SECONDO ME è SBAGLIATA QUI!!!! DANIELE

            _movement.Series[macToIndex[mac]].Configuration = Mappers.Xy<PhoneInfo>().X(b => b.Position.Ascissa).Y(b => b.Position.Ordinata);
            _movement.Series[macToIndex[mac]].Values = new ChartValues<PhoneInfo>(scatter);
            _movement.AxisX[0].MinValue = minX - 0.1;
            _movement.AxisX[0].MaxValue = maxX + 0.1;
            _movement.AxisY[0].MinValue = minY - 0.1;
            _movement.AxisY[0].MaxValue = maxY + 0.1;
        }

        public void AddNextPoint() {
            long tmp=animationTimestampEnd;
            PhoneInfo p=null;
            bool found=false;

            foreach (string mac in macToIndex.Keys)
            { 
                //aggiungere l'elemento minore timestamp e poi reinserie la serie
                foreach (PhoneInfo ph in macList)
                {
                    if (ph.MacAddr == mac)
                    {
                        if (ph.Timestamp > animationCurrTimestamp && ph.Timestamp <= animationTimestampEnd && ph.Timestamp < tmp)
                        {
                            p = ph;
                            tmp = ph.Timestamp;
                            found = true;
                        }
                    }
                }
            }

            if (found)
            {
                animationCurrTimestamp = tmp;
                _movement.Series[macToIndex[p.MacAddr]].Values.Add(p);
                if (p.Position.Ascissa < minX)
                    minX = p.Position.Ascissa;
                if (p.Position.Ascissa > maxX)
                    maxX = p.Position.Ascissa;
                if (p.Position.Ordinata < minY)
                    minY = p.Position.Ordinata;
                if (p.Position.Ordinata > maxY)
                    maxY = p.Position.Ordinata;
                _movement.AxisX[0].MinValue = minX - 0.1;
                _movement.AxisX[0].MaxValue = maxX + 0.1;
                _movement.AxisY[0].MinValue = minY - 0.1;
                _movement.AxisY[0].MaxValue = maxY + 0.1;
            }    
            
        }

        public void PreviousPoint() {
            long tmp = animationStartTimestamp;
            animationCurrTimestamp = animationStartTimestamp;
            PhoneInfo p = null;
            bool found = false;
            long[] timestamps = new long[2]{0, 0};

            foreach (string mac in macToIndex.Keys)
            {
                foreach (PhoneInfo ph in _movement.Series[macToIndex[mac]].Values)
                {  
                    if (ph.MacAddr == mac)
                    {
                        if (ph.Timestamp > timestamps[0])
                        {
                            if (ph.Timestamp > timestamps[1])
                            {
                                timestamps[0] = timestamps[1];
                                timestamps[1] = ph.Timestamp;
                            }
                            else {
                                timestamps[0] = ph.Timestamp;
                            }
                        }

                        if (ph.Timestamp < animationTimestampEnd && ph.Timestamp >= tmp)
                        {
                            p = ph;
                            tmp = ph.Timestamp;
                            found = true;
                        }
                    }
                }
            }

            if (found)
            {
                animationCurrTimestamp = timestamps[0];
                _movement.Series[macToIndex[p.MacAddr]].Values.Remove(p);
                if (p.Position.Ascissa<minX)
                    minX = p.Position.Ascissa;
                if (p.Position.Ascissa > maxX)
                    maxX = p.Position.Ascissa;
                if (p.Position.Ordinata<minY)
                    minY = p.Position.Ordinata;
                if (p.Position.Ordinata > maxY)
                    maxY = p.Position.Ordinata;
                _movement.AxisX[0].MinValue = minX - 0.1;
                _movement.AxisX[0].MaxValue = maxX + 0.1;
                _movement.AxisY[0].MinValue = minY - 0.1;
                _movement.AxisY[0].MaxValue = maxY + 0.1;
            }

        }
        public void RemoveSeries(string rmac) {
            long tmp = animationStartTimestamp;//trovo il massimo timestamp
            bool found = false;
            int count = _movement.Series.Count - 1;
            

            for (int i=0; i<_movement.Series.Count;i++)
            {
                if (i == count)
                    break;

                if (macToIndex[rmac] == i && found == false)
                    found = true;
                    
                if (found)
                    _movement.Series[i].Values = _movement.Series[i+1].Values;
              

            }

            for (int i = 0; i < _movement.Series.Count; i++)
                if (macToIndex[macToIndex.ElementAt(i).Key] > macToIndex[rmac])
                    macToIndex[macToIndex.ElementAt(i).Key]--;


                macToIndex.Remove(rmac);
            _movement.Series.RemoveAt(_movement.Series.Count-1);

            foreach (string mac in macToIndex.Keys)
            {
                foreach (PhoneInfo ph in _movement.Series[macToIndex[mac]].Values)
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

        public void CreateHeatChart(int start, int end) {
            DateTime dStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(start);

            heatPoints.Clear();
            Application.Current.Dispatcher.Invoke(() =>
            {
                _temporalDistribution.AxisX.Clear();
                _temporalDistribution.AxisY.Clear();
            });

            List<PhoneInfo> mostFreq = (start == -1 && end == -1) ? new List<PhoneInfo>() : _statCalc.MostFrequentPhones(5, start, end);
            map.Clear();

            if(start == -1 && end == -1)
            {
                return;
            }

            if (mostFreq == null)
            {
                System.Windows.MessageBox.Show("Error with the database connection. Please check that the database is online.", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if(mostFreq.Count == 0)
            {
                System.Windows.MessageBox.Show("No data found within the given time interval.",
                    "No data found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            foreach (PhoneInfo ph in mostFreq)
            {
                if (map.ContainsKey(ph.MacAddr)) {
                    map[ph.MacAddr].Add(ph.Timestamp);
                }
                else {
                    map.Add(ph.MacAddr, new List<int> { ph.Timestamp });
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Axis xAxis = new Axis();
                Axis yAxis = new Axis();
                yAxis.Labels = map.Keys.ToList().Select<String, String>((s) => Utils.FormatMACAddr(s)).ToList();
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
                List<string> xAxisLabels = new List<string>();

                double size = (double)end - start;
                if (size > 0)
                {
                    if (TimeSpan.FromSeconds(size).TotalHours < 3)//10 minuti
                    {

                        int nIntervals = (int)(size / TimeSpan.FromMinutes(10).TotalSeconds) + 1;
                        SetIntervals(10, start, end);
                        xAxis.Title = "Time[hh:mm]";
                        xAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddMinutes(i * 10);
                            DateTime fin = dStart.AddMinutes(i * 10 + 10);
                            xAxisLabels.Add(init.Hour.ToString("00") + ":" +
                                            init.Minute.ToString("00") + " - " +
                                            fin.Hour.ToString("00") + ":" +
                                            fin.Minute.ToString("00"));
                        }
                        xAxis.Labels = xAxisLabels;
                    }
                    else if (TimeSpan.FromSeconds(size).TotalHours < 12)//ora
                    {
                        SetIntervals(60, start, end);
                        int nIntervals = (int)((end - start) / TimeSpan.FromHours(1).TotalSeconds) + 1;
                        xAxis.Title = "Time[hh:mm]";
                        xAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddHours(i);
                            DateTime fin = dStart.AddHours(i + 1);
                            xAxisLabels.Add(init.Hour.ToString("00") + ":" +
                                            init.Minute.ToString("00") + " - " +
                                            fin.Hour.ToString("00") + ":" +
                                            fin.Minute.ToString("00"));
                        }
                        xAxis.Labels = xAxisLabels;

                    }
                    else if (TimeSpan.FromSeconds(size).TotalDays < 3)//ore
                    {
                        SetIntervals(300, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromHours(5).TotalSeconds) + 1;
                        xAxis.Title = "Time[dd/mm hh:mm]";
                        xAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddHours(i * 5);
                            DateTime fin = dStart.AddHours(i * 5 + 5);
                            xAxisLabels.Add(init.Day.ToString("00") + "/" +
                                            init.Month.ToString("00") + "  " +
                                            init.Hour.ToString("00") + ":" +
                                            init.Minute.ToString("00") + " - " +
                                            fin.Day.ToString("00") + "/" +
                                            fin.Month.ToString("00") + "  " +
                                            fin.Hour.ToString("00") + ":" +
                                            fin.Minute.ToString("00"));
                        }
                        xAxis.Labels = xAxisLabels;
                    }
                    else if (TimeSpan.FromSeconds(size).TotalDays < 21)//giorni
                    {
                        SetIntervals(1440, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromDays(1).TotalSeconds) + 1;
                        xAxis.Title = "Time[dd/mm/yyyy]";
                        xAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddDays(i);
                            DateTime fin = dStart.AddDays(i + 1);
                            xAxisLabels.Add(init.Day.ToString("00") + "/" +
                                            init.Month.ToString("00") + "/" +
                                            init.Year.ToString("00") + " - " +
                                            fin.Day.ToString("00") + "/" +
                                            fin.Month.ToString("00") + "/" +
                                            fin.Year.ToString("00"));
                        }
                        xAxis.Labels = xAxisLabels;

                    }
                    else if (TimeSpan.FromSeconds(size).TotalDays < 90)//settimana
                    {
                        SetIntervals(10080, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromDays(7).TotalSeconds) + 1;
                        xAxis.Title = "Time[dd/mm/yyyy]";
                        xAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddDays(i * 7);
                            DateTime fin = dStart.AddDays(i * 7 + 7);
                            xAxisLabels.Add(init.Day.ToString("00") + "/" +
                                            init.Month.ToString("00") + "/" +
                                            init.Year.ToString("00") + " - " +
                                            fin.Day.ToString("00") + "/" +
                                            fin.Month.ToString("00") + "/" +
                                            fin.Year.ToString("00"));
                        }
                        xAxis.Labels = xAxisLabels;
                    }
                    else if (TimeSpan.FromSeconds(size).TotalDays < 800)//mese
                    {
                        SetIntervals(43200, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromDays(30).TotalSeconds) + 1;
                        xAxis.Title = "Time[dd/mm/yyyy]";
                        xAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddDays(i * 30);
                            DateTime fin = dStart.AddDays(i * 30 + 30);
                            xAxisLabels.Add(init.Day.ToString("00") + "/" +
                                            init.Month.ToString("00") + "/" +
                                            init.Year.ToString("00") + " - " +
                                            fin.Day.ToString("00") + "/" +
                                            fin.Month.ToString("00") + "/" +
                                            fin.Year.ToString("00"));
                        }
                        xAxis.Labels = xAxisLabels;
                    }
                    else//anno
                    {
                        SetIntervals(525600, start, end);

                        int nIntervals = (int)((end - start) / TimeSpan.FromDays(365).TotalSeconds) + 1;
                        xAxis.Title = "Time[mm/yyyy]";
                        xAxis.FontSize = 15;

                        for (int i = 0; i < nIntervals; i++)
                        {
                            DateTime init = dStart.AddDays(i * 365);
                            DateTime fin = dStart.AddDays(i * 365 + 365);
                            xAxisLabels.Add(init.Month.ToString("00") + "/" +
                                            init.Year.ToString("00") + " - " +
                                            fin.Month.ToString("00") + "/" +
                                            fin.Year.ToString("00"));
                        }
                        xAxis.Labels = xAxisLabels;
                    }
                }
            });
        }

        private void SetIntervals(int minutes, int start, int end) {
            int nIntervals = (int)((end - start) / TimeSpan.FromMinutes(minutes).TotalSeconds) + 1;
            int[] intervals = new int[nIntervals];
            
            int n = 0; //indice colonna (mac)
            foreach (string s in map.Keys)
            {

                for (int i = 0; i < nIntervals; i++)
                    intervals[i] = 0;
                foreach (int t in map[s])
                {
                    intervals[(int)((t - start) / TimeSpan.FromMinutes(minutes).TotalSeconds)]++; //posizione intervallo
                }
                for (int i = 0; i < nIntervals; i++)
                {
                    heatPoints.Add(new HeatPoint(i, n, intervals[i]));
                }

                n++;

            }
            _temporalDistribution.Series[0].Values = heatPoints;
        }

    }
}
