using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Xml;
using LoadInjector.Runtime.EngineComponents;
using LoadInjectorCommanCentre.Views;
using LoadInjectorCommandCentre;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Win32;

namespace LoadInjectorCommanCentre {

    public class CCController : ICCController {
        public MainWindow View { get; set; }
        public CentralMessagingHub MessageHub { get; set; }
        public string ArchiveRoot { get; }

        public string WebServerURL { get; }
        public SimpleHTTPServer WebServer { get; }

        private Dictionary<string, ClientControl> clients = new Dictionary<string, ClientControl>();

        private int gridRefreshRate = 1;
        private Timer refreshTimer;

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
            SetRefreshRate(gridRefreshRate);
        }

        public void StopAll() {
            View.prepAllBtn.IsEnabled = true;
            View.execAllBtn.IsEnabled = false;
            View.stopAllBtn.IsEnabled = false;

            MessageHub.Hub.Clients.All.Stop();
        }

        internal void AssignAll() {
            string archiveRoot = ArchiveRoot;
            Directory.CreateDirectory(archiveRoot);

            OpenFileDialog open = new OpenFileDialog {
                Filter = "Load Injector Archive Files(*.lia)|*.lia",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (open.ShowDialog() == true) {
                File.Copy(open.FileName, archiveRoot + "\\" + open.SafeFileName, true);
                MessageHub.Hub.Clients.All.RetrieveArchive(WebServerURL + "/" + open.SafeFileName);
            }
        }

        internal void DisconnectAll() {
            MessageHub.Hub.Clients.All.Disconnect();
        }

        internal void RefreshClients(bool clear = true) {
            View.RecordsCollection.Clear();
            if (clear) {
                View.clientControlStack.Children.RemoveRange(0, View.clientControlStack.Children.Count);
            }
            clients.Clear();
            MessageHub.Hub.Clients.All.Refresh();
        }

        internal void SetFilterCriteria(string executionNodeID, string ConnectionID) {
            View.SetFilterCriteria(executionNodeID, ConnectionID);

            // Disable details from all clients
            MessageHub.Hub.Clients.All.DisableDetails();
            ClientControl client = clients.Values.FirstOrDefault<ClientControl>(x => x.ExecutionNodeID == executionNodeID);

            if (client != null) {
                // The config for the selected node
                StringBuilder sb = new StringBuilder();
                TextWriter tr = new StringWriter(sb);
                XmlTextWriter wr = new XmlTextWriter(tr) {
                    Formatting = Formatting.Indented
                };

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(client.XML);
                doc.Save(wr);
                wr.Close();

                View.configConsole.Text = sb.ToString();

                View.consoleWriter.Clear();

                // Enable details for the selected client
                foreach (KeyValuePair<string, ClientControl> pair in clients) {
                    if (pair.Value.ExecutionNodeID == executionNodeID) {
                        MessageHub.Hub.Clients.Client(pair.Key).EnableDetails();
                        break;
                    }
                }
            }
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

        public void RefreshResponse(string processID, string ipAddress, string osversion, string xml, string status, HubCallerContext context) {
            InterrogateResponse(processID, ipAddress, osversion, xml, status, context);
        }

        public void InterrogateResponse(string processID, string ipAddress, string osversion, string xml, string status, HubCallerContext context) {
            if (!clients.ContainsKey(context.ConnectionId)) {
                try {
                    Application.Current.Dispatcher.Invoke((Action)delegate {
                        ClientControl cl = new ClientControl(context.ConnectionId, MessageHub, this) { StatusText = status };
                        clients.Add(context.ConnectionId, cl);

                        View.clientControlStack.Children.Add(cl);
                        View.OnPropertyChanged("ShowDetailPanel");
                    });
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }

            ClientControl client = clients[context.ConnectionId];

            client.IP = ipAddress;
            client.ProcessID = processID;
            client.OSVersion = osversion;
            client.StatusText = status;
            client.XML = xml;

            if (xml != null) {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                try {
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
                                ExecutionNodeID = node.Attributes["executionNodeUuid"]?.Value,
                                SourceDestination = "Destination",
                                Protocol = node.Attributes["protocol"]?.Value
                            };

                            View.AddUpdateExecutionRecord(rec);
                            client.ExecutionNodeID = rec.ExecutionNodeID;
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
                                ExecutionNodeID = node.Attributes["executionNodeUuid"]?.Value,
                                SourceDestination = "Rate Driven Source",
                                Protocol = node.Attributes["dataSource"]?.Value
                            };

                            View.AddUpdateExecutionRecord(rec);
                            client.ExecutionNodeID = rec.ExecutionNodeID;
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
                                ExecutionNodeID = node.Attributes["executionNodeUuid"]?.Value,
                                SourceDestination = "Data Driven Source"
                            };

                            View.AddUpdateExecutionRecord(rec);
                            client.ExecutionNodeID = rec.ExecutionNodeID;
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
                                ExecutionNodeID = node.Attributes["executionNodeUuid"]?.Value,
                                SourceDestination = "Chained Source"
                            };

                            View.AddUpdateExecutionRecord(rec);
                            client.ExecutionNodeID = rec.ExecutionNodeID;
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public void UpdateLine(string executionNodeID, string uuid, string message, int messagesSent, double currentRate, double messagesPerMinute, HubCallerContext context) {
            if (clients.ContainsKey(context.ConnectionId)) {
                Application.Current.Dispatcher.Invoke(delegate {
                    try {
                        ExecutionRecordClass r = View.RecordsCollection.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == uuid);

                        if (r != null) {
                            r.MM = currentRate.ToString();
                            r.Sent = messagesSent;
                            if (this.gridRefreshRate == 0) {
                                View.statusGrid.Items.Refresh();
                            }
                        } else {
                            ExecutionRecordClass rec = new ExecutionRecordClass() {
                                Sent = messagesSent,
                                MM = currentRate.ToString(),
                                ExecutionLineID = uuid,
                                ExecutionNodeID = executionNodeID
                            };
                            View.RecordsCollection.Add(rec);
                            View.statusGrid.Items.Refresh();
                        }
                    } catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                    }
                });
            }
        }

