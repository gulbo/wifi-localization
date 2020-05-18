﻿using System;
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

namespace PDSClient.ConnectionManager
{
    /// <summary>
    /// Interaction logic for EspConfig.xaml
    /// </summary>
    public partial class EspConfig : Window
    {
        private static VerticalAlignment MyVerticalAlignment = VerticalAlignment.Bottom;
        private static TextAlignment TextBoxTextAlignment = TextAlignment.Right;
        private static double ElementHeight = 30;
        private static double TextBlockWidth = 30;
        private static double TextBoxWidth = 60;
        private static Thickness TextBlockMargin = new Thickness(0, 0, 0, 0);
        private static Thickness TextBoxMargin = new Thickness(10, 0, 30, 10);
        private ObservableCollection<Board> Boards;
        //private List<Board> boards;

        public int NBoards { get; private set; }
        private DBConnect _dbConnection { get; set; }
        private StartConfiguration _initialWindow { get; set; }

        public EspConfig(List<Board> Boards, DBConnect DBConnection, StartConfiguration initialWindow)
        {
            //Load the compiled page of a component.(because we use XAML)
            InitializeComponent();
            if (Boards.Count > 0)
            {
                this.Boards = new ObservableCollection<Board>(Boards);
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
            this._initialWindow = initialWindow;
        }
        public EspConfig( DBConnect DBConnection, StartConfiguration initialWindow)
        {
            //Load the compiled page of a component.(because we use XAML)
            InitializeComponent();

            this.Boards = new ObservableCollection<Board>();
            ListView boards = Boards_box;
            boards.ItemsSource = Boards;
            DataTemplate dataTemplate = Boards_box.ItemTemplate;
             
           


            this._dbConnection = DBConnection;
            this._initialWindow = initialWindow;
            
        }
        private void backButton(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
            _initialWindow.Show();
        }
        private void closeButton(object sender, System.Windows.RoutedEventArgs e)
        {
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                App.Current.Windows[intCounter].Close();
            //close this process and tells the Operating system the exit code (usefull because if there might be other foreground threads running. They would stay running )
            Environment.Exit(Environment.ExitCode);
        }        

        private bool ValidPos(List<Board> boards)
        {
            HashSet<Tuple<double, double>> boardPos = new HashSet<Tuple<double, double>>();

            foreach (Board b in boards)
            {
                if (boardPos.Contains(new Tuple<double, double>(b.P.X, b.P.Y)))
                    return false;
                else
                    boardPos.Add(new Tuple<double, double>(b.P.X, b.P.Y));
            }
            return true;

        }

        private void ButtonAdd(object sender, RoutedEventArgs e)
        {
            Board newBoard =new Board();
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
            List<Board> boards = new List<Board>();
            foreach (Board board in Boards_box.Items)
            {
                boards.Add(board);
            }

            foreach (Board board in boards)
            {
                if (idBoards.Contains(board.Id))
                {
                    System.Windows.MessageBox.Show("Duplicate id not allowed. Please check your configuration.", 
                        "Conflict with ids detected", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Warning);
                    return;
                }
                idBoards.Add(board.Id);
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
                bool res = _dbConnection.DeleteBoards();
                if (!res)
                {
                    System.Windows.MessageBox.Show("Unable to connect to database.",
                        "Database error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
                res = _dbConnection.InsertBoard(Boards);
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
            MainWindow mw = new MainWindow(_dbConnection, boards);

            this.Close();
            _initialWindow.Close();
            mw.Show();
        }

        private void ButtonCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ButtonDelete(object sender, RoutedEventArgs e)
        {
            Button ClickedButton = (Button) sender;
            Board DeletedBoard = (Board) ClickedButton.DataContext;
            Boards.Remove(DeletedBoard);
        }
    }
}
