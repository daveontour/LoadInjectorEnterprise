using LoadInjector.RunTime.Views;
using LoadInjectorCommandCentre.Views;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml;

namespace LoadInjectorCommandCentre {

    [ValueConversion(typeof(bool), typeof(GridLength))]
    public class BoolToGridRowHeightConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return ((Visibility)value != Visibility.Collapsed) ? new GridLength(5, GridUnitType.Star) : new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {    // Don't need any convert back
            return null;
        }
    }

    public class BoolToGridRowHeightConverter2 : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return ((Visibility)value != Visibility.Collapsed) ? new GridLength(5) : new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {    // Don't need any convert back
            return null;
        }
    }

    public class TabContentSelector : DataTemplateSelector {

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            Window win = Application.Current.MainWindow;

            if (item is ClientTabControl) {
                return win.FindResource("ClientTabContentTemplate") as DataTemplate;
            }

            return win.FindResource("SummaryTabContentTemplate") as DataTemplate;
        }
    }

    public class TabItemSelector : DataTemplateSelector {

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            Window win = Application.Current.MainWindow;

            if (item is ClientTabControl) {
                return win.FindResource("ClientTabItemTemplate") as DataTemplate;
            }

            return win.FindResource("SummaryTabItemTemplate") as DataTemplate;
        }
    }

    public partial class MainWindow : Window, INotifyPropertyChanged {
        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
        private MainCommandCenterController cccontroller;

        private ExecutionRecords _records;
        private int gridRefreshRate = 1;
        public ControlWriter consoleWriter;
        private int numClients = 0;
        private string webServerURL = "http://localhost:6230/";
        private string signalRURL = "http://localhost:6220/";
        private string autoArchiveFile;
        private bool autoExecute = false;
        private DataGrid _visdataGrid;
        private string[] args;
        private readonly bool runFromConfig = false;

        public ExecutionRecords RecordsCollection {
            get { return this._records; }
        }

        public MainWindow(string[] args = null) {
            this.args = args;

            if (args != null) {
                foreach (string arg in args) {
                    if (arg.Contains("runFromConfig")) {
                        this.runFromConfig = true;
                    }
                    if (arg.StartsWith("-autoAssign:")) {
                        AutoAssignArchive = arg.Replace("-autoAssign:", "");
                        AutoExecute = false;
                    }
                }
            }

            InitializeComponent();
            DataContext = this;
            this.ClientTabDatas = new ObservableCollection<object>();

            if (this.runFromConfig) {
                NumClients = 1;
                SignalRIP = "localhost";
                SignalRPort = "6220";
                ServerPort = "6230";
                ExecutablePath = null;
                OnPropertyChanged("NoAutoAssign");

                return;
            }

            try {
                XmlDocument doc = new XmlDocument();
                doc.Load(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LoadInjectorCommandCentre\\Config.xml");

                NumClients = Int32.Parse(doc.SelectSingleNode(".//initialClient")?.InnerText);
                SignalRIP = doc.SelectSingleNode(".//signalRIP")?.InnerText;
                SignalRPort = doc.SelectSingleNode(".//signalRPort")?.InnerText;
                ServerPort = doc.SelectSingleNode(".//serverPort")?.InnerText;
                AutoExecute = bool.Parse(doc.SelectSingleNode(".//autoStart")?.InnerText);
                AutoAssignArchive = doc.SelectSingleNode(".//autoAssignFile")?.InnerText;
                ExecutablePath = doc.SelectSingleNode(".//clientPath")?.InnerText;
            } catch (Exception) {
                NumClients = 1;
                SignalRIP = "localhost";
                SignalRPort = "6220";
                ServerPort = "6230";
                AutoExecute = false;
                AutoAssignArchive = null;
                ExecutablePath = null;
            }
            OnPropertyChanged("NoAutoAssign");

            try {
                if (args != null) {
                    foreach (string arg in args) {
                        if (arg.StartsWith("-autoAssign:")) {
                            AutoAssignArchive = arg.Replace("-autoAssign:", "");
                            AutoExecute = false;
                        }
                    }
                }
            } catch (Exception) {
                //NO-OP
            }
        }

        public void OnPropertyChanged(string propName) {
            try {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            } catch (Exception) {
                //NO-OP
            }
        }

        public DataGrid VisibleDataGrid {
            get {
                return _visdataGrid;
            }
            set { _visdataGrid = value; }
        }

        private Canvas _visConfigCanvas;

        public Canvas VisibleConfigCanvas {
            get {
                return _visConfigCanvas;
            }
            set { _visConfigCanvas = value; }
        }

        public ObservableCollection<object> ClientTabDatas { get; set; }

        public bool NoAutoAssign {
            get {
                if (AutoAssignArchive == null || AutoAssignArchive == "") {
                    return true;
                } else {
                    return false;
                }
            }
        }

        public int NumClients { get { return numClients; } set { numClients = value; } }

        public string SignalRIP { get; set; }
        public string SignalRPort { get; set; }
        public string ServerPort { get; set; }

        public string SignalRURL { get { return signalRURL; } set { signalRURL = value; } }
        public string ServerURL { get { return webServerURL; } set { webServerURL = value; } }
        public string AutoAssignArchive { get { return autoArchiveFile; } set { autoArchiveFile = value; OnPropertyChanged("AutoAssignArchive"); } }

        public string ExecutablePath { get; set; }

        public bool AutoExecute { get { return autoExecute; } set { autoExecute = value; OnPropertyChanged("AutoExecute"); } }

        public int NumConnectedClients {
            get {
                return clientControlStack.Children.Count;
            }
        }

        public string SelectedConnectionID { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OutputConsole_Initialized(object sender, EventArgs e) {
        }

        private void PrepAllBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.PrepAll();
        }

        private void ExecAllBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.ExecuteAll();
        }

        private void StopAllBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.StopAll();
        }

        private void AssignBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.AssignAll();
        }

        private void RefreshBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.RefreshClients();
        }

        private void DisconnectAllBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.DisconnectAll();
        }

        private void LocalClientBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.StartClients(1);
        }

        private void completionReportBtn_Click(object sender, RoutedEventArgs e) {
            cccontroller.RequestCompletionReports();
        }

        public void AddUpdateExecutionRecord(ExecutionRecordClass rec) {
            try {
                Application.Current.Dispatcher.Invoke(delegate {
                    ExecutionRecordClass r = RecordsCollection.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == rec.ExecutionLineID);

                    if (r != null) {
                        r.MM = rec.MM;
                        r.Sent = rec.Sent;
                        r.Name = rec.Name;
                        r.Type = rec.Type;
                        OnPropertyChanged("RecordsCollection");
                    } else {
                        RecordsCollection.Add(rec);
                        OnPropertyChanged("RecordsCollection");
                    }
                });
            } catch (Exception ex) {
                Console.WriteLine("Unmanaged error.   " + ex.Message);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (refreshRate.SelectedIndex == 0) {
                this.gridRefreshRate = 1;
            }
            if (refreshRate.SelectedIndex == 1) {
                this.gridRefreshRate = 3;
            }
            if (refreshRate.SelectedIndex == 2) {
                this.gridRefreshRate = 10;
            }
            if (refreshRate.SelectedIndex == 3) {
                this.gridRefreshRate = 0;
            }

            cccontroller?.SetRefreshRate(this.gridRefreshRate);
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            cccontroller.Close();
        }

        private void resetAllBtn_Click(object sender, RoutedEventArgs e) {
            cccontroller.ResetAll();
        }

        private void Window_ContentRendered(object sender, EventArgs e) {
            logger.Info("Window_ContentRendered");

            try {
                if (!runFromConfig) {
                    LoadInjectorCommandCentreWelcome welcome = new LoadInjectorCommandCentreWelcome(this) {
                        Owner = this,
                        DataContext = this
                    };
                    welcome.ShowDialog();
                }
            } catch (Exception ex) {
                logger.Error(ex, "Welcome window error");
            }

            ServerURL = $"http://{SignalRIP}:{ServerPort}/";
            SignalRURL = $"http://{SignalRIP}:{SignalRPort}";

            try {
                XmlDocument doc = new XmlDocument();

                XmlElement root = doc.CreateElement("config");
                doc.AppendChild(root);

                XmlElement elem = doc.CreateElement("initialClient");
                XmlText text = doc.CreateTextNode(NumClients.ToString());
                root.AppendChild(elem);
                root.LastChild.AppendChild(text);

                XmlElement elem1 = doc.CreateElement("signalRIP");
                XmlText text1 = doc.CreateTextNode(SignalRIP);
                root.AppendChild(elem1);
                root.LastChild.AppendChild(text1);

                XmlElement elem2 = doc.CreateElement("serverPort");
                XmlText text2 = doc.CreateTextNode(ServerPort);
                root.AppendChild(elem2);
                root.LastChild.AppendChild(text2);

                XmlElement elem3 = doc.CreateElement("autoStart");
                XmlText text3 = doc.CreateTextNode(AutoExecute.ToString());
                root.AppendChild(elem3);
                root.LastChild.AppendChild(text3);

                XmlElement elem4 = doc.CreateElement("autoAssignFile");
                XmlText text4 = doc.CreateTextNode(AutoAssignArchive);
                root.AppendChild(elem4);
                root.LastChild.AppendChild(text4);

                XmlElement elem5 = doc.CreateElement("signalRPort");
                XmlText text5 = doc.CreateTextNode(SignalRPort);
                root.AppendChild(elem5);
                root.LastChild.AppendChild(text5);

                XmlElement elem6 = doc.CreateElement("clientPath");
                XmlText text6 = doc.CreateTextNode(ExecutablePath);
                root.AppendChild(elem6);
                root.LastChild.AppendChild(text6);

                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LoadInjectorCommandCentre");
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LoadInjectorCommandCentre\\Config.xml", doc.OuterXml);
            } catch (Exception ex) {
                logger.Error(ex.Message, "XMLDoc");
            }

            OnPropertyChanged("NoAutoAssign");
            cccontroller = new MainCommandCenterController(this, NumClients, SignalRURL, ServerURL, AutoAssignArchive);
            _records = (ExecutionRecords)this.Resources["records"];
        }

        public void AddClientTab(ClientTabControl clientTabData) {
            ObservableCollection<object> newCollection = new ObservableCollection<object>();
            foreach (object o in ClientTabDatas) {
                newCollection.Add(o);
            }
            newCollection.Add(clientTabData);

            ClientTabDatas = newCollection;
            OnPropertyChanged("ClientTabDatas");

            if (clientTabData.ConnectionID == "summary") {
                nodeTabHolder.SelectedIndex = 0;
                EnumVisualDataGrid(nodeTabHolder);
            }
        }

        public void AddClientControl(ClientControl clientControl) {
            clientControlStack.Children.Add(clientControl);
            OnPropertyChanged("NumConnectedClients");
        }

        public void RemoveClientControl(ClientControl clientControl) {
            clientControlStack.Children.Remove(clientControl);
            OnPropertyChanged("NumConnectedClients");
        }

        public void RemoveClientTab(string connectionID) {
            nodeTabHolder.SelectedIndex = 0;
            ObservableCollection<object> newCollection = new ObservableCollection<object>();
            foreach (object o in ClientTabDatas) {
                if (o is ClientTabControl control && control.ConnectionID == connectionID) {
                    continue;
                }
                newCollection.Add(o);
            }

            ClientTabDatas = newCollection;
            OnPropertyChanged("ClientTabDatas");
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.Source is TabControl) {
                TabControl tabControl = e.Source as TabControl;
                if (tabControl.SelectedValue is ClientTabControl control) {
                    control.TabSelected();
                    SelectedConnectionID = control.ConnectionID;
                }
            }

            EnumVisualDataGrid(nodeTabHolder);
            VisibleDataGrid?.Items.Refresh();
        }

        public void EnumVisualDataGrid(Visual myVisual) {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(myVisual); i++) {
                // Retrieve child visual at specified index value.
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(myVisual, i);

                if (childVisual is DataGrid) {
                    if (((DataGrid)childVisual).Name == "statusGrid") {
                        VisibleDataGrid = childVisual as DataGrid;
                        break;
                    }
                }

                // Do processing of the child visual object.

                // Enumerate children of the child visual object.
                EnumVisualDataGrid(childVisual);
            }
        }

        private void outputConsole_TextChanged(object sender, TextChangedEventArgs e) {
            (sender as TextBox).ScrollToEnd();
        }

        private void statusBtn_Click(object sender, RoutedEventArgs e) {
            LoadInjectorCommandCentreStatus welcome = new LoadInjectorCommandCentreStatus(this) {
                Owner = this,
                DataContext = this
            };
            welcome.ShowDialog();

            try {
                XmlDocument doc = new XmlDocument();

                XmlElement root = doc.CreateElement("config");
                doc.AppendChild(root);

                XmlElement elem = doc.CreateElement("initialClient");
                XmlText text = doc.CreateTextNode(NumClients.ToString());
                root.AppendChild(elem);
                root.LastChild.AppendChild(text);

                XmlElement elem1 = doc.CreateElement("signalRIP");
                XmlText text1 = doc.CreateTextNode(SignalRIP);
                root.AppendChild(elem1);
                root.LastChild.AppendChild(text1);

                XmlElement elem2 = doc.CreateElement("serverPort");
                XmlText text2 = doc.CreateTextNode(ServerPort);
                root.AppendChild(elem2);
                root.LastChild.AppendChild(text2);

                XmlElement elem3 = doc.CreateElement("autoStart");
                XmlText text3 = doc.CreateTextNode(AutoExecute.ToString());
                root.AppendChild(elem3);
                root.LastChild.AppendChild(text3);

                XmlElement elem4 = doc.CreateElement("autoAssignFile");
                XmlText text4 = doc.CreateTextNode(AutoAssignArchive);
                root.AppendChild(elem4);
                root.LastChild.AppendChild(text4);

                XmlElement elem5 = doc.CreateElement("signalRPort");
                XmlText text5 = doc.CreateTextNode(SignalRPort);
                root.AppendChild(elem5);
                root.LastChild.AppendChild(text5);

                XmlElement elem6 = doc.CreateElement("clientPath");
                XmlText text6 = doc.CreateTextNode(ExecutablePath);
                root.AppendChild(elem6);
                root.LastChild.AppendChild(text6);

                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LoadInjectorCommandCentre\\Config.xml", doc.OuterXml);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            OnPropertyChanged("NoAutoAssign");
        }

        private void clearBtn_Click(object sender, RoutedEventArgs e) {
            cccontroller.ClearGrid();
        }

        private void saveReportBtn_Click(object sender, RoutedEventArgs e) {
            cccontroller.saveReport(SelectedConnectionID);
        }

        private void retrieveReportBtn_Click(object sender, RoutedEventArgs e) {
            cccontroller.retrieveReport(SelectedConnectionID);
        }
    }
}