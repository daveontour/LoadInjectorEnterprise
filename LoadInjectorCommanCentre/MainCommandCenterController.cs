using LoadInjector.Runtime.EngineComponents;
using LoadInjectorBase.Common;

using LoadInjectorCommandCentre.Views;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Xml;

namespace LoadInjectorCommandCentre {

    public class MainCommandCenterController {
        public MainWindow View { get; set; }
        public string AutoAssignArchive { get; private set; }
        public CentralMessagingHub MessageHub { get; set; }
        public string ArchiveRoot { get; }
        public string WebServerURL { get; }
        public SimpleHTTPServer WebServer { get; }
        public int NumClients { get; set; }
        public int SignalRPort { get; set; }
        public string ServerURL { get; }
        public ClientControl SelectedClient { get; set; }

        private Dictionary<string, ClientControl> clientControls = new Dictionary<string, ClientControl>();
        public Dictionary<string, ClientTabControl> clientTabControls = new Dictionary<string, ClientTabControl>();

        private int gridRefreshRate = 1;
        private Timer refreshTimer;

        public MainCommandCenterController(MainWindow mainWindow, int numClients, string signalRURL, string serverURL, string autoAssignArchive) {
            NumClients = numClients;
            ServerURL = serverURL;
            View = mainWindow;
            ArchiveRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LoadInjectorCommandCentre";
            this.AutoAssignArchive = autoAssignArchive;

            ClientTabControl tabControl = new ClientTabControl("Summary", this) { IsSummary = true, ConnectionID = "summary" };
            clientTabControls.Add("summary", tabControl);
            View.AddClientTab(tabControl);

            if (AutoAssignArchive != null && AutoAssignArchive != "") {
                string[] a = AutoAssignArchive.Split('\\');
                string safeName = a[a.Length - 1];
                File.Copy(AutoAssignArchive, ArchiveRoot + "\\" + safeName, true);
                AutoAssignArchive = safeName;
            } else {
                AutoAssignArchive = null;
            }

            MessageHub = new CentralMessagingHub(this);
            MessageHub.StartHub(signalRURL);

            int webport = Utils.GetAvailablePort(49152);
            WebServerURL = serverURL;

            WebServer = new SimpleHTTPServer(ArchiveRoot, WebServerURL);
            StartClients(NumClients);
        }

        public void Close() {
            WebServer.Stop();
            MessageHub.StoptHub();
        }

        public void ClearGridData(string connectionID = null) {
            View.RecordsCollection.Clear();
            View.OnPropertyChanged("RecordsCollection");

            foreach (ClientTabControl tabControl in clientTabControls.Values) {
                if (connectionID == null || tabControl.ConnectionID == connectionID) {
                    tabControl.TabExecutionRecords.Clear();
                    tabControl.OnPropertyChanged("TabExecutionRecords");
                    tabControl.ConsoleText = null;
                }

                if (tabControl.ConnectionID == "summary" && connectionID != null) {
                    ExecutionRecords newCollection = new ExecutionRecords();
                    foreach (ExecutionRecordClass rec in tabControl.TabExecutionRecords) {
                        if (rec.ConnectionID != connectionID) {
                            newCollection.Add(rec);
                        }
                    }

                    tabControl.TabExecutionRecords = newCollection;
                    tabControl.OnPropertyChanged("TabExecutionRecords");
                }
            }
            if (View.VisibleDataGrid == null) {
                View.EnumVisual(View.nodeTabHolder);
            }
            View.VisibleDataGrid?.Items.Refresh();
        }

        private void ClearTabs(string connectionID = null) {
            ObservableCollection<object> newCollection = new ObservableCollection<object>();
            newCollection.Add(View.ClientTabDatas[0]);

            if (connectionID != null) {
                foreach (var client in View.ClientTabDatas) {
                    if ((client as ClientTabControl).ConnectionID == connectionID || (client as ClientTabControl).ConnectionID == "summary") {
                        continue;
                    }
                    newCollection.Add(client);
                }
            }

            View.ClientTabDatas = newCollection;
            View.OnPropertyChanged("ClientTabDatas");
        }

