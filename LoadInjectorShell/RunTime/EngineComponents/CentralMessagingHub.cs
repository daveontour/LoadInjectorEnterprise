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
            WebApp.Start<StartupHub>(url);
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
            Console.WriteLine(message);
        }

        public void SetStatusLabel(string executionnodeID, string node, string message) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.StatusLabel = message;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting button status error. " + ex.Message);
                }
            });
        }

        public void SetTriggerLabel(string executionnodeID, string node, string message) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.TriggerLabel = message;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting button status error. " + ex.Message);
                }
            });
        }

        public void SetButtonStatus(string executionnodeID, string node, bool execute, bool prepare, bool stop) {
            Debug.WriteLine($"Set Button Status - Host execute: {execute}, prepare:{prepare}, stop:{stop}");

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

        public void SetCancelBtnHidden(string executionnodeID, string node) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.CancelBtn.IsEnabled = false;
                    CentralMessagingHub.executionUI.CancelBtn.Visibility = Visibility.Hidden;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting button status error. " + ex.Message);
                }
            });
        }

        public void SetCancelBtnShow(string executionnodeID, string node) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.CancelBtn.IsEnabled = true;
                    CentralMessagingHub.executionUI.CancelBtn.Visibility = Visibility.Visible;
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
                    Debug.WriteLine("Setting button status error. " + ex.Message);
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
                    Debug.WriteLine("Setting button status error. " + ex.Message);
                }
            });
        }

        public void PercentComplete(string executionNodeID, string uuid, int percent, bool clearConsole, string timestr) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    CentralMessagingHub.executionUI.PercentComplete = percent;
                    CentralMessagingHub.executionUI.ElapsedString = timestr;
                } catch (Exception ex) {
                    Debug.WriteLine("Setting button status error. " + ex.Message);
                }
            });
        }
    }

    public class LoggingPipelineModule : HubPipelineModule {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        protected override bool OnBeforeIncoming(IHubIncomingInvokerContext context) {
            Debug.WriteLine("=> Invoking " + context.MethodDescriptor.Name + " on hub " + context.MethodDescriptor.Hub.Name);
            return base.OnBeforeIncoming(context);
        }

        protected override bool OnBeforeOutgoing(IHubOutgoingInvokerContext context) {
            Debug.WriteLine("<= Invoking " + context.Invocation.Method + " on client hub " + context.Invocation.Hub);
            return base.OnBeforeOutgoing(context);
        }

        protected override bool OnBeforeConnect(IHub hub) {
            Debug.WriteLine("<= Before Connect ");
            return base.OnBeforeConnect(hub);
        }
    }
}