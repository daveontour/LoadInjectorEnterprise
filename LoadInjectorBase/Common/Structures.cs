using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadInjectorBase.Common {

    public struct LineRecord {
        public bool IsDestination, IsRateDriven, IsDataDriven, IsChained, IsSource;
        public string UUID, ExecutionNodeUUID, Name, DestinationType, SourceType, Description;
        public int MessagesSent, MessagesFailed, ExecutionElapsedTime;
        public DateTime ExecutionStart, ExecutionEnd;
    }

    public class IterationRecord {
        private List<LineRecord> sourceLineRecords = new List<LineRecord>();
        private List<LineRecord> destinationLineRecords = new List<LineRecord>();

        public List<LineRecord> SourceLineRecords {
            get { return sourceLineRecords; }
            set { sourceLineRecords = value; }
        }

        public List<LineRecord> DestinationLineRecords {
            get { return destinationLineRecords; }
            set { destinationLineRecords = value; }
        }

        public DateTime ExecutionStart, ExecutionEnd;
        public string UUID { get; set; }
        public int IterationNumber { get; set; }
    }

    public class CompletionReport {
        public string ExecutionNodeID { get; set; }
        public string IPAddress { get; set; }
        public string ProcessID { get; set; }
        public string WorkPacakage { get; set; }

        private List<IterationRecord> iterationRecords = new List<IterationRecord>();
        public List<IterationRecord> Records { get { return iterationRecords; } set { iterationRecords = value; } }

        public void Clear() {
            iterationRecords.Clear();
            WorkPacakage = null;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("{0,27}{1,15}", "Execution Node IP:", IPAddress));
            sb.AppendLine(string.Format("{0,27}{1,15}", "Execution Node Process ID:", ProcessID));
            sb.AppendLine(string.Format("{0,27}{1,15}", "Work Package:", WorkPacakage));

            int index = 0;
            foreach (IterationRecord itRec in Records) {
                index++;
                if (Records.Count() > 1) {
                    sb.AppendLine($"\nExecution Iteration Number: {index}");
                }

                sb.AppendLine(string.Format("{0, -20}{1}", "Start of Execution:", itRec.ExecutionStart));
                sb.AppendLine(string.Format("{0, -20}{1}", "End of Execution:", itRec.ExecutionEnd));

                sb.AppendLine("\nSources:");
                sb.AppendLine(string.Format("{0, -15}{1, -30}{2, -15} Source", "Type", "Description", "Trigers Fired"));
                foreach (LineRecord rec in itRec.SourceLineRecords) {
                    sb.AppendLine(string.Format("{0, -15}{1, -30}{2, -15} {3}", rec.SourceType, rec.Name, rec.MessagesSent, rec.Description));
                }

                sb.AppendLine("\nDestination:");
                sb.AppendLine(string.Format("{0, -15}{1, -30}{2, -15}{3, -15} Destination", "Type", "Description", "Messages Sent", "Messages Failed"));
                foreach (LineRecord rec in itRec.DestinationLineRecords) {
                    sb.AppendLine(string.Format("{0, -15}{1, -30}{2, -15}{3,-15} {4}", rec.DestinationType, rec.Name, rec.MessagesSent, rec.MessagesFailed, rec.Description));
                }
            }

            return sb.ToString();
        }
    }
}