        public void StartClients(int num) {

            try {
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
            } catch (Exception ex) {

            }
        }

        public void ClientConnectionInitiated(HubCallerContext context) {
            try {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    ClientControl client = new ClientControl(context.ConnectionId, MessageHub, this);
                    clientControls.Add(context.ConnectionId, client);
                    ClientTabControl tabControl = new ClientTabControl("PID Pending", this) { IsSummary = false, ConnectionID = context.ConnectionId };
                    if (AutoAssignArchive != null) {
                        tabControl.WorkPackage = AutoAssignArchive;
                    }
                    clientTabControls.Add(context.ConnectionId, tabControl);

                    View.AddClientControl(client);
                    View.AddClientTab(tabControl);
                });
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            MessageHub.Hub.Clients.Client(context.ConnectionId).Interrogate();

            if (AutoAssignArchive != null) {
                MessageHub.Hub.Clients.Client(context.ConnectionId).RetrieveArchive(WebServerURL + "/" + AutoAssignArchive);
            }
        }

        internal void ShowTab(string connectionID) {
            View.nodeTabHolder.SelectedItem = this.clientTabControls[connectionID];
        }

        public void InterrogateResponse(string processID, string ipAddress, string osversion, string xml, string status, HubCallerContext context) {
            if (!clientControls.ContainsKey(context.ConnectionId)) {
                try {
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ClientControl cl = new ClientControl(context.ConnectionId, MessageHub, this) { StatusText = status };
                        if (clientControls.ContainsKey(context.ConnectionId)) {
                            clientControls[context.ConnectionId] = cl;
                        } else {
                            clientControls.Add(context.ConnectionId, cl);
                        }

                        ClientTabControl tabControl = new ClientTabControl($"PID:{processID}", this) { IsSummary = false, ConnectionID = context.ConnectionId };

                        if (clientTabControls.ContainsKey(context.ConnectionId)) {
                            clientTabControls[context.ConnectionId] = tabControl;
                        } else {
                            clientTabControls.Add(context.ConnectionId, tabControl);
                        }
                        View.AddClientControl(cl);
                        View.AddClientTab(tabControl);
                    });
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }

            ClientControl client = clientControls[context.ConnectionId];
            ClientTabControl clientTabControl = clientTabControls[context.ConnectionId];
            ClientTabControl summaryTabControl = clientTabControls["summary"];

