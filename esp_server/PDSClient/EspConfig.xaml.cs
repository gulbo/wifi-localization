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

        public int NBoards { get; private set; }
        private DBConnect _dbConnection { get; set; }
        private StartConfiguration _initialWindow { get; set; }

        public EspConfig(int NBoards, DBConnect DBConnection, StartConfiguration initialWindow)
        {
            InitializeComponent();

            Debug.Assert(NBoards > 0);

            this.NBoards = NBoards;
            this._dbConnection = DBConnection;
            this._initialWindow = initialWindow;

            for(int i = 0; i < NBoards; i++)
            {
                parentPanel.Children.Add(EspLineConfig(i + 1));
            }
        }


        private void Button_Close(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        public static EspConfig GenerateFromList(List<Board> boards, DBConnect dbConnect, StartConfiguration initialWindow)
        {
            EspConfig result = new EspConfig(boards.Count, dbConnect, initialWindow);

            for(int i = 0; i < boards.Count; i++)
            {
                result.SetBoardAtLine(i + 1, boards[i]);
                result.SetReadOnlyLine(i + 1);
            }

            return result;
        }

        private void SetReadOnlyLine(int idx)
        {
            TextBox id = FindChild<TextBox>(parentPanel, "txt_id" + idx);
            TextBox x = FindChild<TextBox>(parentPanel, "txt_x" + idx);
            TextBox y = FindChild<TextBox>(parentPanel, "txt_y" + idx);

            id.IsReadOnly = true;
            x.IsReadOnly = true;
            y.IsReadOnly = true;
        }

        private void SetBoardAtLine(int idx, Board board)
        {
            TextBox id = FindChild<TextBox>(parentPanel, "txt_id" + idx);
            TextBox x = FindChild<TextBox>(parentPanel, "txt_x" + idx);
            TextBox y = FindChild<TextBox>(parentPanel, "txt_y" + idx);

            id.Text = board.Id.ToString();
            x.Text = board.P.X.ToString("0.00", CultureInfo.InvariantCulture);
            y.Text = board.P.Y.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private StackPanel EspLineConfig(int i)
        {
            var panel = new StackPanel {
                Orientation = Orientation.Horizontal,
                Height = 40,
                VerticalAlignment = MyVerticalAlignment,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };
            panel.Children.Add(new TextBlock {
                Text = i.ToString() + ") Id: ",
                Width = TextBlockWidth,
                Height = ElementHeight,
                VerticalAlignment = MyVerticalAlignment,
                Margin = TextBlockMargin
            });
            panel.Children.Add(new TextBox { Name = "txt_id" + i,
                Width = TextBoxWidth,
                Height = ElementHeight,
                VerticalAlignment = MyVerticalAlignment,
                TextAlignment = TextBoxTextAlignment,
                Margin = TextBoxMargin
            });
            panel.Children.Add(new TextBlock { Text = "x: ",
                Width = TextBlockWidth,
                Height = ElementHeight,
                VerticalAlignment = MyVerticalAlignment,
                Margin = TextBlockMargin
            });
            panel.Children.Add(new TextBox { Name = "txt_x" + i,
                Width = TextBoxWidth,
                Height = ElementHeight,
                VerticalAlignment = MyVerticalAlignment,
                TextAlignment = TextBoxTextAlignment,
                Margin = TextBoxMargin
            });
            panel.Children.Add(new TextBlock { Text = "y: ",
                Width = TextBlockWidth,
                Height = ElementHeight,
                VerticalAlignment = MyVerticalAlignment,
                Margin = TextBlockMargin
            });
            panel.Children.Add(new TextBox { Name = "txt_y" + i,
                Width = TextBoxWidth,
                Height = ElementHeight,
                VerticalAlignment = MyVerticalAlignment,
                TextAlignment = TextBoxTextAlignment,
                Margin = TextBoxMargin
            });

            return panel;
        }

        private List<Board> GetBoards()
        {
            List <Board> result = new List<Board>();

            for(int i = 0; i < NBoards; i++)
            {
                var idBox = FindChild<TextBox>(parentPanel, "txt_id" + (i + 1));
                var xBox = FindChild<TextBox>(parentPanel, "txt_x" + (i + 1));
                var yBox = FindChild<TextBox>(parentPanel, "txt_y" + (i + 1));

                if (!(idBox.Text.Length != 0) || !(xBox.Text.Length != 0) || !(yBox.Text.Length != 0))
                    return null;
                try
                {
                    int id = int.Parse(idBox.Text.Trim());
                    double x = double.Parse(xBox.Text.Trim().Replace(",", "."), CultureInfo.InvariantCulture);
                    double y = double.Parse(yBox.Text.Trim().Replace(",", "."), CultureInfo.InvariantCulture);
               
                    result.Add(new Board(id, x, y));
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return result;
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

        public static T FindChild<T>(DependencyObject parent, string childName)
            where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        private void ButtonOk(object sender, RoutedEventArgs e)
        {
            List<Board> boards = GetBoards();
            HashSet<int> idBoards = new HashSet<int>();

            if(boards == null)
            {
                System.Windows.MessageBox.Show("Invalid parameter(s) inserted.",
                    "Invalid parameters",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            Debug.Assert(boards.Count == NBoards);

            foreach(Board board in boards)
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
                res = _dbConnection.InsertBoard(boards);
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
    }
}
