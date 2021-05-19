using System;
using System.Diagnostics;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;
using NLog;
using System.Net;
using System.IO;
using System.Text;
using LoadInjector.RunTime;

[assembly: OwinStartup(typeof(LoadInjector.Runtime.EngineComponents.StartupHub))]

namespace LoadInjector.Runtime.EngineComponents {

    public class CentralMessagingHub {

        // Need to fix for new structure
        public static ICCController iccController;

        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");

        public int port = 6220;

        public IHubContext Hub {
            get => GlobalHost.ConnectionManager.GetHubContext<MyHub>();
        }

        public CentralMessagingHub(ICCController iccController) {
            CentralMessagingHub.iccController = iccController;
        }

        public CentralMessagingHub(int port) {
            this.port = port;
        }

        public void SetExecutionUI(ICCController iccController) {
            CentralMessagingHub.iccController = iccController;
        }

        public void StartHub() {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses.
            // See http://msdn.microsoft.com/library/system.net.httplistener.aspx
            // for more information.
            string url = $"http://localhost:{port}";
            try {
                WebApp.Start<StartupHub>(url);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Hub Started On" + url);
        }
    }

    internal class StartupHub {

        public void Configuration(IAppBuilder app) {
            try {
                app.UseCors(CorsOptions.AllowAll);
                GlobalHost.HubPipeline.AddModule(new LoggingPipelineModule());
                app.MapSignalR();
            } catch (Exception ex) {
                Console.Write("SignalR Config Error. " + ex.Message);
            }
        }
    }

    public class MyHub : Hub {
        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");

        public void Send(string name, string message) {
            Clients.All.addMessage(name, message);
        }

        public void Prepare() {
            Clients.All.Prepare();
        }

        public override Task OnConnected() {
            //string name = Context.User.Identity.Name;
            //Console.WriteLine($"New Connection.  ConnectionID:{Context.ConnectionId}");
            CentralMessagingHub.iccController.InitialInterrogation(Context);
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stop) {
            //string name = Context.User.Identity.Name;
            //Console.WriteLine($"New Connection.  ConnectionID:{Context.ConnectionId}");
            CentralMessagingHub.iccController.Disconnect(Context);
            return base.OnConnected();
        }

        public void ConsoleMsg(string executionnodeID, string node, string message) {
            //CentralMessagingHub.executionUI.ConsoleWriter.WriteLine(message);

            logger.Info(message);
        }