            clientTabControl.Header = $"PID:{processID}";
            clientTabControl.IP = ipAddress;
            clientTabControl.ProcessID = processID;
            clientTabControl.OSVersion = osversion;
            clientTabControl.StatusText = status;
            clientTabControl.XML = xml;

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
                            clientTabControl.ExecutionNodeID = rec.ExecutionNodeID;
                            clientTabControl.AddUpdateExecutionRecord(rec);
                            summaryTabControl.AddUpdateExecutionRecord(rec);
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
                            clientTabControl.ExecutionNodeID = rec.ExecutionNodeID;
                            clientTabControl.AddUpdateExecutionRecord(rec);
                            summaryTabControl.AddUpdateExecutionRecord(rec);
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
                            clientTabControl.ExecutionNodeID = rec.ExecutionNodeID;
                            clientTabControl.AddUpdateExecutionRecord(rec);
                            summaryTabControl.AddUpdateExecutionRecord(rec);
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
                            clientTabControl.ExecutionNodeID = rec.ExecutionNodeID;
                            clientTabControl.AddUpdateExecutionRecord(rec);
                            summaryTabControl.AddUpdateExecutionRecord(rec);
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);
                }
            }

            clientTabControl.OnPropertyChanged("Title");
        }

        public void PrepAll() {
            int ready = clientControls.Values.Count<ClientControl>(x => x.StatusText == ClientState.Assigned.Value);
            if (ready == 0) {
                MessageBoxResult res = MessageBox.Show($"No Execution Nodes are ready for preparation", "Prepare All", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int notready = clientControls.Values.Count<ClientControl>(x => x.StatusText != ClientState.Assigned.Value);
            if (notready > 0) {
                MessageBoxResult res = MessageBox.Show($"{notready} Execution Nodes have not been assigned work packages. Prepare nodes that have been assigned?", "Prepare All", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes) {
                    foreach (ClientControl c in clientControls.Values) {
                        if (c.StatusText == ClientState.Assigned.Value) {
                            MessageHub.Hub.Clients.Client(c.ConnectionID).ClearAndPrepare();
                        }
                    }
                }
                return;
            }

            MessageHub.Hub.Clients.All.ClearAndPrepare();
        }

        internal void ClearGrid() {
            ClearGridData();
        }

        public void ExecuteAll() {
            int ready = clientControls.Values.Count<ClientControl>(x => x.StatusText == ClientState.Ready.Value);
            if (ready == 0) {
                MessageBoxResult res = MessageBox.Show($"No Execution Nodes are ready for execution", "Execute All", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int notready = clientControls.Values.Count<ClientControl>(x => x.StatusText != ClientState.Ready.Value);
            if (notready > 0) {
                MessageBoxResult res = MessageBox.Show($"{notready} Execution Nodes are not ready to execute. Execute nodes that are ready?", "Execute All", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes) {
                    foreach (ClientControl c in clientControls.Values) {
                        if (c.StatusText == ClientState.Ready.Value) {
                            MessageHub.Hub.Clients.Client(c.ConnectionID).Execute();
                        }
                    }
                }
                return;
            }

            MessageHub.Hub.Clients.All.Execute();
            SetRefreshRate(gridRefreshRate);
        }

        public void ExecuteClient(string clientID) {
            MessageHub.Hub.Clients.Client(clientID).Execute();
            SetRefreshRate();
        }

        public void StopAll() {
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
                View.nodeTabHolder.SelectedIndex = 0;
                ClearGridData();
                if (View.VisibleDataGrid == null) {
                    View.EnumVisual(View.nodeTabHolder);
                }

                View.VisibleDataGrid.Items.Refresh();

                foreach (ClientTabControl tabControl in clientTabControls.Values) {
                    tabControl.WorkPackage = open.SafeFileName;
                    tabControl.OnPropertyChanged("WorkPackage");
                }
                MessageHub.Hub.Clients.All.RetrieveArchive(WebServerURL + "/" + open.SafeFileName);
            }
        }

        internal void DisconnectAll() {
            MessageBoxResult res = MessageBox.Show($"Do you really want to disconnect and shutdown all the connected clients?", "Disconnet Client", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) {
                return;
            }

            ClearTabs();
            ClearGridData();

            MessageHub.Hub.Clients.All.Disconnect();
            try {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    SelectedClient = null;
                    View.RecordsCollection.Clear();
                });
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        public void ResetAll() {
            MessageBoxResult res = MessageBox.Show($"Do you really want to reset all the connected clients?", "Reset Clients", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) {
                return;
            }
            Application.Current.Dispatcher.Invoke((Action)delegate
            {
                ClearGridData();
                View.VisibleDataGrid?.Items.Refresh();
            });

            try {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    View.RecordsCollection.Clear();
                    View.VisibleDataGrid?.Items?.Refresh();
                });
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

            MessageHub.Hub.Clients.All.Reset();
        }

        internal void DisconnectClient(string id) {
            //Tell the client to disconnect

            ClearTabs(id);
            ClearGridData(id);
            MessageHub.Hub.Clients.Client(id).Disconnect();

            try {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    // The client control for the disconnecting
                    ClientControl client = clientControls.Values.FirstOrDefault<ClientControl>(x => x.ConnectionID == id);
                    //Remove it from the list of execution nodes.
                    View.clientControlStack.Children.Remove(client);
                    View.OnPropertyChanged("NumConnectedClients");

                    //Remove any entries from the messaging grid
                    var query = View.RecordsCollection.ToList<ExecutionRecordClass>().Where(rec => rec.ConnectionID == id);
                    foreach (ExecutionRecordClass x in query) {
                        View.RecordsCollection.Remove(x);
                    }

                    MessageHub.Hub.Clients.All.Refresh();
                    View.VisibleDataGrid?.Items?.Refresh();
                });
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        internal void RefreshClients(bool clear = true) {
            View.RecordsCollection.Clear();
            //View.statusGrid.Items.Refresh();
            if (clear) {
                View.clientControlStack.Children.RemoveRange(0, View.clientControlStack.Children.Count);
                View.OnPropertyChanged("NumConnectedClients");
                ClearGridData();
                ClearTabs();
            }
            clientControls.Clear();
            MessageHub.Hub.Clients.All.Refresh();
        }

        public void RefreshResponse(string processID, string ipAddress, string osversion, string xml, string status, Dictionary<string, Tuple<string, string, string, int, double, double>> latestSourceReport, Dictionary<string, Tuple<string, string, int, int, double>> latestDestinationReport, HubCallerContext context) {
            InterrogateResponse(processID, ipAddress, osversion, xml, status, context);
            foreach (Tuple<string, string, string, int, double, double> rec in latestSourceReport.Values) {
                UpdateSourceLine(rec.Item1, rec.Item2, rec.Item3, rec.Item4, rec.Item5, rec.Item6, context, true);
            }

            foreach (Tuple<string, string, int, int, double> rec in latestDestinationReport.Values) {
                UpdateDestinationLine(rec.Item1, rec.Item2, rec.Item3, rec.Item4, context, true);
            }
        }

        public void UpdateSourceLine(string executionNodeID, string uuid, string message, int messagesSent, double currentRate, double messagesPerMinute, HubCallerContext context, bool forceUpdate = false) {
            if (clientControls.ContainsKey(context.ConnectionId)) {
                Application.Current.Dispatcher.Invoke(delegate
                {
                    try {
                        ExecutionRecordClass r = View.RecordsCollection.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == uuid);

                        if (r != null) {
                            r.MM = currentRate.ToString();
                            r.Sent = messagesSent;
                            if (this.gridRefreshRate == 0 || forceUpdate) {
                                View.VisibleDataGrid?.Items.Refresh();
                            }
                        }
                    } catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                    }
                });
            }
        }

        public void UpdateDestinationLine(string executionNodeID, string uuid, int messagesSent, int messageFail, HubCallerContext context, bool forceUpdate = false) {
            if (clientControls.ContainsKey(context.ConnectionId)) {
                try {
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        ExecutionRecordClass r = View.RecordsCollection.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == uuid);
                        if (r != null) {
                            r.Sent = messagesSent;
                            r.Fail = messageFail;
                            if (this.gridRefreshRate == 0 || forceUpdate) {
                                View.VisibleDataGrid?.Items.Refresh();
                            }
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
                    if (View.VisibleDataGrid == null) {
                        View.EnumVisual(View.nodeTabHolder);
                    }
                    View.VisibleDataGrid?.Items.Refresh();
                } catch (Exception ex) {
                    Debug.WriteLine("Updating Grid Error. " + ex.Message);
                }
            });
        }

        public void Disconnect(HubCallerContext context) {
            if (clientControls.ContainsKey(context.ConnectionId)) {
                ClientControl client = clientControls[context.ConnectionId];
                clientControls.Remove(context.ConnectionId);
                Application.Current.Dispatcher.Invoke(delegate
                {
                    try {
                        View.RemoveClientControl(client);
                        View.RemoveClientTab(client.ConnectionID);
                    } catch (Exception ex) {
                        Debug.WriteLine("Removing Client Error. " + ex.Message);
                    }
                });
            }
        }

        public void SetExecutionNodeStatus(string executionNodeID, string message, HubCallerContext context) {
            Application.Current.Dispatcher.Invoke(delegate
            {
                if (clientControls.ContainsKey(context.ConnectionId)) {
                    ClientControl client = clientControls[context.ConnectionId];
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
                if (clientControls.ContainsKey(context.ConnectionId)) {
                    ClientControl client = clientControls[context.ConnectionId];
                    client.SetCompletionReportText(report);
                }
            });
        }

        public void SetConsoleMessage(string message, HubCallerContext context) {
            ClientTabControl clientTabControl = clientTabControls[context.ConnectionId];
            Application.Current.Dispatcher.Invoke(delegate
            {
                clientTabControl.ConsoleText = clientTabControl.ConsoleText + message + "\n";
            });
        }
    }
}