using MySql.Data.MySqlClient;
using PDSClient.ConnectionManager;
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
using Microsoft.Win32;
using System.IO;
using Newtonsoft.Json;

using System.Collections.ObjectModel;
using System.ComponentModel;

using Point = PDSClient.ConnectionManager.Point;
using System.Timers;
using PDSClient.StatModule;


using System.Globalization;
using System.Threading;



namespace PDSClient
{
    /// <summary>
    /// Interaction logic for StartConfiguration.xaml
    /// </summary>
    public partial class StartConfiguration : Window
    {
        private DBConnect _dbConnection { get; set; }

        public StartConfiguration()
        {
            //created and managed by Windows Forms designer and it defines everything you see on the form
            InitializeComponent();
            //connection to database DBConnect(string server, string uid, string password) + pair key/value thread safe accessible from more thread at the same time for saving boards
            _dbConnection = new DBConnect("localhost", "root", "");

        }

        //close each window objects
        private void closeButton(object sender, System.Windows.RoutedEventArgs e)
        {
            //for each objects in window
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                App.Current.Windows[intCounter].Close();
            //close this process and tells the Operating system the exit code (usefull because if there might be other foreground threads running. They would stay running )
            Environment.Exit(Environment.ExitCode);
            
        }



        private void ButtonNewConfig(object sender, RoutedEventArgs e)
        {           
            //Hide the current window
            this.Hide();
            //call to boards configuration
            EspConfig boardConfig = new EspConfig(_dbConnection, this);
            //show boardConfig window
            boardConfig.ShowDialog();
            
        }

        private void ButtonLoadFromDB(object sender, RoutedEventArgs e)
        {
            List<Scheda> boards = new List<Scheda>();
            try
            {
                boards = _dbConnection.SelezionaSchede();
                if(boards == null)
                {
                    System.Windows.MessageBox.Show("Database error. Please check if the database is online",
                        "Database connection error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
                else if(boards.Count == 0)
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

            foreach(Scheda b in boards)
            {
                if(b.ID_scheda <= 0)
                {
                    System.Windows.MessageBox.Show("Invalid board id found: " + b.ID_scheda,
                        "Invalid board id",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }
            //Hide the current window
            this.Hide();
            //show the config page with boards already displayed
            EspConfig configWindow = new EspConfig(boards, _dbConnection, this);
            configWindow.ShowDialog();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }

}
