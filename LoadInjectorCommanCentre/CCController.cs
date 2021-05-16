using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Documents;
using System.Xml;
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

        private Dictionary<string, ClientControl> clients = new Dictionary<string, ClientControl>();

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

            MessageHub.Hub.Clients.All.ClearAndPrepare();
        }

        public void ExecuteAll() {
            View.prepAllBtn.IsEnabled = false;
            View.execAllBtn.IsEnabled = false;
            View.stopAllBtn.IsEnabled = true;

            MessageHub.Hub.Clients.All.Execute();
        }

        public void StopAll() {
            View.prepAllBtn.IsEnabled = true;
            View.execAllBtn.IsEnabled = false;
            View.stopAllBtn.IsEnabled = false;

            MessageHub.Hub.Clients.All.Stop();
        }

        // Called after a client makes a connection
        public void InitialInterrogation(HubCallerContext context) {
            Console.Write("New Client Connection" + context.ConnectionId);

            try {
                Application.Current.Dispatcher.Invoke((Action)delegate {
                    ClientControl client = new ClientControl(context.ConnectionId, MessageHub, this);
                    clients.Add(context.ConnectionId, client);
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

        public void InterrogateResponse(string processID, string ipAddress, string osversion, string xml, HubCallerContext context) {
            ClientControl client = clients[context.ConnectionId];

            client.IP = ipAddress;
            client.ProcessID = processID;
            client.OSVersion = osversion;

            if (xml != null) {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                foreach (XmlNode node in doc.SelectNodes(".//*")) {
                    if (node.Name == "destination") {
                        ExecutionRecordClass rec = new ExecutionRecordClass() {
                            IP = ipAddress,
                            ProcessID = processID,
                            Type = "Destination",
                            Name = node.Attributes["name"]?.Value,
                            ConfigMM = node.Attributes["messagesPerMinute"]?.Value,
                            MM = "-",
                            ExecutionLineID = node.Attributes["uuid"]?.Value,
                            ExecutionNodeID = node.Attributes["executionNodeUuid"]?.Value
                        };

                        client.AddUpdateExecutionRecord(rec);
                        View.AddUpdateExecutionRecord(rec);
                    }
                    if (node.Name == "ratedriven") {
                        ExecutionRecordClass rec = new ExecutionRecordClass() {
                            IP = ipAddress,
                            ProcessID = processID,
                            Type = "Rate Driven Source",
                            Name = node.Attributes["name"]?.Value,
                            ConfigMM = node.Attributes["messagesPerMinute"]?.Value,
                            MM = "-",
                            ExecutionLineID = node.Attributes["uuid"]?.Value,
                            ExecutionNodeID = node.Attributes["executionNodeUuid"]?.Value
                        };

                        client.AddUpdateExecutionRecord(rec);
                        View.AddUpdateExecutionRecord(rec);
                    }
                    if (node.Name.Contains("datadriven")) {
                        ExecutionRecordClass rec = new ExecutionRecordClass() {
                            IP = ipAddress,
                            ProcessID = processID,
                            Type = "Data Driven Source",
                            Name = node.Attributes["name"]?.Value,
                            ConfigMM = "-",
                            MM = "-",
                            ExecutionLineID = node.Attributes["uuid"]?.Value,
                            ExecutionNodeID = node.Attributes["executionNodeUuid"]?.Value
                        };

                        client.AddUpdateExecutionRecord(rec);
                        View.AddUpdateExecutionRecord(rec);
                    }
                    if (node.Name.Contains("chain")) {
                        ExecutionRecordClass rec = new ExecutionRecordClass() {
                            IP = ipAddress,
                            ProcessID = processID,
                            Type = "Chained Source",
                            Name = node.Attributes["name"]?.Value,
                            ConfigMM = "-",
                            MM = "-",
                            ExecutionLineID = node.Attributes["uuid"]?.Value,
                            ExecutionNodeID = node.Attributes["executionNodeUuid"]?.Value
                        };

                        client.AddUpdateExecutionRecord(rec);
                        View.AddUpdateExecutionRecord(rec);
                    }
                }
            }
        }
    }
}