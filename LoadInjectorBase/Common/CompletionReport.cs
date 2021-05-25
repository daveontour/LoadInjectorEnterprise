using System.Collections.Generic;
using System.Text;

namespace LoadInjectorBase.Common {

    public class CompletionReport {

        private List<IterationCompletionReport> iteratiopnReports = new List<IterationCompletionReport>();
        private string executionNodeID;
        private string connectionID;
        private string processID;
        private string IP;

        public List<IterationCompletionReport> IteratiopnReports { get => iteratiopnReports; set => iteratiopnReports = value; }
        public string ExecutionNodeID { get => executionNodeID; set => executionNodeID = value; }
        public string ConnectionID { get => connectionID; set => connectionID = value; }

        public CompletionReport(string nodeID, string IP, string processID) {
            this.ExecutionNodeID = nodeID;
            this.IP = IP;
            this.processID = processID;
        }

        public override string ToString() {

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("{0,27}{1,15}", "Execution Node ID:", ExecutionNodeID));
            sb.AppendLine(string.Format("{0,27}{1,15}", "Execution Node IP:", IP));
            sb.AppendLine(string.Format("{0,27}{1,15}", "Execution Node Process ID:", processID));



            return sb.ToString();
        }
    }

    public class SourceReport {
        public string name;
        public string type;
        public string lineUUID;
        public string executionNodeUUID;
        public double configMessageRate;
        public double actualMessageRate;
        public int messagesSent;
        public List<SourceReport> chainedSources = new List<SourceReport>();
    }
    public class DestinationReport {
        public string name;
        public string type;
        public string lineUUID;
        public string executionNodeUUID;
        public double actualMessageRate;
        public int messagesSent;
    }
    public class IterationCompletionReport {
        public int iterationNumber;
        public List<SourceReport> sourceReports = new List<SourceReport>();
        public List<DestinationReport> destinationReports = new List<DestinationReport>();

    }


}
