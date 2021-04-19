using LoadInjector.RunTime.Models;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Xml;
using static LoadInjector.RunTime.Models.ControllerStatusReport;

namespace LoadInjector.RunTime {

    public partial class ChainedEventsUI : UserControl, INotifyPropertyChanged {
        public XmlNode node;
        public readonly Progress<ControllerStatusReport> controllerProgress;
        public readonly string executionNodeID;
        public readonly string uuid;

        public string LineName { get; set; }
        public string Output { get; private set; }
        public double ActualRate { get; private set; }
        public double ConfigRate { get; private set; }
        public double MessagesSent { get; private set; }

        public int ChainedDepth { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ChainedEventsUI(XmlNode node, int chaindepth) {
            this.ChainedDepth = chaindepth;
            this.LineName = node.Attributes["name"].Value;
            InitializeComponent();
            this.DataContext = this;
            this.node = node;
            // this.controllerProgress = new Progress<ControllerStatusReport>(ControllerStatusChanged);
            this.executionNodeID = node.Attributes["executionNodeUuid"]?.Value;
            this.uuid = node.Attributes["uuid"]?.Value;
        }

        //private void ControllerStatusChanged(ControllerStatusReport e) {
        //    Operation op = e.Type;

        //    if (op == Operation.LineReport) {
        //        Output = e.Consolestr;
        //        ActualRate = e.Actual;
        //        ConfigRate = e.Config;
        //        MessagesSent = e.Sent;

        //        OnPropertyChanged("Output");
        //        OnPropertyChanged("ActualRate");
        //        OnPropertyChanged("ConfigRate");
        //        OnPropertyChanged("MessagesSent");

        //        return;
        //    }

        //    if ((op & Operation.Console) == Operation.Console) {
        //        SetOutput(e.OutputString);
        //    }
        //    if ((op & Operation.LineRate) == Operation.LineRate) {
        //        SetActualRate(e.OutputDouble);
        //    }
        //    if ((op & Operation.LineConfigRate) == Operation.LineConfigRate) {
        //        SetConfigRate(e.OutputDouble);
        //    }
        //    if ((op & Operation.LineSent) == Operation.LineSent) {
        //        SetMessagesSent(e.Sent);
        //    }
        //}

        protected void OnPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void SetOutput(string output) {
            this.Output = output;
            OnPropertyChanged("Output");
        }

        public void SetActualRate(double output) {
            this.ActualRate = output;
            OnPropertyChanged("ActualRate");
        }

        public void SetConfigRate(double output) {
            this.ConfigRate = output;
            OnPropertyChanged("ConfigRate");
        }

        public void SetMessagesSent(double output) {
            this.MessagesSent = output;
            OnPropertyChanged("MessagesSent");
        }
    }
}