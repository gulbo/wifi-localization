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
            InitializeComponent();

            _dbConnection = new DBConnect("localhost", "root", "");

        }

        private void Button_Close(object sender, System.Windows.RoutedEventArgs e)
        {
            for (int intCounter = App.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                App.Current.Windows[intCounter].Close();

            Environment.Exit(Environment.ExitCode);

        }



        private void ButtonNewConfig(object sender, RoutedEventArgs e)
        {
            int nBoards = 0;
            try
            {
                String txt = txt_boards.Text.Trim();
                if(txt.Length <= 0)
                {
                    System.Windows.MessageBox.Show("Invalid number inserted", "Invalid value", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                nBoards = int.Parse(txt_boards.Text.Trim());
            }
            catch (OverflowException)
            {
                System.Windows.MessageBox.Show("Invalid number: overflow error. Please change it.", 
                    "Overflow Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Invalid number. Please insert a valid value.",
                    "Invalid number",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if(nBoards <= 0)
            {
                System.Windows.MessageBox.Show("Negative values and 0 are not accepted.", 
                    "Invalid value", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }

            EspConfig configurationWindow = new EspConfig(nBoards, _dbConnection, this);
            configurationWindow.ShowDialog();
        }

        private void ButtonLoadFromDB(object sender, RoutedEventArgs e)
        {
            List<Board> boards = new List<Board>();
            try
            {
                boards = _dbConnection.GetBoards();
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

            foreach(Board b in boards)
            {
                if(b.Id <= 0)
                {
                    System.Windows.MessageBox.Show("Invalid board id found: " + b.Id,
                        "Invalid board id",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }
            }

            EspConfig configWindow = EspConfig.GenerateFromList(boards, _dbConnection, this);
            configWindow.ShowDialog();
        }

        private void ButtonLoadFromJSON(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            bool? result = ofd.ShowDialog();

            if(!result.HasValue || (result.HasValue && !result.Value))
            {
                return;
            }

            String path = ofd.FileName;
            Board[] boards = LoadFromJson(path);
            if(boards == null)
            {
                /*
                System.Windows.MessageBox.Show("Unable to read the specified file",
                    "Conversion error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                */
                return;
            }

            EspConfig configWindow = EspConfig.GenerateFromList(boards.ToList(), _dbConnection, this);
            configWindow.ShowDialog();
        }

        private Board[] LoadFromJson(String path)
        {
            FileInfo fi = new FileInfo(path);
            Board[] result;

            if (!fi.Exists)
                return null;
            try
            {
                using (StreamReader r = new StreamReader(path))
                {
                    String json = r.ReadToEnd();
                    result = JsonConvert.DeserializeObject<Board[]>(json);
                }
            }
            catch (JsonReaderException)
            {
                System.Windows.MessageBox.Show("Error parsing the JSON file",
                    "JSON error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("Error reading the file",
                    "File error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }

            return result;
        }
    }
}
