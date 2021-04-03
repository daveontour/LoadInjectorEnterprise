using LoadInjector.RunTime.Models;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Xml;
using static LoadInjector.RunTime.Models.ControllerStatusReport;

namespace LoadInjector.RunTime {

    public partial class TriggeredEventsUI : UserControl, INotifyPropertyChanged {
        public XmlNode node;

        public readonly Progress<ControllerStatusReport> controllerProgress;
        public string LineName { get; set; }
        public string Output { get; private set; }
        public string ActualRate { get; private set; }
        public string ConfigRate { get; private set; }
        public double MessagesSent { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public TriggeredEventsUI(XmlNode node) {
            LineName = node.Attributes["name"].Value;
            InitializeComponent();
            DataContext = this;
            this.node = node;
            controllerProgress = new Progress<ControllerStatusReport>(ControllerStatusChanged);
        }

        private void ControllerStatusChanged(ControllerStatusReport e) {
            Operation op = e.Type;

            if ((op & Operation.Console) == Operation.Console) {
                Output = e.OutputString;
                OnPropertyChanged("Output");
            }
            if ((op & Operation.LineRate) == Operation.LineRate) {
                ActualRate = e.OutputString;
                OnPropertyChanged("ActualRate");
            }
            if ((op & Operation.LineConfigRate) == Operation.LineConfigRate) {
                ConfigRate = e.OutputString;
                OnPropertyChanged("ConfigRate");
            }
            if ((op & Operation.LineSent) == Operation.LineSent) {
                MessagesSent = e.OutputDouble;
                OnPropertyChanged("MessagesSent");
            }
        }
    }
}