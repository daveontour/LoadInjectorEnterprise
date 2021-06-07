using LoadInjectorBase.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace LoadInjectorCommandCentre.Views {

    public class ClientTabControl : INotifyPropertyChanged {
        private ExecutionRecords tabRecords = new ExecutionRecords();

        public ExecutionRecords TabExecutionRecords {
            get {
                return tabRecords;
            }
            set {
                tabRecords = value;
                OnPropertyChanged("TabExecutionRecords");
            }
        }

        private string header;

        public string Header {
            get { return header; }
            set {
                header = value;
                OnPropertyChanged("Header");
            }
        }

        public MainCommandCenterController MainController { get; private set; }

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

        private string statusText = ClientState.UnAssigned.Value;

        public string StatusText {
            get {
                return $"{statusText}";
            }
            set {
                statusText = value;
                OnPropertyChanged("StatusText");
            }
        }

        public Visibility DetailVisibility {
            get {
                if (IsSummary) {
                    return Visibility.Collapsed;
                } else {
                    return Visibility.Visible;
                }
            }
        }

        public string Title {
            get {
                if (IsSummary) {
                    return "All Connected Nodes";
                } else {
                    return $"Execution Node: {IP}, {ProcessID}.  Work Package:  {WorkPackage}";
                }
            }
        }

        public bool IsSummary { get; set; }

        public string ConnectionID { get; set; }

        private string xml;

        public string XML {
            get {
                return xml;
            }
            set {
                xml = Utils.FormatXML(value);
                OnPropertyChanged("XML");
            }
        }

        private string consoleText;

        public string ConsoleText {
            get { return consoleText; }
            set {
                consoleText = value;
                OnPropertyChanged("ConsoleText");
            }
        }

        private string workPackage;

        public string WorkPackage {
            get {
                return workPackage;
            }
            set {
                workPackage = value;
                OnPropertyChanged("WorkPackage");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ClientTabControl(string tabTitle, MainCommandCenterController mainCommandCenterController) {
            this.Header = tabTitle;
            this.MainController = mainCommandCenterController;
        }

        public void AddUpdateExecutionRecord(ExecutionRecordClass rec) {
            try {
                Application.Current.Dispatcher.Invoke(delegate {
                    ExecutionRecordClass r = TabExecutionRecords.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == rec.ExecutionLineID);

                    if (r != null) {
                        r.MM = rec.MM;
                        r.Sent = rec.Sent;
                        r.Name = rec.Name;
                        r.Type = rec.Type;
                        OnPropertyChanged("TabExecutionRecords");
                    } else {
                        TabExecutionRecords.Add(rec);
                        OnPropertyChanged("TabExecutionRecords");
                    }
                });
            } catch (Exception ex) {
                Console.WriteLine("Unmanaged error.   " + ex.Message);
            }
        }

        internal void TabSelected() {
            if (MainController?.View.RecordsCollection != null) {
                MainController?.View?.RecordsCollection.Clear();
                foreach (ExecutionRecordClass rec in this.TabExecutionRecords) {
                    MainController?.View?.RecordsCollection.Add(rec);
                }
                MainController?.View?.OnPropertyChanged("ExecutionRecords");
            }
        }
    }
}