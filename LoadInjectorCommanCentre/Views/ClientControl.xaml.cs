using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using LoadInjector.Runtime.EngineComponents;
using LoadInjectorBase.Commom;
using LoadInjectorCommandCentre;
using Microsoft.Win32;

namespace LoadInjectorCommanCentre.Views {

    /// <summary>
    /// Interaction logic for ClientControl.xaml
    /// </summary>
    public partial class ClientControl : UserControl, INotifyPropertyChanged {
        //private ObservableCollection<ExecutionRecordClass> _myCollection = new ObservableCollection<ExecutionRecordClass>();

        //public ObservableCollection<ExecutionRecordClass> RecordsCollection {
        //    get { return this._myCollection; }
        //    set { this._myCollection = value; }
        //}

        public string ConnectionID { get; }
        public CentralMessagingHub MessageHub { get; }

        private string executionID;

        public string ExecutionNodeID {
            get {
                return executionID;
            }
            set {
                executionID = value;
                OnPropertyChanged("ExecutionNodeID");
            }
        }

        private string osversion;

        public string OSVersion {
            get {
                return $"{osversion}";
            }
            set {
                osversion = value;
                OnPropertyChanged("OSVersion");
            }
        }

        private string statusText = "Un Assigned";

        public string StatusText {
            get {
                return $"{statusText}";
            }
            set {
                statusText = value;
                if (statusText == ClientState.UnAssigned.Value) {
                    assignBtn.IsEnabled = true;
                    prepBtn.IsEnabled = false;
                    execBtn.IsEnabled = false;
                    stopBtn.IsEnabled = false;
                }
                if (statusText == ClientState.Assigned.Value) {
                    assignBtn.IsEnabled = true;
                    prepBtn.IsEnabled = true;
                    execBtn.IsEnabled = false;
                    stopBtn.IsEnabled = false;
                }
                if (statusText == ClientState.Ready.Value) {
                    assignBtn.IsEnabled = true;
                    prepBtn.IsEnabled = true;
                    execBtn.IsEnabled = true;
                    stopBtn.IsEnabled = false;
                }
                if (statusText == ClientState.Executing.Value || statusText == ClientState.WaitingNextIteration.Value || statusText == ClientState.ExecutionPending.Value) {
                    assignBtn.IsEnabled = false;
                    prepBtn.IsEnabled = false;
                    execBtn.IsEnabled = false;
                    stopBtn.IsEnabled = true;
                }
                if (statusText == ClientState.Stopped.Value) {
                    assignBtn.IsEnabled = true;
                    prepBtn.IsEnabled = true;
                    execBtn.IsEnabled = false;
                    stopBtn.IsEnabled = false;
                }
                OnPropertyChanged("StatusText");
            }
        }

        private string ip;

        public string IP {
            get {
                return $"Client IP: {ip}";
            }
            set {
                ip = value;
                OnPropertyChanged("IP");
            }
        }

        private string processID;

        public string ProcessID {
            get {
                return $"Process ID: {processID}";
            }
            set {
                processID = value;
                OnPropertyChanged("ProcessID");
            }
        }

        public string XML { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private CCController cCController;

        public void OnPropertyChanged(string propName) {
            try {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            } catch (Exception ex) {
                Console.WriteLine("On Property Error. " + ex.Message);
            }
        }

        public ClientControl(string connectionID, CentralMessagingHub messageHub, CCController cCController) {
            InitializeComponent();
            DataContext = this;
            ConnectionID = connectionID;
            MessageHub = messageHub;
            this.cCController = cCController;
        }

        private void Prep_OnClick(object sender, RoutedEventArgs e) {
            MessageHub.Hub.Clients.Client(ConnectionID).ClearAndPrepare();
        }

        private void Exec_OnClick(object sender, RoutedEventArgs e) {
            MessageHub.Hub.Clients.Client(ConnectionID).Execute();
            this.cCController.SetRefreshRate();
        }

        private void Stop_OnClick(object sender, RoutedEventArgs e) {
            MessageHub.Hub.Clients.Client(ConnectionID).Stop();
        }

        private void Status_OnClick(object sender, RoutedEventArgs e) {
            this.cCController.SetFilterCriteria(ExecutionNodeID, ConnectionID);
            MessageHub.Hub.Clients.Client(ConnectionID).Refresh();
        }

        private void Disconnect_OnClick(object sender, RoutedEventArgs e) {
            MessageHub.Hub.Clients.Client(ConnectionID).Disconnect();
        }

        private void Assign_OnClick(object sender, RoutedEventArgs e) {
            string archiveRoot = cCController.ArchiveRoot;
            Directory.CreateDirectory(archiveRoot);

            OpenFileDialog open = new OpenFileDialog {
                Filter = "Load Injector Archive Files(*.lia)|*.lia",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (open.ShowDialog() == true) {
                File.Copy(open.FileName, archiveRoot + "\\" + open.SafeFileName, true);
                MessageHub.Hub.Clients.Client(ConnectionID).RetrieveArchive(cCController.WebServerURL + "/" + open.SafeFileName);
            }
        }

        public void SetStatusText(string message) {
            StatusText = message;
        }
    }
}