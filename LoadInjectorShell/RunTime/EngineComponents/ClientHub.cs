using Microsoft.AspNet.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LoadInjector.RunTime.EngineComponents {

    public class ClientHub {
        public IHubProxy hubProxy;
        public HubConnection hubConnection;
        private readonly NgExecutionController ngExecutionController;

        public ClientHub(string hostURL, NgExecutionController ngExecutionController) {
            this.ngExecutionController = ngExecutionController;

            Task.Run(async delegate {
                await StartListnener(hostURL);
            });
        }

        public async Task StartListnener(string hostURL) {
            try {
                hubConnection = new HubConnection(hostURL);
                hubProxy = hubConnection.CreateHubProxy("MyHub");

                hubProxy.On("ClearAndPrepare", () => {
                    Debug.WriteLine("Client Side. ClearAndPrepare");
                    try {
                        ngExecutionController.PrepareAsync().Wait();
                    } catch (Exception ex) {
                        Debug.WriteLine("error in client Prepare Aync " + ex.Message);
                    }
                });
                hubProxy.On("Execute", () => {
                    Debug.WriteLine("Client Side. Execute");
                    ngExecutionController.Run();
                });

                hubProxy.On("Stop", (mode) => {
                    Debug.WriteLine("Client Side. Stop");
                    ngExecutionController.Stop(mode);
                });

                hubConnection.Start().Wait();
                Debug.WriteLine(hubConnection.State);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private void PrepareReceived() {
            ngExecutionController.PrepareAsync();
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
                this.hubProxy.Invoke("ConsoleMsg", executionNodeID, uuid, s);
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

        internal void PercentComplete(string executionNodeID, string uuid, int percent, bool clearConsole, string timestr) {
            Task.Run(() => {
                this.hubProxy.Invoke("PercentComplete", executionNodeID, uuid, percent, clearConsole, timestr);
            });
        }

        internal void SetCancelBtnHidden(string executionNodeID, string uuid) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetCancelBtnHidden", executionNodeID, uuid);
            });
        }

        internal void SetCancelBtnShow(string executionNodeID, string uuid) {
            Task.Run(() => {
                this.hubProxy.Invoke("SetCancelBtnShow", executionNodeID, uuid);
            });
        }

        internal void LockVM(string executionNodeID, string uuid, bool l) {
            Task.Run(() => {
                this.hubProxy.Invoke("LockVM", executionNodeID, uuid, l);
            });
        }

        internal void SendDestinationReport(string executionNodeID, string uuid, int messagesSent, double rate) {
            Debug.WriteLine($"Client Side Destination report {uuid}, sent {messagesSent}, rate {rate}");
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