using System;
using System.Diagnostics;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;
using LoadInjector.RunTime;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(LoadInjector.Runtime.EngineComponents.StartupHub))]

namespace LoadInjector.Runtime.EngineComponents {

    internal class CentralMessagingHub {
        public static ExecutionUI executionUI;

        public IHubContext Hub {
            get => GlobalHost.ConnectionManager.GetHubContext<MyHub>();
        }

        public CentralMessagingHub(ExecutionUI executionUI) {
            CentralMessagingHub.executionUI = executionUI;
        }

        public void StartHub() {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses.
            // See http://msdn.microsoft.com/library/system.net.httplistener.aspx
            // for more information.
            string url = "http://localhost:6220";
            try {
                WebApp.Start<StartupHub>(url);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }

    internal class StartupHub {

        public void Configuration(IAppBuilder app) {
            app.UseCors(CorsOptions.AllowAll);
            GlobalHost.HubPipeline.AddModule(new LoggingPipelineModule());
            app.MapSignalR();
        }
    }

    public class MyHub : Hub {

        public void Send(string name, string message) {
            Clients.All.addMessage(name, message);
        }

        public void Prepare() {
            Clients.All.Prepare();
        }

        public void ConsoleMsg(string executionnodeID, string node, string message) {
            CentralMessagingHub.executionUI.consoleWriter.WriteLine(message);
        }

        public void SetStatusLabel(string executionnodeID, string node, string message) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.StatusLabel = message;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting status label error. " + ex.Message);
                }
            });
        }

        public void SetSourceLineOutput(string executionnodeID, string uuid, string s) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    var ui = CentralMessagingHub.executionUI.sourceUIMap[uuid];
                    ui?.SetOutput(s);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Source Line error. " + ex.Message);
                }
            });
        }

        public void SourceReport(string executionNodeID, string uuid, string v, int messagesSent, double currentRate, double messagesPerMinute) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    var ui = CentralMessagingHub.executionUI.sourceUIMap[uuid];
                    if (messagesSent > ui.GetSentSeqNum()) {
                        ui?.SetOutput(v);
                        ui?.SetMessagesSent(messagesSent);
                        ui?.SetActualRate(currentRate);
                    }
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Source Report error. " + ex.Message);
                }
            });
        }

        public void SetTriggerLabel(string executionnodeID, string node, string message) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.TriggerLabel = message;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Trigger Label error. " + ex.Message);
                }
            });
        }

        public void SetButtonStatus(string executionnodeID, string node, bool execute, bool prepare, bool stop) {
            Console.WriteLine($"Set Button Status - Host execute: {execute}, prepare:{prepare}, stop:{stop}");

            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.ExecuteBtn.IsEnabled = execute;
                    CentralMessagingHub.executionUI.PrepareBtn.IsEnabled = prepare;
                    CentralMessagingHub.executionUI.StopBtn.IsEnabled = stop;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting button status error. " + ex.Message);
                }
            });
        }

        public void ReadyToRun(string executionNodeID, bool ready) {
            CentralMessagingHub.executionUI.consoleWriter.WriteLine($"Ready To Run = {ready}");

            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.ExecuteBtn.IsEnabled = true;
                    CentralMessagingHub.executionUI.PrepareBtn.IsEnabled = true;
                    CentralMessagingHub.executionUI.StopBtn.IsEnabled = false;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting button status error. " + ex.Message);
                }
            });
        }

        public void LockVM(string executionnodeID, string node, bool l) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.VM.LockExecution = l;
                    CentralMessagingHub.executionUI.CancelBtn.Visibility = Visibility.Visible;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting LOCKVM error. " + ex.Message);
                }
            });
        }

        public void ClearTriggerData(string executionnodeID, string node) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.SchedTriggers.Clear();
                    CentralMessagingHub.executionUI.OnPropertyChanged("lvTriggers");
                    CentralMessagingHub.executionUI.FiredTriggers.Clear();
                    CentralMessagingHub.executionUI.OnPropertyChanged("lvFiredTriggers");
                } catch (Exception ex) {
                    Debug.WriteLine("Clearing Trigger Data error. " + ex.Message);
                }
            });
        }

        public void SetDestinationRate(string executionNodeID, string uuid, double s) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    var ui = CentralMessagingHub.executionUI.destUIMap[uuid];
                    ui.SetRate(s);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Dest Rate error. " + ex.Message);
                }
            });
        }

        public void SetDestinationSent(string executionNodeID, string uuid, int s) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    var ui = CentralMessagingHub.executionUI.destUIMap[uuid];
                    if (s > ui.SentSeqNumber) {
                        ui.Sent(s);
                    }
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Dest sent error. " + ex.Message);
                }
            });
        }

        public void SetDestinationMsgPerMinute(string executionNodeID, string uuid, string s) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    var ui = CentralMessagingHub.executionUI.destUIMap[uuid];
                    ui.MsgPerMinExecution = s;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Dest MsgPer Min error. " + ex.Message);
                }
            });
        }

        public void SetDestinationConfigMsgPerMin(string executionNodeID, string uuid, string s) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    var ui = CentralMessagingHub.executionUI.destUIMap[uuid];
                    ui.MsgPerMin = s;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Dest Config Msg Per Min error. " + ex.Message);
                }
            });
        }

        public void SetDestinationOutput(string executionNodeID, string uuid, string s) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    var ui = CentralMessagingHub.executionUI.destUIMap[uuid];
                    ui.SetOutput(s);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Dest Output error. " + ex.Message);
                }
            });
        }

        public void SendDestinationReport(string executionNodeID, string uuid, int messagesSent, double rate) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    var ui = CentralMessagingHub.executionUI.destUIMap[uuid];
                    if (messagesSent > ui.SentSeqNumber) {
                        ui.Sent(messagesSent);
                        ui.SetRate(rate);
                    }
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Dest Report error. " + ex.Message);
                }
            });
        }

        public void DispatcherDistributeMessage(string executionNodeID, TriggerRecord rec) {
            Task.Run(() => {
                TriggerRecord remove = null;

                foreach (TriggerRecord r in CentralMessagingHub.executionUI.SchedTriggers) {
                    if (r.uuid == rec.uuid) {
                        remove = r;
                        break;
                    }
                }
                if (remove != null) {
                    try {
                        Application.Current.Dispatcher.Invoke(delegate {
                            CentralMessagingHub.executionUI.SchedTriggers.Remove(remove);
                            CentralMessagingHub.executionUI.OnPropertyChanged("lvTriggers");
                            CentralMessagingHub.executionUI.FiredTriggers.Add(rec);
                            CentralMessagingHub.executionUI.OnPropertyChanged("lvFiredTriggers");
                        });
                    } catch (Exception ex) {
                        Debug.WriteLine("Dispatcher Distribute Error " + ex.Message);
                    }
                } else {
                    try {
                        Application.Current.Dispatcher.Invoke(delegate {
                            CentralMessagingHub.executionUI.FiredTriggers.Add(rec);
                            CentralMessagingHub.executionUI.OnPropertyChanged("lvFiredTriggers");
                        });
                    } catch (Exception ex) {
                        Debug.WriteLine("Dispatcher Distribute Error " + ex.Message);
                    }
                }
            });
        }

        public void AddSchedTrigger(string executionNodeUuid, TriggerRecord triggerRecord) {
            try {
                Application.Current.Dispatcher.Invoke(delegate {
                    CentralMessagingHub.executionUI.SchedTriggers.Add(triggerRecord);
                });
            } catch (Exception ex) {
                Debug.WriteLine("Add Sched Triggers " + ex.Message);
            }
        }

        public void SetSchedTrigger(string executionNodeUuid, List<TriggerRecord> sortedTriggers) {
            Application.Current.Dispatcher.Invoke(delegate {
                CentralMessagingHub.executionUI.SchedTriggers.Clear();

                foreach (TriggerRecord t in sortedTriggers) {
                    CentralMessagingHub.executionUI.SchedTriggers.Add(t);
                }
                CentralMessagingHub.executionUI.OnPropertyChanged("lvTriggers");
            });
        }
    }

    public class LoggingPipelineModule : HubPipelineModule {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context) {
            // Console.WriteLine("=> Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
            return base.OnBeforeIncoming(context);
        }

        protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context) {
            // Console.WriteLine("<= Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub);
            return base.OnBeforeOutgoing(context);
        }

        protected override bool OnBeforeConnect(IHub hub) {
            // Console.WriteLine("<= Before Connect ");
            return base.OnBeforeConnect(hub);
        }
    }
}