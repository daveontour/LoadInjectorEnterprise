using LoadInjector.Common;
using LoadInjector.RunTime.Models;
using LoadInjector.RunTime.Views;
using LoadInjector.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml;
using static LoadInjector.RunTime.Models.ControllerStatusReport;

namespace LoadInjector.RunTime {

    public partial class ExecutionUI : Window, INotifyPropertyChanged {
        public XmlDocument dataModel;
        public NgExecutionController executionControl;
        private const int consoleLength = 16000;

        public event PropertyChangedEventHandler PropertyChanged;

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
        public ObservableCollection<TriggerRecord> SchedTriggers { get; set; }
        public ObservableCollection<TriggerRecord> FiredTriggers { get; set; }

        public void OnPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private ControlWriter consoleWriter;

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
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            executionControl.AutoStartAsync(args);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
            set => trigLabel = value;
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
        }

        private TreeEditorViewModel myWin;

        public TreeEditorViewModel VM {
            get => myWin;
            set => myWin = value;
        }

        public XmlDocument DataModel {
            get => dataModel;
            set {
                dataModel = value;

                Progress<ControllerStatusReport> controllerProgress = new Progress<ControllerStatusReport>(report => {
                    ControllerStatusChanged(report);
                }
                );

                executionControl = new NgExecutionController(value, controllerProgress);
                executionControl.SetExecutiuonUI(this);

                // Construct the UI for all the lines of execution
                PrepareLineUI();
            }
        }

