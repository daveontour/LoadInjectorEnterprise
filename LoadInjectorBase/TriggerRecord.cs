using LoadInjector.Filters;
using LoadInjectorBase;
using LoadInjectorBase.Interfaces;
using System;
using System.Collections.Generic;

namespace LoadInjector.RunTime
{
    public class TriggerRecord
    {
        public string lineUid;
        public bool refreshFlight;
        public IQueueFilter topLevelFilter;
        public Expression expression;
        public string baseTime;
        public Tuple<Dictionary<string, string>> record;
        public string uuid = Guid.NewGuid().ToString();

        public string ID { get; set; }

        public DateTime TIME { get; set; }

        public string DATA
        {
            get
            {
                string rec = "";

                if (isRelative)
                {
                    rec = $"Relative Trigger. {rec}";
                }

                if (record.Item1 != null)
                {
                    rec += "Data: ";
                    foreach (string key in record.Item1.Keys)
                    {
                        rec += $"{key}:{record.Item1[key]}; ";
                    }
                }
                return rec;
            }
        }

        private string status = "Queued";
        public bool isRelative;
        public List<IChainedSourceController> chain;

        public string STATUS
        {
            get => status;
            set => status = value;
        }

        public TriggerRecord(List<IChainedSourceController> chain, DateTime triggerTime, string baseTime, bool isRelative, string triggerID, Tuple<Dictionary<string, string>> record, string uid, bool refreshFlight, IQueueFilter topLevelFilter = null, Expression expression = null)
        {
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

        public override string ToString()
        {
            return $"Trigger Record. Data Only. ID: {ID}, Line ID: {lineUid}, Time: {TIME}";
        }
    }
}