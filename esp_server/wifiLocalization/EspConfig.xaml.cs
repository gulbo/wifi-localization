using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Globalization;
using System.Collections.ObjectModel;
using WifiLocalization.StatModule;

namespace WifiLocalization.ConnectionManager
{
    /// <summary>
    /// Interaction logic for EspConfig.xaml
    /// </summary>
    public partial class EspConfig : Window
    {

        private ObservableCollection<Scheda> Boards;
        //private List<Board> boards;

        public int NBoards { get; private set; }
        private DBConnect _dbConnection { get; set; }

        public EspConfig()
        {

            //created and managed by Windows Forms designer and it defines everything you see on the form
            InitializeComponent();
            //connection to database DBConnect(string server, string uid, string password) + pair key/value thread safe accessible from more thread at the same time for saving boards
            _dbConnection = new DBConnect("localhost", "root", "");
            //osservable collection for the list of board
            this.Boards = new ObservableCollection<Scheda>();
            ListView boards = Boards_box;
            boards.ItemsSource = Boards;
            DataTemplate dataTemplate = Boards_box.ItemTemplate;
        }
        public EspConfig(List<Scheda> Boards, DBConnect DBConnection)
        {
            //Load the compiled page of a component.(because we use XAML)
            InitializeComponent();
            if (Boards.Count > 0)
            {
                this.Boards = new ObservableCollection<Scheda>(Boards);
                ListView boards = Boards_box;
                boards.ItemsSource = this.Boards;
                
            }
            else
            {
                System.Windows.MessageBox.Show("No boards found in database.",
                        "Empty board list",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
            }
            this._dbConnection = DBConnection;
        }

       
        private bool ValidPos(List<Scheda> boards)
        {
            HashSet<Tuple<double, double>> boardPos = new HashSet<Tuple<double, double>>();

            foreach (Scheda b in boards)
            {
                if (boardPos.Contains(new Tuple<double, double>(b.Punto.Ascissa, b.Punto.Ordinata)))
                    return false;
                else
                    boardPos.Add(new Tuple<double, double>(b.Punto.Ascissa, b.Punto.Ordinata));
            }
            return true;

        }
        private void backButton(object sender, System.Windows.RoutedEventArgs e)
        {
            Boards.Clear();
            startWindow.Visibility = Visibility.Visible;
            configurationGrid.Visibility = Visibility.Hidden;
        }

        private void closeButton(object sender, System.Windows.RoutedEventArgs e)
        {
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                App.Current.Windows[intCounter].Close();
            //close this process and tells the Operating system the exit code (usefull because if there might be other foreground threads running. They would stay running )
            Environment.Exit(Environment.ExitCode);
        }

        private void ButtonAdd(object sender, RoutedEventArgs e)
        {
            Scheda newBoard =new Scheda();
            Boards.Add(newBoard);
            
            return;
        }

        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            HashSet<int> idBoards = new HashSet<int>();
            
            if(Boards == null)
            {
                System.Windows.MessageBox.Show("Invalid parameter(s) inserted.",
                    "Invalid parameters",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            List<Scheda> boards = new List<Scheda>();
            foreach (Scheda board in Boards_box.Items)
            {
                if (idBoards.Contains(board.ID_scheda))
                {
                    System.Windows.MessageBox.Show("Duplicate id not allowed. Please check your configuration.",
                        "Conflict with ids detected",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                idBoards.Add(board.ID_scheda);
                boards.Add(board);
            }
            if (!ValidPos(boards)) {
                System.Windows.MessageBox.Show("Boards in the same position are not allowed",
                       "Conflict with position detected",
                       MessageBoxButton.OK,
                       MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool res = _dbConnection.RimuoviSchede();
                if (!res)
                {
                    System.Windows.MessageBox.Show("Unable to connect to database.",
                        "Database error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
                res = _dbConnection.InserisciScheda(boards);
                if (!res)
                {
                    System.Windows.MessageBox.Show("Unable to insert boards' information to the database. Please check the connection and retry.",
                        "Database error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }
            catch (MySqlException)
            {
                System.Windows.MessageBox.Show("Unable to connect to database. Please check the connection and retry.", 
                    "Database Connection error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }
            System.Diagnostics.Debug.WriteLine("Boards correctly inserted into db");
            if ((bool) trunkateCheckBox.IsChecked)
            {
                _dbConnection.RimuoviPacchetti();
                _dbConnection.RimuoviPosizioni();
            }
            MainWindow mw = new MainWindow(_dbConnection, boards);

            this.Close();
            mw.Show();
            
        }

        private void ButtonDelete(object sender, RoutedEventArgs e)
        {
            Button ClickedButton = (Button) sender;
            Scheda DeletedBoard = (Scheda) ClickedButton.DataContext;
            Boards.Remove(DeletedBoard);
            
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBlockText.Foreground = Utils.lightOrange;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBlockText.Foreground = Brushes.DarkGray;
        }

        private void ButtonLoadFromDB(object sender, RoutedEventArgs e)
        {
            List<Scheda> boards = new List<Scheda>();
            try
            {
                boards = _dbConnection.SelezionaSchede();
                
                if (boards == null)
                {
                    System.Windows.MessageBox.Show("Database error. Please check if the database is online",
                        "Database connection error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
                else if (boards.Count == 0)
                {
                    System.Windows.MessageBox.Show("No boards found in database.",
                        "Empty board list",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }
            catch (MySqlException)
            {
                System.Windows.MessageBox.Show("Error connecting with the database",
                    "Database connection error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            foreach (Scheda b in boards)
            {
                if (b.ID_scheda <= 0)
                {
                    System.Windows.MessageBox.Show("Invalid board id found: " + b.ID_scheda,
                        "Invalid board id",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
                if (!Boards.Contains(b))
                {
                    Boards.Add(b);
                }  
               
            }          
        }

        private void StartConfiguration(object sender, RoutedEventArgs e)
        {
            startWindow.Visibility = Visibility.Hidden;
            configurationGrid.Visibility = Visibility.Visible;
        }
    }
}
