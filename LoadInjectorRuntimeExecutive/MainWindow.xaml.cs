using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using LoadInjector.Common;
using LoadInjector.Runtime.EngineComponents;
using LoadInjector.RunTime;
using LoadInjector.RunTime.Views;
using LoadInjector.Views;

namespace LoadInjectorRuntimeExecutive {

    public partial class MainWindow : Window, IExecutionUI, INotifyPropertyChanged {
        private XmlDocument dataModel;
        private int duration;
        private int repeats;
        private int repeatRest;
        private int restRemaining;
        private Timer percentCompleteTimer;
        private Timer executionTimer;
        private int repeatsExecuted = 0;
        private Timer repetitionTimer;
        private Timer restSecondTimer;

        public event PropertyChangedEventHandler PropertyChanged;

        public LoadInjector.RunTime.Views.ControlWriter consoleWriter;

        public StackPanel GetLinePanel() => Lines;

        public StackPanel GetSourcePanel() => Sources;

        private readonly Stopwatch stopWatch = new Stopwatch();

        public readonly List<LineUserControl> amsLinesUserControls = new List<LineUserControl>();
        public readonly List<LineUserControl> directLinesUserControls = new List<LineUserControl>();
        public readonly List<TriggeredEventsUI> amsDataDrivenLinesUserControls = new List<TriggeredEventsUI>();
        public readonly List<TriggeredEventsUI> csvDataDrivenLinesUserControls = new List<TriggeredEventsUI>();
        public readonly List<TriggeredEventsUI> excelDataDrivenLinesUserControls = new List<TriggeredEventsUI>();
        public readonly List<TriggeredEventsUI> xmlDataDrivenLinesUserControls = new List<TriggeredEventsUI>();
        public readonly List<TriggeredEventsUI> jsonDataDrivenLinesUserControls = new List<TriggeredEventsUI>();
        public readonly List<TriggeredEventsUI> databaseDataDrivenLinesUserControls = new List<TriggeredEventsUI>();
        public readonly List<RateDrivenEventsUI> rateDrivenLinesUserControls = new List<RateDrivenEventsUI>();
        public readonly List<ChainedEventsUI> chainDrivenLinesUserControls = new List<ChainedEventsUI>();

        public readonly Dictionary<string, LineUserControl> destUIMap = new Dictionary<string, LineUserControl>();
        public readonly Dictionary<string, SourceUI> sourceUIMap = new Dictionary<string, SourceUI>();

        private NgExecutionController cnt;

        public ObservableCollection<TriggerRecord> SchedTriggers { get; set; }
        public ObservableCollection<TriggerRecord> FiredTriggers { get; set; }

        private CentralMessagingHub centralMessagingHub;

        public Dictionary<string, LineUserControl> DestUIMap { get => destUIMap; }
        public Dictionary<string, SourceUI> SourceUIMap { get => sourceUIMap; }
        public ControlWriter ConsoleWriter { get => consoleWriter; }
        private bool disableScroll;

        public bool DisableScroll {
            get => disableScroll;
            set {
                disableScroll = value;
                consoleWriter.DisableScroll = value;
            }
        }

        private int percentComplete;

        public int PercentComplete {
            get => percentComplete;
            set {
                percentComplete = value;
                OnPropertyChanged("PercentComplete");
            }
        }

        private string elapsedString = "00:00:00";

        public string ElapsedString {
            get => elapsedString;
            set {
                elapsedString = value;
                OnPropertyChanged("ElapsedString");
            }
        }

        private string statusText;

        public string StatusLabel {
            get => statusText;
            set {
                statusText = value;
                OnPropertyChanged("StatusLabel");
            }
        }

        private string trigLabel = "Available Triggers";

        public string TriggerLabel {
            get => trigLabel;
            set {
                trigLabel = value;
                OnPropertyChanged("TriggerLabel");
            }
        }

        private string statusMessage = "No configuration file loaded";

        public string STATUSMESSAGE {
            get => statusMessage;
            set {
                statusMessage = value;
                OnPropertyChanged("STATUSMESSAGE");
            }
        }

        public string hubPort;

        public string HUBPORT {
            get => hubPort;
            set {
                hubPort = value;
                OnPropertyChanged("HUBPORT");
            }
        }

        public void OnPropertyChanged(string propName) {
            try {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            } catch (Exception ex) {
                Console.WriteLine("On Property Error. " + ex.Message);
            }
        }

        private string[] args = null;

