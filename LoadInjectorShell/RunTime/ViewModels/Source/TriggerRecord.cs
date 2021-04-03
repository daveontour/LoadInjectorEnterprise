using LoadInjector.Filters;
using System;
using System.Collections.Generic;

namespace LoadInjector.RunTime {

    public class TriggerRecord {
        public string lineUid;
        public bool refreshFlight;
        public IQueueFilter topLevelFilter;
        public Expression expression;
        public string baseTime;
        public Tuple<Dictionary<string, string>, FlightNode> record;

        public string ID { get; set; }

        public DateTime TIME { get; set; }

        public string DATA {
            get {
                string rec = "";
                if (record.Item2 != null) {
                    rec = $"Flight: {record.Item2.airlineCode}{record.Item2.fltNumber} at {record.Item2.dateTime}  {record.Item2.departureAirport}->{record.Item2.arrivalAirport}. ";
                }
                if (isRelative) {
                    rec = $"Relative Trigger. {rec}";
                }

                if (record.Item1 != null) {
                    rec += "Data: ";
                    foreach (string key in record.Item1.Keys) {
                        rec += $"{key}:{record.Item1[key]}; ";
                    }
                }
                return rec;
            }
        }

        private string status = "Queued";
        public bool isRelative;
        public List<RateDrivenSourceController> chain;

        public string STATUS {
            get => status;
            set => status = value;
        }

        public TriggerRecord(List<RateDrivenSourceController> chain, DateTime triggerTime, string baseTime, bool isRelative, string triggerID, Tuple<Dictionary<string, string>, FlightNode> record, string uid, bool refreshFlight, IQueueFilter topLevelFilter = null, Expression expression = null) {
            TIME = triggerTime;
            ID = triggerID;
            this.record = record;
            lineUid = uid;
            this.refreshFlight = refreshFlight;
            this.topLevelFilter = topLevelFilter;
            this.expression = expression;
            this.baseTime = baseTime;
            this.isRelative = isRelative;
            this.chain = chain;
        }

        public override string ToString() {
            if (record.Item1 != null && record.Item2 != null) {
                return $"Trigger Record. Data and Flight. ID: {ID}, Line ID: {lineUid}, Flight: {record.Item2.airlineCode}{record.Item2.fltNumber}, Time: {TIME}";
            }
            if (record.Item1 == null && record.Item2 != null) {
                return $"Trigger Record. Flight Only. ID: {ID}, Line ID: {lineUid}, Flight: {record.Item2.airlineCode}{record.Item2.fltNumber}, Time: {TIME}";
            }
            if (record.Item1 != null && record.Item2 == null) {
                return $"Trigger Record. Data Only. ID: {ID}, Line ID: {lineUid}, Time: {TIME}";
            }

            return "Trigger Data Error";
        }
    }
}