namespace LoadInjectorCommanCentre {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public class ExecutionRecordClass {
        private string ip;
        private string processID;
        private string name;
        private string type;
        private string configMM;
        private string mM;
        private int sent;

        private string executionNodeID;
        private string executionLineID;

        public string ExecutionNodeID {
            get { return executionNodeID; }
            set { executionNodeID = value; }
        }

        public string ExecutionLineID {
            get { return executionLineID; }
            set { executionLineID = value; }
        }

        public string IP {
            get { return ip; }
            set { ip = value; }
        }

        public string ProcessID {
            get { return processID; }
            set { processID = value; }
        }

        public string Name {
            get { return name; }
            set { name = value; }
        }

        public string Type {
            get { return type; }
            set { type = value; }
        }

        public string ConfigMM {
            get { return configMM; }
            set { configMM = value; }
        }

        public string MM {
            get { return mM; }
            set { mM = value; }
        }

        public int Sent {
            get { return sent; }
            set { sent = value; }
        }
    }
}