using LoadInjectorBase.Common;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace LoadInjectorCommandCentre.Views {

    public class SummaryTabControl : INotifyPropertyChanged {
        private ExecutionRecords tabRecords = new ExecutionRecords();

        public ExecutionRecords TabRecords {
            get {
                return tabRecords;
            }
            set {
                tabRecords = value;
                OnPropertyChanged("TabRecords");
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

        public MainCommandCenterController MainController { get; set; }

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SummaryTabControl(string tabTitle, MainCommandCenterController mainCommandCenterController) {
            this.Header = tabTitle;
            this.MainController = mainCommandCenterController;
        }

        public void AddUpdateExecutionRecord(ExecutionRecordClass rec) {
            try {
                Application.Current.Dispatcher.Invoke(delegate {
                    ExecutionRecordClass r = TabRecords.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == rec.ExecutionLineID);

                    if (r != null) {
                        r.MM = rec.MM;
                        r.Sent = rec.Sent;
                        r.Name = rec.Name;
                        r.Type = rec.Type;
                        OnPropertyChanged("TabRecords");
                    } else {
                        TabRecords.Add(rec);
                        OnPropertyChanged("TabRecords");
                    }
                });
            } catch (Exception ex) {
                Console.WriteLine("Unmanaged error.   " + ex.Message);
            }
        }

        internal void TabSelected() {
            MainController.View.RecordsCollection.Clear();
            foreach (ExecutionRecordClass rec in this.TabRecords) {
                MainController.View.RecordsCollection.Add(rec);
            }
            MainController.View.OnPropertyChanged("ExecutionRecords");
        }
    }
}