        public void SetStatusLabel(string executionnodeID, string node, string message) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    //  CentralMessagingHub.executionUI.StatusLabel = message;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting status label error. " + ex.Message);
                }
            });
        }

        public void SetSourceLineOutput(string executionnodeID, string uuid, string s) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    //  var ui = CentralMessagingHub.executionUI.SourceUIMap[uuid];
                    //     ui?.SetOutput(s);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Source Line error. " + ex.Message);
                }
            });
        }

        public void InterrogateResponse(string processID, string ipAddress, string osversion, string xml) {
            CentralMessagingHub.iccController.InterrogateResponse(processID, ipAddress, osversion, xml, Context);
        }

        public void RefreshResponse(string processID, string ipAddress, string osversion, string xml) {
            CentralMessagingHub.iccController.RefreshResponse(processID, ipAddress, osversion, xml, Context);
        }

        public void SourceReport(string executionNodeID, string uuid, string message, int messagesSent, double currentRate, double messagesPerMinute) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.iccController.UpdateLine(executionNodeID, uuid, message, messagesSent, currentRate, messagesPerMinute, Context);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Source Report error. " + ex.Message);
                }
            });
        }

        public void SetTriggerLabel(string executionnodeID, string node, string message) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    //      CentralMessagingHub.executionUI.TriggerLabel = message;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Trigger Label error. " + ex.Message);
                }
            });
        }

        public void SetButtonStatus(string executionnodeID, string node, bool execute, bool prepare, bool stop) {
            Console.WriteLine($"Set Button Status - Host execute: {execute}, prepare:{prepare}, stop:{stop}");

            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    //         CentralMessagingHub.executionUI.SetExecuteBtnEnabled(execute);
                    //          CentralMessagingHub.executionUI.SetPrepareBtnEnabled(prepare);
                    //          CentralMessagingHub.executionUI.SetStopBtnEnabled(stop);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting button status error. " + ex.Message);
                }
            });
        }

        public void ReadyToRun(string executionNodeID, bool ready) {
            //    CentralMessagingHub.executionUI.ConsoleWriter.WriteLine($"Ready To Run = {ready}");

            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    //            CentralMessagingHub.executionUI.SetExecuteBtnEnabled(true);
                    //            CentralMessagingHub.executionUI.SetPrepareBtnEnabled(true);
                    //            CentralMessagingHub.executionUI.SetStopBtnEnabled(false);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting button status error. " + ex.Message);
                }
            });
        }

        public void ClearTriggerData(string executionnodeID, string node) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    //           CentralMessagingHub.executionUI.SchedTriggers.Clear();
                    //           CentralMessagingHub.executionUI.OnPropertyChanged("lvTriggers");
                    //           CentralMessagingHub.executionUI.FiredTriggers.Clear();
                    //           CentralMessagingHub.executionUI.OnPropertyChanged("lvFiredTriggers");
                } catch (Exception ex) {
                    Debug.WriteLine("Clearing Trigger Data error. " + ex.Message);
                }
            });
        }

        //public void SetDestinationRate(string executionNodeID, string uuid, double s) {
        //    Application.Current.Dispatcher.Invoke(delegate {
        //        try {
        //            //            var ui = CentralMessagingHub.executionUI.DestUIMap[uuid];
        //            //             ui.SetRate(s);
        //        } catch (Exception ex) {
        //            Debug.WriteLine("Setting Dest Rate error. " + ex.Message);
        //        }
        //    });
        //}

        //public void SetDestinationSent(string executionNodeID, string uuid, int s) {
        //    Application.Current.Dispatcher.Invoke(delegate {
        //        try {
        //            //          var ui = CentralMessagingHub.executionUI.DestUIMap[uuid];
        //            //           if (s > ui.SentSeqNumber) {
        //            //               ui.Sent(s);
        //            //           }
        //        } catch (Exception ex) {
        //            Debug.WriteLine("Setting Dest sent error. " + ex.Message);
        //        }
        //    });
        //}

        //public void SetDestinationMsgPerMinute(string executionNodeID, string uuid, string s) {
        //    Application.Current.Dispatcher.Invoke(delegate {
        //        try {
        //            //var ui = CentralMessagingHub.executionUI.DestUIMap[uuid];
        //            //ui.MsgPerMinExecution = s;
        //        } catch (Exception ex) {
        //            Debug.WriteLine("Setting Dest MsgPer Min error. " + ex.Message);
        //        }
        //    });
        //}

        //public void SetDestinationConfigMsgPerMin(string executionNodeID, string uuid, string s) {
        //    Application.Current.Dispatcher.Invoke(delegate {
        //        try {
        //            //var ui = CentralMessagingHub.executionUI.DestUIMap[uuid];
        //            //ui.MsgPerMin = s;
        //        } catch (Exception ex) {
        //            Debug.WriteLine("Setting Dest Config Msg Per Min error. " + ex.Message);
        //        }
        //    });
        //}

        //public void SetDestinationOutput(string executionNodeID, string uuid, string s) {
        //    Application.Current.Dispatcher.Invoke(delegate {
        //        try {
        //            //var ui = CentralMessagingHub.executionUI.DestUIMap[uuid];
        //            //ui.SetOutput(s);
        //        } catch (Exception ex) {
        //            Debug.WriteLine("Setting Dest Output error. " + ex.Message);
        //        }
        //    });
        //}

        public void SendDestinationReport(string executionNodeID, string uuid, int messagesSent, double rate) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.iccController.UpdateLine(executionNodeID, uuid, messagesSent, Context);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Source Report error. " + ex.Message);
                }
            });
        }

        public void DispatcherDistributeMessage(string executionNodeID, TriggerRecord rec) {
            Task.Run(() => {
                TriggerRecord remove = null;

                //foreach (TriggerRecord r in CentralMessagingHub.executionUI.SchedTriggers) {
                //    if (r.uuid == rec.uuid) {
                //        remove = r;
                //        break;
                //    }
                //}
                //if (remove != null) {
                //    try {
                //        Application.Current.Dispatcher.Invoke(delegate {
                //            CentralMessagingHub.executionUI.SchedTriggers.Remove(remove);
                //            CentralMessagingHub.executionUI.OnPropertyChanged("lvTriggers");
                //            CentralMessagingHub.executionUI.FiredTriggers.Add(rec);
                //            CentralMessagingHub.executionUI.OnPropertyChanged("lvFiredTriggers");
                //        });
                //    } catch (Exception ex) {
                //        Debug.WriteLine("Dispatcher Distribute Error " + ex.Message);
                //    }
                //} else {
                //    try {
                //        Application.Current.Dispatcher.Invoke(delegate {
                //            CentralMessagingHub.executionUI.FiredTriggers.Add(rec);
                //            CentralMessagingHub.executionUI.OnPropertyChanged("lvFiredTriggers");
                //        });
                //    } catch (Exception ex) {
                //        Debug.WriteLine("Dispatcher Distribute Error " + ex.Message);
                //    }
                //}
            });
        }

        public void AddSchedTrigger(string executionNodeUuid, TriggerRecord triggerRecord) {
            //try {
            //    Application.Current.Dispatcher.Invoke(delegate {
            //        CentralMessagingHub.executionUI.SchedTriggers.Add(triggerRecord);
            //    });
            //} catch (Exception ex) {
            //    Debug.WriteLine("Add Sched Triggers " + ex.Message);
            //}
        }

        public void SetSchedTrigger(string executionNodeUuid, List<TriggerRecord> sortedTriggers) {
            Application.Current.Dispatcher.Invoke(delegate {
                //CentralMessagingHub.executionUI.SchedTriggers.Clear();

                //foreach (TriggerRecord t in sortedTriggers) {
                //    CentralMessagingHub.executionUI.SchedTriggers.Add(t);
                //}
                //CentralMessagingHub.executionUI.OnPropertyChanged("lvTriggers");
            });
        }
    }

    public class LoggingPipelineModule : HubPipelineModule {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context) {
            Console.WriteLine("=> Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
            return base.OnBeforeIncoming(context);
        }

        protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context) {
            Console.WriteLine("<= Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub);
            return base.OnBeforeOutgoing(context);
        }

        //protected override bool OnBeforeConnect(IHub hub) {
        //    Console.WriteLine("<= Before Connect ");
        //    return base.OnBeforeConnect(hub);
        //}

        protected override void OnAfterConnect(IHub hub) {
            Console.WriteLine("<= After Connect ");
            //  Console.WriteLine($"ConnectionID:{hub.Context.ConnectionId}");
            //CentralMessagingHub.iccController.InitialInterrogation(hub.Context.ConnectionId);
            base.OnAfterConnect(hub);
        }

        protected override bool OnBeforeDisconnect(IHub hub, bool stopCalled) {
            Console.WriteLine("Disconnect called for " + hub.Context.ConnectionId);
            return base.OnBeforeDisconnect(hub, stopCalled);
        }

        //protected override bool OnBeforeAuthorizeConnect(HubDescriptor hubDescriptor, IRequest request) {
        //    // Console.WriteLine($"Authorise Connect Request. " + request);
        //    return true;
        //}
    }
}