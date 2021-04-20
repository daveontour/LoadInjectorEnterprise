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
                Console.WriteLine($"Timers Clear Error {ex.Message}");
            }
            try {
                ClearHandlers();
            } catch (Exception ex) {
                Console.WriteLine($"Clear Handler Error {ex.Message}");
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
                        Console.WriteLine($"Error Firing trigger {e.Message}");
                        throw;
                    }
                } else {
                    throw new DispatcherNullException();
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error Firing trigger {ex.Message}");
                throw;
            }
        }

        public void DistributeMessage(TriggerRecord rec) {
            //Used by event driven sources
            this.executionController.clientHub.DispatcherDistributeMessage(executionController.executionNodeUuid, rec);
            //try {
            //    Application.Current.Dispatcher.Invoke(delegate // <--- HERE
            //    {
            //        exUI.SchedTriggers.Remove(rec);
            //        exUI.OnPropertyChanged("lvTriggers");
            //        exUI.FiredTriggers.Add(rec);
            //        exUI.OnPropertyChanged("lvFiredTriggers");
            //    });
            //} catch (Exception ex) {
            //    Console.WriteLine(ex.Message);
            //}

            if (rec.refreshFlight && rec.record.Item2 != null) {
                try {
                    FlightNode update = rec.record.Item2.RefeshFlight(executionController.amshost, executionController.token, executionController.apt_code).Result;
                    if (update != null)
                        rec.record.Item2.UpdateFlight(update);
                } catch (Exception) {
                    Console.WriteLine("Failed to update flight. Using original data");
                }
            }

            if (rec.record.Item2 != null) {
                if (rec.expression != null) {
                    bool pass = rec.expression.Pass(rec.record.Item2.FightXML);
                    if (!pass) {
                        Console.WriteLine("Post Filtering: Flight did not pass filter");
                        return;
                    }
                }
                if (rec.topLevelFilter != null) {
                    bool pass = rec.topLevelFilter.Pass(rec.record.Item2.FightXML);
                    if (!pass) {
                        Console.WriteLine("Post Filtering: Flight did not pass filter");
                        return;
                    }
                }
            }
            map[rec.ID]?.Fire(new TriggerFiredEventArgs(rec.ID, rec.record));

            // Fire any chained elements
            foreach (RateDrivenSourceController chain in rec.chain) {
                chain.ParentFired(rec.record);
            }
        }

        internal bool ScheduleEvent(TriggerRecord triggerRecord) {
            double tickTime = (triggerRecord.TIME - DateTime.Now).TotalMilliseconds;
            if (tickTime < 0) {
                return false;
            }
            triggerRecords.Add(triggerRecord);

            // Add the trigger to list in the the tabbed table
            this.executionController.clientHub.AddSchedTrigger(executionController.executionNodeUuid, triggerRecord);
            //try {
            //    Application.Current.Dispatcher.Invoke(delegate {
            //        exUI.SchedTriggers.Add(triggerRecord);
            //    });
            //} catch (Exception ex) {
            //    Console.WriteLine(ex.Message);
            //}
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
            //Application.Current.Dispatcher.Invoke(delegate {
            //    exUI.SchedTriggers.Clear();

            //    foreach (TriggerRecord t in sortedTriggers) {
            //        exUI.SchedTriggers.Add(t);
            //    }
            //    exUI.OnPropertyChanged("lvTriggers");
            //});

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
                Console.WriteLine($"Firing Trigger: {triggerRecord}");
                DistributeMessage(triggerRecord);
                try {
                    if (triggerQueue.Count == 0) {
                        return;
                    }
                    ScheduleNext(triggerQueue.Dequeue());
                } catch (Exception ex) {
                    Console.WriteLine($"Error dequeuing next event {ex.Message}");
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
            Console.WriteLine($"Started Event Timer for: {triggerRecord}");
        }

        public void Timer_Elapsed(TriggerRecord rec) {
            DistributeMessage(rec);
            try {
                ScheduleNext(triggerQueue.Dequeue());
            } catch (Exception ex) {
                Console.WriteLine($"Error dequeuing next event {ex.Message}");
            }
        }

        internal void AddMonitorHandler(string ID, RateDrivenSourceController rateSourceController) {
            try {
                if (!rateCntlMap.ContainsKey(rateSourceController)) {
                    rateCntlMap.Add(rateSourceController, rateSourceController);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Could not be added handler to map. {ID}. Error = {ex.Message}");
            }
            try {
                map[ID].TriggerFire += rateSourceController.TriggerHandler;
            } catch (Exception ex) {
                Console.WriteLine($"Monitor Handler could not be added for trigger {ID}. Error = {ex.Message}");
            }
        }

        internal void AddMonitorHandler(string trigger, DataDrivenSourceController execCntl) {
            try {
                if (!execCntlMap.ContainsKey(execCntl)) {
                    execCntlMap.Add(execCntl, execCntl);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Could not be added handler to map. {trigger}. Error = {ex.Message}");
            }
            try {
                map[trigger].TriggerFire += execCntl.TriggerHandler;
            } catch (Exception ex) {
                Console.WriteLine($"Monitor Handler could not be added for trigger {trigger}. Error = {ex.Message}");
            }
        }

        internal void AddAMSLineHandler(string trigger, AmsDirectExecutionController cntl) {
            try {
                if (!amsDirectCntlMap.ContainsKey(cntl)) {
                    amsDirectCntlMap.Add(cntl, cntl);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Could not be added handler to map. {trigger}. Error = {ex.Message}");
            }
            try {
                map[trigger].TriggerFire += cntl.TriggerHandler;
            } catch (Exception ex) {
                Console.WriteLine($"Listenter could not be added for trigger {trigger}. Error = {ex.Message}");
            }
        }

        internal void AddLineHandler(string trigger, LineExecutionController cntl) {
            try {
                if (!lineCntlMap.ContainsKey(cntl)) {
                    lineCntlMap.Add(cntl, cntl);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Could not be added handler to map. {trigger}. Error = {ex.Message}");
            }
            try {
                map[trigger].TriggerFire += cntl.TriggerHandler;
            } catch (Exception ex) {
                Console.WriteLine($"Listenter could not be added for trigger {trigger}. Error = {ex.Message}");
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