using LoadInjector.RunTime.EngineComponents;
using LoadInjector.RunTime.ViewModels;
using LoadInjector.RuntimeCore;
using LoadInjectorBase;
using LoadInjectorBase.Common;

using LoadInjectorBase.Common;

using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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

        public readonly List<FlightNode> flights = new List<FlightNode>();
        public readonly List<FlightNode> arrflights = new List<FlightNode>();
        public readonly List<FlightNode> depflights = new List<FlightNode>();

        public readonly List<LineExecutionController> destLines = new List<LineExecutionController>();

        public readonly List<AmsDirectExecutionController> amsLines = new List<AmsDirectExecutionController>();
        public readonly List<DataDrivenSourceController> amsDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<DataDrivenSourceController> csvDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<DataDrivenSourceController> excelDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<DataDrivenSourceController> xmlDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<DataDrivenSourceController> jsonDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<DataDrivenSourceController> databaseDataDrivenLines = new List<DataDrivenSourceController>();
        public readonly List<RateDrivenSourceController> rateDrivenLines = new List<RateDrivenSourceController>();

        private readonly List<Tuple<int, int>> flightSets = new List<Tuple<int, int>>();

        private Timer timer;

        private Timer timerStart;

        private int duration = -1;
        private string startAt;
        private bool startAtEnabled;

        public string token;
        public string apt_code;
        public string apt_icao_code;
        public string amshost;

        public int serverOffset;
        public int amstimeout;

        private int maxOffset = Int32.MinValue;
        private int minOffset = Int32.MaxValue;

        private bool requiresFlights;
        private string logLevel;

        private int repeats;
        private int executedRepeats;
        private int repeatRest;

        private XmlNodeList amsEventDriven;
        private XmlNodeList csvEventDriven;
        private XmlNodeList excelEventDriven;
        private XmlNodeList xmlEventDriven;
        private XmlNodeList jsonEventDriven;
        private XmlNodeList databaseEventDriven;
        private XmlNodeList rateDriven;
        private XmlNodeList amsDirect;
        private XmlNodeList destinations;

        public ClientHub clientHub;
        public IterationRecords iterationRecords = new IterationRecords();

        private readonly string getFlightsTemplate = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ams6=""http://www.sita.aero/ams6-xml-api-webservice"">
   <soapenv:Header/>
   <soapenv:Body>
      <ams6:GetFlights>
         <ams6:sessionToken>@token</ams6:sessionToken>
         <ams6:from>@from</ams6:from>
         <ams6:to>@to</ams6:to>
         <ams6:airport>@airport</ams6:airport>
         <ams6:airportIdentifierType>IATACode</ams6:airportIdentifierType>
      </ams6:GetFlights>
   </soapenv:Body>
