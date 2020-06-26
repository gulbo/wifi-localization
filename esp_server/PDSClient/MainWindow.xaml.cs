using PDSClient.ConnectionManager;
using System.Windows.Controls;
using PDSClient.StatModule;
using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;


namespace PDSClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow 
    {

        private List<Scheda> _boards { get; set; }
        public bool ConnectionError { get; set; }
        public Mutex ConnectionErrorMutex { get; set; }

        DBConnect DBConnection;
        StaticChart sc;
        EspServer espServer;
        DataReceiver dr;

        public MainWindow(DBConnect DBConnection, List<Scheda> boards)
        {
            InitializeComponent();

            this.ConnectionErrorMutex = new Mutex(false);
            this.ConnectionError = false;

            Action connectionErrorAction = new Action(() =>
            {
                this.ConnectionErrorMutex.WaitOne();
                if (!this.ConnectionError)
                    this.ConnectionError = true;
                else
                {
                    this.ConnectionErrorMutex.ReleaseMutex();
                    return;
                }
                this.ConnectionErrorMutex.ReleaseMutex();
                System.Windows.MessageBox.Show("Errore inviando/ricevendo pacchetti dalla scheda. Controlla la connessione e le schede infine riavvia il sistema.",
                    "Alert",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                if (espServer != null)
                    espServer.stop();
                if (dr != null)
                    dr.Shutdown();

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
                if (dr != null)
                    dr.Shutdown();

                Environment.Exit(Environment.ExitCode);
            });

            _boards = boards;
            this.DBConnection = DBConnection;

            espServer = new EspServer(_boards.Count, DBConnection, connectionErrorAction, this);
            dr = new DataReceiver(this,_boards.Count, espServer, DBConnection, scatterplot, fiveMinutes, keyNotFoundAction);
            sc = new StaticChart(DBConnection, CheckListbox,movement,temporalDistribution);
            sc.animationCurrTimestamp = (DateTime.Now.Ticks - 621355968000000000) / 10000000;
            boardCounter.Text = espServer.getBoardsConnected().ToString();
            DataContext = this;
        }

        private void Button_Close(object sender, System.Windows.RoutedEventArgs e)
        {
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                App.Current.Windows[intCounter].Close();

            if (espServer != null)
                espServer.stop();
            if (dr != null)
                dr.Shutdown();

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
           
            long endRange = sc.animationCurrTimestamp;
            long startRange = sc.animationStartTimestamp;
            var checkBox = e.OriginalSource as CheckBox;
            PhoneInfo ph = checkBox?.DataContext as PhoneInfo;

            if (ph != null)
            {               
                sc.AddSeries(ph.MacAddr);
            }
        }

        private void CheckBoxUnchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            var checkBox = e.OriginalSource as CheckBox;

            PhoneInfo ph = checkBox?.DataContext as PhoneInfo;

            if (ph != null)
            {
                sc.RemoveSeries(ph.MacAddr);
            }

        }
        
        //popola la list box con i mac di cui abbiamo le posizioni nell'intervallo definito 
        private void Search(object sender, System.Windows.RoutedEventArgs e)
        {
            int temporalRange = Int32.Parse(sliderText.Text);


            if (temporalRange < 1)
            {
                System.Windows.MessageBox.Show("Specificare un intervallo temporale maggiore di 0", "Alert", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                return;
            }
            long endRange = (DateTime.Now.Ticks - 621355968000000000) / 10000000;
            long startRange = endRange - (temporalRange * 60);

            //accoda la creazione della lista in un threadPool 
            Task.Run(() => sc.CreateListBox(startRange, endRange));
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
                int start = (Int32)(_startDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                int end = (Int32)(_endDate.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                Task.Factory.StartNew(() => sc.CreatePercentualChart(start, end), TaskCreationOptions.LongRunning);
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

            if (MAC.Text != "") {
                foreach (char c in MAC.Text.ToCharArray()) {
                    if (c <= 'F' || (c >= '0' && c <= '9')){
                    }
                    else {
                        invalid = true;
                        errMsg = "L'indirizzo MAC contiene caratteri non validi!";
                        break;
                    }
                }
                if (MAC.Text.Length < 17)
                {
                    invalid = true;
                    errMsg = "indirizzo MAC non valido";
                }

            }
            else if(MAC.Text == "")
            {
                invalid = true;
                errMsg = "Nessun indirizzo MAC inserito";
            }
            if (!invalid)
            {

                startButton.Visibility = System.Windows.Visibility.Hidden;
                stopButton.Visibility = System.Windows.Visibility.Visible;
                MACTextBox.IsReadOnly = true;
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

            dr.RemoveSearch();   
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

        public void UpdateBoardCounterTextBlock (int boardCount)
        {
            boardCounter.Text = boardCount.ToString();
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
        private void BackToConfigBtnUp(object sender, MouseButtonEventArgs e)
        {

        }
    }

}