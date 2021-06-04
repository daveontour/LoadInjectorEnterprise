using LoadInjector.RunTime;
using LoadInjectorBase.Common;
using LoadInjectorCommandCentre;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using NLog;
using Owin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

[assembly: OwinStartup(typeof(LoadInjector.Runtime.EngineComponents.StartupHub))]

namespace LoadInjector.Runtime.EngineComponents {

    public class CentralMessagingHub {

        // Need to fix for new structure
        public static MainCommandCenterController iccController;

        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");

        public int port = 6220;
        private IDisposable hubServer;

        public IHubContext Hub {
            get => GlobalHost.ConnectionManager.GetHubContext<MyHub>();
        }

        public CentralMessagingHub(MainCommandCenterController iccController) {
            CentralMessagingHub.iccController = iccController;
        }

        public CentralMessagingHub(int port) {
            this.port = port;
        }

        public void SetExecutionUI(MainCommandCenterController iccController) {
            CentralMessagingHub.iccController = iccController;
        }

        public void StartHub() {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses.
            // See http://msdn.microsoft.com/library/system.net.httplistener.aspx
            // for more information.
            string url = $"http://localhost:{port}";
            try {
                hubServer = WebApp.Start<StartupHub>(url);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Hub Started On" + url);
        }

        public void StartHub(int port) {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses.
            // See http://msdn.microsoft.com/library/system.net.httplistener.aspx
            // for more information.
            this.port = port;
            string url = $"http://localhost:{port}";
            try {
                hubServer = WebApp.Start<StartupHub>(url);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Hub Started On" + url);
        }

        public void StartHub(string url) {
            // This will *ONLY* bind to localhost, if you want to bind to all addresses
            // use http://*:8080 to bind to all addresses.
            // See http://msdn.microsoft.com/library/system.net.httplistener.aspx
            // for more information.
            try {
                hubServer = WebApp.Start<StartupHub>(url);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Hub Started On" + url);
        }

        public void StoptHub() {
            hubServer.Dispose();
            Console.WriteLine("Hub Stopped");
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
            CentralMessagingHub.iccController.ClientConnectionInitiated(Context);
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stop) {
            CentralMessagingHub.iccController.Disconnect(Context);
            return base.OnDisconnected(stop);
        }

        public void ConsoleMsg(string executionnodeID, string node, string message) {
            CentralMessagingHub.iccController.SetConsoleMessage(message, Context);
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

        public void InterrogateResponse(string processID, string ipAddress, string osversion, string xml, string status) {
            CentralMessagingHub.iccController.InterrogateResponse(processID, ipAddress, osversion, xml, status, Context);
        }

        public void RefreshResponse(string processID, string ipAddress, string osversion, string xml, string status, Dictionary<string, Tuple<string, string, string, int, double, double>> latestSourceReport, Dictionary<string, Tuple<string, string, int, double>> latestDestinationReport) {
            CentralMessagingHub.iccController.RefreshResponse(processID, ipAddress, osversion, xml, status, latestSourceReport, latestDestinationReport, Context);
        }

        public void CompletionReportResponse(string executionNodeID, CompletionReport report) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.iccController.SetCompletionReport(executionNodeID, report, Context);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Source Report error. " + ex.Message);
                }
            });
        }

        public void SetExecutionNodeStatus(string executionNodeID, string message) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.iccController.SetExecutionNodeStatus(executionNodeID, message, Context);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Source Report error. " + ex.Message);
                }
            });
        }

        public void SourceReport(string executionNodeID, string uuid, string message, int messagesSent, double currentRate, double messagesPerMinute) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.iccController.UpdateSourceLine(executionNodeID, uuid, message, messagesSent, currentRate, messagesPerMinute, Context);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Source Report error. " + ex.Message);
                }
            });
        }

        public void SendDestinationReport(string executionNodeID, string uuid, int messagesSent, double rate) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.iccController.UpdateDestinationLine(executionNodeID, uuid, messagesSent, Context);
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