using LoadInjectorBase.Common;
using Microsoft.AspNet.SignalR.Client;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjector.RunTime.EngineComponents {

    public class ClientHub {
        public IHubProxy hubProxy;
        public HubConnection hubConnection;
        private readonly NgExecutionController ngExecutionController;
        private bool localOnly = false;
        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
        public static readonly Logger destLogger = LogManager.GetLogger("destLogger");
        public static readonly Logger sourceLogger = LogManager.GetLogger("sourceLogger");
        public static string hubURL;
        public StringBuilder consoleMessages = new StringBuilder();
        public bool sendDetails = false;
        public Dictionary<string, Tuple<string, string, string, int, double, double>> latsetSourceReport = new Dictionary<string, Tuple<string, string, string, int, double, double>>();
        public Dictionary<string, Tuple<string, string, int, int, double>> latsetDestinationReport = new Dictionary<string, Tuple<string, string, int, int, double>>();

        public ClientHub(string hostURL, NgExecutionController ngExecutionController) {
            this.ngExecutionController = ngExecutionController;
            hubURL = hostURL;
            this.ConfigureHub(hostURL);

            Task.Run(async delegate {
                StartSignalRClientHub();
            });
        }

        public ClientHub(NgExecutionController ngExecutionController) {
            this.ngExecutionController = ngExecutionController;
            this.localOnly = true;
        }

        public void ConfigureHub(string hostURL) {
            try {
                hubConnection = new HubConnection(hostURL);
                hubProxy = hubConnection.CreateHubProxy("MyHub");

                hubProxy.On("ClearAndPrepare", () => {
                    try {
                        ngExecutionController.PrepareAsync().Wait();
                    } catch (Exception ex) {
                        logger.Info("Error in client Prepare Aync " + ex.Message);
                    }
                });
                hubProxy.On("Execute", () => {
                    ngExecutionController.RunLocal();
                });

                hubProxy.On("WaitForNextExecute", (message) => {
                    logger.Info(message);
                });

                hubProxy.On("CompletionReport", () => {
                    logger.Info("Completion report requested");
                    ngExecutionController.ProduceCompletionReport();
                });

                hubProxy.On("DisableDetails", () => {
                    sendDetails = false;
                });
                hubProxy.On("EnableDetails", () => {
                    sendDetails = true;

                    // Send the accumulated Console Messages.
                    Task.Run(() => {
                        try {
                            this.hubProxy.Invoke("ConsoleMsg", ngExecutionController.executionNodeUuid, null, consoleMessages.ToString());
                        } catch (Exception ex) {
                            logger.Info($"Console Message Error. {ex.Message}");
                        }
                    });
                });

                hubProxy.On("PrepareAndExecute", () => {
                    logger.Info("PrepareAndExecute Received");
                    bool okToRun = ngExecutionController.PrepareAsync().Result;
                    if (okToRun) {
                        ngExecutionController.Run();
                    } else {
                        logger.Warn("Prepare did not complete successfully. Execution aborted");
                    }
                });

                hubProxy.On("RetrieveStandAlone", (url) => {
                    try {
                        ngExecutionController.RetrieveStandAlone(url);
                    } catch (Exception ex) {
                        logger.Info("Error in client Prepare Aync " + ex.Message);
                    }
                });

                hubProxy.On("Refresh", () => {
                    logger.Info("Refresh Command Received");
                    RefreshResponse();
                });
                hubProxy.On("RetrieveArchive", (url) => {
                    logger.Warn($"Requested to retrieve archive {url}");
                    ngExecutionController.RetrieveArchive(url);
                    InterrogateResponse();
                });

                hubProxy.On("Stop", () => {
                    ngExecutionController.Stop();
                });

                hubProxy.On("Reset", () => {
                    ngExecutionController.Reset();
                });

                hubProxy.On("Disconnect", () => {
                    hubConnection.Stop();
                    ngExecutionController.ProgramStop();
                    logger.Warn("Disconnection requested by host");
                    System.Environment.Exit(1);
                });

                hubProxy.On("Cancel", (mode) => {
                    ngExecutionController.Cancel();
                });

                hubProxy.On("LocalOnly", (mode) => {
                    localOnly = mode;
                });

                hubProxy.On("InitModel", (model) => {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(model);
                    ngExecutionController.InitModel(doc);
                });

                hubProxy.On("Interrogate", () => {
                    Process currentProcess = Process.GetCurrentProcess();
                    InterrogateResponse();
                });
            } catch (Exception ex) {
                logger.Info(ex.Message);
            }
        }

        public async Task StartSignalRClientHub() {
            bool started = false;

            while (!started) {
                try {
                    hubConnection.Start().Wait();
                    logger.Info($"{hubConnection.State} ({hubURL})");
                    if (hubConnection.State == ConnectionState.Connected) {
                        started = true;
                    } else {
                        Thread.Sleep(5000);
                    }
                } catch (Exception) {
                    logger.Info($"Waiting.. ({hubURL})");
                }
            }
        }

        internal void RefreshResponse() {
            try {
                Process currentProcess = Process.GetCurrentProcess();
                Task.Run(() => {
                    this.hubProxy.Invoke("RefreshResponse", currentProcess.Id.ToString(),
                        Utils.GetLocalIPAddress(),
                        Environment.OSVersion.VersionString,
                        ngExecutionController.dataModel?.OuterXml,
                        ngExecutionController.state.Value,
                        this.latsetSourceReport,
                        this.latsetDestinationReport,
                        ngExecutionController.archName);
                });
            } catch (Exception ex) {
                logger.Info("Error in client Prepare Aync " + ex.Message);
            }
        }

        internal void InterrogateResponse() {
            try {
                Process currentProcess = Process.GetCurrentProcess();
                Task.Run(() => {
                    this.hubProxy.Invoke("InterrogateResponse", currentProcess.Id.ToString(),
                        Utils.GetLocalIPAddress(),
                        Environment.OSVersion.VersionString,
                        ngExecutionController.dataModel?.OuterXml,
                        ngExecutionController.state.Value,
                        ngExecutionController.archName);
                });
            } catch (Exception ex) {
                logger.Info("Error in client Interrogate respnse " + ex.Message);
            }
        }

        internal void SetMsgPerMin(string executionNodeID, string uuid, string s) {
            if (localOnly) {
                logger.Info(s);
            } else {
                Task.Run(() => { this.hubProxy.Invoke("MessagesPerMinute", executionNodeID, uuid, s); });
            }
        }

        internal void SetConfiguredMsgPerMin(string executionNodeID, string uuid, string s) {
            if (localOnly) {
                logger.Info(s);
            } else {
                Task.Run(() => { this.hubProxy.Invoke("ConfiguredMessagesPerMinute", executionNodeID, uuid, s); });
            }
        }

        internal void SetTriggerLabel(string executionNodeID, string uuid, string s) {
            if (localOnly) {
                logger.Info(s);
            } else {
                Task.Run(() => { this.hubProxy.Invoke("SetTriggerLabel", executionNodeID, uuid, s); });
            }
        }

        internal void SetSourceLineOutput(string executionNodeID, string uuid, string s) {
            //if (localOnly) {
            //    sourceLogger.Info(s);
            //} else {
            //    sourceLogger.Info(s);
            //    Task.Run(() => { this.hubProxy.Invoke("SetSourceLineOutput", executionNodeID, uuid, s); });
            //}
            ConsoleMsg(executionNodeID, uuid, s);
        }

        internal void ConsoleMsg(string executionNodeID, string uuid, string s) {
            if (localOnly) {
                logger.Info(s);
            } else {
                if (true) {
                    Task.Run(() => {
                        logger.Info($"ConsoleMag: {uuid}, {s}");
                        try {
                            this.hubProxy.Invoke("ConsoleMsg", executionNodeID, uuid, $"[{DateTime.Now:HH:mm:ss.ffff}] {s}");
                        } catch (Exception ex) {
                            logger.Info($"Console Message Error. {ex.Message}");
                        }
                    });
                    consoleMessages.AppendLine($"[{DateTime.Now:HH:mm:ss.ffff}] {s}");
                } else {
                    consoleMessages.AppendLine($"[{DateTime.Now:HH:mm:ss.ffff}] {s}");
                }
            }
        }

        internal void ClearTriggerData(string executionNodeID, string uuid) {
            if (localOnly) {
                sourceLogger.Info("Clear Data Triggers");
            } else {
                Task.Run(() => { this.hubProxy.Invoke("ClearTriggerData", executionNodeID, uuid); });
            }
        }

        internal void SendSourceReport(string executionNodeID, string uuid, string v, int messagesSent, double currentRate, double messagesPerMinute) {
            Tuple<string, string, string, int, double, double> rec = new Tuple<string, string, string, int, double, double>(executionNodeID, uuid, v, messagesSent, currentRate, messagesPerMinute);
            if (latsetSourceReport.ContainsKey(uuid)) {
                latsetSourceReport[uuid] = rec;
            } else {
                latsetSourceReport.Add(uuid, rec);
            }

            if (ngExecutionController.state.Value == ClientState.ExecutionComplete.Value) {
                logger.Warn("Post completion message received");
                ngExecutionController.ReviseCompletion();
            }

            if (localOnly) {
                sourceLogger.Info($"{v} Messages Sent: {messagesSent}, Current Rate: {currentRate}, Messages Per Minute: {messagesPerMinute}");
            } else {
                Task.Run(() => {
                    this.hubProxy.Invoke("SourceReport", executionNodeID, uuid, v, messagesSent, currentRate, messagesPerMinute);
                });
            }
        }

        internal void SendDestinationReport(string executionNodeID, string uuid, int messagesSent, int messageFail, double rate) {
            Tuple<string, string, int, int, double> rec = new Tuple<string, string, int, int, double>(executionNodeID, uuid, messagesSent, messageFail, rate);

            if (latsetDestinationReport.ContainsKey(uuid)) {
                latsetDestinationReport[uuid] = rec;
            } else {
                latsetDestinationReport.Add(uuid, rec);
            }

            if (ngExecutionController.state.Value == ClientState.ExecutionComplete.Value) {
                logger.Warn("Post completion message received");
                ngExecutionController.ReviseCompletion();
            }

            if (localOnly) {
                destLogger.Info($"Client Side Destination Report {uuid}, sent {messagesSent}, failed {messageFail}, rate {rate}");
            } else {
                destLogger.Info($"Client Side Destination Report {uuid}, sent {messagesSent}, failed {messageFail}, rate {rate}");
                Task.Run(() => {
                    try {
                        this.hubProxy.Invoke("SendDestinationReport", executionNodeID, uuid, messagesSent, messageFail, rate);
                    } catch (Exception ex) {
                        Debug.WriteLine("Error call SendDestination Report " + ex.Message);
                    }
                });
            }
        }

        internal void SendCompletionReport(string executionNodeID, CompletionReport report) {
            if (localOnly) {
                logger.Info("Completion Report");
                logger.Info(report);
            } else {
                logger.Info("Completion Report");
                logger.Info(report.ToString());
                Task.Run(() => {
                    this.hubProxy.Invoke("CompletionReportResponse", executionNodeID, report);
                });
            }
        }

        internal void SetStatus(string executionNodeID) {
            logger.Info($"Node Status:  {ngExecutionController.state.Value}");

            Task.Run(() => {
                this.hubProxy.Invoke("SetExecutionNodeStatus", executionNodeID, ngExecutionController.state.Value);
            });
        }

        internal void SourceReportChain(string executionNodeID, string uuid, string v1, int messagesSent1, string v2, int messagesSent2) {
        }

        internal void DispatcherDistributeMessage(string executionNodeID, TriggerRecord rec) {
            if (localOnly) {
                logger.Info("Dispatcher Message Distribute");
            } else {
                Task.Run(() => { this.hubProxy.Invoke("DispatcherDistributeMessage", executionNodeID, rec); });
            }
        }

        internal void LockVM(string executionNodeID, string uuid, bool l) {
            if (localOnly) {
                logger.Info("Lock VM");
            } else {
                Task.Run(() => { this.hubProxy.Invoke("LockVM", executionNodeID, uuid, l); });
            }
        }

        internal void SetDestinationOutput(string executionNodeID, string uuid, string s) {
            ConsoleMsg(executionNodeID, uuid, s);
            //if (localOnly) {
            //    destLogger.Info(s);
            //} else {
            //    if (sendDetails) {
            //        Task.Run(() => { this.hubProxy.Invoke("SetDestinationOutput", executionNodeID, uuid, s); });
            //    }
            //}
        }

        internal void SetDestinationRate(string executionNodeID, string uuid, double s) {
            if (localOnly) {
                destLogger.Info(s);
            } else {
                Task.Run(() => { this.hubProxy.Invoke("SetDestinationRate", executionNodeID, uuid, s); });
            }
        }

        internal void SetDestinationSent(string executionNodeID, string uuid, int s) {
            if (localOnly) {
                destLogger.Info(s);
            } else {
                Task.Run(() => { this.hubProxy.Invoke("SetDestinationSent", executionNodeID, uuid, s); });
            }
        }

        internal void AddSchedTrigger(string executionNodeUuid, TriggerRecord triggerRecord) {
            if (localOnly) {
                logger.Info("Add trigger Record");
            } else {
                Task.Run(() => { this.hubProxy.Invoke("AddSchedTrigger", executionNodeUuid, triggerRecord); });
            }
        }

        internal void SetSchedTrigger(string executionNodeUuid, List<TriggerRecord> sortedTriggers) {
            if (localOnly) {
                logger.Info("Set Sched Trigger");
            } else {
                Task.Run(() => { this.hubProxy.Invoke("SetSchedTrigger", executionNodeUuid, sortedTriggers); });
            }
        }
    }
}