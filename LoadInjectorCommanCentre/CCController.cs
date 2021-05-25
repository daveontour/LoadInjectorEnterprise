using LoadInjector.Runtime.EngineComponents;
using LoadInjectorBase.Commom;
using LoadInjectorBase.Common;
using LoadInjectorCommanCentre.Views;
using LoadInjectorCommandCentre;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Xml;

namespace LoadInjectorCommanCentre {

    public class CCController {
        public MainWindow View { get; set; }
        public string AutoAssignArchive { get; private set; }
        public CentralMessagingHub MessageHub { get; set; }
        public string ArchiveRoot { get; }

        public string WebServerURL { get; }
        public SimpleHTTPServer WebServer { get; }
        public int NumClients { get; set; }
        public int SignalRPort { get; set; }
        public string ServerURL { get; }

        private Dictionary<string, ClientControl> clients = new Dictionary<string, ClientControl>();

        private int gridRefreshRate = 1;
        private Timer refreshTimer;
        private ClientControl selectedClient;

        public ClientControl SelectedClient {
            get { return selectedClient; }
            set { selectedClient = value; }
        }

        //public CCController(MainWindow mainWindow) {
        //    View = mainWindow;
        //    MessageHub = new CentralMessagingHub(this);
        //    MessageHub.StartHub();

        //    ArchiveRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LoadInjectorCommandCentre";
        //    int webport = GetAvailablePort(49152);
        //    WebServerURL = $"http://localhost:{webport}/";
        //    WebServer = new SimpleHTTPServer(ArchiveRoot, WebServerURL);
        //    Console.WriteLine("Webserver Port: " + webport);

        //    StartClients(4);
        //}

        public CCController(MainWindow mainWindow, int numClients, string signalRURL, string serverURL, string autoAssignArchive) {
            NumClients = numClients;

            ServerURL = serverURL;

            View = mainWindow;

            ArchiveRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LoadInjectorCommandCentre";

            this.AutoAssignArchive = autoAssignArchive;
            if (AutoAssignArchive != null) {
                string[] a = AutoAssignArchive.Split('\\');
                string safeName = a[a.Length - 1];
                File.Copy(AutoAssignArchive, ArchiveRoot + "\\" + safeName, true);
                AutoAssignArchive = safeName;
            }

            MessageHub = new CentralMessagingHub(this);
            MessageHub.StartHub(signalRURL);

            int webport = Utils.GetAvailablePort(49152);
            // WebServerURL = $"http://localhost:{webport}/";
            WebServerURL = serverURL;

            WebServer = new SimpleHTTPServer(ArchiveRoot, WebServerURL);
            //Console.WriteLine("Webserver Port: " + webport);

            StartClients(NumClients);
        }

        public void Close() {
            WebServer.Stop();
        }

        public void StartClients(int num) {
            for (int i = 0; i < num; i++) {
                Process process = new Process();
                // Configure the process using the StartInfo properties.

                string lir = @"C:\Users\dave_\source\repos\LoadInjectorEnterprise\LoadInjectorRuntime\bin\Debug\LoadInjectorRuntime.exe";
                process.StartInfo.WorkingDirectory = @"C:\Users\dave_\source\repos\LoadInjectorEnterprise\LoadInjectorRuntime\bin\Debug";
                process.StartInfo.FileName = lir;
                process.StartInfo.Arguments = $"-server:http://localhost:6220/";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

                process.Start();
            }
        }

        public void PrepAll() {
            View.prepAllBtn.IsEnabled = true;
            View.execAllBtn.IsEnabled = true;
            View.stopAllBtn.IsEnabled = false;

            MessageHub.Hub.Clients.All.ClearAndPrepare();
        }

        internal void ViewAll() {
            View.SetFilterCriteria(null);
            RefreshClients(true);
            View.ShowDetailPanel = Visibility.Collapsed;
        }

        public void ExecuteAll() {
            View.prepAllBtn.IsEnabled = false;
            View.execAllBtn.IsEnabled = false;
            View.stopAllBtn.IsEnabled = true;

            MessageHub.Hub.Clients.All.Execute();
            SetRefreshRate(gridRefreshRate);
        }

        public void ExecuteClient(string clientID) {
            MessageHub.Hub.Clients.Client(clientID).Execute();
            SetRefreshRate();
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
            try {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    SelectedClient = null;
                    View.RecordsCollection.Clear();
                    View.statusGrid.Items.Refresh();
                    View.ShowDetailPanel = Visibility.Collapsed;
                });
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        internal void DisconnectClient(string id) {
            //Tell the client to disconnect
            MessageHub.Hub.Clients.Client(id).Disconnect();

            try {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    // The client control for the disconnecting
                    ClientControl client = clients.Values.FirstOrDefault<ClientControl>(x => x.ConnectionID == id);
                    //Remove it from the list of execution nodes.
                    View.clientControlStack.Children.Remove(client);

                    //Remove any entries from the messaging grid
                    var query = View.RecordsCollection.ToList<ExecutionRecordClass>().Where(rec => rec.ConnectionID == id);
                    foreach (ExecutionRecordClass x in query) {
                        View.RecordsCollection.Remove(x);
                    }

                    // Close the detail panel if it was open for this connection
                    if (View.filterConnectionID == id) {
                        View.ShowDetailPanel = Visibility.Collapsed;
                        View.SetFilterCriteria(null);
                    }
                    MessageHub.Hub.Clients.All.Refresh();
                    View.statusGrid.Items.Refresh();
                });
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        internal void RefreshClients(bool clear = true) {
            View.RecordsCollection.Clear();
            View.statusGrid.Items.Refresh();
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
            SelectedClient = client;

            if (client != null) {
                View.configConsole.Text = Utils.FormatXML(client.XML);
                View.consoleWriter.Clear();

                // Enable details for the selected client
                MessageHub.Hub.Clients.Client(ConnectionID).EnableDetails();
            }
        }

