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
        private EspServer _esp_server;
        private StatCalc _statCalc;
        private Thread t;
        private CartesianChart scatterChart;
        private CartesianChart fiveMinutesChart;
        private ChartValues<Scheda> boardsPos;
        private ChartValues<DatiDispositivo> phonePos;
        private ChartValues<DatiDispositivo> hiddenPhonePos;
        private ChartValues<DatiDispositivo> selectedPhonePos;
        private ChartValues<double> fiveMinutesPhone;
        private ChartValues<double> fiveMinutesHiddenPhone;
        private ChartValues<double> fiveMinutesVisiblePhone;
        private CancellationTokenSource _cts;
        private CancellationToken _ct;
        private Action _errorAction;
        private string selectedMAC="";
        List<DatiDispositivo> dati_Dispositivi;

        public List<DatiDispositivo> DatiDispositivi { get { return dati_Dispositivi; } }

        public DataReceiver(MainWindow window,int nBoards, EspServer esp_client, DBConnect dbC, CartesianChart scatterChart, CartesianChart fiveMinutesChart, Action errorAction)
        {
            _window = window;
            _nBoards = nBoards;
            _esp_server = esp_client;
            _statCalc = new StatCalc(dbC);
            _errorAction = errorAction;
            _cts = new CancellationTokenSource();
            _ct = _cts.Token;
            this.scatterChart = scatterChart;
            this.fiveMinutesChart = fiveMinutesChart;
            boardsPos = new ChartValues<Scheda>();
            hiddenPhonePos = new ChartValues<DatiDispositivo>();
            phonePos = new ChartValues<DatiDispositivo>();
            selectedPhonePos = new ChartValues<DatiDispositivo>();
            scatterChart.Series.Add(new ScatterSeries(boardsPos)
            {
                Title = "Schede",
                MinPointShapeDiameter = 15,
                PointGeometry = DefaultGeometries.Triangle,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = Utils.green

            }) ;
            scatterChart.Series.Add(new ScatterSeries(phonePos)
            {
                Title = "Dispositivi Visibili",
                MinPointShapeDiameter = 15,
                PointGeometry = DefaultGeometries.Diamond,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = Utils.orange,
                
            });
            scatterChart.Series.Add(new ScatterSeries(hiddenPhonePos)
            {
                Title = "Dispositivi Nascosti",
                MinPointShapeDiameter = 15,
                PointGeometry = DefaultGeometries.Diamond,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = Utils.purple
            });

            scatterChart.Series.Add(new ScatterSeries(selectedPhonePos)
            {
                Title = "MAC selezionato",
                MinPointShapeDiameter = 20,
                PointGeometry = DefaultGeometries.Cross,                
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = Utils.red
            });

            scatterChart.AxisX.Add(new Axis
            {
                MinValue = -2,
                MaxValue = 2,
                LabelFormatter = x => Math.Round(x, 2).ToString(),
                Title = "posizione [m]"
            });

            scatterChart.AxisY.Add(new Axis
            {
                MinValue = -2,
                MaxValue = 2,
                LabelFormatter = x => Math.Round(x, 2).ToString(),
                Title = "posizione [m]"
            });

            InitializeFiveMinuteChart();

            Axis xAxis = new Axis();
            Axis yAxis = new Axis();

            xAxis.Separator.Step = 1;
            xAxis.FontSize = 15;
            xAxis.Title = "Tempo [minuti]";

            yAxis.FontSize = 15;
            yAxis.Title = "Numero dispositivi";
            yAxis.Separator.Step = 1;
            xAxis.MinValue = 0;
            xAxis.MaxValue = 5;
            yAxis.MinValue = 0;
            yAxis.MaxValue = 10;

            fiveMinutesChart.AxisX.Add(xAxis);
            fiveMinutesChart.AxisY.Add(yAxis);

            scatterChart.Series[0].Configuration = Mappers.Xy<Scheda>().X(b => b.Punto.Ascissa).Y(b => b.Punto.Ordinata);
            scatterChart.Series[0].Values = boardsPos;
            scatterChart.Series[1].Configuration = Mappers.Xy<DatiDispositivo>().X(b => b.Posizione.Ascissa).Y(b => b.Posizione.Ordinata);
            scatterChart.Series[1].Values = phonePos;
            scatterChart.Series[2].Configuration = Mappers.Xy<DatiDispositivo>().X(b => b.Posizione.Ascissa).Y(b => b.Posizione.Ordinata);
            scatterChart.Series[2].Values = hiddenPhonePos;
            scatterChart.Series[3].Configuration = Mappers.Xy<DatiDispositivo>().X(b => b.Posizione.Ascissa).Y(b => b.Posizione.Ordinata);
            scatterChart.Series[3].Values = selectedPhonePos;

            scatterChart.Series[0].LabelPoint = point => string.Format("ID scheda: {0}\n X:{1} Y:{2}",
                                                            ((Scheda)point.Instance).ID_scheda,
                                                            Math.Round(((Scheda)point.Instance).Punto.Ascissa, 2),
                                                            Math.Round(((Scheda)point.Instance).Punto.Ordinata, 2));
            scatterChart.Series[1].LabelPoint = point => string.Format(" MAC: {0}\n Timestamp:{1} \n X:{2}  Y:{3}",
                                                                Utils.Formatta_MAC_Address(((DatiDispositivo)point.Instance).MAC_Address),
                                                                Utils.UnixTimestampToDateTime(((DatiDispositivo)point.Instance).Timestamp),
                                                                Math.Round(((DatiDispositivo)point.Instance).Posizione.Ascissa, 2),
                                                                Math.Round(((DatiDispositivo)point.Instance).Posizione.Ordinata, 2));
            scatterChart.Series[2].LabelPoint = point => string.Format(" MAC: {0}\n Timestamp:{1} \n X:{2}  Y:{3}",
                                                                Utils.Formatta_MAC_Address(((DatiDispositivo)point.Instance).MAC_Address),
                                                                Utils.UnixTimestampToDateTime(((DatiDispositivo)point.Instance).Timestamp),
                                                                Math.Round(((DatiDispositivo)point.Instance).Posizione.Ascissa, 2),
                                                                Math.Round(((DatiDispositivo)point.Instance).Posizione.Ordinata, 2));
            scatterChart.Series[3].LabelPoint = point => string.Format(" MAC: {0}\n Timestamp:{1} \n X:{2}  Y:{3}",
                                                                Utils.Formatta_MAC_Address(((DatiDispositivo)point.Instance).MAC_Address),
                                                                Utils.UnixTimestampToDateTime(((DatiDispositivo)point.Instance).Timestamp),
                                                                Math.Round(((DatiDispositivo)point.Instance).Posizione.Ascissa, 2),
                                                                Math.Round(((DatiDispositivo)point.Instance).Posizione.Ordinata, 2));


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
                    _esp_server.waitAllBoardsData();
                    System.Threading.Thread.Sleep(5000);

                    dati_Dispositivi = _statCalc.GetLastMinutePositions(_nBoards, 1);
                    List<Scheda> boards = _statCalc.GetBoardsPosition();

                    if (dati_Dispositivi == null || boards == null)
                    {
                        //TODO stampare messaggio di errore tramite testo rosso nella GUI
                        System.Windows.MessageBox.Show("Error when connecting to database. Please check that the database is online.", "Database error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        continue;
                    }
                    //TODO rimuovere messaggio di errore tramite testo rosso

                    scatterChart.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        DrawOneMinutesChart(boards, dati_Dispositivi);
                    }));

                    fiveMinutesChart.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        DrawFiveMinutesChart(dati_Dispositivi);
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

        private void DrawOneMinutesChart(List<Scheda> schede, List<DatiDispositivo> dati_Dispositivi)
        {
            double minX, minY, maxX, maxY;

            int correlatedPhones = 0;
            int totalHiddenPhones;
            double error;

            List<DatiDispositivo> hiddenMacs = new List<DatiDispositivo>();
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
            foreach (DatiDispositivo p in dati_Dispositivi)
            {
                if (p.Posizione.Ascissa > maxX)
                    maxX = p.Posizione.Ascissa;
                if (p.Posizione.Ascissa < minX)
                    minX = p.Posizione.Ascissa;

                if (p.Posizione.Ordinata > maxY)
                    maxY = p.Posizione.Ordinata;
                if (p.Posizione.Ordinata < minY)
                    minY = p.Posizione.Ordinata;

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

            foreach(DatiDispositivo phone in hiddenMacs)
            {
                List<String> res = _statCalc.Connection.CountHiddenPhones(phone, 0.6);
  
                    foreach (String mac in res)
                        if (!countedMacs.Contains(mac)) 
                            countedMacs.Add(mac);
            }
            correlatedPhones = countedMacs.Count;
            totalHiddenPhones = hiddenMacs.Count;
            error =(double) 1 -  correlatedPhones / totalHiddenPhones;

            _window.UpdateTextBlocks(totalHiddenPhones,correlatedPhones,error);

            if (selectedMAC != "")
                SearchMac(selectedMAC);

            scatterChart.AxisX[0].MinValue = minX - 1;
            scatterChart.AxisX[0].MaxValue = maxX + 1;
            scatterChart.AxisY[0].MinValue = minY - 1;
            scatterChart.AxisY[0].MaxValue = maxY + 1;

        }

        private void DrawFiveMinutesChart(List<DatiDispositivo> dati_Dispositivi)
        {

            var phoneTuple = SplitList(dati_Dispositivi);
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

        private Tuple<List<DatiDispositivo>, List<DatiDispositivo>> SplitList(List<DatiDispositivo> dati_Dispositivi)
        {
            List<DatiDispositivo> visiblePhones = new List<DatiDispositivo>();
            List<DatiDispositivo> hiddenPhones = new List<DatiDispositivo>();

            foreach(DatiDispositivo pi in dati_Dispositivi)
            {
                if (!pi.Global)
                {
                    visiblePhones.Add(pi);
                }
                else
                {
                    hiddenPhones.Add(pi);                }
            }

            return new Tuple<List<DatiDispositivo>, List<DatiDispositivo>>(visiblePhones, hiddenPhones);
        }

        
        private void InitializeFiveMinuteChart() {

            fiveMinutesPhone = new ChartValues<double>();
            fiveMinutesHiddenPhone = new ChartValues<double>();
            fiveMinutesVisiblePhone = new ChartValues<double>();

            fiveMinutesChart.Series.Add(new LineSeries(fiveMinutesPhone)
            {
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 20,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = Utils.green,
                Title = "Dispositivi"
            });

            fiveMinutesChart.Series.Add(new LineSeries(fiveMinutesHiddenPhone)
            {
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 20,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = Utils.purple,
                Title= "Dispositivi Nascosti"
   
            });

            fiveMinutesChart.Series.Add(new LineSeries(fiveMinutesVisiblePhone)
            {
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 20,
                StrokeThickness = 4,
                Fill = System.Windows.Media.Brushes.Transparent,
                Stroke = Utils.orange,
                Title = "Dispositivi Visibili"
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
                    foreach (DatiDispositivo p in scatterChart.Series[2].Values)
                    {
                        if (p.MAC_Address.CompareTo(cleanSelectedMAC) == 0)
                        {
                            hiddenPhonePos.Remove(p);
                            selectedPhonePos.Add(p);
                        }
                    }
                }
                else
                {
                    //visiblePhone
                    foreach (DatiDispositivo p in scatterChart.Series[1].Values)
                    {
                        if (p.MAC_Address.CompareTo(cleanSelectedMAC) == 0)
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
                DatiDispositivo p = selectedPhonePos.First<DatiDispositivo>();
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
