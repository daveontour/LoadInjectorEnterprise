using Microsoft.AspNet.SignalR.Client;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
                        bool readyToRun = ngExecutionController.PrepareAsync().Result;
                        ReadyToRun(ngExecutionController.executionNodeUuid, readyToRun);
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
                        bool readyToRun = ngExecutionController.PrepareAsync().Result;
                        ReadyToRun(ngExecutionController.executionNodeUuid, readyToRun);
                        Process currentProcess = Process.GetCurrentProcess();
                        Task.Run(() => {
                            this.hubProxy.Invoke("InterrogateResponse", currentProcess.Id.ToString(),
                                GetLocalIPAddress(),
                                Environment.OSVersion.VersionString,
                                ngExecutionController.dataModel?.OuterXml);
                        });
                    } catch (Exception ex) {
                        logger.Info("Error in client Prepare Aync " + ex.Message);
                    }
                });

                hubProxy.On("Refresh", () => {
                    try {
                        Process currentProcess = Process.GetCurrentProcess();
                        Task.Run(() => {
                            this.hubProxy.Invoke("RefreshResponse", currentProcess.Id.ToString(),
                                GetLocalIPAddress(),
                                Environment.OSVersion.VersionString,
                                ngExecutionController.dataModel?.OuterXml);
                        });
                    } catch (Exception ex) {
                        logger.Info("Error in client Prepare Aync " + ex.Message);
                    }
                });
                hubProxy.On("RetrieveArchive", (url) => {
                    logger.Warn($"Requested to retrieve archive {url}");
                    ngExecutionController.RetrieveArchive(url);
                    Process currentProcess = Process.GetCurrentProcess();
                    Task.Run(() => {
                        this.hubProxy.Invoke("InterrogateResponse", currentProcess.Id.ToString(),
                            GetLocalIPAddress(),
                            Environment.OSVersion.VersionString,
                            ngExecutionController.dataModel?.OuterXml);
                    });
                });

                hubProxy.On("Stop", () => {
                    ngExecutionController.Stop();
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
                    Task.Run(() => {
                        this.hubProxy.Invoke("InterrogateResponse", currentProcess.Id.ToString(),
                            GetLocalIPAddress(),
                            Environment.OSVersion.VersionString,
                            ngExecutionController.dataModel?.OuterXml);
                    });
                });
            } catch (Exception ex) {
                logger.Info(ex.Message);
            }
        }

        public static string GetLocalIPAddress() {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
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
                } catch (Exception ex) {
                    logger.Info($"Waiting.. ({hubURL})");
                }
            }
        }

        internal void ReadyToRun(string executionNodeID, bool ready) {
            if (localOnly) {
                logger.Info("Ready To Run");
            } else {
                Task.Run(() => { this.hubProxy.Invoke("ReadyToRun", executionNodeID, ready); });
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
            if (localOnly) {
                sourceLogger.Info(s);
            } else {
                Task.Run(() => { this.hubProxy.Invoke("SetSourceLineOutput", executionNodeID, uuid, s); });
            }
        }

        internal void ConsoleMsg(string executionNodeID, string uuid, string s) {
            if (localOnly) {
                logger.Info(s);
            } else {
                Task.Run(() => {
                    logger.Info($"ConsoleMag: {uuid}, {s}");
                    try {
                        this.hubProxy.Invoke("ConsoleMsg", executionNodeID, uuid, s);
                    } catch (Exception ex) {
                        logger.Info($"Console Message Error. {ex.Message}");
                    }
                });
            }
        }

        internal void ClearTriggerData(string executionNodeID, string uuid) {
            if (localOnly) {
                sourceLogger.Info("Clear Data Triggers");
            } else {
                Task.Run(() => { this.hubProxy.Invoke("ClearTriggerData", executionNodeID, uuid); });
            }
        }

        internal void SourceReport(string executionNodeID, string uuid, string v, int messagesSent, double currentRate, double messagesPerMinute) {
            if (localOnly) {
                sourceLogger.Info($"{v} Messages Sent: {messagesSent}, Current Rate: {currentRate}, Messages Per Minute: {messagesPerMinute}");
            } else {
                Task.Run(() => {
                    this.hubProxy.Invoke("SourceReport", executionNodeID, uuid, v, messagesSent, currentRate,
                        messagesPerMinute);
                });
            }
        }

        internal void SourceReportChain(string executionNodeID, string uuid, string v1, int messagesSent1, string v2, int messagesSent2) {
            // throw new NotImplementedException();
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

        internal void SendDestinationReport(string executionNodeID, string uuid, int messagesSent, double rate) {
            if (localOnly) {
                destLogger.Info($"Client Side Destination Report {uuid}, sent {messagesSent}, rate {rate}");
            } else {
                Task.Run(() => {
                    try {
                        this.hubProxy.Invoke("SendDestinationReport", executionNodeID, uuid, messagesSent, rate);
                    } catch (Exception ex) {
                        Debug.WriteLine("Error call SendDestination Report " + ex.Message);
                    }
                });
            }
        }

        internal void SetDestinationOutput(string executionNodeID, string uuid, string s) {
            if (localOnly) {
                destLogger.Info(s);
            } else {
                Task.Run(() => { this.hubProxy.Invoke("SetDestinationOutput", executionNodeID, uuid, s); });
            }
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