        public void PrepareLineUI() {
            //Add the data driven sources
            bool hasAMDDDLines = false;
            if (Parameters.SITAAMS) {
                hasAMDDDLines = AddDataDrivenUI("AMS Data Driven Source", executionControl.amsDataDrivenLines, amsDataDrivenLinesUserControls);
            }
            bool hasCSV = AddDataDrivenUI("CSV Data Driven Source", executionControl.csvDataDrivenLines, csvDataDrivenLinesUserControls);
            bool hasExcel = AddDataDrivenUI("Excel Data Driven Source", executionControl.excelDataDrivenLines, excelDataDrivenLinesUserControls);
            bool hasXML = AddDataDrivenUI("XML Data Driven Source", executionControl.xmlDataDrivenLines, xmlDataDrivenLinesUserControls);
            bool hasJSON = AddDataDrivenUI("JSON Data Driven Source", executionControl.jsonDataDrivenLines, jsonDataDrivenLinesUserControls);
            bool hasDB = AddDataDrivenUI("Database Data Driven Source", executionControl.databaseDataDrivenLines, databaseDataDrivenLinesUserControls);

            //Add the rateDataDriven lines
            if (rateDrivenLinesUserControls.Count > 0) {
                AddSourceLabel("Rate Driven Source");
            }
            foreach (RateDrivenSourceController source in executionControl.rateDrivenLines) {
                if (CheckDisabled(source.node)) {
                    Disabled d = new Disabled(source.node);
                    GetSourcePanel().Children.Add(d);
                    continue;
                }
                RateDrivenEventsUI lineUI = new RateDrivenEventsUI(source.node, 0);
                GetSourcePanel().Children.Add(lineUI);
                rateDrivenLinesUserControls.Add(lineUI);
                source.SetLineProgress(lineUI.controllerProgress);

                foreach (RateDrivenSourceController chain in source.chainedController) {
                    chain.AddChainedUI(1, GetSourcePanel().Children, chainDrivenLinesUserControls);
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
                if (executionControl.amsLines.Count > 0) {
                    AddLabel("AMS Direct Update Destinations");
                }
                foreach (AmsDirectExecutionController direct in executionControl.amsLines) {
                    if (CheckDisabled(direct.config)) {
                        Disabled d = new Disabled(direct.config);
                        GetLinePanel().Children.Add(d);
                        continue;
                    }
                    LineUserControl lineUI = new LineUserControl(direct.config);
                    GetLinePanel().Children.Add(lineUI);
                    amsLinesUserControls.Add(lineUI);
                    direct.SetLineProgress(lineUI.controllerProgress);
                }
            }

            // Add the Destination Lines
            if (executionControl.destLines.Count > 0) {
                AddLabel("Destinations");
            }
            foreach (LineExecutionController cnt in executionControl.destLines) {
                if (CheckDisabled(cnt.config)) {
                    Disabled d = new Disabled(cnt.config);
                    GetLinePanel().Children.Add(d);
                    continue;
                }
                LineUserControl lineUI = new LineUserControl(cnt.config);
                GetLinePanel().Children.Add(lineUI);
                directLinesUserControls.Add(lineUI);
                cnt.SetLineProgress(lineUI.controllerProgress);
            }
        }

        private bool AddDataDrivenUI(String label, List<DataDrivenSourceController> sourceList, List<TriggeredEventsUI> uiList) {
            bool hasLines = false;
            if (sourceList.Count > 0) {
                AddSourceLabel(label);
                hasLines = true;
            }
            foreach (DataDrivenSourceController source in sourceList) {
                if (CheckDisabled(source.node)) {
                    Disabled d = new Disabled(source.node);
                    GetSourcePanel().Children.Add(d);
                    continue;
                }
                TriggeredEventsUI lineUI = new TriggeredEventsUI(source.node);
                GetSourcePanel().Children.Add(lineUI);
                uiList.Add(lineUI);
                source.SetLineProgress(lineUI.controllerProgress);

                foreach (RateDrivenSourceController chain in source.chainedController) {
                    chain.AddChainedUI(1, GetSourcePanel().Children, chainDrivenLinesUserControls);
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
                executionControl.Stop();
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
            executionControl.ClearLines();
            _ = executionControl.PrepareAsync(true);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            StatusLabel = null;
            CancelBtn.IsEnabled = false;
            CancelBtn.Visibility = Visibility.Hidden;
            executionControl.Cancel();
        }

        private void Execute_Click(object sender, RoutedEventArgs e) {
            Task.Run(() => executionControl.Run());
        }

        public void Stop_Click(object sender, RoutedEventArgs e) {
            try {
                executionControl.Stop(true);
            } catch (Exception ex) {
                Console.WriteLine($"Test Execution Manually Stopped Error {ex.Message}");
            }
            Console.WriteLine("Test Execution Manually Stopped");
        }

        private void OutputConsole_Initialized(object sender, EventArgs e) {
            consoleWriter = new ControlWriter(outputConsole);
            Console.SetOut(consoleWriter);
        }

        public void ControllerStatusChanged(ControllerStatusReport e) {
            Operation op = e.Type;

            if ((op & Operation.ExecuteBtn) == Operation.ExecuteBtn) {
                ExecuteBtn.IsEnabled = e.Execute;
            }
            if ((op & Operation.PrepareBtn) == Operation.PrepareBtn) {
                PrepareBtn.IsEnabled = e.Prepare;
            }
            if ((op & Operation.StopBtn) == Operation.StopBtn) {
                StopBtn.IsEnabled = e.Stop;
            }
            if ((op & Operation.Percent) == Operation.Percent) {
                PercentComplete = e.PercentComplete;
            }
            if ((op & Operation.ClearConsole) == Operation.ClearConsole && e.ClearConsole) {
                // NO-OP
            }
            if ((op & Operation.TimeStr) == Operation.TimeStr) {
                ElapsedString = e.Timestr;
            }
            if ((op & Operation.SchedStart) == Operation.SchedStart) {
                StatusLabel = e.SchedStart;
            }
            if ((op & Operation.Console) == Operation.Console && outputConsole != null) {
                outputConsole.Text += e.OutputString;
                if (outputConsole.Text.Length > consoleLength) {
                    int len = outputConsole.Text.Length - consoleLength;
                    outputConsole.Text = outputConsole.Text.Substring(len, outputConsole.Text.Length - len);
                }
                outputConsole.ScrollToEnd();
            }
            if ((op & Operation.AddUILineElement) == Operation.AddUILineElement && e.UiElement != null) {
                GetLinePanel().Children.Add(e.UiElement);
            }
            if ((op & Operation.AddUILabel) == Operation.AddUILabel) {
                AddLabel(e.Label);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            try {
                executionControl.Stop();
                VM.LockExecution = false;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
    }
}