        public MainWindow(string[] args = null) {
            this.args = args;

            InitializeComponent();
            DataContext = this;

            SchedTriggers = new ObservableCollection<TriggerRecord>();
            lvTriggers.ItemsSource = SchedTriggers;
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(lvTriggers.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("TIME", ListSortDirection.Ascending));

            FiredTriggers = new ObservableCollection<TriggerRecord>();
            lvFiredTriggers.ItemsSource = FiredTriggers;
            CollectionView view2 = (CollectionView)CollectionViewSource.GetDefaultView(lvFiredTriggers.ItemsSource);
            view2.SortDescriptions.Add(new SortDescription("TIME", ListSortDirection.Descending));

            commandBarView.StopRequested += new EventHandler(Stop_Click);
            commandBarView.PrepareRequested += new EventHandler(Prepare_Click);
            commandBarView.LIExecuteRequested += new EventHandler(Execute_Click);
            commandBarView.DocumentLoaded += new EventHandler<DocumentLoadedEventArgs>(OnDocumentLoaded);

            SetExecuteBtnEnabled(false);
            SetPrepareBtnEnabled(false);
            SetStopBtnEnabled(false);
            StartUp();
        }

        private void StartUp() {
            if (args == null) {
                //NO-OP
            } else {
                XmlDocument document = new XmlDocument();
                try {
                    string filename = args[0];
                    document.Load(filename);
                    DocumentLoadedEventArgs a = new DocumentLoadedEventArgs() { Path = filename, Document = document, FileName = filename };
                    commandBarView.OnDocumentLoaded(this, a);
                } catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        public void OnDocumentLoaded(object sender, DocumentLoadedEventArgs e) {
            DataModel = e.Document;
        }

        public XmlDocument DataModel {
            get => dataModel;
            set {
                dataModel = value;

                StringBuilder sb = new StringBuilder();
                TextWriter tr = new StringWriter(sb);
                XmlTextWriter wr = new XmlTextWriter(tr) {
                    Formatting = Formatting.Indented
                };
                DataModel.Save(wr);
                wr.Close();
                configConsole.Text = sb.ToString();

                string executionNodeID = Guid.NewGuid().ToString();

                // Add unique Identifier for each node to the XML as well as an ID for the execution node
                foreach (XmlNode node in dataModel.SelectNodes("//*")) {
                    XmlAttribute newAttribute = DataModel.CreateAttribute("uuid");
                    newAttribute.Value = Guid.NewGuid().ToString();
                    node.Attributes.Append(newAttribute);

                    XmlAttribute newAttribute2 = DataModel.CreateAttribute("executionNodeUuid");
                    newAttribute2.Value = executionNodeID;
                    node.Attributes.Append(newAttribute2);
                }

                XDocument doc = XDocument.Parse(dataModel.OuterXml);
                try {
                    duration = int.Parse(doc.Descendants("duration").FirstOrDefault().Value);
                } catch (Exception) {
                    duration = 15;
                }

                try {
                    repeats = int.Parse(doc.Descendants("repeats").FirstOrDefault().Value);
                } catch (Exception) {
                    repeats = 1;
                }

                try {
                    repeatRest = int.Parse(doc.Descendants("repeatRest").FirstOrDefault().Value);
                } catch (Exception) {
                    repeatRest = 0;
                }

                PrepareLineUI();

                SetPrepareBtnEnabled(true);

                //     if (args == null) {
                STATUSMESSAGE = "Configuration File Loaded";
                //     } else {
                //         STATUSMESSAGE = "Auto Start Enabled";
                //         Prepare_Click(null, null);
                //         Execute_Click(null, null);
                //     }
            }
        }

        public void PrepareLineUI() {
            STATUSMESSAGE = "Configuring UI Lines";

            //Add the data driven sources
            bool hasAMDDDLines = false;

            XmlNodeList amsEventDriven = dataModel.SelectNodes("//amsdatadriven");
            XmlNodeList csvEventDriven = dataModel.SelectNodes("//csvdatadriven");
            XmlNodeList excelEventDriven = dataModel.SelectNodes("//exceldatadriven");
            XmlNodeList xmlEventDriven = dataModel.SelectNodes("//xmldatadriven");
            XmlNodeList jsonEventDriven = dataModel.SelectNodes("//jsondatadriven");
            XmlNodeList databaseEventDriven = dataModel.SelectNodes("//databasedatadriven");
            XmlNodeList rateDriven = dataModel.SelectNodes("//ratedriven");
            XmlNodeList amsDirect = dataModel.SelectNodes("//amsdirect");
            XmlNodeList destinations = dataModel.SelectNodes("//destination");

            hasAMDDDLines = AddDataDrivenUI("AMS Data Driven Source", amsEventDriven, amsDataDrivenLinesUserControls);
            bool hasCSV = AddDataDrivenUI("CSV Data Driven Source", csvEventDriven, csvDataDrivenLinesUserControls);
            bool hasExcel = AddDataDrivenUI("Excel Data Driven Source", excelEventDriven, excelDataDrivenLinesUserControls);
            bool hasXML = AddDataDrivenUI("XML Data Driven Source", xmlEventDriven, xmlDataDrivenLinesUserControls);
            bool hasJSON = AddDataDrivenUI("JSON Data Driven Source", jsonEventDriven, jsonDataDrivenLinesUserControls);
            bool hasDB = AddDataDrivenUI("Database Data Driven Source", databaseEventDriven, databaseDataDrivenLinesUserControls);

            //Add the rateDataDriven lines
            if (rateDrivenLinesUserControls.Count > 0) {
                AddSourceLabel("Rate Driven Source");
            }
            foreach (XmlNode node in rateDriven) {
                if (CheckDisabled(node)) {
                    Disabled d = new Disabled(node);
                    GetSourcePanel().Children.Add(d);
                    continue;
                }
                RateDrivenEventsUI lineUI = new RateDrivenEventsUI(node, 0);
                GetSourcePanel().Children.Add(lineUI);
                sourceUIMap.Add(node.Attributes["uuid"].Value, lineUI);
                rateDrivenLinesUserControls.Add(lineUI);
                //source.SetLineProgress(lineUI.controllerProgress);

                foreach (XmlNode ch in node.SelectNodes("./chained")) {
                    this.AddChainedUI(1, ch);
                }
            }

            if (hasJSON || hasXML || hasExcel || hasCSV || hasExcel || hasDB || hasAMDDDLines) {
                // Hide Trigger Tabs.
                TabItem tab = (TabItem)tabControl.Items.GetItemAt(1);
                tab.Visibility = Visibility.Visible;
                TabItem tab2 = (TabItem)tabControl.Items.GetItemAt(2);
                tab2.Visibility = Visibility.Visible;
            }

            // Add the AMS Direct Injection Lines
            if (amsDirect.Count > 0) {
                AddLabel("AMS Direct Update Destinations");
            }
            foreach (XmlNode node in amsDirect) {
                if (CheckDisabled(node)) {
                    Disabled d = new Disabled(node);
                    GetLinePanel().Children.Add(d);
                    continue;
                }
                LineUserControl lineUI = new LineUserControl(node);
                GetLinePanel().Children.Add(lineUI);
                amsLinesUserControls.Add(lineUI);
            }

            // Add the Destination Lines
            if (destinations.Count > 0) {
                AddLabel("Destinations");
            }
            foreach (XmlNode node in destinations) {
                if (CheckDisabled(node)) {
                    Disabled d = new Disabled(node);
                    GetLinePanel().Children.Add(d);
                    continue;
                }
                LineUserControl lineUI = new LineUserControl(node);
                GetLinePanel().Children.Add(lineUI);
                directLinesUserControls.Add(lineUI);
                destUIMap.Add(node.Attributes["uuid"].Value, lineUI);
            }

            if (centralMessagingHub == null) {
                int port = GetAvailablePort(49152);
                centralMessagingHub = new CentralMessagingHub(port);
                centralMessagingHub.SetExecutionUI(this);
                centralMessagingHub.StartHub();
                cnt = new NgExecutionController(this.centralMessagingHub.port);
                HUBPORT = port.ToString();
            }

            outputConsole.Clear();
            SchedTriggers.Clear();

            // Sent Seq Numbers keep track of the highest messageSent, so we dont process out of sequence messages
            foreach (LineUserControl li in this.directLinesUserControls) {
                li.SentSeqNumber = 0;
            }
            foreach (LineUserControl li in this.amsLinesUserControls) {
                li.SentSeqNumber = 0;
            }

            foreach (SourceUI src in this.sourceUIMap.Values) {
                src.SentSeqNumber = 0;
            }

            STATUSMESSAGE = "Waiting for user to initiate Prepare phase";
        }

        private bool AddDataDrivenUI(String label, XmlNodeList sourceList, List<TriggeredEventsUI> uiList) {
            bool hasLines = false;
            if (sourceList.Count > 0) {
                AddSourceLabel(label);
                hasLines = true;
            }
            foreach (XmlNode source in sourceList) {
                if (CheckDisabled(source)) {
                    Disabled d = new Disabled(source);
                    GetSourcePanel().Children.Add(d);
                    continue;
                }
                TriggeredEventsUI lineUI = new TriggeredEventsUI(source);
                GetSourcePanel().Children.Add(lineUI);
                sourceUIMap.Add(source.Attributes["uuid"].Value, lineUI);
                uiList.Add(lineUI);

                foreach (XmlNode ch in source.SelectNodes("./chained")) {
                    this.AddChainedUI(1, ch);
                }
            }

            return hasLines;
        }

        public void AddChainedUI(int depth, XmlNode node) {
            ChainedEventsUI chainUI = new ChainedEventsUI(node, depth);
            GetSourcePanel().Children.Add(chainUI);
            chainDrivenLinesUserControls.Add(chainUI);
            sourceUIMap.Add(chainUI.uuid, chainUI);
            foreach (XmlNode ch in node.SelectNodes("./chained")) {
                this.AddChainedUI(depth + 1, ch);
            }
        }

        private bool CheckDisabled(XmlNode node) {
            bool disabled = false;
            if (node.Attributes["disabled"] != null) {
                try {
                    disabled = bool.Parse(node.Attributes["disabled"].Value);
                } catch (Exception) {
                    disabled = false;
                }
            }
            return disabled;
        }

        public void AddSourceLabel(string s) {
            Label label = new Label {
                FontSize = 12,
                Content = s,
                FontWeight = FontWeight.FromOpenTypeWeight(600),
                Foreground = new SolidColorBrush { Color = Colors.White }
            };

            GetSourcePanel().Children.Add(label);
        }

        public void AddLabel(string s) {
            Label label = new Label {
                FontSize = 12,
                Content = s,
                FontWeight = FontWeight.FromOpenTypeWeight(600),
                Foreground = new SolidColorBrush { Color = Colors.White }
            };

            GetLinePanel().Children.Add(label);
        }

        internal void SetCentralMessagingHub(CentralMessagingHub centralMessagingHub) {
            this.centralMessagingHub = centralMessagingHub;
            cnt = new NgExecutionController(this.centralMessagingHub.port);
        }

        private void Prepare_Click(object sender, EventArgs e) {
            STATUSMESSAGE = "Preparing source and destinations";

            Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedIndex = 3));
            try {
                centralMessagingHub.Hub.Clients.All.InitModel(dataModel.OuterXml);
                centralMessagingHub.Hub.Clients.All.ClearAndPrepare();
            } catch (Exception ex) {
                STATUSMESSAGE = "Preparing phase error. " + ex.Message;
            }

            STATUSMESSAGE = "Ready to Execute";
        }

