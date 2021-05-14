using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Documents;
using LoadInjector.Runtime.EngineComponents;
using LoadInjectorCommanCentre.Views;
using Microsoft.AspNet.SignalR.Hubs;

namespace LoadInjectorCommanCentre {

    public class CCController : ICCController {
        public MainWindow View { get; set; }
        public CentralMessagingHub MessageHub { get; set; }
        public string ArchiveRoot { get; }

        public string WebServerURL { get; }
        public SimpleHTTPServer WebServer { get; }

        private List<ClientControl> clients = new List<ClientControl>();

        public CCController(MainWindow mainWindow) {
            View = mainWindow;
            MessageHub = new CentralMessagingHub(this);
            MessageHub.StartHub();

            ArchiveRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LoadInjectorCommandCentre";
            int webport = GetAvailablePort(49152);
            WebServerURL = $"http://localhost:{webport}/";
            WebServer = new SimpleHTTPServer(ArchiveRoot, WebServerURL);
            Console.WriteLine("Webserver Port: " + webport);
        }

        public void PrepAll() {
            View.prepAllBtn.IsEnabled = true;
            View.execAllBtn.IsEnabled = true;
            View.stopAllBtn.IsEnabled = false;

            MessageHub.Hub.Clients.All.PrepareAndExecute();
        }

        public void ExecuteAll() {
            View.prepAllBtn.IsEnabled = false;
            View.execAllBtn.IsEnabled = false;
            View.stopAllBtn.IsEnabled = true;
        }

        public void StopAll() {
            View.prepAllBtn.IsEnabled = true;
            View.execAllBtn.IsEnabled = false;
            View.stopAllBtn.IsEnabled = false;
        }

        // Called after a client makes a connection
        public void InitialInterrogation(HubCallerContext context) {
            Console.Write("New Client Connection" + context.ConnectionId);

            try {
                Application.Current.Dispatcher.Invoke((Action)delegate {
                    ClientControl client = new ClientControl(context.ConnectionId, MessageHub, this);
                    clients.Add(client);
                    View.clientControlStack.Children.Add(client);
                });
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            MessageHub.Hub.Clients.Client(context.ConnectionId).Interrogate();
        }

        public static int GetAvailablePort(int startingPort) {
            if (startingPort > ushort.MaxValue) throw new ArgumentException($"Can't be greater than {ushort.MaxValue}", nameof(startingPort));
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            var connectionsEndpoints = ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint);
            var tcpListenersEndpoints = ipGlobalProperties.GetActiveTcpListeners();
            var udpListenersEndpoints = ipGlobalProperties.GetActiveUdpListeners();
            var portsInUse = connectionsEndpoints.Concat(tcpListenersEndpoints)
                .Concat(udpListenersEndpoints)
                .Select(e => e.Port);

            return Enumerable.Range(startingPort, ushort.MaxValue - startingPort + 1).Except(portsInUse).FirstOrDefault();
        }

        public void InterrogateResponse(string processID, string ipAddress, string osversion, HubCallerContext context) {
            foreach (ClientControl client in clients) {
                if (client.ConnectionID == context.ConnectionId) {
                    client.IP = ipAddress;
                    client.ProcessID = processID;
                    client.OSVersion = osversion;
                    break;
                }
            }
            {
            }
        }
    }
}