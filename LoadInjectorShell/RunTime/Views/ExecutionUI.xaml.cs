using LoadInjector.Common;
using LoadInjector.Runtime.EngineComponents;
using LoadInjector.RunTime.Views;
using LoadInjector.ViewModels;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace LoadInjector.RunTime {

    public partial class ExecutionUI : Window, INotifyPropertyChanged {
        public XmlDocument dataModel;

        //public NgExecutionController executionControl;
        private const int consoleLength = 16000;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public void OnPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public ControlWriter consoleWriter;

        public StackPanel GetLinePanel() => Lines;

        public StackPanel GetSourcePanel() => Sources;

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

        internal void AutoStart(string[] args) {
            Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedIndex = 3));
            // executionControl.AutoStartAsync(args);
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

        public ExecutionUI() {
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

            //cnt = new NgExecutionController(this.centralMessagingHub.port);

            //centralMessagingHub = new CentralMessagingHub(this);
            //centralMessagingHub.StartHub();
        }

        private TreeEditorViewModel myWin;
        private Timer percentCompleteTimer;
        private int duration;
        private Timer executionTimer;
        private int repeats;
        private int repeatRest;
        private int repeatsExecuted = 0;
        private Timer repetitionTimer;

        public TreeEditorViewModel VM {
            get => myWin;
            set => myWin = value;
        }

        public XmlDocument DataModel {
            get => dataModel;
            set {
                dataModel = value;

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
            }
        }

        public void PrepareLineUI() {
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

            if (Parameters.SITAAMS) {
                hasAMDDDLines = AddDataDrivenUI("AMS Data Driven Source", amsEventDriven, amsDataDrivenLinesUserControls);
            }
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

            if (Parameters.SITAAMS) {
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

        public void ExecutionWindow_Closing(object sender, CancelEventArgs e) {
            try {
                centralMessagingHub.Hub.Clients.All.Stop();
            } catch (Exception) {
                // NO-OP
            }
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

        public void AddSourceLabel(string s) {
            Label label = new Label {
                FontSize = 12,
                Content = s,
                FontWeight = FontWeight.FromOpenTypeWeight(600),
                Foreground = new SolidColorBrush { Color = Colors.White }
            };

            GetSourcePanel().Children.Add(label);
        }

        private void Prepare_Click(object sender, RoutedEventArgs e) {
            Dispatcher.BeginInvoke((Action)(() => tabControl.SelectedIndex = 3));
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

            ExecuteBtn.IsEnabled = false;
            PrepareBtn.IsEnabled = false;
            StopBtn.IsEnabled = false;

            try {
                centralMessagingHub.Hub.Clients.All.InitModel(dataModel.OuterXml);
                centralMessagingHub.Hub.Clients.All.ClearAndPrepare();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            this.StatusLabel = null;
            CancelBtn.IsEnabled = false;
            CancelBtn.Visibility = Visibility.Hidden;

            try {
                centralMessagingHub.Hub.Clients.All.Cancel();
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            ExecuteBtn.IsEnabled = false;
            PrepareBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
        }

        private void Execute_Click(object sender, RoutedEventArgs e) {
            try {
                centralMessagingHub.Hub.Clients.All.Execute();

                ExecuteBtn.IsEnabled = false;
                PrepareBtn.IsEnabled = false;
                StopBtn.IsEnabled = true;

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

        public void Stop_Click(object sender, RoutedEventArgs e) {
            try {
                //centralMessagingHub.Hub.Clients.All.Stop(true);
                //StopBtn.IsEnabled = false;
                //PrepareBtn.IsEnabled = true;
                //ExecuteBtn.IsEnabled = false;

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
            //          Console.SetOut(consoleWriter);
        }

        private void OnSecondEvent(Object source, ElapsedEventArgs e) {
            double elapsed = stopWatch.Elapsed.TotalSeconds;
            int sec = stopWatch.Elapsed.Seconds;

            if (sec % Parameters.PROGRESSEPOCH != 0) {
                return;
            }

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

        private string GetElapsedString(Stopwatch stopWatch) {
            int sec = stopWatch.Elapsed.Seconds;
            int min = stopWatch.Elapsed.Minutes;
            int hour = stopWatch.Elapsed.Hours;

            string secStr = sec < 10 ? $"0{sec}" : $"{sec}";
            string minStr = min < 10 ? $"0{min}" : $"{min}";
            string hourStr = hour < 10 ? $"0{hour}" : $"{hour}";
            return $"{hourStr}:{minStr}:{secStr}";
        }

        private void OnExecutionCompleteEvent(bool stopped = false) {
            // This is an Event Handler that handles the action when the timer goes off
            // signalling the end of the test.
            repeatsExecuted++;
            centralMessagingHub.Hub.Clients.All.Stop(false);

            consoleWriter.WriteLine($"Test Execution Reptition {repeatsExecuted} of {repeats} Complete");

            if (repeatsExecuted >= repeats || stopped) {
                try {
                    Dispatcher.BeginInvoke((Action)(() => {
                        StopBtn.IsEnabled = false;
                        PrepareBtn.IsEnabled = true;
                        ExecuteBtn.IsEnabled = false;
                    }));
                } catch (Exception ex) {
                    Console.WriteLine($"Test Execution Completed Stopped Error {ex.Message}");
                }

                return;
            }

            centralMessagingHub.Hub.Clients.All.WaitForNextExecute($"Test Execution Reptition {repeatsExecuted} of {repeats} Complete. Waiting {repeatRest} seconds for next repitiion");

            repetitionTimer = new Timer {
                Interval = repeatRest * 1000,
                AutoReset = false,
                Enabled = true
            };
            repetitionTimer.Elapsed += NextExecution;
        }

        private void NextExecution(object sender, ElapsedEventArgs e) {
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

        private void Window_Closing(object sender, CancelEventArgs e) {
            try {
                try {
                    centralMessagingHub.Hub.Clients.All.Stop();
                } catch (Exception) {
                    // NO-OP
                }
                VM.LockExecution = false;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        internal void SetCentralMessagingHub(CentralMessagingHub centralMessagingHub) {
            this.centralMessagingHub = centralMessagingHub;
            cnt = new NgExecutionController(this.centralMessagingHub.port);
        }
    }
}