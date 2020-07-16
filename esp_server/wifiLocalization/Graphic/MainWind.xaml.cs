
using System.Windows.Controls;
using WifiLocalization.ConnectionManager;
using WifiLocalization.Utilities;
using WifiLocalization.ChartManager;
using WifiLocalization.Graphic;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WifiLocalization.Graphic
{
    /// <summary>
    /// Interaction logic for MainWind.xaml
    /// </summary>
    /// 
    public partial class MainWind 
    {

        private List<Scheda> boardsList { get; set; }
        public bool ConnectionErr { get; set; }
        public Mutex ConnectionErrMtx { get; set; }

        DBConnect DBConnection;
        chartsManager chartsManager;
        EspServer espServer;
        chartDataHandler chartDataHandler;

        public MainWind(DBConnect DBConnection, List<Scheda> boards)
        {
            Action connectionErrorAction;

            InitializeComponent();
            this.ConnectionErrMtx = new Mutex(false);
            this.ConnectionErr = false;

            connectionErrorAction = new Action(() =>
            {             
                this.ConnectionErrMtx.WaitOne();
                if (!this.ConnectionErr)
                    this.ConnectionErr = true;
                else
                {
                    this.ConnectionErrMtx.ReleaseMutex();
                    return;
                }
                this.ConnectionErrMtx.ReleaseMutex();
                System.Windows.MessageBox.Show("Errore inviando/ricevendo pacchetti dalla scheda. Controlla la connessione e le schede infine riavvia il sistema.",
                    "Alert",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                if (espServer != null)
                    espServer.stop();
                if (chartDataHandler != null)
                    chartDataHandler.Shutdown();

                Environment.Exit(Environment.ExitCode);
            });

            Action keyNotFoundAction = new Action(() =>
            {
                System.Windows.MessageBox.Show("Le schede impostate nella configurazione sono sbagliate. Riavvia il sistema con la lista di schede corretta.",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                if (espServer != null)
                    espServer.stop();
                if (chartDataHandler != null)
                    chartDataHandler.Shutdown();

                Environment.Exit(Environment.ExitCode);
            });

            boardsList = boards;
            this.DBConnection = DBConnection;

            espServer = new EspServer(boardsList.Count, DBConnection, connectionErrorAction, this);
            chartDataHandler = new chartDataHandler(this, boardsList.Count, espServer, DBConnection, scatterplot, fiveMinutes, keyNotFoundAction);
            chartsManager = new chartsManager(DBConnection, CheckListbox,movement,temporalDistribution);
            chartsManager.animationCurrTimestamp = (DateTime.Now.Ticks - 621355968000000000) / 10000000;
            boardCounter.Text = espServer.getBoardsConnected().ToString();
            DataContext = this;
        }

        private void Button_Close(object sender, System.Windows.RoutedEventArgs e)
        {
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                App.Current.Windows[intCounter].Close();

            if (espServer != null)
                espServer.stop();
            if (chartDataHandler != null)
                chartDataHandler.Shutdown();

            Environment.Exit(Environment.ExitCode);

        }

        private void Button_Maximize(object sender, System.Windows.RoutedEventArgs e)
        {
            if(this.WindowState==System.Windows.WindowState.Normal)
                this.WindowState = System.Windows.WindowState.Maximized;
            else
                this.WindowState = System.Windows.WindowState.Normal;
        }

        private void Button_minimize(object sender, System.Windows.RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        private void CheckBoxChecked(object sender, System.Windows.RoutedEventArgs e)
        {
           
            long endRange = chartsManager.animationCurrTimestamp;
            long startRange = chartsManager.animationStartTimestamp;
            var checkBox = e.OriginalSource as CheckBox;
            DatiDispositivo ph = checkBox?.DataContext as DatiDispositivo;

            if (ph != null)
            {
                chartsManager.AddSeries(ph.MAC_Address);
            }
        }

        private void CheckBoxUnchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            DatiDispositivo ph = checkBox?.DataContext as DatiDispositivo;

            if (ph != null)
            {
                chartsManager.RemoveSeries(ph.MAC_Address);
            }

        }
        
        //popola la list box con i mac di cui abbiamo le posizioni nell'intervallo definito 
        private void Search(object sender, System.Windows.RoutedEventArgs e)
        {
            int temporalRange;
            try
            {
                temporalRange = Int32.Parse(sliderText.Text);
            }
            catch (FormatException ex)
            {
                temporalRange = 0;
            }
            if (temporalRange < 1)
            {
                System.Windows.MessageBox.Show("Inserire un intervallo di tempo valido (maggiore di 0)", "Alert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                return;
            }
            movement.Series.Clear();
            long endRange = (DateTime.Now.Ticks - 621355968000000000) / 10000000;
            long startRange = endRange - (temporalRange * 60);

            //accoda la creazione della lista in un threadPool 
            Task.Run(() => chartsManager.CreateListBox(startRange, endRange));
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void StartDateInitialized(object sender, EventArgs e)
        {
            startDate.Value = DateTime.Now;
        }

        private void EndDateInitialized(object sender, EventArgs e)
        {
            endDate.Value = DateTime.Now;
        }

        //ricerca dei MAC più ricorrenti nell'intervallo temporale
        private void SearchRange(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sDate.Value != null && eDate.Value != null)
            {
                if (sDate.Value >= eDate.Value)
                {
                    System.Windows.MessageBox.Show("Date scelte non valide", "Alert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                    return;
                }
                DateTime _startDate = (DateTime)sDate.Value;
                DateTime _endDate = (DateTime)eDate.Value;
                temporalDistribution.Series.Clear();
                int start = (Int32)(_startDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                int end = (Int32)(_endDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                Task.Factory.StartNew(() => chartsManager.CreatePercentualChart(start, end), TaskCreationOptions.LongRunning);
            }

        }

        private void SearchMac(object sender, System.Windows.RoutedEventArgs e)
        {
            bool invalid = false;
            string errMsg = "";
            Button startButton = (Button) sender;
            Grid FindMACGrid = (Grid)startButton.Parent;
            Button stopButton = (Button)FindMACGrid.FindName("stopButton");
            Grid MACTextBoxGrid = (Grid)FindMACGrid.FindName("MACTextBoxGrid");
            TextBox MACTextBox = (TextBox)MACTextBoxGrid.FindName("MAC");
            String pattern = "^([0-9A-F]{2}[:-]){5}([0-9A-F]{2})$";
            Regex regex = new Regex(pattern);
            if (!regex.IsMatch(Utils.Formatta_MAC_Address(MAC.Text)))
            {
                invalid = true;
                errMsg = "indirizzo MAC non valido";
            }
            if (!invalid)
            {

                startButton.Visibility = System.Windows.Visibility.Hidden;
                stopButton.Visibility = System.Windows.Visibility.Visible;
                MACTextBox.IsReadOnly = true;
                chartDataHandler.SearchMac(MAC.Text);
            }
            else
                System.Windows.MessageBox.Show(errMsg, "Attention ", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
        }

        private void RemoveMac(object sender, System.Windows.RoutedEventArgs e)
        {
            Button  stopButton = (Button)sender;
            Grid FindMACGrid = (Grid)stopButton.Parent;
            Button startButton = (Button)FindMACGrid.FindName("startButton");
            Grid MACTextBoxGrid = (Grid)FindMACGrid.FindName("MACTextBoxGrid");
            TextBox MACTextBox = (TextBox)MACTextBoxGrid.FindName("MAC");

            stopButton.Visibility = System.Windows.Visibility.Hidden;
            startButton.Visibility = System.Windows.Visibility.Visible;
            MACTextBox.IsReadOnly = false;

            chartDataHandler.RemoveSearch();   
        }
        private void EDateInitialized(object sender, EventArgs e)
        {
            eDate.Value = DateTime.Now;
        }

        private void SDateInitialized(object sender, EventArgs e)
        {
            sDate.Value = DateTime.Now;
        }

        public void UpdateTextBlocks(int tot, int corr, double err)
        {
            total.Text = tot.ToString();
            correlated.Text = corr.ToString();
            error.Text = (err * 100).ToString("0.00", CultureInfo.InvariantCulture) + " %";
        }

        public void UpdateBoardStatusTextBlock (String[] boards_status, int boards_number)
        {
            int boards_counter = 0;
            String boards_online = "";
            for (int i=0; i< boards_number; i++)
            {
                boards_online += "Board" + (i+1) + ":" + boards_status[i] + " ";
                if (boards_status[i] != "Offline")
                    boards_counter++;
            }
            boardCounter.Text = boards_counter.ToString();
            boardsOnline.Text = boards_online;
        }

        //metodo per aprire il menù laterale
        private void openMenuBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            closeMenuBtn.Visibility = System.Windows.Visibility.Visible;
            openMenuBtn.Visibility = System.Windows.Visibility.Collapsed;
        }
        //metodo per chiudere il menù laterale
        private void closeMenuBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            openMenuBtn.Visibility = System.Windows.Visibility.Visible;
            closeMenuBtn.Visibility = System.Windows.Visibility.Collapsed;
        }
        //metodo per il bottone (all'interno del menù) della posizione in real time
        private void realTimeChartBtnUp(object sender, MouseButtonEventArgs e)
        {
            realTimePosChart.Visibility = System.Windows.Visibility.Visible;
            deviceNumberChart.Visibility = System.Windows.Visibility.Hidden;
            movementChart.Visibility = System.Windows.Visibility.Hidden;
            temporalDistrChart.Visibility = System.Windows.Visibility.Hidden;
        }
        //metodo per il bottone (all'interno del menù) numero di dispositivi
        private void deviceNumberChartBtnUp(object sender, MouseButtonEventArgs e)
        {
            realTimePosChart.Visibility = System.Windows.Visibility.Hidden;
            deviceNumberChart.Visibility = System.Windows.Visibility.Visible;
            movementChart.Visibility = System.Windows.Visibility.Hidden;
            temporalDistrChart.Visibility = System.Windows.Visibility.Hidden;
        }
        //metodo per il bottone (all'interno del menù) movimento
        private void movementChartBtnUp(object sender, MouseButtonEventArgs e)
        {
            realTimePosChart.Visibility = System.Windows.Visibility.Hidden;
            deviceNumberChart.Visibility = System.Windows.Visibility.Hidden;
            movementChart.Visibility = System.Windows.Visibility.Visible;
            temporalDistrChart.Visibility = System.Windows.Visibility.Hidden;
        }
        //metodo per il bottone (all'interno del menù) ricorrenze MAC
        private void MACDistributionChartBtnUp(object sender, MouseButtonEventArgs e)
        {
            realTimePosChart.Visibility = System.Windows.Visibility.Hidden;
            deviceNumberChart.Visibility = System.Windows.Visibility.Hidden;
            movementChart.Visibility = System.Windows.Visibility.Hidden;
            temporalDistrChart.Visibility = System.Windows.Visibility.Visible;
        }
    }

}