        private void Execute_Click(object sender, EventArgs e) {
            STATUSMESSAGE = "Executing";
            try {
                centralMessagingHub.Hub.Clients.All.Execute();

                SetExecuteBtnEnabled(false);
                SetPrepareBtnEnabled(false);
                SetStopBtnEnabled(true);

                stopWatch.Reset();
                stopWatch.Start();

                percentCompleteTimer = new Timer {
                    Interval = 1000,
                    AutoReset = true,
                    Enabled = true
                };
                percentCompleteTimer.Elapsed += OnSecondEvent;

                executionTimer = new Timer {
                    Interval = duration * 1000,
                    AutoReset = false,
                    Enabled = true
                };
                executionTimer.Elapsed += OnExecutionCompleteEvent;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private void OnSecondEvent(Object source, ElapsedEventArgs e) {
            double elapsed = stopWatch.Elapsed.TotalSeconds;
            int sec = stopWatch.Elapsed.Seconds;

            //if (sec % Parameters.PROGRESSEPOCH != 0) {
            //    return;
            //}

            double percentage = 100 * (elapsed / duration);
            int value = Convert.ToInt32(percentage);

            Application.Current.Dispatcher.Invoke(delegate {
                try {
                    PercentComplete = value;
                    ElapsedString = GetElapsedString(stopWatch);
                } catch (Exception ex) {
                    Debug.WriteLine("Setting Percent Complete" + ex.Message);
                }
            });
        }

        private void OnExecutionCompleteEvent(Object source, ElapsedEventArgs e) {
            percentCompleteTimer.Enabled = false;
            PercentComplete = 100;
            ElapsedString = GetElapsedString(stopWatch);

            OnExecutionCompleteEvent(false);
        }

        private void OnExecutionCompleteEvent(bool stopped = false) {
            // This is an Event Handler that handles the action when the timer goes off
            // signalling the end of the test.
            repeatsExecuted++;
            centralMessagingHub.Hub.Clients.All.Stop(false);

            consoleWriter.WriteLine($"Test Execution Repetition {repeatsExecuted} of {repeats} Complete");
            STATUSMESSAGE = $"Test Execution Repetition {repeatsExecuted} of {repeats} Complete";

            if (repeatsExecuted >= repeats || stopped) {
                try {
                    Dispatcher.BeginInvoke((Action)(() => {
                        SetExecuteBtnEnabled(false);
                        SetPrepareBtnEnabled(true);
                        SetStopBtnEnabled(false);
                    }));
                } catch (Exception ex) {
                    Console.WriteLine($"Test Execution Completed Stopped Error {ex.Message}");
                }

                return;
            }

            restRemaining = repeatRest;
            centralMessagingHub.Hub.Clients.All.WaitForNextExecute($"Test Execution Repetition {repeatsExecuted} of {repeats} Complete. Waiting {repeatRest} seconds for next repetition");
            STATUSMESSAGE = $"Test Execution Repetition {repeatsExecuted} of {repeats} Complete. Waiting {repeatRest} seconds for next repetition";

            restSecondTimer = new Timer {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            restSecondTimer.Elapsed += NextRestSecond;

            repetitionTimer = new Timer {
                Interval = repeatRest * 1000,
                AutoReset = false,
                Enabled = true
            };
            repetitionTimer.Elapsed += NextExecution;
        }

        private void NextRestSecond(object sender, ElapsedEventArgs e) {
            restRemaining = restRemaining - 1;
            STATUSMESSAGE = $"Test Execution Repetition {repeatsExecuted} of {repeats} Complete. Waiting {restRemaining} seconds for next repetition";
        }

        private void NextExecution(object sender, ElapsedEventArgs e) {
            restSecondTimer.Enabled = false;
            try {
                // Sent Seq Numbers keep track of the highest messageSent, so we dont process out of sequence messages
                foreach (LineUserControl li in this.directLinesUserControls) {
                    li.SentSeqNumber = 0;
                    li.Sent(0);
                }
                foreach (LineUserControl li in this.amsLinesUserControls) {
                    li.SentSeqNumber = 0;
                    li.Sent(0);
                }

                foreach (SourceUI src in this.sourceUIMap.Values) {
                    src.SentSeqNumber = 0;
                    src.SetMessagesSent(0);
                }
                try {
                    Dispatcher.BeginInvoke((Action)(() => {
                        try {
                            SchedTriggers.Clear();
                            FiredTriggers.Clear();
                        } catch (Exception ex) {
                            Console.WriteLine("Clear exception " + ex.Message);
                        }
                    }));
                } catch (Exception ex) {
                    Console.WriteLine($"Test Execution Completed Stopped Error {ex.Message}");
                }

                centralMessagingHub.Hub.Clients.All.PrepareAndExecute();

                STATUSMESSAGE = "Executing";

                stopWatch.Reset();
                stopWatch.Start();

                percentCompleteTimer = new Timer {
                    Interval = 1000,
                    AutoReset = true,
                    Enabled = true
                };
                percentCompleteTimer.Elapsed += OnSecondEvent;

                executionTimer = new Timer {
                    Interval = duration * 1000,
                    AutoReset = false,
                    Enabled = true
                };
                executionTimer.Elapsed += OnExecutionCompleteEvent;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private string GetElapsedString(Stopwatch stopWatch) {
            int sec = stopWatch.Elapsed.Seconds;
            int min = stopWatch.Elapsed.Minutes;
            int hour = stopWatch.Elapsed.Hours;

            string secStr = sec < 10 ? $"0{sec}" : $"{sec}";
            string minStr = min < 10 ? $"0{min}" : $"{min}";
            string hourStr = hour < 10 ? $"0{hour}" : $"{hour}";
            return $"{hourStr}:{minStr}:{secStr}";
        }

        private void Stop_Click(object sender, EventArgs e) {
            STATUSMESSAGE = "STOP";
            try {
                centralMessagingHub.Hub.Clients.All.Stop(true);
                SetExecuteBtnEnabled(false);
                SetPrepareBtnEnabled(true);
                SetStopBtnEnabled(false);

                OnExecutionCompleteEvent(true);

                percentCompleteTimer.Enabled = false;
                PercentComplete = 100;
            } catch (Exception ex) {
                Console.WriteLine($"Test Execution Manually Stopped Error {ex.Message}");
            }
            Console.WriteLine("Test Execution Manually Stopped");
        }

        private void OutputConsole_Initialized(object sender, EventArgs e) {
            consoleWriter = new ControlWriter(outputConsole);
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

        public void SetExecuteBtnEnabled(bool v) {
            commandBarView.SetExecuteBtnEnabled(v);
        }

        public void SetPrepareBtnEnabled(bool v) {
            commandBarView.SetPrepareBtnEnabled(v);
        }

        public void SetStopBtnEnabled(bool v) {
            commandBarView.SetStopBtnEnabled(v);
        }
    }
}