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
using WifiLocalization.Utilities;
using WifiLocalization.ConnectionManager;

namespace WifiLocalization.Graphic
{
    /// <summary>
    /// Interaction logic for Configuration.xaml
    /// </summary>
    public partial class Configuration : Window
    {

        private ObservableCollection<Scheda> Boards;
        //private List<Board> boards;

        public int NBoards { get; private set; }
        private DBConnect _dbConnection { get; set; }
        /**costruttore di Default*/
        public Configuration()
        {
            //osservable collection for the list of board
            this.Boards = new ObservableCollection<Scheda>();
            ListView boardsBox;
            DataTemplate boardDataTemplate;
            //created and managed by Windows Forms designer and it defines everything you see on the form
            InitializeComponent();
            //connection to database DBConnect(string server, string uid, string password) + pair key/value thread safe accessible from more thread at the same time for saving boards
            _dbConnection = new DBConnect("localhost", "root", "");          
            boardsBox = Boards_box;
            boardsBox.ItemsSource = Boards;
            boardDataTemplate = Boards_box.ItemTemplate;
        }
        
        /// <summary>
        /// controlla se ci sono schede con la stessa posizione 
        /// </summary>
        /// <param name="boards">lista delle schede</param>
        /// <returns>true nessuna scheda nella stessa posizione false altrimenti</returns>
        private bool CheckPosition(List<Scheda> boardsList)
        {

            HashSet<Tuple<double, double>> boardsPositions = new HashSet<Tuple<double, double>>();
            foreach (Scheda board in boardsList)
            {
                if (boardsPositions.Contains(new Tuple<double, double>(board.Punto.Ascissa, board.Punto.Ordinata)))
                    return false;
                else
                    boardsPositions.Add(new Tuple<double, double>(board.Punto.Ascissa, board.Punto.Ordinata));
            }
            return true;
        }
        private void WindowDrag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
        //gestisce il tasto "indietro", pulisce la lista di schede caricata in quella istanza e cambia ciò che viene visualizzato
        private void StartConfiguration(object sender, RoutedEventArgs e)
        {
            startWindow.Visibility = Visibility.Hidden;
            configurationGrid.Visibility = Visibility.Visible;
        }
        private void backButton(object sender, System.Windows.RoutedEventArgs e)
        {
            Boards.Clear();
            startWindow.Visibility = Visibility.Visible;
            configurationGrid.Visibility = Visibility.Hidden;
        }

        //esegue una serie di controlli prima dell'invio dei dati
        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            HashSet<int> BoardsIdList = new HashSet<int>();
            List<Scheda> boards = new List<Scheda>();
            bool DBresult;
            MainWind mainWindow;

            //controllo: lista vuota
            if (Boards == null)
            {
                MessageBox.Show("Lista di schede vuota, impossibile procedere all'invio.",
                    "Lista vuota",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            foreach (Scheda board in Boards_box.Items)
            {
                //controllo: duplicati
                if (BoardsIdList.Contains(board.ID_scheda))
                {
                    MessageBox.Show("Presenti schede duplicate, eliminare i duplicati",
                        " ID schede duplicati",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                int result;
                //controllo: id valido
                if (board.ID_scheda <= 0)
                {
                    MessageBox.Show("Id scheda non valido ",
                        "Id non valido",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                BoardsIdList.Add(board.ID_scheda);
                boards.Add(board);
            }
            //controllo: posizioni condivise
            if (!CheckPosition(boards))
            {
                System.Windows.MessageBox.Show("Due o più schede nella stessa posizione",
                       "posizione schede condivisa",
                       MessageBoxButton.OK,
                       MessageBoxImage.Warning);
                return;
            }
           

            try
            {
                DBresult = _dbConnection.RimuoviSchede();
                if (DBresult)
                {
                    DBresult = _dbConnection.InserisciScheda(boards);                                    
                }
                if (!DBresult)
                {
                    MessageBox.Show("Impossibile connettersi al Database.",
                        "errore di connessione",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }
            catch (MySqlException)
            {
                MessageBox.Show("Impossibile connettersi al Database.",
                    "errore di connessione",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            if ((bool)trunkateCheckBox.IsChecked)
            {
                _dbConnection.RimuoviPacchetti();
                _dbConnection.RimuoviPosizioni();
            }

            mainWindow = new MainWind(_dbConnection, boards);
            this.Close();
            mainWindow.Show();

        }

        private void closeButton(object sender, System.Windows.RoutedEventArgs e)
        {
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                App.Current.Windows[intCounter].Close();
            //close this process and tells the Operating system the exit code (usefull because if there might be other foreground threads running. They would stay running )
            Environment.Exit(Environment.ExitCode);
        }

        private void ButtonAddBoard(object sender, RoutedEventArgs e)
        {
            Scheda newBoard =new Scheda();
            Boards.Add(newBoard);
            
            return;
        }
        private void ButtonLoad(object sender, RoutedEventArgs e)
        {
            List<Scheda> boards = new List<Scheda>();
            try
            {
                boards = _dbConnection.SelezionaSchede();

                if (boards == null)
                {
                    MessageBox.Show("Impossibile connettersi al Database.",
                        "errore di connessione",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
                else if (boards.Count == 0)
                {
                    MessageBox.Show("Nessuna scheda salvata.",
                        "Lista vuota",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }
            catch (MySqlException)
            {
                MessageBox.Show("Impossibile connettersi al Database.",
                        "errore di connessione",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                return;
            }

            foreach (Scheda b in boards)
            {
                if (b.ID_scheda <= 0)
                {
                    MessageBox.Show("Id scheda non valido " + b.ID_scheda,
                        "Id non valido",
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
        private void ButtonDeleteBoard(object sender, RoutedEventArgs e)
        {
            Button ClickedButton = (Button) sender;
            Scheda DeletedBoard = (Scheda) ClickedButton.DataContext;
            Boards.Remove(DeletedBoard);
            
        }
     
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            checkBlockText.Foreground = Utils.lightOrange;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBlockText.Foreground = Brushes.DarkGray;
        }
        
    }
}
