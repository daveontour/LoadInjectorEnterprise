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

        public int port;
        private IDisposable hubServer;

        public IHubContext Hub {
            get => GlobalHost.ConnectionManager.GetHubContext<MyHub>();
        }

        public CentralMessagingHub(MainCommandCenterController icc) {
            iccController = icc;
        }

        public CentralMessagingHub(int port) {
            this.port = port;
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
        }

        public void StoptHub() {
            hubServer.Dispose();
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

        public void InterrogateResponse(string processID, string ipAddress, string osversion, string xml, string status, string archName, int duration) {
            CentralMessagingHub.iccController.InterrogateResponse(processID, ipAddress, osversion, xml, status, archName, duration, Context);
        }

        public void RefreshResponse(string processID, string ipAddress, string osversion, string xml, string status, Dictionary<string, Tuple<string, string, string, int, double, double>> latestSourceReport, Dictionary<string, Tuple<string, string, int, int, double>> latestDestinationReport, string archName, int duration) {
            CentralMessagingHub.iccController.RefreshResponse(processID, ipAddress, osversion, xml, status, latestSourceReport, latestDestinationReport, archName, duration, Context);
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

        public void SendDestinationReport(string executionNodeID, string uuid, int messagesSent, int messageFail, double rate) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.iccController.UpdateDestinationLine(executionNodeID, uuid, messagesSent, messageFail, Context);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Source Report error. " + ex.Message);
                }
            });
        }
    }

    public class LoggingPipelineModule : HubPipelineModule {

        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context) {
            Console.WriteLine("=> Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
            return base.OnBeforeIncoming(context);
        }

        protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context) {
            Console.WriteLine("<= Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub);
            return base.OnBeforeOutgoing(context);
        }

        protected override void OnAfterConnect(IHub hub) {
            Console.WriteLine("<= After Connect ");
            base.OnAfterConnect(hub);
        }

        protected override bool OnBeforeDisconnect(IHub hub, bool stopCalled) {
            Console.WriteLine("Disconnect called for " + hub.Context.ConnectionId);
            return base.OnBeforeDisconnect(hub, stopCalled);
        }
    }
}