        // Called after a client makes a connection
        public void InitialInterrogation(HubCallerContext context) {
            Console.Write("New Client Connection" + context.ConnectionId);

            try {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    ClientControl client = new ClientControl(context.ConnectionId, MessageHub, this);
                    clients.Add(context.ConnectionId, client);
                    View.clientControlStack.Children.Add(client);
                    View.nodeTabHolder.Items.Insert(View.nodeTabHolder.Items.Count - 1, new UserControl1());
                });
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            MessageHub.Hub.Clients.Client(context.ConnectionId).Interrogate();

            if (AutoAssignArchive != null) {
                MessageHub.Hub.Clients.Client(context.ConnectionId).RetrieveArchive(WebServerURL + "/" + AutoAssignArchive);
            }
        }

        public void RefreshResponse(string processID, string ipAddress, string osversion, string xml, string status, Dictionary<string, Tuple<string, string, string, int, double, double>> latestSourceReport, Dictionary<string, Tuple<string, string, int, double>> latestDestinationReport, HubCallerContext context) {
            InterrogateResponse(processID, ipAddress, osversion, xml, status, context);
            foreach (Tuple<string, string, string, int, double, double> rec in latestSourceReport.Values) {
                UpdateSourceLine(rec.Item1, rec.Item2, rec.Item3, rec.Item4, rec.Item5, rec.Item6, context, true);
            }

            foreach (Tuple<string, string, int, double> rec in latestDestinationReport.Values) {
                UpdateDestinationLine(rec.Item1, rec.Item2, rec.Item3, context, true);
            }
        }

        public void InterrogateResponse(string processID, string ipAddress, string osversion, string xml, string status, HubCallerContext context) {
            if (!clients.ContainsKey(context.ConnectionId)) {
                try {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
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
                                Protocol = node.Attributes["protocol"]?.Value,
                                ConnectionID = context.ConnectionId
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
                                Protocol = node.Attributes["dataSource"]?.Value,
                                ConnectionID = context.ConnectionId
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
                                SourceDestination = "Data Driven Source",
                                ConnectionID = context.ConnectionId
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
                                SourceDestination = "Chained Source",
                                ConnectionID = context.ConnectionId
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

        public void UpdateSourceLine(string executionNodeID, string uuid, string message, int messagesSent, double currentRate, double messagesPerMinute, HubCallerContext context, bool forceUpdate = false) {
            if (clients.ContainsKey(context.ConnectionId)) {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    try {
                        ExecutionRecordClass r = View.RecordsCollection.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == uuid);

                        if (r != null) {
                            r.MM = currentRate.ToString();
                            r.Sent = messagesSent;
                            if (this.gridRefreshRate == 0 || forceUpdate) {
                                View.statusGrid.Items.Refresh();
                            }
                        } else {
                            ExecutionRecordClass rec = new ExecutionRecordClass() {
                                Sent = messagesSent,
                                MM = currentRate.ToString(),
                                ExecutionLineID = uuid,
                                ExecutionNodeID = executionNodeID,
                                ConnectionID = context.ConnectionId
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

        public void UpdateDestinationLine(string executionNodeID, string uuid, int messagesSent, HubCallerContext context, bool forceUpdate = false) {
            if (clients.ContainsKey(context.ConnectionId)) {
                try {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        ExecutionRecordClass r = View.RecordsCollection.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == uuid);
                        if (r != null) {
                            r.Sent = messagesSent;
                            if (this.gridRefreshRate == 0 || forceUpdate) {
                                View.statusGrid.Items.Refresh();
                            }
                        } else {
                            ExecutionRecordClass rec = new ExecutionRecordClass() {
                                Sent = messagesSent,
                                ExecutionLineID = uuid,
                                ExecutionNodeID = executionNodeID,
                                ConnectionID = context.ConnectionId
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
            Application.Current.Dispatcher.Invoke(delegate
            {
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
                Application.Current.Dispatcher.Invoke(delegate
                {
                    try {
                        View.clientControlStack.Children.Remove(client);
                    } catch (Exception ex) {
                        Debug.WriteLine("Removing Client Error. " + ex.Message);
                    }
                });
            }
        }

        public void SetExecutionNodeStatus(string executionNodeID, string message, HubCallerContext context) {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (clients.ContainsKey(context.ConnectionId)) {
                    ClientControl client = clients[context.ConnectionId];
                    client.SetStatusText(message);
                }
            });

            if (message == ClientState.Assigned.Value && View.AutoExecute) {
                MessageHub.Hub.Clients.Client(context.ConnectionId).ClearAndPrepare();
            }

            if (message == ClientState.Ready.Value && View.AutoExecute) {
                SetRefreshRate(gridRefreshRate);
                MessageHub.Hub.Clients.Client(context.ConnectionId).Execute();
            }
        }

        public void SetCompletionReport(string executionNodeID, CompletionReport report, HubCallerContext context) {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (clients.ContainsKey(context.ConnectionId)) {
                    ClientControl client = clients[context.ConnectionId];
                    client.SetCompletionReportText(report);
                }
            });
        }

        public void SetConsoleMessage(string message) {
            Application.Current.Dispatcher.Invoke(delegate
            {
                View.consoleWriter.WriteLineNoDate(message);
            });
        }
    }
}