using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Xml;

namespace LoadInjector.RunTime {

    public partial class ChainedEventsUI : UserControl, SourceUI, INotifyPropertyChanged {
        public XmlNode node;

        public readonly string executionNodeID;

        public readonly string uuid;

        public string LineName { get; set; }
        public string Output { get; private set; }
        public double ActualRate { get; private set; }
        public double ConfigRate { get; private set; }
        public int MessagesSent { get; private set; }

        public int SentSeqNumber { get; set; }

        public int ChainedDepth { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public ChainedEventsUI(XmlNode node, int chaindepth) {
            this.ChainedDepth = chaindepth;
            this.LineName = node.Attributes["name"].Value;
            InitializeComponent();
            this.DataContext = this;
            this.node = node;
            this.executionNodeID = node.Attributes["executionNodeUuid"]?.Value;
            this.uuid = node.Attributes["uuid"]?.Value;
        }

        protected void OnPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public int GetSentSeqNum() {
            return SentSeqNumber;
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

        public void SetMessagesSent(int output) {
            this.MessagesSent = output;
            SentSeqNumber = output;  // Used to protect against out of sequence Messages
            OnPropertyChanged("MessagesSent");
        }

        public void SeConfigRate(double s) {
            throw new NotImplementedException();
        }
    }
}