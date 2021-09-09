using LoadInjector.RunTime.EngineComponents;
using LoadInjector.RunTime.ViewModels;
using LoadInjectorBase.Common;
using NLog;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Linq;

namespace LoadInjector.RunTime {

    public enum ExecutionControllerType {
        StandAlone,
        Client,
        Local
    }

    public class NgExecutionController {
        public TriggerEventDistributor eventDistributor;

        public XmlDocument dataModel;
        private readonly Stopwatch stopWatch = new Stopwatch();
        public ClientState state = ClientState.UnAssigned;

        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
        public static readonly Logger destLogger = LogManager.GetLogger("destLogger");
        public static readonly Logger sourceLogger = LogManager.GetLogger("sourceLogger");

        public readonly List<LineExecutionController> destLines = new List<LineExecutionController>();

        public readonly List<DataDrivenSourceController> csvDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<DataDrivenSourceController> excelDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<DataDrivenSourceController> xmlDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<DataDrivenSourceController> jsonDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<DataDrivenSourceController> databaseDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<RateDrivenSourceController> rateDrivenLines = new List<RateDrivenSourceController>();

        private Timer timer;
        private Timer timerStart;

        public int duration = -1;
        private string startAt;
        private bool startAtEnabled;

        public string token;
        public string apt_code;
        public string apt_icao_code;

        public int serverOffset;

        private string logLevel;

        private int repeats;
        private int executedRepeats;
        private int repeatRest;

        private XmlNodeList csvEventDriven;
        private XmlNodeList excelEventDriven;
        private XmlNodeList xmlEventDriven;
        private XmlNodeList jsonEventDriven;
        private XmlNodeList databaseEventDriven;
        private XmlNodeList rateDriven;
        private XmlNodeList destinations;

        public ClientHub clientHub;
        public CompletionReport iterationRecords = new CompletionReport();

        public string executionNodeUuid;
        public bool slaveMode = false;
        private Timer executionTimer;
        private int repeatsExecuted = 0;
        private Timer repetitionTimer;

        public string archName = "-Not Assigned-";
        private DateTime executionStarted;
        private readonly bool standAloneMode;
        private readonly string reportFile;

        public string LocalStart { get; }

        private string archiveDirectory;

        public string ArchiveDirectory {
            get {
                if (archiveDirectory == null) {
                    archiveDirectory = GetTemporaryDirectory();
                }
                return archiveDirectory;
            }
        }

        public NgExecutionController(ExecutionControllerType type, string serverHub = null, string executeFile = null, string reportFile = null, string localStart = null) {
            // Client Server Mode

            try {
                this.reportFile = reportFile;
                this.LocalStart = localStart;

                if (type == ExecutionControllerType.Client || type == ExecutionControllerType.Local) {
                    try {
                        this.clientHub = new ClientHub(serverHub, this);
                        this.eventDistributor = new TriggerEventDistributor(this);
                    } catch (Exception ex) {
                        ConsoleMsg(ex.Message);
                    }
                }

                // Running directly from a local file
                if (type == ExecutionControllerType.StandAlone) {
                    this.standAloneMode = true;
                    XmlDocument doc = new XmlDocument();
                    if (executeFile.ToLower().EndsWith(".lia")) {
                        doc = Utils.ExtractArchiveToDirectory(executeFile, ArchiveDirectory, "lia.lia");
                    } else {
                        doc.Load(executeFile);
                    }

                    this.clientHub = new ClientHub(this);
                    this.eventDistributor = new TriggerEventDistributor(this);
                    this.InitModel(doc);
                    this.RunLocal();
                }
            } catch (Exception ex) {
                Console.WriteLine($"\nError Starting Load Injector Client: {ex.Message}");
                Console.WriteLine("\nHit any key to exit");
                Console.Read();

                throw (ex);
            }
        }

        public void Reset() {
            Stop();
            dataModel = null;

            iterationRecords.Clear();
            repeatsExecuted = 0;
            repeatRest = 0;
            repeats = 0;

            executionNodeUuid = null;
            timer?.Stop();
            timer = null;

            timerStart?.Stop();
            timerStart = null;

            executionTimer?.Stop();
            executionTimer = null;

            repetitionTimer?.Stop();
            repetitionTimer = null;

            archName = "-Not Assigned-";

            destLines.Clear();
            csvDataDrivenLines.Clear();
            excelDataDrivenLines.Clear();
            xmlDataDrivenLines.Clear();
            jsonDataDrivenLines.Clear();
            databaseDataDrivenLines.Clear();
            rateDrivenLines.Clear();
            clientHub?.consoleMessages?.Clear();
            this.state = ClientState.UnAssigned;
            clientHub?.SetStatus(executionNodeUuid);
            clientHub?.RefreshResponse();
        }

