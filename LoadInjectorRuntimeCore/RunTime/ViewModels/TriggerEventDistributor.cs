using LoadInjectorBase;
using LoadInjectorBase.Interfaces;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Timers;
using System.Windows;

namespace LoadInjector.RunTime.ViewModels {

    public enum FlightType {
        Arrival,
        Departure,
        Both,
        None
    }

    public class TriggerFiredEventArgs : EventArgs {
        public string triggerID;
        public Tuple<Dictionary<string, string>, FlightNode> record;

        public TriggerFiredEventArgs(string triggerID, Tuple<Dictionary<string, string>, FlightNode> record) {
            TriggerName = triggerID;
            this.record = record;
            Flight = record.Item2;
        }

        public string TriggerName { get; set; }

        public FlightNode Flight { get; set; }

        public string FiredString(string name) {
            return $"[{DateTime.Now}] Source Line: {name}, Trigger: {TriggerName}\n";
        }
    }

    public class TriggerEventDistributor {
        private static TriggerEventDistributor instance;
        public ConcurrentDictionary<string, Dispatcher> map = new ConcurrentDictionary<string, Dispatcher>();
        public List<Timer> timers = new List<Timer>();
        private readonly Dictionary<DataDrivenSourceController, DataDrivenSourceController> execCntlMap = new Dictionary<DataDrivenSourceController, DataDrivenSourceController>();
        private readonly Dictionary<RateDrivenSourceController, RateDrivenSourceController> rateCntlMap = new Dictionary<RateDrivenSourceController, RateDrivenSourceController>();
        private readonly Dictionary<AmsDirectExecutionController, AmsDirectExecutionController> amsDirectCntlMap = new Dictionary<AmsDirectExecutionController, AmsDirectExecutionController>();
        private readonly Dictionary<LineExecutionController, LineExecutionController> lineCntlMap = new Dictionary<LineExecutionController, LineExecutionController>();
        public List<TriggerRecord> triggerRecords = new List<TriggerRecord>();
        public Queue<TriggerRecord> triggerQueue = new Queue<TriggerRecord>();

        //internal ExecutionUI exUI;
        private readonly NgExecutionController executionController;

        public static readonly Logger sourceLogger = LogManager.GetLogger("sourceLogger");

        public DateTime NextTrigger { get; set; }

        public static TriggerEventDistributor Instance => instance;

        public TriggerEventDistributor(NgExecutionController executionController) {
            this.executionController = executionController;
            instance = this;
        }

