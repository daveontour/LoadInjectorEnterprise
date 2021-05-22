using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LoadInjectorCommandCentre {

    public class ExecutionRecords : ObservableCollection<ExecutionRecordClass> {
        // Creating the Records collection in this way enables data binding from XAML.
    }

    public class ExecutionRecordClass : INotifyPropertyChanged {
        private string ip;
        private string processID;
        private string name;
        private string type;
        private string configMM;
        private string mM;
        private int sent;
        private string protocol;
        private string connectionID;

        private string executionNodeID;
        private string executionLineID;
        private string sourceDestination;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName) {
            //if (PropertyChanged != null) {
            //    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            //}
        }

        public string GroupID {
            get { return $"Execution Node IP: {IP}, ProcessID: {ProcessID}"; }
        }

        public string SourceDestination {
            get { return sourceDestination; }
            set {
                sourceDestination = value;
                NotifyPropertyChanged("SourceDestination");
            }
        }

        public string ConnectionID {
            get { return connectionID; }
            set {
                connectionID = value;
            }
        }

        public string ExecutionNodeID {
            get { return executionNodeID; }
            set {
                executionNodeID = value;
                NotifyPropertyChanged("ExecutionNodeID");
            }
        }

        public string ExecutionLineID {
            get { return executionLineID; }
            set {
                executionLineID = value;
                NotifyPropertyChanged("ExecutionLineID");
            }
        }

        public string IP {
            get { return ip; }
            set {
                ip = value;
                NotifyPropertyChanged("IP");
            }
        }

        public string ProcessID {
            get { return processID; }
            set {
                processID = value;
                NotifyPropertyChanged("ProcessID");
            }
        }

        public string Name {
            get { return name; }
            set {
                name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public string Protocol {
            get { return protocol; }
            set {
                protocol = value;
                NotifyPropertyChanged("Protocol");
            }
        }

        public string Type {
            get { return type; }
            set {
                type = value;
                NotifyPropertyChanged("Type");
            }
        }

        public string ConfigMM {
            get { return configMM; }
            set {
                configMM = value;
                NotifyPropertyChanged("ConfigMM");
            }
        }

        public string MM {
            get { return mM; }
            set {
                mM = value;
                NotifyPropertyChanged("MM");
            }
        }

        public int Sent {
            get { return sent; }
            set {
                sent = value;
                NotifyPropertyChanged("Sent");
            }
        }
    }
}