        public void InitModel(XmlDocument model) {
            dataModel = model;

            List<string> triggersInUse = TriggersInUse(dataModel);

            XDocument doc = XDocument.Parse(dataModel.OuterXml);

            this.duration = int.Parse(doc.Descendants("duration").FirstOrDefault().Value);
            this.executionNodeUuid = dataModel.SelectSingleNode("//settings").Attributes["executionNodeUuid"]?.Value;

            try {
                string serverDiff = doc.Descendants("serverDiff").FirstOrDefault().Value;
                serverOffset = Convert.ToInt32(60 * double.Parse(serverDiff));
            } catch (Exception) {
                serverOffset = 0;
            }

            csvEventDriven = dataModel.SelectNodes("//datadriven[contains(@dataType,'csv')]");
            excelEventDriven = dataModel.SelectNodes("//datadriven[contains(@dataType,'excel')]");
            xmlEventDriven = dataModel.SelectNodes("//datadriven[contains(@dataType,'xml')]");
            jsonEventDriven = dataModel.SelectNodes("/datadriven[contains(@dataType,'json')]");
            databaseEventDriven = dataModel.SelectNodes("//datadriven[contains(@sourceType,'database')]");
            rateDriven = dataModel.SelectNodes("//ratedriven");
            destinations = dataModel.SelectNodes("//destination");

            ValidateModel();

            // Add the controllers
            AddDestinationControllerType(csvEventDriven, csvDataDrivenLines, triggersInUse);
            AddDestinationControllerType(excelEventDriven, excelDataDrivenLines, triggersInUse);
            AddDestinationControllerType(xmlEventDriven, xmlDataDrivenLines, triggersInUse);
            AddDestinationControllerType(jsonEventDriven, jsonDataDrivenLines, triggersInUse);
            AddDestinationControllerType(databaseEventDriven, databaseDataDrivenLines, triggersInUse);

            // Add the Rate Driven Lines
            foreach (XmlNode node in rateDriven) {
                if (CheckDisabled(node)) {
                    continue;
                }
                RateDrivenSourceController line = new RateDrivenSourceController(node, 0, triggersInUse, serverOffset, this);

                rateDrivenLines.Add(line);

                if (!line.ConfigOK) {
                    ConsoleMsg("Error: Configuration is not valid");
                    this.state = ClientState.ConfigInvalid;
                }
            }

            // Add the Destination Lines
            foreach (XmlNode node in destinations) {
                if (CheckDisabled(node)) {
                    continue;
                }

                LineExecutionController line = new LineExecutionController(node, this);
                destLines.Add(line);
            }

            if (this.state != ClientState.ConfigInvalid) {
                this.state = ClientState.Assigned;
            }
            clientHub.SetStatus(executionNodeUuid);
        }

        public void RunLocal() {
            if (!standAloneMode && state.Value != ClientState.Ready.Value) {
                logger.Warn("Execute requested, but not in ready state " + state.Value);
                clientHub.ConsoleMsg(executionNodeUuid, null, "Execute requested, but not in ready state " + state.Value);
                return;
            }

            repeatsExecuted = 0;
            PrepareAsync().Wait();
            this.executionStarted = DateTime.Now;
            Run();

            stopWatch.Reset();
            stopWatch.Start();

            executionTimer = new Timer {
                Interval = duration * 1000,
                AutoReset = false,
                Enabled = true
            };
            executionTimer.Elapsed += OnExecutionCompleteEvent;
        }

        private void OnExecutionCompleteEvent(Object source, ElapsedEventArgs e) {
            // This is an Event Handler that handles the action when the timer goes off
            // signalling the end of the test.

            repeatsExecuted++;
            try {
                logger.Warn("Execution Time Expired");
                Stop();
                logger.Warn("Stopped");
            } catch (Exception ex) {
                logger.Error(ex, "Error Stopping");
            }

            if (repeatsExecuted < repeats) {
                logger.Info($"Test Execution Repetition {repeatsExecuted} of {repeats} Complete");
                repetitionTimer = new Timer {
                    Interval = repeatRest * 1000,
                    AutoReset = false,
                    Enabled = true
                };
                repetitionTimer.Elapsed += NextExecution;
                state = ClientState.WaitingNextIteration;
                clientHub.SetStatus(this.executionNodeUuid);
            } else {
                logger.Info($"Test Execution Repetition {repeatsExecuted} of {repeats} Complete");
                state = ClientState.ExecutionComplete;
                clientHub.SetStatus(this.executionNodeUuid);
                PrepareIterationCompletionReport();
                if (reportFile != null && standAloneMode) {
                    SaveExcelCompletionReport();
                }
            }
        }