        public void Stop() {
            foreach (Timer timer in timers) {
                try {
                    timer.Stop();
                    timer.Close();
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
            try {
                timers.Clear();
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Timers Clear Error");
            }
            try {
                ClearHandlers();
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Clear Handler Error");
            }
        }

        // One place to hold all the dispatchers for each trigger
        public Dispatcher AddDispatcher(string triggerID) {
            if (map.ContainsKey(triggerID)) {
                return map[triggerID];
            } else {
                Dispatcher dispatcher = new Dispatcher();
                map.TryAdd(triggerID, dispatcher);
                return dispatcher;
            }
        }

        public void DistributeMessage(string triggerID, Tuple<Dictionary<String, String>, FlightNode> record) {
            // used by rate driven sources

            try {
                if (!map.ContainsKey(triggerID)) {
                    return;
                }
                Dispatcher dispatcher = map[triggerID];
                if (dispatcher != null) {
                    try {
                        dispatcher.Fire(new TriggerFiredEventArgs(triggerID, record));
                    } catch (Exception e) {
                        sourceLogger.Error(e, "Error Firing trigger");
                        throw;
                    }
                } else {
                    throw new DispatcherNullException();
                }
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Error Firing trigger");
                throw;
            }
        }

        public void DistributeMessage(TriggerRecord rec) {
            //Used by event driven sources

            if (rec.refreshFlight && rec.record.Item2 != null) {
                try {
                    FlightNode update = rec.record.Item2.RefeshFlight(executionController.amshost, executionController.token, executionController.apt_code).Result;
                    if (update != null)
                        rec.record.Item2.UpdateFlight(update);
                } catch (Exception e) {
                    sourceLogger.Warn(e, "Failed to update flight. Using original data");
                }
            }

            if (rec.record.Item2 != null) {
                if (rec.expression != null) {
                    bool pass = rec.expression.Pass(rec.record.Item2.FightXML);
                    if (!pass) {
                        sourceLogger.Trace("Post Filtering: Flight did not pass filter");
                        return;
                    }
                }
                if (rec.topLevelFilter != null) {
                    bool pass = rec.topLevelFilter.Pass(rec.record.Item2.FightXML);
                    if (!pass) {
                        sourceLogger.Trace("Post Filtering: Flight did not pass filter");
                        return;
                    }
                }
            }
            map[rec.ID]?.Fire(new TriggerFiredEventArgs(rec.ID, rec.record));

            // Fire any chained elements
            foreach (IChainedSourceController chain in rec.chain) {
                chain.ParentFired(rec.record);
            }

            this.executionController.clientHub.DispatcherDistributeMessage(executionController.executionNodeUuid, rec);
        }

        internal bool ScheduleEvent(TriggerRecord triggerRecord) {
            double tickTime = (triggerRecord.TIME - DateTime.Now).TotalMilliseconds;
            if (tickTime < 0) {
                return false;
            }
            triggerRecords.Add(triggerRecord);

            this.executionController.clientHub.AddSchedTrigger(executionController.executionNodeUuid, triggerRecord);

            return true;
        }

        internal Tuple<int, int, TriggerRecord> InitTriggers(int duration) {
            if (triggerRecords.Count == 0) {
                return new Tuple<int, int, TriggerRecord>(0, 0, null);
            }
            DateTime endOfTest = DateTime.Now.AddSeconds(duration);

            int lateTriggers = 0;
            int earlyTriggers = 0;

            List<TriggerRecord> sortedTriggers = new List<TriggerRecord>();

            DateTime now = DateTime.Now;

            foreach (TriggerRecord rec in triggerRecords) {
                // First, modify the time relative to start if it is a relative trigger.
                if (rec.isRelative) {
                    bool hasOffset = double.TryParse(rec.baseTime, out double offset);
                    if (hasOffset) {
                        rec.TIME = now.AddMilliseconds(offset * 1000);
                    }
                }

                if (rec.TIME > endOfTest) {
                    lateTriggers++;
                    continue;
                }
                if (rec.TIME < now) {
                    earlyTriggers++;
                    continue;
                }

                sortedTriggers.Add(rec);
            }

            sortedTriggers.Sort((x, y) => { return x.TIME.CompareTo(y.TIME); });

            this.executionController.clientHub.SetSchedTrigger(executionController.executionNodeUuid, sortedTriggers);

            triggerQueue = new Queue<TriggerRecord>(sortedTriggers);

            if (triggerQueue.Count > 0) {
                TriggerRecord first = triggerQueue.Peek();
                return new Tuple<int, int, TriggerRecord>(earlyTriggers, lateTriggers, first);
            } else {
                return null;
            }
        }

        public void ScheduleFirst() {
            try {
                if (triggerQueue.Count > 0) {
                    TriggerRecord first = triggerQueue.Dequeue();
                    if (first != null) {
                        ScheduleNext(first);
                    }
                }
            } catch (Exception) {
                // NO-OP
            }
        }

        public void ScheduleNext(TriggerRecord triggerRecord) {
            DateTime triggerTime = triggerRecord.TIME;
            double tickTime = (triggerTime - DateTime.Now).TotalMilliseconds;
            if (tickTime < 0) {
                sourceLogger.Info($"Firing Trigger: {triggerRecord}");
                DistributeMessage(triggerRecord);
                try {
                    if (triggerQueue.Count == 0) {
                        return;
                    }
                    ScheduleNext(triggerQueue.Dequeue());
                } catch (Exception ex) {
                    sourceLogger.Error(ex, "Error dequeuing next event (2) " + ex.Message);
                }
                return;
            }
            timers.Clear();
            Timer timer = new Timer(tickTime) {
                AutoReset = false
            };
            timer.Elapsed += (sender, e) => { Timer_Elapsed(triggerRecord); };
            timer.Start();
            timers.Add(timer);
            NextTrigger = triggerRecord.TIME;
            sourceLogger.Trace($"Started Event Timer for: {triggerRecord}");
        }

        public void Timer_Elapsed(TriggerRecord rec) {
            DistributeMessage(rec);
            try {
                if (triggerQueue.Count == 0) {
                    return;
                }
                ScheduleNext(triggerQueue.Dequeue());
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Error dequeuing next event (1)");
            }
        }

        internal void AddMonitorHandler(string ID, RateDrivenSourceController rateSourceController) {
            try {
                if (!rateCntlMap.ContainsKey(rateSourceController)) {
                    rateCntlMap.Add(rateSourceController, rateSourceController);
                }
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Could not be added handler to map. {ID}.");
            }
            try {
                map[ID].TriggerFire += rateSourceController.TriggerHandler;
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Monitor Handler could not be added for trigger {ID}.");
            }
        }

        internal void AddMonitorHandler(string trigger, DataDrivenSourceController execCntl) {
            try {
                if (!execCntlMap.ContainsKey(execCntl)) {
                    execCntlMap.Add(execCntl, execCntl);
                }
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Could not be added handler to map. {trigger}.");
            }
            try {
                map[trigger].TriggerFire += execCntl.TriggerHandler;
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Monitor Handler could not be added for trigger {trigger}.");
            }
        }

        internal void AddAMSLineHandler(string trigger, AmsDirectExecutionController cntl) {
            try {
                if (!amsDirectCntlMap.ContainsKey(cntl)) {
                    amsDirectCntlMap.Add(cntl, cntl);
                }
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Could not be added handler to map. {trigger}.");
            }
            try {
                map[trigger].TriggerFire += cntl.TriggerHandler;
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Listener could not be added for trigger {trigger}.");
            }
        }

        internal void AddLineHandler(string trigger, LineExecutionController cntl) {
            try {
                if (!lineCntlMap.ContainsKey(cntl)) {
                    lineCntlMap.Add(cntl, cntl);
                }
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Could not be added handler to map. {trigger}.");
            }
            try {
                map[trigger].TriggerFire += cntl.TriggerHandler;
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Listener could not be added for trigger {trigger}.");
            }
        }

        public void ClearHandlers() {
            triggerQueue.Clear();
            triggerRecords.Clear();

            foreach (Dispatcher d in map.Values) {
                foreach (DataDrivenSourceController execCntl in execCntlMap.Values) {
                    try {
                        d.TriggerFire -= execCntl.TriggerHandler;
                    } catch (Exception) {
                        // NO-OP
                    }
                }
                foreach (RateDrivenSourceController execCntl in rateCntlMap.Values) {
                    try {
                        d.TriggerFire -= execCntl.TriggerHandler;
                    } catch (Exception) {
                        // NO-OP
                    }
                }
                foreach (AmsDirectExecutionController execCntl in amsDirectCntlMap.Values) {
                    try {
                        d.TriggerFire -= execCntl.TriggerHandler;
                    } catch (Exception) {
                        // NO-OP
                    }
                }
                foreach (LineExecutionController execCntl in lineCntlMap.Values) {
                    try {
                        d.TriggerFire -= execCntl.TriggerHandler;
                    } catch (Exception) {
                        // NO-OP
                    }
                }
            }
        }
    }
}