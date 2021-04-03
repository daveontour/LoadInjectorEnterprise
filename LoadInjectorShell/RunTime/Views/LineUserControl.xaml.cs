using LoadInjector.RunTime.Models;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using static LoadInjector.RunTime.Models.ControllerStatusReport;

namespace LoadInjector.RunTime {

    public partial class LineUserControl : UserControl, INotifyPropertyChanged {
        private string msgPerMin;
        public string msgPerMinExecution;
        private int msgSent;
        private string rate = "0.0";
        private string output = "Messages go here";

        private readonly string triggerType = "rate";

        private readonly SolidColorBrush redBrush = new SolidColorBrush {
            Color = Colors.Red
        };

        private readonly SolidColorBrush yellowBrush = new SolidColorBrush {
            Color = Colors.Orange
        };

        private readonly SolidColorBrush blackBrush = new SolidColorBrush {
            Color = Colors.Black
        };

        public IProgress<ControllerStatusReport> controllerProgress;
        public XmlNode node;

        public LineUserControl(XmlNode node, IProgress<ControllerStatusReport> controllerProgress) {
            this.node = node;
            this.controllerProgress = controllerProgress;

            DestType = node.Name;
            LineName = node.Attributes["name"].Value;
            try {
                LineType = node.Attributes["protocol"].Value;
            } catch (Exception) {
                LineType = "AMS Direct";
            }

            InitializeComponent();
            DataContext = this;
        }

        public LineUserControl(XmlNode node) : this(node, null) {
            controllerProgress = new Progress<ControllerStatusReport>(ControllerStatusChanged);
        }

        private void ControllerStatusChanged(ControllerStatusReport e) {
            Operation op = e.Type;

            if (op == Operation.DestinataionSendReport) {
                MsgSent = Convert.ToInt32(e.Sent);
                OnPropertyChanged("MsgSent");
                Rate = e.Actual.ToString(CultureInfo.CurrentCulture);
                OnPropertyChanged("Rate");

                return;
            }

            if ((op & Operation.Console) == Operation.Console) {
                SetOutput(e.OutputString);
            }

            if ((op & Operation.LineSent) == Operation.LineSent) {
                Sent(e.OutputInt);
            }
            if ((op & Operation.LineRate) == Operation.LineRate) {
                SetRate(e.OutputDouble);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public string LineName { get; set; }
        public string DestType { get; set; }
        public string LineType { get; set; }

        public string MsgPerMin {
            get {
                if (triggerType == "trigger") {
                    return "Event Triggered";
                } else {
                    return msgPerMin;
                }
            }
            set { msgPerMin = value; OnPropertyChanged("MsgPerMin"); }
        }

        public string MsgPerMinExecution {
            get {
                if (triggerType == "trigger") {
                    return "";
                } else if (msgPerMinExecution != null) {
                    return msgPerMinExecution;
                } else {
                    return msgPerMin;
                }
            }
            set {
                msgPerMinExecution = value;
                OnPropertyChanged("MsgPerMinExecution");
            }
        }

        public int MsgSent { get => msgSent; private set => msgSent = value; }

        public string Rate {
            get {
                if (triggerType == "trigger") {
                    return "";
                } else {
                    return rate;
                }
            }
            private set => rate = value;
        }

        public string Output { get => output; private set => output = value; }

        public void Sent(int num) {
            MsgSent = num;
            OnPropertyChanged("MsgSent");
        }

        public void SetRate(double rate) {
            if (triggerType == "trigger") {
                return;
            }
            Rate = rate.ToString(CultureInfo.CurrentCulture);
            OnPropertyChanged("Rate");
        }

        public void SetOutput(string output) {
            Output = output;
            if (output.StartsWith("Error")) {
                OutputText.Foreground = redBrush;
            } else if (output.StartsWith("Warning")) {
                OutputText.Foreground = yellowBrush;
            } else {
                OutputText.Foreground = blackBrush;
            }
            OnPropertyChanged("Output");
        }
    }
}