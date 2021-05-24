using LoadInjector.RunTime.Views;
using LoadInjectorCommanCentre.Views;
using LoadInjectorCommandCentre;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LoadInjectorCommanCentre {

    public partial class MainWindow : Window, INotifyPropertyChanged {
        private CCController cccontroller;

        private string filterNodeID;
        public string filterConnectionID;
        private ExecutionRecords _records;
        private int gridRefreshRate = 1;
        public ControlWriter consoleWriter;
        private int numClients = 1;
        private string webServerURL = "http://localhost:49152/";
        private string signalRURL = "http://localhost:6220/";
        private string autoArchiveFile;
        private bool autoExecute = true;

        public ObservableCollection<ExecutionRecordClass> RecordsCollection {
            get { return this._records; }
        }

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
        }

        public void OnPropertyChanged(string propName) {
            try {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            } catch (Exception) {
                //NO-OP
            }
        }

        private Visibility vis = Visibility.Collapsed;

        public Visibility ShowDetailPanel {
            get {
                return vis;
            }
            set {
                vis = value;
                OnPropertyChanged("ShowDetailPanel");
            }
        }

        public int NumClients { get { return numClients; } set { numClients = value; } }
        public string SignalRURL { get { return signalRURL; } set { signalRURL = value; } }
        public string ServerURL { get { return webServerURL; } set { webServerURL = value; } }
        public string AutoAssignArchive { get { return autoArchiveFile; } set { autoArchiveFile = value; OnPropertyChanged("AutoAssignArchive"); } }

        public bool AutoExecute { get { return autoExecute; } set { autoExecute = value; OnPropertyChanged("AutoExecute"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OutputConsole_Initialized(object sender, EventArgs e) {
            consoleWriter = new ControlWriter(outputConsole);
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

        private void ViewAllBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.ViewAll();
        }

        private void DisconnectAllBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.DisconnectAll();
        }

        private void LocalClientBtn_OnClick(object sender, RoutedEventArgs e) {
            cccontroller.StartClients(1);
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
                        statusGrid.Items.Refresh();
                    }
                });
            } catch (Exception ex) {
                Console.WriteLine("Unmanaged error.   " + ex.Message);
            }
        }

        public void SetFilterCriteria(string nodeID, string ConnectionID = null) {
            this.filterNodeID = nodeID;
            this.filterConnectionID = ConnectionID;

            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    RecordsCollection.Clear();
                    statusGrid.Items.Refresh();
                    if (ConnectionID != null) {
                        cccontroller.MessageHub.Hub.Clients.Client(ConnectionID).Refresh();
                    }
                    if (nodeID != null) {
                        ShowDetailPanel = Visibility.Visible;
                    }
                } catch (Exception ex) {
                    Debug.WriteLine("Updating Grid Error. " + ex.Message);
                }
            });
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e) {
            if (filterNodeID == null) {
                e.Accepted = true;
            } else {
                ExecutionRecordClass rec = e.Item as ExecutionRecordClass;
                if (rec.ExecutionNodeID == filterNodeID) {
                    e.Accepted = true;
                } else {
                    e.Accepted = false;
                }
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
            cccontroller.MessageHub.Hub.Clients.All.Reset();
        }

        private void Window_ContentRendered(object sender, EventArgs e) {
            LoadInjectorCommandCentreWelcome welcome = new LoadInjectorCommandCentreWelcome(this) {
                Owner = this,
                DataContext = this
            };
            welcome.ShowDialog();

            cccontroller = new CCController(this, NumClients, SignalRURL, ServerURL, AutoAssignArchive);
            _records = (ExecutionRecords)this.Resources["records"];
        }

        private void autoStartBtn_Click(object sender, RoutedEventArgs e) {
            AutoExecute = false;
        }
    }
}