        internal void ReviseCompletion() {
            iterationRecords.Clear();
            PrepareIterationCompletionReport();
            if (standAloneMode) {
                logger.Warn("Post Completion Update");
                logger.Info(iterationRecords.ToString());
                if (reportFile != null) {
                    SaveExcelCompletionReport();
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SaveExcelCompletionReport() {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            ExcelPackage excel = new ExcelPackage();

            foreach (IterationRecord itRec in iterationRecords.Records) {
                var workSheet = excel.Workbook.Worksheets.Add($"Iteration {itRec.IterationNumber}");

                workSheet.Cells[1, 1].Value = "Execution Node IP";
                workSheet.Cells[2, 1].Value = "Execution Process";
                workSheet.Cells[3, 1].Value = "Work Package";

                workSheet.Cells[5, 1].Value = "Execution Start";
                workSheet.Cells[6, 1].Value = "Execution End";

                workSheet.Column(1).AutoFit();
                workSheet.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                workSheet.Column(1).Style.Font.Bold = true;

                workSheet.Cells[1, 2].Value = iterationRecords.IPAddress;
                workSheet.Cells[2, 2].Value = iterationRecords.ProcessID;
                workSheet.Cells[3, 2].Value = iterationRecords.WorkPacakage;

                workSheet.Cells[5, 2].Value = itRec.ExecutionStart.ToString("yyyy-MM-dd  HH:mm:ss");
                workSheet.Cells[6, 2].Value = itRec.ExecutionEnd.ToString("yyyy-MM-dd  HH:mm:ss");
                workSheet.Column(2).AutoFit();

                workSheet.Cells[7, 3].Value = "Sources:";
                workSheet.Row(7).Style.Font.Bold = true;
                workSheet.Cells[8, 4].Value = "Type";
                workSheet.Cells[8, 5].Value = "Description";
                workSheet.Column(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                workSheet.Column(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                workSheet.Cells[8, 6].Value = "Triggers Fired";
                workSheet.Column(6).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Cells[8, 8].Value = "Source";
                workSheet.Row(8).Style.Font.Bold = true;

                int row = 9;
                foreach (LineRecord rec in itRec.SourceLineRecords) {
                    workSheet.Cells[row, 4].Value = rec.SourceType;
                    workSheet.Cells[row, 5].Value = rec.Name;
                    workSheet.Cells[row, 6].Value = rec.MessagesSent;
                    workSheet.Cells[row, 8].Value = rec.Description;
                    row++;
                }

                workSheet.Column(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                workSheet.Column(5).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                workSheet.Column(6).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Column(7).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Column(8).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                row++;

                workSheet.Cells[row, 3].Value = "Destinations:";
                workSheet.Row(row).Style.Font.Bold = true;
                row++;
                workSheet.Cells[row, 4].Value = "Type";
                workSheet.Cells[row, 5].Value = "Description";
                workSheet.Cells[row, 6].Value = "Messages Sent";
                workSheet.Cells[row, 7].Value = "Messages Fail";
                workSheet.Cells[row, 8].Value = "Destination";
                workSheet.Row(row).Style.Font.Bold = true;
                row++;
                foreach (LineRecord rec in itRec.DestinationLineRecords) {
                    workSheet.Cells[row, 4].Value = rec.DestinationType;
                    workSheet.Cells[row, 5].Value = rec.Name;
                    workSheet.Cells[row, 6].Value = rec.MessagesSent;
                    workSheet.Cells[row, 7].Value = rec.MessagesFailed;
                    workSheet.Cells[row, 8].Value = rec.Description;
                    row++;
                }

                workSheet.Column(3).AutoFit();
                workSheet.Column(4).AutoFit();
                workSheet.Column(5).AutoFit();
                workSheet.Column(6).AutoFit();
                workSheet.Column(7).AutoFit();
                workSheet.Column(8).AutoFit();
            }

            try {
                if (this.reportFile != null) {
                    if (File.Exists(this.reportFile))
                        File.Delete(this.reportFile);

                    // Create excel file on physical disk
                    FileStream objFileStrm = File.Create(this.reportFile);
                    objFileStrm.Close();

                    // Write content to excel file
                    File.WriteAllBytes(this.reportFile, excel.GetAsByteArray());
                    //Close Excel package
                }
            } catch (Exception ex) {
                logger.Error(ex, "Writing report error");
            }
            excel.Dispose();
        }

        private void PrepareIterationCompletionReport() {
            // Collect the data for the Completion Report
            try {
                IterationRecord itRecord = new IterationRecord() {
                    UUID = this.executionNodeUuid,
                    ExecutionStart = this.executionStarted,
                    ExecutionEnd = DateTime.Now,
                    IterationNumber = repeatsExecuted,
                };
                logger.Warn("Iteration record Created");

                foreach (LineExecutionController line in destLines) {
                    LineRecord lr = new LineRecord {
                        DestinationType = line.LineType,
                        Name = line.name,
                        MessagesSent = line.messagesSent,
                        MessagesFailed = line.messagesFail,
                        Description = line.destinationEndPoint.endPointDestination.GetDestinationDescription()
                    };

                    itRecord.DestinationLineRecords.Add(lr);
                }

                foreach (RateDrivenSourceController line in rateDrivenLines) {
                    LineRecord lr = new LineRecord {
                        SourceType = "Rate Driven",
                        Name = line.name,
                        MessagesSent = line.messagesSent
                    };
                    switch (line.dataSourceType) {
                        case "csv":
                        case "excel":
                        case "xml":
                        case "json":
                            lr.Description = $"{line.dataSourceType}: {line.dataFile}, Configured Rate: {line.messagesPerMinute} msgs/min";
                            break;

                        case "mssql":
                        case "mysql":
                        case "oracle":
                            lr.Description = $"{line.dataSourceType}: {line.connStr}, Configured Rate: {line.messagesPerMinute} msgs/min";
                            break;

                        default:
                            lr.Description = $"Configured Rate: {line.messagesPerMinute} msgs/min";
                            break;
                    }
                    itRecord.SourceLineRecords.Add(lr);
                }

                foreach (List<DataDrivenSourceController> controller in new List<List<DataDrivenSourceController>>() { null, csvDataDrivenLines, excelDataDrivenLines, xmlDataDrivenLines, jsonDataDrivenLines, databaseDataDrivenLines }) {
                    if (controller != null) {
                        foreach (DataDrivenSourceController line in controller) {
                            LineRecord lr = new LineRecord {
                                SourceType = line.dataSourceType + "Data Driven",
                                Name = line.name,
                                MessagesSent = line.messagesSent
                            };

                            switch (line.dataSourceType) {
                                case "csv":
                                case "excel":
                                case "xml":
                                case "json":
                                    lr.Description = $"{line.dataSourceType}: {line.dataFile}";
                                    break;

                                case "mssql":
                                case "mysql":
                                case "oracle":
                                    lr.Description = $"{line.dataSourceType}: {line.connStr}";
                                    break;
                            }
                            itRecord.SourceLineRecords.Add(lr);
                        }
                    }
                }

                iterationRecords.Records.Add(itRecord);
            } catch (Exception ex) {
                logger.Error(ex, $"Iteration records{ex.Message}");
            }

            if (standAloneMode) {
                logger.Info(iterationRecords.ToString());
            }
        }

        private void NextExecution(object sender, ElapsedEventArgs e) {
            try {
                // Sent Seq Numbers keep track of the highest messageSent, so we dont process out of sequence messages
                ConsoleMsg("Start of NextExecution() called");
                Stop();
                PrepareAsync().Wait();
                this.executionStarted = DateTime.Now;
                Run();

                stopWatch.Reset();
                stopWatch.Start();

                executionTimer = new Timer {
                    Interval = duration * 1000,
                    AutoReset = false,
                    Enabled = true
                };
                executionTimer.Elapsed += OnExecutionCompleteEvent;
            } catch (Exception ex) {
                logger.Error(ex.Message);
            }
        }

        private void AddDestinationControllerType(XmlNodeList nodeList, List<DataDrivenSourceController> typeList, List<string> triggersInUse) {
            foreach (XmlNode node in nodeList) {
                if (CheckDisabled(node)) {
                    continue;
                }

                DataDrivenSourceController line = new DataDrivenSourceController(node, triggersInUse, serverOffset, this);
                typeList.Add(line);
                if (!line.ConfigOK) {
                    ConsoleMsg("Error: Configuration is not valid");
                }
            }
        }

        private void ValidateModel() {
            ConsoleMsg($"CSV Data Driven Source =  {csvEventDriven.Count}");
            ConsoleMsg($"Excel Data Driven Source =  {excelEventDriven.Count}");
            ConsoleMsg($"XML Data Driven Source =  {xmlEventDriven.Count}");
            ConsoleMsg($"JSON Data Driven Source =  {jsonEventDriven.Count}");
            ConsoleMsg($"Database Data Driven Source =  {databaseEventDriven.Count}");
            ConsoleMsg($"Rate Driven Source =  {rateDriven.Count}");

            ConsoleMsg($"Destination Lines =  {destinations.Count}");

            if ((destinations.Count) == 0) {
                ConsoleMsg("====> Config Error. No Output Lines Defined <====");
            }

            List<string> csvFields = new List<string>();

            foreach (XmlNode node in csvEventDriven) {
                int numTriggers = node.SelectNodes(".//trigger").Count;
                if (numTriggers == 0) {
                    ConsoleMsg($"====> Config Error. No Event Triggers Defined for {node.Attributes["name"].Value} <====");
                }
                foreach (XmlNode trigNode in node.SelectNodes(".//trigger")) {
                    string id = trigNode.Attributes["triggerID"]?.Value;
                    if (id != null) {
                        if (csvFields.Contains(id)) {
                            ConsoleMsg($"====> Config Error. CSV Event Trigger ID Defined Multiple Times: {id} <====");
                        } else {
                            csvFields.Add(id);
                        }
                    } else {
                        ConsoleMsg("====> Config Error. CSV Event Trigger ID is NULL <====");
                    }
                }
            }

            foreach (XmlNode node in excelEventDriven) {
                int numTriggers = node.SelectNodes(".//trigger").Count;
                if (numTriggers == 0) {
                    ConsoleMsg($"====> Config Error. No Event Triggers Defined for {node.Attributes["name"].Value} <====");
                }
                foreach (XmlNode trigNode in node.SelectNodes(".//trigger")) {
                    string id = trigNode.Attributes["triggerID"]?.Value;
                    if (id != null) {
                        if (csvFields.Contains(id)) {
                            ConsoleMsg($"====> Config Error. Excel Event Trigger ID Defined Multiple Times: {id} <====");
                        } else {
                            csvFields.Add(id);
                        }
                    } else {
                        ConsoleMsg("====> Config Error. Excel Event Trigger ID is NULL <====");
                    }
                }
            }
        }

        public void Configure(XmlDocument xDoc = null) {
            XDocument doc = XDocument.Parse(xDoc.OuterXml);

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

            try {
                startAtEnabled = bool.Parse(doc.Descendants("startAt").FirstOrDefault().Attribute("enabled").Value);
            } catch (Exception) {
                startAtEnabled = false;
            }

            try {
                startAt = doc.Descendants("startAt").FirstOrDefault().Value;
            } catch (Exception) {
                startAt = null;
                startAtEnabled = false;
            }

            try {
                logLevel = doc.Descendants("logLevel").FirstOrDefault().Value;
            } catch (Exception) {
                logLevel = null;
            }

            logger.Info($"Duration = {duration}");

            try {
                foreach (var rule in LogManager.Configuration.LoggingRules) {
                    rule.EnableLoggingForLevel(LogLevel.Info);
                    rule.EnableLoggingForLevel(LogLevel.Error);
                    rule.EnableLoggingForLevel(LogLevel.Warn);

                    if (logLevel.ToLower() == "trace") {
                        rule.EnableLoggingForLevel(LogLevel.Trace);
                    } else {
                        rule.DisableLoggingForLevel(LogLevel.Trace);
                    }
                }

                LogManager.ReconfigExistingLoggers();
            } catch (Exception ex) {
                ConsoleMsg($"Logging error.{ex.Message}");
            }
        }

        public async Task AutoStartAsync(string[] args) {
            if (args == null) {
                return;
            }
            if (args.Length == 0) {
                return;
            }

            if (args.Length > 1) {
                startAtEnabled = true;
                startAt = args[1];
            }
            bool ok = await PrepareAsync();

            if (ok) {
                Run(args);
            }
        }

        public async Task<bool> PrepareAsync(bool resetRepeats = false) {


            if (resetRepeats) {
                executedRepeats = 0;
            }

            if (dataModel == null) {
                logger.Error("Prepare initiated, but configuration has not been set.");
                return false;
            }
            Configure(dataModel);
            eventDistributor.Stop();
            eventDistributor.ClearHandlers();

            ClearTriggerData();

            bool atLeastOneActive = false;

            ConsoleMsg("Preparing  Data Driven Injectors");

            foreach (DataDrivenSourceController line in csvDataDrivenLines) {
                if (!line.InUse()) {
                    line.SetSourceLineOutput("No Destinations Using this Source");
                    continue;
                } else {
                    atLeastOneActive = true;
                }
                line.PrepareCSV();
            }

            foreach (DataDrivenSourceController line in excelDataDrivenLines) {
                if (!line.InUse()) {
                    line.SetSourceLineOutput("No Destinations Using this Source");
                    continue;
                } else {
                    atLeastOneActive = true;
                }
                line.PrepareExcel();
            }

            foreach (DataDrivenSourceController line in xmlDataDrivenLines) {
                if (!line.InUse()) {
                    line.SetSourceLineOutput("No Destinations Using this Source");
                    continue;
                } else {
                    atLeastOneActive = true;
                }
                line.PrepareXML();
            }

            foreach (DataDrivenSourceController line in jsonDataDrivenLines) {
                if (!line.InUse()) {
                    line.SetSourceLineOutput("No Destinations Using this Source");
                    continue;
                } else {
                    atLeastOneActive = true;
                }
                line.PrepareJSON();
            }

            foreach (DataDrivenSourceController line in databaseDataDrivenLines) {
                if (!line.InUse()) {
                    line.SetSourceLineOutput("No Destinations Using this Source");
                    continue;
                } else {
                    atLeastOneActive = true;
                }
                line.PrepareDB();
            }

            ConsoleMsg($"Preparing Rate Driven Injector  {rateDrivenLines.Count}");

            try {
                foreach (RateDrivenSourceController line in rateDrivenLines) {
                    bool result = line.Prepare();
                    if (!line.InUse()) {
                        line.SetSourceLineOutput("No Destinations Using this Source");
                    } else {
                        atLeastOneActive = true;
                    }
                }
            } catch (Exception ex) {
                logger.Error($"Rate Source Error  = {ex.Message}");
            }

            foreach (LineExecutionController line in destLines) {
                Task.Run(() => line.PrePrepare()).Wait();
            }

            if (!atLeastOneActive) {
                ConsoleMsg("Preparation Phase Complete - No Active Destination Lines");
                this.state = ClientState.NoActive;
                this.clientHub.SetStatus(this.executionNodeUuid);
                return false;
            } else {
                ConsoleMsg("Preparation Phase Complete");
                this.state = ClientState.Ready;
                this.clientHub.SetStatus(this.executionNodeUuid);
                return true;
            }


        }

        public void Run(string[] args = null) {
            state = ClientState.Executing;
            clientHub.SetStatus(this.executionNodeUuid);
            if (args != null && args.Length > 1) {
                startAtEnabled = true;
                startAt = args[1];
            }

            if (startAt != null && startAtEnabled) {
                DateTime start = DateTime.Parse(startAt);
                logger.Info($"Scheduling start for {start}");

                double wait = (start - DateTime.Now).TotalMilliseconds;

                if (wait < 0) {
                    start = start.AddDays(1);
                }

                wait = (start - DateTime.Now).TotalMilliseconds;

                if (wait > 0) {
                    timerStart = new Timer {
                        Interval = wait,
                        AutoReset = false,
                        Enabled = true
                    };
                    timerStart.Elapsed += OnStartEvent;
                } else {
                    ConsoleMsg("Scheduled Start is in the past - immediate execution");
                    Task.Run(() => RunInternal());
                }

                ConsoleMsg($"Scheduling start for {start}");
                state = ClientState.ExecutionPending;
                this.clientHub.SetStatus(this.executionNodeUuid);
            } else {
                Task.Run(() => RunInternal());
            }
        }

        public void RunInternal() {
            bool prepareOK = true;

            foreach (LineExecutionController line in destLines) {
                bool linePrep = line.Prepare();
                if (!linePrep) {
                    prepareOK = false;
                }
            }

            if (!prepareOK) {
                ConsoleMsg("==>  Error - Could Not Prepare Destination Lines <==");
                return;
            }

            ConsoleMsg($"Test Execution Start. Duration = {duration} seconds");

            Tuple<int, int, TriggerRecord> result = eventDistributor.InitTriggers(duration);

            if (result == null) {
                ConsoleMsg("No Triggers available for the configured time interval the test will be running");
                Stop(true);
                return;
            }

            foreach (DataDrivenSourceController line in csvDataDrivenLines) {
                if (line.InUse()) line.Start(result.Item3);
            }

            foreach (DataDrivenSourceController line in excelDataDrivenLines) {
                if (line.InUse()) line.Start(result.Item3);
            }
            foreach (DataDrivenSourceController line in jsonDataDrivenLines) {
                if (line.InUse()) line.Start(result.Item3);
            }
            foreach (DataDrivenSourceController line in databaseDataDrivenLines) {
                if (line.InUse()) line.Start(result.Item3);
            }
            foreach (DataDrivenSourceController line in xmlDataDrivenLines) {
                if (line.InUse()) line.Start(result.Item3);
            }
            foreach (RateDrivenSourceController line in rateDrivenLines) {
                if (line.InUse()) line.Start();
            }

            stopWatch.Reset();
            stopWatch.Start();

            foreach (LineExecutionController line in destLines) {
                try {
                    line.Start();
                } catch (Exception ex) {
                    logger.Error(ex.Message);
                }
            }

            eventDistributor.ScheduleFirst();

            SetTriggerLabel("Scheduled Triggers");

            CheckForCompletion();
        }

        private List<string> TriggersInUse(XmlDocument dataModel) {
            List<string> triggers = new List<string>();
            try {
                XmlNodeList nodes = dataModel.SelectNodes("//subscribed");
                foreach (XmlNode node in nodes) {
                    XmlNode parent = node.ParentNode;
                    if (parent.Attributes["disabled"]?.Value == "true" || parent.Attributes["disabled"]?.Value == "True") {
                        continue;
                    }
                    triggers.Add(node.InnerText);
                }
            } catch (Exception ex) {
                ConsoleMsg($"Error processing triggers in use: {ex.Message} ");
            }
            return triggers;
        }

        internal bool CheckForCompletion() {
            bool completed = true;
            foreach (RateDrivenSourceController line in rateDrivenLines) {
                if (!line.finished) {
                    completed = false;
                }
            }

            if (eventDistributor.triggerQueue.Count > 0) {
                completed = false;
            }

            if (completed) {
                Stop(false);
            }

            return completed;
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

        public string GetFileName(string uuid) {
            /*
             *    Accepts the uuid of a file entry and returns the full local path name
             *    taking into account if it is a local file or in the archive
             */
            XmlNode fileNode = dataModel.SelectSingleNode($"//*[@uuid = '{uuid}']");
            bool isArchiveFile = bool.Parse(fileNode.Attributes["IsInArchive"].Value);
            if (!isArchiveFile) {
                return fileNode.Attributes["fullPath"].Value;
            } else {
                if (fileNode.Name == "datafile") {
                    return Path.Combine(ArchiveDirectory, "Data", fileNode.Attributes["name"].Value);
                } else {
                    return Path.Combine(ArchiveDirectory, "Templates", fileNode.Attributes["name"].Value);
                }
            }
        }

        public string GetFileContent(string uuid) {
            string fileName = GetFileName(uuid);
            if (File.Exists(fileName)) {
                return File.ReadAllText(fileName);
            } else {
                return null;
            }
        }

        public void RetrieveStandAlone(string remoteUri) {
            Reset();
            archName = remoteUri.Substring(remoteUri.LastIndexOf('/') + 1);
            WebClient myWebClient = new WebClient();
            // Download home page data.
            string archiveRoot = ArchiveDirectory;
            logger.Warn("Downloading " + remoteUri + " to " + ArchiveDirectory);           // Download the Web resource and save it into a data buffer.
            byte[] myDataBuffer = myWebClient.DownloadData(remoteUri);
            dataModel = LoadInjectorBase.Common.Utils.ExtractArchiveToDirectory(myDataBuffer, archiveRoot, "lia.lia", false);
        }

        public void RetrieveArchive(string remoteUri) {
            Reset();

            archName = remoteUri.Substring(remoteUri.LastIndexOf('/') + 1);
            Directory.Delete(ArchiveDirectory, true);
            WebClient myWebClient = new WebClient();
            // Download home page data.
            string archiveRoot = ArchiveDirectory;
            logger.Warn("Downloading " + archName + " to " + ArchiveDirectory);            // Download the Web resource and save it into a data buffer.
            byte[] myDataBuffer = myWebClient.DownloadData(remoteUri);
            dataModel = Utils.ExtractArchiveToDirectory(myDataBuffer, archiveRoot, "lia.lia", true);
            this.InitModel(dataModel);
        }

        public void StopService() {
            Directory.Delete(ArchiveDirectory, true);
            Stop();
        }

        public void Stop(bool manual = false) {
            if (!(state.Value == ClientState.Executing.Value ||
                state.Value == ClientState.ExecutionPending.Value ||
                state.Value == ClientState.WaitingNextIteration.Value
                )) {
                clientHub.ConsoleMsg(executionNodeUuid, null, "Stop received, but not in executing state");
            }

            try {
                this.repetitionTimer?.Stop();
            } catch (Exception ex) {
                ConsoleMsg(ex.Message);
            }
            try {
                executionTimer?.Stop();
            } catch (Exception ex) {
                ConsoleMsg(ex.Message);
            }

            try {
                timer?.Stop();
            } catch (Exception ex) {
                ConsoleMsg(ex.Message);
            }
            try {
                eventDistributor?.Stop();
            } catch (Exception ex) {
                ConsoleMsg($"Shutdown error: {ex.Message}");
            }
            try {
                stopWatch?.Stop();
            } catch (Exception ex) {
                ConsoleMsg($"Shutdown error: {ex.Message}");
            }

            ConsoleMsg("Shutting down lines");
            StopLines();
            logger.Info("Test Iteration Complete");
            state = ClientState.ExecutionComplete;
            clientHub.SetStatus(this.executionNodeUuid);

            if (standAloneMode && manual) {
                PrepareIterationCompletionReport();
                SaveExcelCompletionReport();
            }
        }

        public void Cancel() {
            try {
                timerStart?.Stop();
            } catch (Exception) {
                // NO-OP
            }

            eventDistributor?.Stop();
            ConsoleMsg("Scheduled Start Cancelled");
        }

        public void ProgramStop() {
            try {
                Directory.Delete(ArchiveDirectory, true);
            } catch (Exception) {
                //NO-OP
            }
        }

        public void StopLines() {
            foreach (LineExecutionController line in destLines) {
                try {
                    line.Stop();
                } catch (Exception) {
                    // Do Noting
                }
            }

            foreach (RateDrivenSourceController line in rateDrivenLines) {
                try {
                    line.Stop();
                } catch (Exception) {
                    // NO-OP
                }
            }

            foreach (DataDrivenSourceController line in csvDataDrivenLines) {
                try {
                    line.Stop();
                } catch (Exception) {
                    // NO-OP
                }
            }
            foreach (DataDrivenSourceController line in jsonDataDrivenLines) {
                try {
                    line.Stop();
                } catch (Exception) {
                    // NO-OP
                }
            }
            foreach (DataDrivenSourceController line in excelDataDrivenLines) {
                try {
                    line.Stop();
                } catch (Exception) {
                    // NO-OP
                }
            }
            foreach (DataDrivenSourceController line in xmlDataDrivenLines) {
                try {
                    line.Stop();
                } catch (Exception) {
                    // NO-OP
                }
            }
            foreach (DataDrivenSourceController line in databaseDataDrivenLines) {
                try {
                    line.Stop();
                } catch (Exception) {
                    // NO-OP
                }
            }
        }

        public string GetTemporaryDirectory() {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private void OnStartEvent(Object source, ElapsedEventArgs e) {
            ConsoleMsg("--- Scheduled Start Time ---");
            Task.Run(() => RunInternal());
        }

        private void SetTriggerLabel(string label) {
            //       clientHub.SetTriggerLabel(this.executionNodeUuid, null, label);
        }

        private void ConsoleMsg(string msg) {
            clientHub.ConsoleMsg(this.executionNodeUuid, null, msg);
        }

        private void ClearTriggerData() {
            //          clientHub.ClearTriggerData(this.executionNodeUuid, null);
        }

        public void ProduceCompletionReport() {
            try {
                Process currentProcess = Process.GetCurrentProcess();
                this.iterationRecords.IPAddress = Utils.GetLocalIPAddress();
                this.iterationRecords.ProcessID = currentProcess.Id.ToString();
                this.iterationRecords.WorkPacakage = this.archName;
                clientHub.SendCompletionReport(executionNodeUuid, iterationRecords);
            } catch (Exception ex) {
                logger.Warn($"Produce completion report error {ex.Message}");
            }
        }
    }
}