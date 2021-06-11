using LoadInjector.RunTime.ViewModels;
using LoadInjector.RuntimeCore;
using LoadInjectorBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace LoadInjector.RunTime {

    public class RateDrivenSourceController : SourceControllerAbstract, LoadInjectorBase.Interfaces.IChainedSourceController {
        internal bool ConfigOK = true;

        private System.Timers.Timer sendTimer;
        private readonly List<System.Timers.Timer> resetTimers = new List<System.Timers.Timer>();

        private int index;

        private readonly double deferredStart;
        private int intervalCount;
        private double intervalMessagesPerMinute;
        private readonly Stopwatch intervalStopWatch = new Stopwatch();
        private readonly Stopwatch executionStopWatch = new Stopwatch();

        public string MsgPerMin { get; private set; }

        private double interval;
        private readonly int maxNumMessages;
        private readonly int maxRunTime;
        public bool isChained;
        public bool useParentData;
        public int chainDelay;
        private readonly string maxMsgPerMinuteProfile;
        private readonly bool abortOnListEnd;
        private readonly bool sequentialFlight = true;
        public int messagesSent;

        public bool prepOK = true;
        public bool finished;
        private bool STOP;
        private readonly bool abortOnFlightListEnd;

        public RateDrivenSourceController(XmlNode node, int chainDepth, List<string> triggersInUse, int serverOffset, NgExecutionController executionController) : base(node, chainDepth, triggersInUse, serverOffset, executionController) {
            if (chainDepth > 0) {
                isChained = true;
            }
            maxMsgPerMinuteProfile = node.Attributes["maxMsgPerMinuteProfile"]?.Value;

            try {
                var v = node.Attributes["messagesPerMinute"]?.Value;
                messagesPerMinute = (v == null) ? 60.0 : double.Parse(v);
            } catch (Exception) {
                messagesPerMinute = 60.0;
            }
            intervalMessagesPerMinute = messagesPerMinute;

            try {
                interval = 1000 * (60.0 / messagesPerMinute);
            } catch (Exception) {
                interval = 1000 * (60.0);
            }

            try {
                var v = node.Attributes["maxNumMessages"]?.Value;
                maxNumMessages = (v == null) ? -1 : int.Parse(v);
            } catch (Exception) {
                maxNumMessages = -1;
            }

            try {
                var v = node.Attributes["stopOnDataEnd"]?.Value;
                abortOnListEnd = (v != null) && bool.Parse(v);
            } catch (Exception) {
                abortOnListEnd = false;
            }

            try {
                var v = node.Attributes["sequentialFlight"]?.Value;
                sequentialFlight = (v != null) && bool.Parse(v);
            } catch (Exception) {
                sequentialFlight = false;
            }

            try {
                var v = node.Attributes["delay"]?.Value;
                chainDelay = (v == null) ? 0 : int.Parse(v);
            } catch (Exception) {
                chainDelay = 0;
            }

            try {
                var v = node.Attributes["useParentData"]?.Value;
                useParentData = (v != null) && bool.Parse(v);
            } catch (Exception) {
                useParentData = false;
            }

            try {
                var v = node.Attributes["stopOnFlightDataEnd"]?.Value;
                abortOnFlightListEnd = (v != null) && bool.Parse(v);
            } catch (Exception) {
                abortOnFlightListEnd = false;
            }

            try {
                var v = node.Attributes["maxRunTime"]?.Value;
                if (v != null) {
                    maxRunTime = int.Parse(v);
                } else {
                    maxRunTime = -1;
                }
            } catch (Exception) {
                maxRunTime = -1;
            }

            try {
                var v = node.Attributes["deferredStart"]?.Value;
                if (v != null) {
                    deferredStart = double.Parse(v);
                } else {
                    deferredStart = 0.0;
                }
            } catch (Exception) {
                deferredStart = 0.0;
            }

            if (id != null) {
                if (triggersInUse.Contains(id)) {
                    lineInUse = true;
                } else {
                    lineInUse = false;
                }
            } else {
                lineInUse = false;
            }
        }

        public void Start() {
            finished = false;
            STOP = false;

            Report("Execute", 0, 0, messagesPerMinute);
            intervalStopWatch?.Reset();
            executionStopWatch?.Reset();
            sourceLogger.Info($"Scheduling Start of Rate Source Controller: {name}");
            CancellationTokenSource source = new CancellationTokenSource();

            if (lineInUse) {
                Task.Run(async delegate {
                    await Task.Delay(TimeSpan.FromSeconds(deferredStart), source.Token);
                    logger.Info($"Starting Rate Source Controller: {name}");
                    messagesSent = 0;
                    intervalCount = 0;
                    PrepareProfileThreads();  //Set up the threads which modify the rate profile/interval
                    executionStopWatch.Start();
                    intervalStopWatch.Start();

                    sourceLogger.Info($"Starting timer with interval {interval}");
                    sendTimer = new System.Timers.Timer(interval) {
                        Enabled = true,
                        AutoReset = false
                    };
                    sendTimer.Elapsed += FireEvent;
                });
            } else {
                sourceLogger.Warn($"No Destination configured to use Rate Source Controller: {name}");
            }
        }

        public void ParentFired(Tuple<Dictionary<string, string>, FlightNode> data = null) {
            // Called by the parent in a chain when it fires.
            // This schedules the firing of the trigger for this chain link after the delay time

            if (!useParentData) {
                data = Next();
            }

            Task.Run(async delegate {
                sendTimer = new System.Timers.Timer(chainDelay * 1000.0) {
                    Enabled = true,
                    AutoReset = false
                };
                sendTimer.Elapsed += (sender, e) => OnParentFiredTimedEvent(data);
            });
        }

        private void OnParentFiredTimedEvent(Tuple<Dictionary<string, string>, FlightNode> data) {
            // Handler for the firing due to a parent

            try {
                eventDistributor.DistributeMessage(id, data);
                foreach (RateDrivenSourceController child in chainedController) {
                    child.ParentFired(data);
                }
            } catch (Exception ex) {
                sourceLogger.Error(ex, "On Parent Fire error");
            }

            messagesSent++;

            try {
                ReportChain(name, messagesSent);
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Set Output problem.");
            }
        }

        // The sendTimer calls this function each time the timer goes off
        private void FireEvent(object sender, ElapsedEventArgs e) {
            //Unset it if run parameters exceeded
            if (maxRunTime > 0 && executionStopWatch.ElapsedMilliseconds / 1000 >= maxRunTime) {
                sendTimer.Enabled = false;
                sendTimer.Stop();
                sourceLogger.Info($"Rate Source {name}. Maximum Run Time Reached {messagesSent} Messages Sent");
                SetSourceLineOutput($"Maximum Run Time Reached {messagesSent} Messages Sent");
                return;
            }

            if (maxNumMessages > 0 && messagesSent >= maxNumMessages) {
                sendTimer.Enabled = false;
                sendTimer.Stop();
                sourceLogger.Info($"Rate Source {name}. Maximum Number of Messages Reached. {messagesSent} Messages Sent");
                SetSourceLineOutput($"Maximum Number of Messages Reached. {messagesSent} Messages Sent");
                return;
            }

            // Reset the interval timer
            double avg = intervalStopWatch.Elapsed.TotalMilliseconds / intervalCount;
            double currentRate = RoundToSignificantDigits(60000 / avg, 2);
            if (currentRate < intervalMessagesPerMinute) {
                sourceLogger.Trace($"Re scheduling timer with interval {interval / 4}");
                sendTimer.Interval = interval / 4;
            } else {
                sourceLogger.Trace($"Re scheduling timer with interval {interval * 1.1}");
                sendTimer.Interval = interval * 1.1;
            }

            sendTimer.Enabled = true;

            bool immediateSent = false;
            if (intervalMessagesPerMinute > 3800 && currentRate < intervalMessagesPerMinute && !STOP) {
                immediateSent = true;
                sendTimer.Enabled = false;
            }

            // Get the data for this iteration and send it to the event distributor which will notify all interested destinations

            try {
                Tuple<Dictionary<string, string>, FlightNode> data = Next();
                if (data == null) {
                    Console.WriteLine($"Source:{name}.No further data available");
                    SetSourceLineOutput("No further data available");
                    finished = true;
                    sendTimer.Enabled = false;
                    executionController.CheckForCompletion();
                    return;
                }
                messagesSent++;
                intervalCount++;
                avg = intervalStopWatch.Elapsed.TotalMilliseconds / intervalCount;
                currentRate = RoundToSignificantDigits(60000 / avg, 2);

                try {
                    eventDistributor.DistributeMessage(id, data);
                } catch (Exception ex) {
                    sourceLogger.Error(ex, "Distributor Error");
                }
                try {
                    foreach (RateDrivenSourceController child in chainedController) {
                        child.ParentFired(data);
                    }
                } catch (Exception ex) {
                    sourceLogger.Error(ex, "Firing Child Error");
                }
                try {
                    Report(name, messagesSent, currentRate, messagesPerMinute);
                } catch (Exception ex) {
                    sourceLogger.Error(ex, "Set Output Problem.");
                }
            } catch (DispatcherNullException ex) {
                sourceLogger.Error(ex, $"Source:{name}. NULL DISPATCHER");
                SetSourceLineOutput("NULL DISPATCHER");
            } catch (FilterFailException) {
                sourceLogger.Error($"Source:{name}. Post Filtering. Iteration Flight did not pass the configured filter");
                SetSourceLineOutput("Iteration Flight did not pass the configured filter");
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Error Distributing Rate Message");
            }

            if (immediateSent && !STOP) {
                FireEvent(null, null);
            }
        }

        //private void Report(string v, int messagesSent, double currentRate, double messagesPerMinute) {
        //    if (currentRate < Parameters.MAXREPORTRATE || messagesSent % Parameters.REPORTEPOCH == 0) {
        //        ControllerStatusReport report = new ControllerStatusReport() {
        //            Consolestr = v,
        //            Sent = messagesSent,
        //            Actual = currentRate,
        //            Config = messagesPerMinute,
        //            Type = Operation.LineReport
        //        };

        //        lineProgress.Report(report);
        //    }
        //}

        //private void ReportChain(string v, int messagesSent) {
        //    ControllerStatusReport report = new ControllerStatusReport() {
        //        Consolestr = v,
        //        Sent = messagesSent,
        //        Type = Operation.LineSent
        //    };

        //    lineProgress.Report(report);
        //}

        public double RoundToSignificantDigits(double d, int digits) {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, digits);

            double dd = d * scale;
            double di = Math.Round(dd);
            return di / scale;
        }

        public bool Prepare(List<FlightNode> flights, List<FlightNode> arrflights, List<FlightNode> depflights) {
            // Report("Prepare", 0, 0, messagesPerMinute);

            if (!lineInUse) {
                SetSourceLineOutput("No destination lines are configured to use this source");
                return true;
            }

            if (flightSourceType != null && flightSourceType != "none") {
                PrepareFlights(flights, arrflights, depflights);
            }

            prepOK = true;

            switch (dataSourceType) {
                case "CSV":
                    prepOK = PrepareCSV();
                    break;

                case "DATABASE":
                case "MSSQL":
                case "MySQL":
                case "ORACLE":
                    prepOK = PrepareDB();
                    break;

                case "XML":
                    prepOK = PrepareXML();
                    break;

                case "Excel":
                    prepOK = PrepareExcel();
                    break;

                case "JSON":
                    prepOK = PrepareJSON();
                    break;

                case "PULSAR":
                    prepOK = true;
                    break;
            }

            if (!prepOK) {
                return false;
            }

            eventDistributor.AddDispatcher(id);
            eventDistributor.AddMonitorHandler(id, this);

            foreach (RateDrivenSourceController chain in chainedController) {
                chain.Prepare(flights, arrflights, depflights);
            }

            return prepOK;
        }

        public Tuple<Dictionary<string, string>, FlightNode> Next() {
            if (dataSourceType == "PULSAR") {
                return new Tuple<Dictionary<string, string>, FlightNode>(new Dictionary<string, string>(), new FlightNode());
            }
            Dictionary<string, string> data = null;
            FlightNode flt = null;

            try {
                if (dataRecords.Count != 0) {
                    if (index >= dataRecords.Count && abortOnListEnd) {
                        return null;
                    }
                    data = dataRecords[index % dataRecords.Count];
                }
                if (fltRecords.Count != 0) {
                    if (sequentialFlight) {
                        if (index >= fltRecords.Count && abortOnFlightListEnd) {
                            return null;
                        }
                        flt = fltRecords[index % fltRecords.Count];
                    } else {
                        Random rnd = new Random();
                        flt = fltRecords[rnd.Next(fltRecords.Count)];
                    }
                }
                if (refreshFlight && flt != null) {
                    logger.Info($"Refreshing flight {flt.ToString()} from AMS");
                    flt = flt.RefeshFlight(executionController.amshost, executionController.token, executionController.apt_code).Result;
                }
                index++;
            } catch (Exception ex) {
                logger.Error($"Error getting next piece of data {ex.Message}");
            }

            if (expression != null && flt != null && filterTime == "post") {
                bool pass = expression.Pass(flt.FightXML);
                if (!pass) {
                    throw new FilterFailException();
                }
            }

            if (topLevelFilter != null && flt != null && filterTime == "post") {
                bool pass = topLevelFilter.Pass(flt.FightXML);
                if (!pass) {
                    throw new FilterFailException();
                }
            }

            // Return the flight node and data record for this iteration
            return new Tuple<Dictionary<string, string>, FlightNode>(data, flt);
        }

        // Called when one of the messages is sent
        public void TriggerHandler(object sender, TriggerFiredEventArgs e) {
            if (intervalMessagesPerMinute < 1500) {
                sourceLogger.Info($"Rate Event Fired. Source: {name}");
                return;
            }

            if (intervalCount % Parameters.REPORTEPOCH == 0) {
                sourceLogger.Info($"Rate Event Fired. Source: {name}. {Parameters.REPORTEPOCH} instances");
            }
        }

        protected void PrepareProfileThreads() {
            if (maxMsgPerMinuteProfile == null) {
                return;
            }
            try {
                string[] pairs = maxMsgPerMinuteProfile.Split(',');
                foreach (string pair in pairs) {
                    string[] pairString = pair.Split(':');

                    int secFromStart = int.Parse(pairString[0]);
                    double intervalMessagesPerMinute = int.Parse(pairString[1]);
                    double intervalThrottleInterval;

                    if (intervalMessagesPerMinute == 0) {
                        intervalThrottleInterval = double.MaxValue;
                    } else {
                        intervalThrottleInterval = 60000 / intervalMessagesPerMinute;
                    }

                    int waitBeforeStart = secFromStart * 1000;
                    if (waitBeforeStart == 0) {
                        MsgPerMin = pairString[1];
                        messagesPerMinute = double.Parse(MsgPerMin);
                        interval = 1000 * (60.0 / messagesPerMinute);
                        Console.WriteLine($"Line {name}: Scheduling Rate Change at {waitBeforeStart}ms after the start to {intervalMessagesPerMinute}msgs/min");
                        continue;
                    }

                    System.Timers.Timer resetTimer = new System.Timers.Timer {
                        AutoReset = false,
                        Interval = waitBeforeStart,
                        Enabled = true
                    };
                    Console.WriteLine($"Line {name}: Scheduling Rate Change at {waitBeforeStart}ms after the start to {intervalMessagesPerMinute}msgs/min");
                    resetTimer.Elapsed += (sender, e) => ProfileThreadUpdateInterval(intervalThrottleInterval, intervalMessagesPerMinute, intervalMessagesPerMinute);
                    resetTimer.Start();
                    resetTimers.Add(resetTimer);
                }
            } catch (Exception e) {
                sourceLogger.Error("**********************************");
                sourceLogger.Error("* Message Throttle Profile Error *");
                sourceLogger.Error("**********************************");
                sourceLogger.Error(e);
                sourceLogger.Error(e.Message);
            }
        }

        private void ProfileThreadUpdateInterval(double intervalThrottleInterval, double maxMess, double configuredMaxMess) {
            interval = intervalThrottleInterval;
            intervalStopWatch.Restart();
            intervalCount = 0;
            intervalMessagesPerMinute = configuredMaxMess;
            SetConfiguredMsgPerMin(configuredMaxMess.ToString(CultureInfo.CurrentCulture));
            SetMsgPerMin(maxMess.ToString(CultureInfo.CurrentCulture));
            sourceLogger.Error($"Rate Source: {name}. Setting interval to {interval}, interval count to {intervalCount}, messages per minute to {configuredMaxMess}");
        }

        internal void Stop() {
            STOP = true;
            sendTimer?.Stop();
            Report("Test Run Complete.", messagesSent, 0, 0);
        }
    }
}