using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoadInjectorBase.Common {

    public struct LineRecord {
        public bool IsDestination, IsRateDriven, IsDataDriven, IsChained, IsSource;
        public string UUID, ExecutionNodeUUID, Name, DestinationType, SourceType;
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

    public class IterationRecords {
        public string ExecutionNodeID { get; set; }
        public string IPAddress { get; set; }
        public string ProcessID { get; set; }
        public string WorkPacakage { get; set; }

        private List<IterationRecord> iterationRecords = new List<IterationRecord>();
        public List<IterationRecord> Records { get; set; }

        public void Clear() {
            iterationRecords.Clear();
            WorkPacakage = null;
        }
    }
}