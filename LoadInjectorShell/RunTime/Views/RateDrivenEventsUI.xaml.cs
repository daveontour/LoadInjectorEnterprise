using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Xml;

namespace LoadInjector.RunTime {

    public partial class RateDrivenEventsUI : UserControl, SourceUI, INotifyPropertyChanged {
        public XmlNode node;

        public string LineName { get; set; }

        public string Output { get; private set; }
        public double ActualRate { get; private set; }
        public double ConfigRate { get; private set; }
        public double MessagesSent { get; private set; }

        public int ChainedDepth { get; private set; }

        public int SentSeqNumber { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public RateDrivenEventsUI(XmlNode node, int chaindepth) {
            ChainedDepth = chaindepth;
            LineName = node.Attributes["name"].Value;
            for (int i = 0; i < chaindepth; i++) {
                LineName = "-> " + LineName;
            }
            InitializeComponent();
            DataContext = this;
            this.node = node;
        }

        protected void OnPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public void SetOutput(string s) {
            Output = s;
            OnPropertyChanged("Output");
        }

        public void SetActualRate(double s) {
            ActualRate = s;
            OnPropertyChanged("ActualRate");
        }

        public void SeConfigRate(double s) {
            ConfigRate = s;
            OnPropertyChanged("ConfigRate");
        }

        public void SetMessagesSent(int s) {
            SentSeqNumber = s;
            MessagesSent = s;
            OnPropertyChanged("MessagesSent");
        }

        public int GetSentSeqNum() {
            return SentSeqNumber;
        }
    }
}