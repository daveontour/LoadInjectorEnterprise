using LoadInjector.RunTime;
using LoadInjectorCommandCentre;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace LoadInjectorCommanCentre {

    public partial class MainWindow : Window, INotifyPropertyChanged {
        private CCController cccontroller;

        private ObservableCollection<ExecutionRecordClass> _myCollection = new ObservableCollection<ExecutionRecordClass>();
        private string filterNodeID;
        private ExecutionRecords _records;
        private int gridRefreshRate = 1;

        public ObservableCollection<ExecutionRecordClass> RecordsCollection {
            get { return this._records; }
        }

        public MainWindow() {
            InitializeComponent();
            DataContext = this;
            cccontroller = new CCController(this);

            _records = (ExecutionRecords)this.Resources["records"];
        }

        public void OnPropertyChanged(string propName) {
            try {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            } catch (Exception ex) {
                Console.WriteLine("On Property Error. " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

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
            SetFilterCriteria(null);
        }

        private void LocalClientBtn_OnClick(object sender, RoutedEventArgs e) {
            Process process = new Process();
            // Configure the process using the StartInfo properties.

            string lir = @"C:\Users\dave_\source\repos\LoadInjectorEnterprise\LoadInjectorRuntime\bin\Debug\LoadInjectorRuntime.exe";
            process.StartInfo.WorkingDirectory = @"C:\Users\dave_\source\repos\LoadInjectorEnterprise\LoadInjectorRuntime\bin\Debug";
            process.StartInfo.FileName = lir;
            process.StartInfo.Arguments = $"-server:http://localhost:6220/";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            process.Start();
        }

        public void AddUpdateExecutionRecord(ExecutionRecordClass rec) {
            try {
                ExecutionRecordClass r = (from record in RecordsCollection
                                          where rec.ExecutionLineID == record.ExecutionLineID
                                          select record).First();

                //The only thing that is changing is the messages sent and messages per minute
                r.MM = rec.MM;
                r.Sent = rec.Sent;
                OnPropertyChanged("RecordsCollection");
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                try {
                    Application.Current.Dispatcher.Invoke(delegate {
                        RecordsCollection.Add(rec);
                        // RecordsCollection = new ObservableCollection<ExecutionRecordClass>(RecordsCollection.OrderBy(i => i.IP).ThenBy(n => n.ProcessID).ThenBy(m => m.Type));
                        OnPropertyChanged("RecordsCollection");
                        statusGrid.Items.Refresh();
                    });
                } catch (Exception e1) {
                    Console.WriteLine("Dispatcher Distribute Error " + e1.Message);
                }
            }
        }

        public void SetFilterCriteria(string nodeID) {
            this.filterNodeID = nodeID;
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    RecordsCollection.Clear();
                    cccontroller.MessageHub.Hub.Clients.All.Refresh();
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
    }
}