</soapenv:Envelope>";

        public string executionNodeUuid;
        public bool slaveMode = false;
        private Timer executionTimer;
        private int repeatsExecuted = 0;
        private Timer repetitionTimer;

        private string archiveDirectory;
        public string archName = "-Not Assigned-";
        private DateTime executionStarted;

        public string ArchiveDirectory {
            get {
                if (archiveDirectory == null) {
                    archiveDirectory = GetTemporaryDirectory();
                }

                return archiveDirectory;
            }
        }

        public NgExecutionController(ExecutionControllerType type, string serverHub = null, string executeFile = null) {
            // Client Server Mode
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
                XmlDocument doc = new XmlDocument();
                if (executeFile.ToLower().EndsWith(".lia")) {
                    string archiveRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/LoadInjectorRTController";
                    doc = LoadInjectorBase.Common.Utils.ExtractArchiveToDirectory(executeFile, archiveRoot, "lia.lia");
                } else {
                    doc.Load(executeFile);
                }

                this.clientHub = new ClientHub(this);
                this.eventDistributor = new TriggerEventDistributor(this);
                this.InitModel(doc);
                this.RunLocal();
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

            flights.Clear();
            arrflights.Clear();
            depflights.Clear();

            destLines.Clear();
            amsLines.Clear();
            amsDataDrivenLines.Clear();
            csvDataDrivenLines.Clear();
            excelDataDrivenLines.Clear();
            xmlDataDrivenLines.Clear();
            jsonDataDrivenLines.Clear();
            databaseDataDrivenLines.Clear();
            rateDrivenLines.Clear();
            flightSets.Clear();
            clientHub?.consoleMessages?.Clear();
            this.state = ClientState.UnAssigned;
            clientHub?.SetStatus(executionNodeUuid);
            clientHub?.RefreshResponse();
        }

        public void InitModel(XmlDocument model) {
            dataModel = model;

            List<string> triggersInUse = TriggersInUse(dataModel);

            XDocument doc = XDocument.Parse(dataModel.OuterXml);

            this.executionNodeUuid = dataModel.SelectSingleNode("//settings").Attributes["executionNodeUuid"]?.Value;

            try {
                string serverDiff = doc.Descendants("serverDiff").FirstOrDefault().Value;
                serverOffset = Convert.ToInt32(60 * double.Parse(serverDiff));
            } catch (Exception) {
                serverOffset = 0;
            }

            amsEventDriven = dataModel.SelectNodes("//amsdatadriven");
            csvEventDriven = dataModel.SelectNodes("//csvdatadriven");
            excelEventDriven = dataModel.SelectNodes("//exceldatadriven");
            xmlEventDriven = dataModel.SelectNodes("//xmldatadriven");
            jsonEventDriven = dataModel.SelectNodes("//jsondatadriven");
            databaseEventDriven = dataModel.SelectNodes("//databasedatadriven");
            rateDriven = dataModel.SelectNodes("//ratedriven");
            amsDirect = dataModel.SelectNodes("//amsdirect");
            destinations = dataModel.SelectNodes("//destination");

            ValidateModel();

            if (Parameters.SITAAMS) {
                // Add the AMS Direct Injection Lines
                AddDestinationControllerType(amsEventDriven, amsDataDrivenLines, triggersInUse);
            }
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
                if (line.HasFlights() && line.InUse()) {
                    requiresFlights = true;
                }
                flightSets.Add(line.GetFlightSet());
                if (!line.ConfigOK) {
                    ConsoleMsg("Error: Configuration is not valid");
                    this.state = ClientState.ConfigInvalid;
                }
            }

            if (Parameters.SITAAMS) {
                // Add the AMS Direct Injection Lines
                foreach (XmlNode node in amsDirect) {
                    if (CheckDisabled(node)) {
                        continue;
                    }

                    AmsDirectExecutionController line = new AmsDirectExecutionController(node, this);
                    amsLines.Add(line);
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
            if (state.Value != ClientState.Ready.Value) {
                logger.Warn("Execute requested, but not in ready state");
                clientHub.ConsoleMsg(executionNodeUuid, null, "Execute requested, but not in ready state");
                return;
            }

            repeatsExecuted = 0;
            PrepareAsync().Wait();
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
            Stop();

            logger.Info($"Test Execution Repitition {repeatsExecuted} of {repeats} Complete");
            state = ClientState.ExecutionComplete;
            clientHub.SetStatus(this.executionNodeUuid);

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
                    LineRecord lr = new LineRecord();
                    lr.DestinationType = line.LineType;
                    lr.Name = line.name;
                    lr.MessagesSent = line.messagesSent;
                    lr.MessagesFailed = line.messagesFail;
                    logger.Warn("Line record Created");
                    itRecord.DestinationLineRecords.Add(lr);
                    logger.Warn("Line Record Added");
                }

                iterationRecords.Records.Add(itRecord);
                logger.Warn("iteratoin Record Added");
            } catch (Exception ex) {
                logger.Error(ex, $"Iteration records{ex.Message}");
            }

            if (repeatsExecuted < repeats) {
                repetitionTimer = new Timer {
                    Interval = repeatRest * 1000,
                    AutoReset = false,
                    Enabled = true
                };
                repetitionTimer.Elapsed += NextExecution;
                state = ClientState.WaitingNextIteration;
                clientHub.SetStatus(this.executionNodeUuid);
            }
        }

        //private void OnExecutionCompleteEvent(Object source, ElapsedEventArgs e) {
        //    // This is an Event Handler that handles the action when the timer goes off
        //    // signalling the end of the test.
        //    repeatsExecuted++;
        //    Stop();

        //    logger.Info($"Test Execution Repitition {repeatsExecuted} of {repeats} Complete");
        //    state = ClientState.Stopped;
        //    clientHub.SetStatus(this.executionNodeUuid);

        //    // Collect the data for the Completion Report
        //    IterationRecord itRecord = new IterationRecord() {
        //        UUID = this.executionNodeUuid,
        //        ExecutionStart = this.executionStarted,
        //        ExecutionEnd = DateTime.Now,
        //        IterationNumber = repeatsExecuted,
        //    };

        //    foreach (LineExecutionController line in destLines) {
        //        LineRecord lr = new LineRecord();
        //        lr.DestinationType = line.LineType;
        //        lr.Name = line.name;
        //        lr.MessagesSent = line.messagesSent;
        //        lr.MessagesFailed = line.messagesFail;

        //        itRecord.DestinationLineRecords.Add(lr);
        //    }

        //    iterationRecords.Records.Add(itRecord);

        //    if (repeatsExecuted < repeats) {
        //        repetitionTimer = new Timer {
        //            Interval = repeatRest * 1000,
        //            AutoReset = false,
        //            Enabled = true
        //        };
        //        repetitionTimer.Elapsed += NextExecution;
        //        state = ClientState.WaitingNextIteration;
        //        clientHub.SetStatus(this.executionNodeUuid);
        //    } else {
        //        state = ClientState.ExecutionComplete;
        //        clientHub.SetStatus(this.executionNodeUuid);
        //    }
        //}

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
                if (line.HasFlights() && line.InUse()) {
                    requiresFlights = true;
                }
                flightSets.Add(line.GetFlightSet());
                if (!line.ConfigOK) {
                    ConsoleMsg("Error: Configuration is not valid");
                }
            }
        }

        private void ValidateModel() {
            if (Parameters.SITAAMS) {
                ConsoleMsg($"AMS Data Driven Source =  {amsEventDriven.Count}");
            }
            ConsoleMsg($"CSV Data Driven Source =  {csvEventDriven.Count}");
            ConsoleMsg($"Excel Data Driven Source =  {excelEventDriven.Count}");
            ConsoleMsg($"XML Data Driven Source =  {xmlEventDriven.Count}");
            ConsoleMsg($"JSON Data Driven Source =  {jsonEventDriven.Count}");
            ConsoleMsg($"Database Data Driven Source =  {databaseEventDriven.Count}");
            ConsoleMsg($"Rate Driven Source =  {rateDriven.Count}");
            if (Parameters.SITAAMS) {
                ConsoleMsg($"AMS Direct Update Lines =  {amsDirect.Count}");
            }
            ConsoleMsg($"Destination Lines =  {destinations.Count}");

            if ((amsDirect.Count + destinations.Count) == 0) {
                ConsoleMsg("====> Config Error. No Output Lines Defined <====");
            }

            List<string> amsTriggers = new List<string>();

            if (Parameters.SITAAMS) {
                foreach (XmlNode node in amsEventDriven) {
                    int numTriggers = node.SelectNodes(".//trigger").Count;
                    if (numTriggers == 0) {
                        ConsoleMsg($"====> Config Error. No Event Triggers Defined for {node.Attributes["name"].Value} <====");
                    }
                    foreach (XmlNode trigNode in node.SelectNodes(".//trigger")) {
                        string id = trigNode.Attributes["id"]?.Value;
                        if (id != null) {
                            if (amsTriggers.Contains(id)) {
                                ConsoleMsg($"====> Config Error. AMS Event Trigger ID Defined Multiple Times: {id} <====");
                            } else {
                                amsTriggers.Add(id);
                            }
                        } else {
                            ConsoleMsg("====> Config Error. AMS Event Trigger ID is NULL <====");
                        }
                    }
                }
            }

            List<string> csvFields = new List<string>();

            foreach (XmlNode node in csvEventDriven) {
                int numTriggers = node.SelectNodes(".//trigger").Count;
                if (numTriggers == 0) {
                    ConsoleMsg($"====> Config Error. No Event Triggers Defined for {node.Attributes["name"].Value} <====");
                }
                foreach (XmlNode trigNode in node.SelectNodes(".//trigger")) {
                    string id = trigNode.Attributes["id"]?.Value;
                    if (id != null) {
                        if (csvFields.Contains(id) || amsTriggers.Contains(id)) {
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
                    string id = trigNode.Attributes["id"]?.Value;
                    if (id != null) {
                        if (csvFields.Contains(id) || amsTriggers.Contains(id)) {
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

            if (Parameters.SITAAMS) {
                try {
                    var v = doc.Descendants("amstimeout").FirstOrDefault()?.Value;
                    if (v == null) {
                        amstimeout = 60;
                    } else {
                        amstimeout = int.Parse(v);
                    }
                } catch (Exception) {
                    amstimeout = 60;
                }

                try {
                    token = doc.Descendants("amstoken").FirstOrDefault().Value;
                } catch (Exception) {
                    token = null;
                }

                if (token == null) {
                    ConsoleMsg("Warning! AMS Access Token Not Set");
                }

                try {
                    apt_code = doc.Descendants("aptcode").FirstOrDefault().Value;
                } catch (Exception) {
                    apt_code = null;
                }
                if (apt_code == null) {
                    ConsoleMsg("Warning! Airport Code Not Set");
                }
                try {
                    apt_icao_code = doc.Descendants("aptcodeicao").FirstOrDefault().Value;
                } catch (Exception) {
                    apt_icao_code = null;
                }

                try {
                    amshost = doc.Descendants("amshost").FirstOrDefault().Value;
                } catch (Exception) {
                    amshost = "http://localhost/SITAAMSIntegrationService/v2/SITAAMSIntegrationService";
                    ConsoleMsg("Warning! AMS Host set to the default");
                }

                try {
                    string serverDiff = doc.Descendants("serverDiff").FirstOrDefault().Value;
                    serverOffset = Convert.ToInt32(60 * double.Parse(serverDiff));
                } catch (Exception) {
                    serverOffset = 0;
                }

                logger.Info($"Access Token = {token}");
                logger.Info($"AirportCode = {apt_code}");
                logger.Info($"AirportCode (ICAO)= {apt_icao_code}");
            }

            try {
                foreach (var rule in LogManager.Configuration.LoggingRules) {
                    rule.EnableLoggingForLevel(LogLevel.Info);
                    rule.EnableLoggingForLevel(LogLevel.Error);
                    rule.EnableLoggingForLevel(LogLevel.Warn);

                    if (logLevel.ToLower() == "trace") {
                        rule.EnableLoggingForLevel(LogLevel.Trace);
                        logger.Info($"Logging Level set to TRACE for rule {rule.RuleName }");
                    } else {
                        rule.DisableLoggingForLevel(LogLevel.Trace);
                        logger.Info($"Logging Level set to INFO  for rule {rule.RuleName }");
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
            //if (state.Value != ClientState.Assigned.Value) {
            //    clientHub.ConsoleMsg(executionNodeUuid, null, "Prepare requested, but no package assigned");
            //    return false;
            //}

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

            bool prepareOK = true;

            if (Parameters.SITAAMS && requiresFlights) {
                await Task.Run(() => {
                    flights.Clear();
                    arrflights.Clear();
                    depflights.Clear();
                    maxOffset = Int32.MinValue;
                    minOffset = Int32.MaxValue;

                    // Test the connectivity to AMS
                    bool testAMS = GetFlightsAsync(true).Result;

                    if (testAMS) {
                        ConsoleMsg("== AMS Connectivity Verified ==");
                    } else {
                        ConsoleMsg("== Unable to connect to AMS. Preparation Aborted==");
                        prepareOK = false;
                    }

                    ConsoleMsg("=== Retrieving flights from AMS ===");
                    bool amsOK = GetFlightsAsync().Result;
                    if (!amsOK) {
                        ConsoleMsg("== Unable to connect to AMS. Preparation Aborted==");
                        prepareOK = false;
                    } else {
                        ConsoleMsg($"=== {flights.Count} Flights Retrieved from AMS ===");
                    }
                });
            }

            bool atLeastOneActive = false;

            if (prepareOK) {
                ConsoleMsg("Preparing Data Driven AMS Flight Injectors");
                foreach (DataDrivenSourceController line in amsDataDrivenLines) {
                    line.PrepareAMS(flights, arrflights, depflights);
                    if (!line.InUse()) {
                        line.SetSourceLineOutput("No Destinations Using this Source");
                    } else {
                        atLeastOneActive = true;
                    }
                }

                ConsoleMsg("Preparing CSV Data Driven Injector");
                foreach (DataDrivenSourceController line in csvDataDrivenLines) {
                    line.PrepareFlights(flights, arrflights, depflights);
                    if (!line.InUse()) {
                        line.SetSourceLineOutput("No Destinations Using this Source");
                        continue;
                    } else {
                        atLeastOneActive = true;
                    }
                    line.PrepareCSV();
                }

                ConsoleMsg("Preparing Excel Data Driven Injector");
                foreach (DataDrivenSourceController line in excelDataDrivenLines) {
                    line.PrepareFlights(flights, arrflights, depflights);
                    if (!line.InUse()) {
                        line.SetSourceLineOutput("No Destinations Using this Source");
                        continue;
                    } else {
                        atLeastOneActive = true;
                    }
                    line.PrepareExcel();
                }

                ConsoleMsg("Preparing XML Data Driven Injector");
                foreach (DataDrivenSourceController line in xmlDataDrivenLines) {
                    line.PrepareFlights(flights, arrflights, depflights);
                    if (!line.InUse()) {
                        line.SetSourceLineOutput("No Destinations Using this Source");
                        continue;
                    } else {
                        atLeastOneActive = true;
                    }
                    line.PrepareXML();
                }

                ConsoleMsg("Preparing JSON Data Driven Injector");
                foreach (DataDrivenSourceController line in jsonDataDrivenLines) {
                    line.PrepareFlights(flights, arrflights, depflights);
                    if (!line.InUse()) {
                        line.SetSourceLineOutput("No Destinations Using this Source");
                        continue;
                    } else {
                        atLeastOneActive = true;
                    }
                    line.PrepareJSON();
                }
                ConsoleMsg("Preparing DataBase Data Driven Injector");
                foreach (DataDrivenSourceController line in databaseDataDrivenLines) {
                    line.PrepareFlights(flights, arrflights, depflights);
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
                        bool result = line.Prepare(flights, arrflights, depflights);
                        logger.Info($"Rate Source Prepare  = {result}");

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

                if (Parameters.SITAAMS) {
                    foreach (AmsDirectExecutionController line in amsLines) {
                        Task.Run(() => line.PrePrepare()).Wait();
                    }
                }
                ConsoleMsg("Preparation Phase Complete");
            } else {
                ConsoleMsg("Error: Preparing Destination");
            }

            if (!atLeastOneActive) {
                ConsoleMsg("Preparation Phase Complete - No Active Destination Lines");
                this.state = ClientState.NoActive;
                this.clientHub.SetStatus(this.executionNodeUuid);
                return false;
            }

            this.state = ClientState.Ready;
            this.clientHub.SetStatus(this.executionNodeUuid);
            return prepareOK;
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

            if (Parameters.SITAAMS) {
                foreach (AmsDirectExecutionController line in amsLines) {
                    bool linePrep = line.Prepare();
                    if (!linePrep) {
                        prepareOK = false;
                    }
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

            foreach (DataDrivenSourceController line in amsDataDrivenLines) {
                if (line.InUse()) line.Start(result.Item3);
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

            if (Parameters.SITAAMS) {
                foreach (AmsDirectExecutionController line in amsLines) {
                    try {
                        line.Start();
                    } catch (Exception ex) {
                        logger.Error(ex.Message);
                    }
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
            dataModel = LoadInjectorBase.Common.Utils.ExtractArchiveToDirectory(myDataBuffer, archiveRoot, "lia.lia", true);
            this.InitModel(dataModel);
        }

        public void StopService() {
            Directory.Delete(ArchiveDirectory, true);
            Stop();
        }

        public void Stop(bool manual = false) {
            if (state.Value != ClientState.Executing.Value ||
                state.Value != ClientState.ExecutionPending.Value ||
                state.Value != ClientState.WaitingNextIteration.Value
                ) {
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
            state = ClientState.Stopped;
            clientHub.SetStatus(this.executionNodeUuid);
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

        public async Task<bool> GetFlightsAsync(bool test = false) {
            if (!Parameters.SITAAMS) {
                return true;
            }

            if (!test) {
                ConsoleMsg("Preparing Flights");
            }
            try {
                if (test) {
                    minOffset = -10;
                    maxOffset = 10;
                } else {
                    MaxFlightOffsets();
                }
                // Check if any flights are required
                if (minOffset == Int32.MaxValue || maxOffset == Int32.MinValue) {
                    logger.Info("No flights from AMS were required by any of the destinations");
                    ConsoleMsg("No flights from AMS were required by any of the destination");
                    ConsoleMsg("Preparing Flights - Complete");
                    return true;
                }

                minOffset -= serverOffset;
                maxOffset -= serverOffset;

                string flightsQuery = getFlightsTemplate.Replace("@token", token)
                    .Replace("@from", DateTime.Now.AddMinutes(minOffset).ToString("yyyy-MM-ddTHH:mm:ss"))
                    .Replace("@to", DateTime.Now.AddMinutes(maxOffset).ToString("yyyy-MM-ddTHH:mm:ss"))
                    .Replace("@airport", apt_code);

                if (!test) {
                    ConsoleMsg($"Retrieving flights on the server between {DateTime.Now.AddMinutes(minOffset):yyyy-MM-ddTHH:mm:ss} and {DateTime.Now.AddMinutes(maxOffset):yyyy-MM-ddTHH:mm:ss}");
                    ConsoleMsg(flightsQuery);
                }

                try {
                    using (var client = new HttpClient()) {
                        var timeout = TimeSpan.FromSeconds(amstimeout);

                        client.Timeout = timeout;

                        HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, amshost) {
                            Content = new StringContent(flightsQuery, Encoding.UTF8, "text/xml")
                        };
                        requestMessage.Headers.Add("SOAPAction", "http://www.sita.aero/ams6-xml-api-webservice/IAMSIntegrationService/GetFlights");

                        using (HttpResponseMessage response = await client.SendAsync(requestMessage)) {
                            try {
                                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent) {
                                    if (test) {
                                        return true;
                                    } else {
                                        XmlDocument doc = new XmlDocument();
                                        doc.LoadXml(await response.Content.ReadAsStringAsync());
                                        XmlElement flightsElement = doc.DocumentElement;

                                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(flightsElement.OwnerDocument.NameTable);
                                        nsmgr.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");

                                        XmlNodeList fls = flightsElement.SelectNodes("//ams:Flight", nsmgr);
                                        foreach (XmlNode fl in fls) {
                                            FlightNode fn = new FlightNode(fl, nsmgr);
                                            flights.Add(fn);
                                            if (fn.nature == "Arrival") {
                                                arrflights.Add(fn);
                                            }
                                            if (fn.nature == "Departure") {
                                                depflights.Add(fn);
                                            }
                                        }

                                        flights.Sort((x, y) => x.dateTime.CompareTo(y.dateTime));
                                        arrflights.Sort((x, y) => x.dateTime.CompareTo(y.dateTime));
                                        depflights.Sort((x, y) => x.dateTime.CompareTo(y.dateTime));
                                    }
                                } else {
                                    if (!test) {
                                        ConsoleMsg($"Unable to Prepare Flight. Response Status Code: {response.StatusCode}");
                                    } else {
                                        ConsoleMsg($"Unable to Access Flights in AMS. Response Status Code: {response.StatusCode}");
                                    }
                                    return false;
                                }
                            } catch (Exception ex) {
                                ConsoleMsg($"Timeout ({timeout}s) retrieiving flights {ex.Message}");
                                return false;
                            }
                        }
                    }
                } catch (Exception ex) {
                    logger.Error(ex.StackTrace);
                    ConsoleMsg("Unable to Prepare Flights ");
                    return false;
                }
                if (!test) {
                    logger.Info($" Recieved Flights.  {flights.Count} flights available");
                    ConsoleMsg($"Recieved Flights.  {flights.Count} flights available");
                }
                return true;
            } catch (Exception e) {
                logger.Error(e.Message);
                logger.Error(e);
                ConsoleMsg($"Error Retrieving flights from AMS: {e.Message}");
                return false;
            }
        }

        public void MaxFlightOffsets() {
            minOffset = Int32.MaxValue;
            maxOffset = Int32.MinValue;
            try {
                foreach (Tuple<int, int> flightSet in flightSets) {
                    if (flightSet == null) {
                        continue;
                    }
                    int fltSetFrom = flightSet.Item1;
                    int fltSetTo = flightSet.Item2;
                    minOffset = Math.Min(fltSetFrom, minOffset);
                    maxOffset = Math.Max(fltSetTo, maxOffset);
                }
            } catch (Exception ex) {
                ConsoleMsg($"Error determining min/max flight offset: {ex.Message}");
            }
        }

        public void ProgramStop() {
            try {
                Directory.Delete(ArchiveDirectory, true);
            } catch (Exception) { }
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
            foreach (AmsDirectExecutionController line in amsLines) {
                try {
                    line.Stop();
                } catch (Exception) {
                    // NO-OP
                }
            }

            foreach (DataDrivenSourceController line in amsDataDrivenLines) {
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
            clientHub.SetTriggerLabel(this.executionNodeUuid, null, label);
        }

        private void ConsoleMsg(string msg) {
            clientHub.ConsoleMsg(this.executionNodeUuid, null, msg);
        }

        private void ClearTriggerData() {
            clientHub.ClearTriggerData(this.executionNodeUuid, null);
        }

        public void ProduceCompletionReport() {
            try {
                Process currentProcess = Process.GetCurrentProcess();
                //CompletionReport report = new CompletionReport(executionNodeUuid, Utils.GetLocalIPAddress(), currentProcess.Id.ToString());
                this.iterationRecords.IPAddress = Utils.GetLocalIPAddress();
                this.iterationRecords.ProcessID = currentProcess.Id.ToString();
                this.iterationRecords.WorkPacakage = this.archName;
                this.iterationRecords = this.iterationRecords;
                clientHub.SendCompletionReport(executionNodeUuid, iterationRecords);
            } catch (Exception ex) {
                logger.Warn($"Produce completion report error {ex.Message}");
            }
        }
    }
}