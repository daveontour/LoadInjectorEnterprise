using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public ObservableCollection<ExecutionRecordClass> RecordsCollection {
            get { return this._myCollection; }
        }

        public MainWindow() {
            //RecordsCollection.Add(new ExecutionRecordClass() {
            //    IP = "127.12.12.12",
            //    ProcessID = "12344",
            //    Type = "Source",
            //    Name = "Flight",
            //    ConfigMM = "60",
            //    MM = "62.5"
            //});

            InitializeComponent();
            DataContext = this;
            cccontroller = new CCController(this);
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
            cccontroller.StopAll();
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
                        OnPropertyChanged("RecordsCollection");
                        statusGrid.Items.Refresh();
                    });
                } catch (Exception e1) {
                    Console.WriteLine("Dispatcher Distribute Error " + e1.Message);
                }
            }
        }
    }
}