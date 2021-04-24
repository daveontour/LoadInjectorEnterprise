using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjector.RunTime.EngineComponents {

    public class ClientHub {
        public IHubProxy hubProxy;
        public HubConnection hubConnection;
        private readonly NgExecutionController ngExecutionController;

        public ClientHub(string hostURL, NgExecutionController ngExecutionController) {
            this.ngExecutionController = ngExecutionController;

            this.ConfigureHub(hostURL);

            Task.Run(async delegate {
                StartSignalRClientHub();
            });
        }

        public void ConfigureHub(string hostURL) {
            try {
                hubConnection = new HubConnection(hostURL);
                hubProxy = hubConnection.CreateHubProxy("MyHub");

                hubProxy.On("ClearAndPrepare", () => {
                    Console.WriteLine("Client Side. ClearAndPrepare");
                    try {
                        bool readyToRun = ngExecutionController.PrepareAsync().Result;
                        ReadyToRun(ngExecutionController.executionNodeUuid, readyToRun);
                    } catch (Exception ex) {
                        Console.WriteLine("Error in client Prepare Aync " + ex.Message);
                    }
                });
                hubProxy.On("Execute", () => {
                    Console.WriteLine("Client Side. Execute");
                    ngExecutionController.Run();
                });

                hubProxy.On("Stop", (mode) => {
                    Console.WriteLine("Client Side. Stop");
                    ngExecutionController.Stop(mode);
                });

                hubProxy.On("Cancel", (mode) => {
                    Console.WriteLine("Client Side. Cancel");
                    ngExecutionController.Cancel();
                });

                hubProxy.On("InitModel", (model) => {
                    Console.WriteLine("Client Side. InitModel");
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(model);
                    ngExecutionController.InitModel(doc);
                });

                hubConnection.Start().Wait();

                Console.WriteLine(hubConnection.State);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public async Task StartSignalRClientHub() {
            bool started = false;

            while (!started) {
                try {
                    hubConnection.Start().Wait();
                    Console.WriteLine(hubConnection.State);
                    if (hubConnection.State == ConnectionState.Connected) {
                        started = true;
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                    continue;
                }
            }
        }

        internal void ReadyToRun(string executionNodeID, bool ready) {
            Task.Run(() => {
                this.hubProxy.Invoke("ReadyToRun", executionNodeID, ready);
            });
        }

        internal void SetMsgPerMin(string executionNodeID, string uuid, string s) {
            Task.Run(() => {
                this.hubProxy.Invoke("MessagesPerMinute", executionNodeID, uuid, s);
            });
        }

        internal void SetConfiguredMsgPerMin(string executionNodeID, string uuid, string s) {
            Task.Run(() => {
                this.hubProxy.Invoke("ConfiguredMessagesPerMinute", executionNodeID, uuid, s);
            });
        }

        internal void SetStatusLabel(string executionNodeID, string uuid, string s) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetStatusLabel", executionNodeID, uuid, s);
            });
        }

        internal void SetTriggerLabel(string executionNodeID, string uuid, string s) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetTriggerLabel", executionNodeID, uuid, s);
            });
        }

        internal void SetOutput(string executionNodeID, string uuid, string s) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetOutput", executionNodeID, uuid, s);
            });
        }

        internal void SetSourceLineOutput(string executionNodeID, string uuid, string s) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetSourceLineOutput", executionNodeID, uuid, s);
            });
        }

        internal void ConsoleMsg(string executionNodeID, string uuid, string s) {
            Task.Run(() => {
                Console.WriteLine($"ConsoleMag: {uuid}, {s}");
                try {
                    this.hubProxy.Invoke("ConsoleMsg", executionNodeID, uuid, s);
                } catch (Exception ex) {
                    Console.WriteLine($"Console Message Error. {ex.Message}");
                }
            });
        }

        internal void ClearTriggerData(string executionNodeID, string uuid) {
            Task.Run(() => {
                this.hubProxy.Invoke("ClearTriggerData", executionNodeID, uuid);
            });
        }

        internal void SourceReport(string executionNodeID, string uuid, string v, int messagesSent, double currentRate, double messagesPerMinute) {
            Task.Run(() => {
                this.hubProxy.Invoke("SourceReport", executionNodeID, uuid, v, messagesSent, currentRate, messagesPerMinute);
            });
        }

        internal void SourceReportChain(string executionNodeID, string uuid, string v1, int messagesSent1, string v2, int messagesSent2) {
            // throw new NotImplementedException();
        }

        internal void SetButtonStatus(string executionNodeID, string uuid, bool execute, bool prepare, bool stop) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetButtonStatus", executionNodeID, uuid, execute, prepare, stop);
            });
        }

        internal void DispatcherDistributeMessage(string executionNodeID, TriggerRecord rec) {
            Task.Run(() => {
                this.hubProxy.Invoke("DispatcherDistributeMessage", executionNodeID, rec);
            });
        }

        internal void LockVM(string executionNodeID, string uuid, bool l) {
            Task.Run(() => {
                this.hubProxy.Invoke("LockVM", executionNodeID, uuid, l);
            });
        }

        internal void SendDestinationReport(string executionNodeID, string uuid, int messagesSent, double rate) {
            Console.WriteLine($"Client Side Destination Report {uuid}, sent {messagesSent}, rate {rate}");
            Task.Run(() => {
                try {
                    this.hubProxy.Invoke("SendDestinationReport", executionNodeID, uuid, messagesSent, rate);
                } catch (Exception ex) {
                    Debug.WriteLine("Error call SendDestination Report " + ex.Message);
                }
            });
        }

        internal void SetDestinationOutput(string executionNodeID, string uuid, string s) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetDestinationOutput", executionNodeID, uuid, s);
            });
        }

        internal void SetDestinationRate(string executionNodeID, string uuid, double s) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetDestinationRate", executionNodeID, uuid, s);
            });
        }

        internal void SetDestinationSent(string executionNodeID, string uuid, int s) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetDestinationSent", executionNodeID, uuid, s);
            });
        }

        internal void AddSchedTrigger(string executionNodeUuid, TriggerRecord triggerRecord) {
            Task.Run(() => {
                this.hubProxy.Invoke("AddSchedTrigger", executionNodeUuid, triggerRecord);
            });
        }

        internal void SetSchedTrigger(string executionNodeUuid, List<TriggerRecord> sortedTriggers) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetSchedTrigger", executionNodeUuid, sortedTriggers);
            });
        }

        //internal void SetDestinationMsgPerMinute(string executionNodeID, string uuid, string s) {
        //    Task.Run(() => {
        //        this.hubProxy.Invoke("SetDestinationMsgPerMinute", executionNodeID, uuid, s);
        //    });
        //}

        //internal void SetDestinationConfigMsgPerMin(string executionNodeID, string uuid, string s) {
        //    Task.Run(() => {
        //        this.hubProxy.Invoke("SetDestinationConfigMsgPerMin", executionNodeID, uuid, s);
        //    });
        //}
    }
}