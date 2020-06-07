using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using PDSClient.ConnectionManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace PDSClient.StatModule
{
    public class DataReceiver
    {
        private MainWindow _window;
        private int _nBoards;
        private IPositionSender _positionSender;
        private StatCalc _statCalc;
        private Thread t;
        private CartesianChart scatterChart;
        private CartesianChart fiveMinutesChart;
        private ChartValues<Scheda> boardsPos;
        private ChartValues<PhoneInfo> phonePos;
        private ChartValues<PhoneInfo> hiddenPhonePos;
        private ChartValues<PhoneInfo> selectedPhonePos;
        private ChartValues<double> fiveMinutesPhone;
        private ChartValues<double> fiveMinutesHiddenPhone;
        private ChartValues<double> fiveMinutesVisiblePhone;
        private CancellationTokenSource _cts;
        private CancellationToken _ct;
        private Action _errorAction;
        private string selectedMAC="";
        List<PhoneInfo> phoneInfos;

        public List<PhoneInfo> PhoneInfos { get { return phoneInfos; } }

        public DataReceiver(MainWindow window,int nBoards, IPositionSender positionSender, DBConnect dbC, CartesianChart scatterChart, CartesianChart fiveMinutesChart, Action errorAction)
        {
            _window = window;
            _nBoards = nBoards;
            _positionSender = positionSender;
            _statCalc = new StatCalc(dbC);
            _errorAction = errorAction;
            _cts = new CancellationTokenSource();
            _ct = _cts.Token;
            this.scatterChart = scatterChart;
            this.fiveMinutesChart = fiveMinutesChart;
            boardsPos = new ChartValues<Scheda>();
            hiddenPhonePos = new ChartValues<PhoneInfo>();
            phonePos = new ChartValues<PhoneInfo>();
            selectedPhonePos = new ChartValues<PhoneInfo>();
            scatterChart.Series.Add(new ScatterSeries(boardsPos)
            {
                Title = "Board",
                PointGeometry = DefaultGeometries.Triangle,
                Fill = System.Windows.Media.Brushes.Green

            }) ;
            scatterChart.Series.Add(new ScatterSeries(phonePos)
            {
                Title = "Visible Devices",
                PointGeometry = DefaultGeometries.Diamond,
                Fill = System.Windows.Media.Brushes.OrangeRed
            });
            scatterChart.Series.Add(new ScatterSeries(hiddenPhonePos)
            {
                Title = "Hidden Devices"
            });

            scatterChart.Series.Add(new ScatterSeries(selectedPhonePos)
            {
                Title = "Selected MAC",
                Fill =System.Windows.Media.Brushes.Green
            });

            scatterChart.AxisX.Add(new Axis
            {
                MinValue = -2,
                MaxValue = 2,
                LabelFormatter = x => Math.Round(x, 2).ToString()
            });

            scatterChart.AxisY.Add(new Axis
            {
                MinValue = -2,
                MaxValue = 2,
                LabelFormatter = x => Math.Round(x, 2).ToString()
            });

            InitializeFiveMinuteChart();

            Axis xAxis = new Axis();
            Axis yAxis = new Axis();

            xAxis.Separator.Step = 1;
            xAxis.FontSize = 15;
            xAxis.Title = "Time [minutes]";

            yAxis.FontSize = 15;
            yAxis.Title = "Device number";
            yAxis.Separator.Step = 2;
            xAxis.MinValue = 0;
            xAxis.MaxValue = 5;
            yAxis.MinValue = 0;
            yAxis.MaxValue = 10;

            fiveMinutesChart.AxisX.Add(xAxis);
            fiveMinutesChart.AxisY.Add(yAxis);

            scatterChart.Series[0].Configuration = Mappers.Xy<Scheda>().X(b => b.Punto.Ascissa).Y(b => b.Punto.Ordinata);
            scatterChart.Series[0].Values = boardsPos;
            scatterChart.Series[1].Configuration = Mappers.Xy<PhoneInfo>().X(b => b.Position.Ascissa).Y(b => b.Position.Ordinata);
            scatterChart.Series[1].Values = phonePos;
            scatterChart.Series[2].Configuration = Mappers.Xy<PhoneInfo>().X(b => b.Position.Ascissa).Y(b => b.Position.Ordinata);
            scatterChart.Series[2].Values = hiddenPhonePos;
            scatterChart.Series[3].Configuration = Mappers.Xy<PhoneInfo>().X(b => b.Position.Ascissa).Y(b => b.Position.Ordinata);
            scatterChart.Series[3].Values = selectedPhonePos;

            scatterChart.Series[0].LabelPoint = point => string.Format("IdBoard: {0}\n X:{1} Y:{2}",
                                                            ((Scheda)point.Instance).ID_scheda,
                                                            Math.Round(((Scheda)point.Instance).Punto.Ascissa, 2),
                                                            Math.Round(((Scheda)point.Instance).Punto.Ordinata, 2));
            scatterChart.Series[1].LabelPoint = point => string.Format(" MAC: {0}\n Timestamp:{1} \n X:{2}  Y:{3}",
                                                                Utils.FormatMACAddr(((PhoneInfo)point.Instance).MacAddr),
                                                                Utils.UnixTimestampToDateTime(((PhoneInfo)point.Instance).Timestamp),
                                                                Math.Round(((PhoneInfo)point.Instance).Position.Ascissa, 2),
                                                                Math.Round(((PhoneInfo)point.Instance).Position.Ordinata, 2));
            scatterChart.Series[2].LabelPoint = point => string.Format(" MAC: {0}\n Timestamp:{1} \n X:{2}  Y:{3}",
                                                                Utils.FormatMACAddr(((PhoneInfo)point.Instance).MacAddr),
                                                                Utils.UnixTimestampToDateTime(((PhoneInfo)point.Instance).Timestamp),
                                                                Math.Round(((PhoneInfo)point.Instance).Position.Ascissa, 2),
                                                                Math.Round(((PhoneInfo)point.Instance).Position.Ordinata, 2));
            scatterChart.Series[3].LabelPoint = point => string.Format(" MAC: {0}\n Timestamp:{1} \n X:{2}  Y:{3}",
                                                                Utils.FormatMACAddr(((PhoneInfo)point.Instance).MacAddr),
                                                                Utils.UnixTimestampToDateTime(((PhoneInfo)point.Instance).Timestamp),
                                                                Math.Round(((PhoneInfo)point.Instance).Position.Ascissa, 2),
                                                                Math.Round(((PhoneInfo)point.Instance).Position.Ordinata, 2));


            t = new Thread(new ThreadStart(this.ReceiverFunc));
            t.Start();
        }

        private void ReceiverFunc()
        {
            try
            {
                while (true)
                {
                    if (_ct.IsCancellationRequested)
                    {
                        System.Diagnostics.Debug.WriteLine("Cancellation token set in DataReceiver... terminating loop");
                        break;
                    }
                    _positionSender.WaitAll();
                    System.Threading.Thread.Sleep(5000);

                    phoneInfos = _statCalc.GetLastMinutePositions(_nBoards, 1);
                    List<Scheda> boards = _statCalc.GetBoardsPosition();

                    if (phoneInfos == null || boards == null)
                    {
                        //TODO stampare messaggio di errore tramite testo rosso nella GUI
                        System.Windows.MessageBox.Show("Error when connecting to database. Please check that the database is online.", "Database error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        continue;
                    }
                    //TODO rimuovere messaggio di errore tramite testo rosso

                    scatterChart.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        DrawOneMinutesChart(boards, phoneInfos);
                    }));

                    fiveMinutesChart.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        DrawFiveMinutesChart(phoneInfos);
                    }));
                }
            }
            catch(ThreadInterruptedException e)
            {
                System.Diagnostics.Debug.WriteLine("ThreadInterruptedException catched for DataReceiver... terminating");
                return;
            }
            catch(ThreadAbortException e)
            {
                System.Diagnostics.Debug.WriteLine("ThreadAbortException catched for DataReceiver... terminating");
                return;
            }
            catch(KeyNotFoundException e)
            {
                System.Diagnostics.Debug.WriteLine("IdBoard(s) are wrong");
                System.Windows.Application.Current.Dispatcher.BeginInvoke(_errorAction, DispatcherPriority.Send);
                return;
            }
        }

        private void DrawOneMinutesChart(List<Scheda> schede, List<PhoneInfo> phoneInfos)
        {
            double minX, minY, maxX, maxY;

            int correlatedPhones = 0;
            int totalHiddenPhones;
            double error;

            List<PhoneInfo> hiddenMacs = new List<PhoneInfo>();
            HashSet<String> countedMacs = new HashSet<String>();

            minX = maxX = schede.First().Punto.Ascissa;
            minY = maxY = schede.First().Punto.Ordinata;

            boardsPos.Clear();
            foreach (Scheda scheda in schede)
            {
                if (minX > scheda.Punto.Ascissa)
                    minX = scheda.Punto.Ascissa;
                if (maxX < scheda.Punto.Ascissa)
                    maxX = scheda.Punto.Ascissa;
                if (minY > scheda.Punto.Ordinata)
                    minY = scheda.Punto.Ordinata;
                if (maxY < scheda.Punto.Ordinata)
                    maxY = scheda.Punto.Ordinata;

                boardsPos.Add(scheda);
            }
            phonePos.Clear();
            hiddenPhonePos.Clear();
            selectedPhonePos.Clear();
            foreach (PhoneInfo p in phoneInfos)
            {
                if (p.Position.Ascissa > maxX)
                    maxX = p.Position.Ascissa;
                if (p.Position.Ascissa < minX)
                    minX = p.Position.Ascissa;

                if (p.Position.Ordinata > maxY)
                    maxY = p.Position.Ordinata;
                if (p.Position.Ordinata < minY)
                    minY = p.Position.Ordinata;

                if (p.Global)
                {
                    hiddenPhonePos.Add(p);
                    hiddenMacs.Add(p);
                }
                else
                {
                    phonePos.Add(p);
                }
            }

            foreach(PhoneInfo phone in hiddenMacs)
            {
                if(!countedMacs.Contains(phone.MacAddr))
                {
                    List<String> res = _statCalc.Connection.CountHiddenPhones(phone, 0.6);
                    //correlatedPhones += res.Count - 1;
                    foreach (String mac in res)
                        countedMacs.Add(mac);
                }
            }
            correlatedPhones = countedMacs.Count;
            totalHiddenPhones = hiddenMacs.Count;
            error = (double) correlatedPhones / totalHiddenPhones;

            _window.UpdateTextBlocks(totalHiddenPhones,correlatedPhones,error);

            if (selectedMAC != "")
                SearchMac(selectedMAC);

            scatterChart.AxisX[0].MinValue = minX - 1;
            scatterChart.AxisX[0].MaxValue = maxX + 1;
            scatterChart.AxisY[0].MinValue = minY - 1;
            scatterChart.AxisY[0].MaxValue = maxY + 1;

        }

        private void DrawFiveMinutesChart(List<PhoneInfo> phoneInfos)
        {

            var phoneTuple = SplitList(phoneInfos);
            var visiblePhoneInfos = phoneTuple.Item1;
            var hiddenPhoneInfos = phoneTuple.Item2;

            int hiddenPhones = _statCalc.FiveMinuteHiddenPhones(hiddenPhoneInfos);
            int visiblePhones = _statCalc.FiveMinuteVisiblePhones(visiblePhoneInfos);
            int total = hiddenPhones + visiblePhones;

            fiveMinutesHiddenPhone.Add(hiddenPhones);
            fiveMinutesVisiblePhone.Add(visiblePhones);
            fiveMinutesPhone.Add(total);

            fiveMinutesChart.Series[0].Values = fiveMinutesPhone;
            fiveMinutesChart.Series[1].Values = fiveMinutesHiddenPhone;
            fiveMinutesChart.Series[2].Values = fiveMinutesVisiblePhone;

            Axis xAxis = fiveMinutesChart.AxisX[0];
            Axis yAxis = fiveMinutesChart.AxisY[0];
            
            if(fiveMinutesPhone.Count > 5)
            {
                xAxis.MaxValue = fiveMinutesPhone.Count;
                xAxis.MinValue = fiveMinutesPhone.Count - 5;
            }
            if (total > yAxis.MaxValue)
            {
                yAxis.MaxValue = total + 1;
            }
        }

        private Tuple<List<PhoneInfo>, List<PhoneInfo>> SplitList(List<PhoneInfo> phoneInfos)
        {
            List<PhoneInfo> visiblePhones = new List<PhoneInfo>();
            List<PhoneInfo> hiddenPhones = new List<PhoneInfo>();

            foreach(PhoneInfo pi in phoneInfos)
            {
                if (!pi.Global)
                {
                    visiblePhones.Add(pi);
                }
                else
                {
                    hiddenPhones.Add(pi);                }
            }

            return new Tuple<List<PhoneInfo>, List<PhoneInfo>>(visiblePhones, hiddenPhones);
        }

        
        private void InitializeFiveMinuteChart() {

            fiveMinutesPhone = new ChartValues<double>();
            fiveMinutesHiddenPhone = new ChartValues<double>();
            fiveMinutesVisiblePhone = new ChartValues<double>();

            fiveMinutesChart.Series.Add(new LineSeries(fiveMinutesPhone)
            {
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 20,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = System.Windows.Media.Brushes.Black,
                Title = "Total Devices"
            });

            fiveMinutesChart.Series.Add(new LineSeries(fiveMinutesHiddenPhone)
            {
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 20,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = System.Windows.Media.Brushes.Blue,
                Title= "Hidden Devices"
   
            });

            fiveMinutesChart.Series.Add(new LineSeries(fiveMinutesVisiblePhone)
            {
                PointGeometry = DefaultGeometries.Diamond,
                PointGeometrySize = 20,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = System.Windows.Media.Brushes.Red,
                Title = "Visible Devices"
            });

        }

        public void SearchMac(string selected) {
            String cleanSelectedMAC = selected.Replace(":", "");
            if (selected != "")
            {
                selectedMAC = selected;

                if(Pacchetto.CntrlGlobal(selectedMAC))
                {
                    //hiddenPhone
                    foreach (PhoneInfo p in scatterChart.Series[2].Values)
                    {
                        if (p.MacAddr.CompareTo(cleanSelectedMAC) == 0)
                        {
                            hiddenPhonePos.Remove(p);
                            selectedPhonePos.Add(p);
                        }
                    }
                }
                else
                {
                    //visiblePhone
                    foreach (PhoneInfo p in scatterChart.Series[1].Values)
                    {
                        if (p.MacAddr.CompareTo(cleanSelectedMAC) == 0)
                        {
                            phonePos.Remove(p);
                            selectedPhonePos.Add(p);
                        }
                    }
                }
                
                
            }

        }

        public void RemoveSearch()
        {
            if (selectedPhonePos.Count == 1)
            {
                String cleanSelectedMAC = selectedMAC.Replace(":", "");
                PhoneInfo p = selectedPhonePos.First<PhoneInfo>();
                selectedPhonePos.Remove(p);

                if (Pacchetto.CntrlGlobal(selectedMAC))
                {
                    //hiddenPhone
                    hiddenPhonePos.Add(p);
                }
                else
                {
                    //visiblePhone
                    phonePos.Add(p);
                }
            }
        }


        public void Shutdown()
        {
            System.Diagnostics.Debug.WriteLine("Received Shutdown command on DataReceiver... interrupting the thread");
            if(t.IsAlive)
                t.Interrupt();
            _cts.Cancel();
            var result = t.Join(1000);
            if (!result)
                t.Abort();
            return;
        }
    }
}