        public void UpdateLine(string executionNodeID, string uuid, int messagesSent, HubCallerContext context) {
            if (clients.ContainsKey(context.ConnectionId)) {
                try {
                    Application.Current.Dispatcher.Invoke(delegate {
                        ExecutionRecordClass r = View.RecordsCollection.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == uuid);
                        if (r != null) {
                            r.Sent = messagesSent;
                            if (this.gridRefreshRate == 0) {
                                View.statusGrid.Items.Refresh();
                            }
                        } else {
                            ExecutionRecordClass rec = new ExecutionRecordClass() {
                                Sent = messagesSent,
                                ExecutionLineID = uuid,
                                ExecutionNodeID = executionNodeID
                            };
                            View.RecordsCollection.Add(rec);
                            View.statusGrid.Items.Refresh();
                        }
                    });
                } catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        public void SetRefreshRate() {
            SetRefreshRate(this.gridRefreshRate);
        }

        internal void SetRefreshRate(int gridRefreshRate) {
            this.gridRefreshRate = gridRefreshRate;

            refreshTimer?.Stop();

            if (this.gridRefreshRate == 0) {
                return;
            }

            refreshTimer = new Timer {
                Interval = gridRefreshRate * 1000,
                AutoReset = true,
                Enabled = true,
            };
            refreshTimer.Elapsed += OnRefreshGridEvent;
        }

        private void OnRefreshGridEvent(object sender, ElapsedEventArgs e) {
            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    View.statusGrid.Items.Refresh();
                } catch (Exception ex) {
                    Debug.WriteLine("Updating Grid Error. " + ex.Message);
                }
            });
        }

        public void Disconnect(HubCallerContext context) {
            if (clients.ContainsKey(context.ConnectionId)) {
                ClientControl client = clients[context.ConnectionId];
                clients.Remove(context.ConnectionId);
                Application.Current.Dispatcher.Invoke(delegate {
                    try {
                        View.clientControlStack.Children.Remove(client);
                    } catch (Exception ex) {
                        Debug.WriteLine("Removing Client Error. " + ex.Message);
                    }
                });
            }
        }

        public void SetExecutionNodeStatus(string executionNodeID, string message, HubCallerContext context) {
            Application.Current.Dispatcher.Invoke(delegate {
                if (clients.ContainsKey(context.ConnectionId)) {
                    ClientControl client = clients[context.ConnectionId];
                    client.SetStatusText(message);
                }
            });
        }

        public void SetConsoleMessage(string message) {
            Application.Current.Dispatcher.Invoke(delegate {
                View.consoleWriter.WriteLine(message);
            });
        }
    }
}