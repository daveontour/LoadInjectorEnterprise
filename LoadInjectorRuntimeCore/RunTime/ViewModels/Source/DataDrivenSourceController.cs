using LoadInjector.RunTime.ViewModels;
using LoadInjectorBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjector.RunTime {

    public class DataDrivenSourceController : SourceControllerAbstract {
        internal bool ConfigOK = true;

        private readonly bool sequentialFlight;
        private readonly List<string> triggersThisLine = new List<string>();
        private readonly string uid = Guid.NewGuid().ToString();

        private readonly string timeElement;
        private readonly string timeElementFormat;
        private readonly bool relativeTime;

        public DataDrivenSourceController(XmlNode node, List<string> triggersInUse, int serverOffset, NgExecutionController nGExecutionController) : base(node, 1, triggersInUse, serverOffset, nGExecutionController) {
            this.node = node;
            this.triggersInUse = triggersInUse;
            this.serverOffset = serverOffset;

            try {
                var v = node.Attributes["sequentialFlight"]?.Value;
                if (v != null) {
                    sequentialFlight = bool.Parse(node.Attributes["sequentialFlight"].Value);
                } else {
                    sequentialFlight = true;
                }
            } catch (Exception) {
                sequentialFlight = true;
            }

            try {
                var v = node.Attributes["relativeTime"]?.Value;
                if (v != null) {
                    relativeTime = bool.Parse(v);
                } else {
                    relativeTime = false;
                }
            } catch (Exception) {
                relativeTime = false;
            }

            foreach (XmlNode trigger in this.node.SelectNodes("./trigger")) {
                string triggerID = trigger.Attributes["id"]?.Value;
                if (triggerID == null) {
                    continue;
                }
                if (triggersInUse.Contains(triggerID)) {
                    lineInUse = true;
                }
            }

            if (node.Name == "csvdatadriven") {
                dataFile = node.Attributes["dataFile"]?.Value;
                dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                dataRestURL = node.Attributes["dataRestURL"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
            }
            if (node.Name == "exceldatadriven") {
                dataFile = node.Attributes["dataFile"]?.Value;
                excelSheet = node.Attributes["excelSheet"]?.Value;
                excelRowStart = node.Attributes["excelRowStart"]?.Value;
                excelRowEnd = node.Attributes["excelRowEnd"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
            }
            if (node.Name == "xmldatadriven") {
                dataFile = node.Attributes["dataFile"]?.Value;
                dataRestURL = node.Attributes["dataRestURL"]?.Value;
                repeatingElement = node.Attributes["repeatingElement"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
                dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                try {
                    xmlToString = bool.Parse(node.Attributes["xmlToString"]?.Value);
                } catch (Exception) {
                    xmlToString = false;
                }
            }
            if (node.Name == "jsondatadriven") {
                dataFile = node.Attributes["dataFile"]?.Value;
                dataRestURL = node.Attributes["dataRestURL"]?.Value;
                dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                repeatingElement = node.Attributes["repeatingElement"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
            }
            if (node.Name == "databasedatadriven") {
                connStr = node.Attributes["connStr"]?.Value;
                sql = node.Attributes["sql"]?.Value;
                dbType = node.Attributes["sourceType"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
            }
        }

        private bool SetTriggers(List<Dictionary<String, String>> records, string timeStrElement, string timeFormat) {
            if (records == null || records.Count == 0) {
                SetSourceLineOutput("No data records available to process");
                return true;
            }

            int notSetEvents = 0;
            int setEvents = 0;

            int index = 0;

            foreach (Dictionary<String, String> record in records) {
                FlightNode flt = null;

                if (fltRecords.Count > 0) {
                    if (sequentialFlight) {
                        flt = fltRecords[index++ % fltRecords.Count];
                    } else {
                        Random rnd = new Random();
                        flt = fltRecords[rnd.Next(fltRecords.Count)];
                    }
                }

                Tuple<Dictionary<string, string>, FlightNode> triggerData = new Tuple<Dictionary<string, string>, FlightNode>(record, flt);

                foreach (XmlNode trigger in node.SelectNodes("trigger")) {
                    string triggerID = trigger.Attributes["id"].Value;
                    triggersThisLine.Add(triggerID);

                    if (!triggersInUse.Contains(triggerID)) {
                        continue;
                    }

                    if (!relativeTime) {
                        CultureInfo provider = CultureInfo.InvariantCulture;
                        string timeStr = record[timeStrElement];
                        DateTime triggerTime;
                        try {
                            triggerTime = DateTime.Parse(timeStr);
                        } catch (Exception) {
                            try {
                                triggerTime = DateTime.ParseExact(timeStr, timeFormat, provider);
                            } catch (Exception ex) {
                                sourceLogger.Error(ex,$"Error parsing triggerTime. Unparsed string: {timeStr}");
                                break;
                            }
                        }
                        //Apply any offset to the trigger time

                        bool hasOffset = Int32.TryParse(trigger.Attributes["delta"]?.Value, out int offset);
                        if (hasOffset) {
                            triggerTime = triggerTime.AddMinutes(offset);
                        }

                        // Add the offset between the client and the server
                        triggerTime = triggerTime.AddMinutes(serverOffset);

                        bool eventSet = eventDistributor.ScheduleEvent(new TriggerRecord(chainedController, triggerTime, timeStr, relativeTime, triggerID, triggerData, uid, refreshFlight));
                        if (eventSet) {
                            setEvents++;
                        } else {
                            notSetEvents++;
                        }
                    } else {
                        // Time Relative to start
                        CultureInfo provider = CultureInfo.InvariantCulture;
                        string timeStr = record[timeStrElement];
                        DateTime triggerTime = DateTime.Now;

                        //Apply any offset to the trigger time

                        bool hasOffset = double.TryParse(timeStr, out double offset);
                        if (hasOffset) {
                            triggerTime = triggerTime.AddMilliseconds(offset * 1000);
                        }

                        if (offset < 5) {
                            sourceLogger.Warn("Triggering Event Rejected. Time Relative to start must be > 5 seconds");
                            continue;
                        }

                        bool eventSet = eventDistributor.ScheduleEvent(new TriggerRecord(chainedController, triggerTime, timeStr, relativeTime, triggerID, triggerData, uid, refreshFlight));
                        if (eventSet) {
                            setEvents++;
                        } else {
                            notSetEvents++;
                        }
                    }
                }
            }

            foreach (XmlNode trigger in node.SelectNodes("trigger")) {
                string triggerID = trigger.Attributes["id"].Value;
                if (!triggersInUse.Contains(triggerID)) {
                    continue;
                }
                eventDistributor.AddDispatcher(triggerID);
                eventDistributor.AddMonitorHandler(triggerID, this);
            }

            SetSourceLineOutput($"{setEvents} events set for triggering.");

            foreach (RateDrivenSourceController chain in chainedController) {
                chain.Prepare(null, null, null);
            }

            return true;
        }

        public bool PrepareXML() {
            if (PrepareXML(timeElement)) {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            } else {
                return false;
            }
        }

        public bool PrepareJSON() {
            if (PrepareJSON(timeElement)) {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            } else {
                return false;
            }
        }

        public bool PrepareDB() {
            if (PrepareDB(timeElement)) {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            } else {
                return false;
            }
        }

        public bool PrepareExcel() {
            if (PrepareExcel(timeElement)) {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            } else {
                return false;
            }
        }

        public bool PrepareCSV() {
            if (PrepareCSV(timeElement)) {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            } else {
                return false;
            }
        }

        public bool PrepareAMS(List<FlightNode> all, List<FlightNode> arr, List<FlightNode> dep) {
            if (!lineInUse) {
                SetSourceLineOutput("No destination lines configured to use triggers for this type of flight");
                return true;
            }

            if (node.Name != "amsdatadriven") {
                return true;
            }

            //Prepare the iteration data before prepring the flights
            bool prepOK;
            switch (dataSourceType) {
                case "CSV":
                    prepOK = PrepareCSV();
                    break;

                case "DATABASE":
                case "MSSQL":
                case "MySQL":
                case "ORACLE":
                    prepOK = PrepareDB();
                    break;

                case "XML":
                    prepOK = PrepareXML();
                    break;

                case "Excel":
                    prepOK = PrepareExcel();
                    break;

                case "JSON":
                    prepOK = PrepareJSON();
                    break;

                default:
                    prepOK = true;
                    break;
            }

            if (!prepOK) {
               sourceLogger.Error("Preparation of iteration data for AMS Data Driven line was not successful");
                return false;
            }

            DateTime upperTimeLimit = DateTime.Now.AddMinutes(flightSetTo).AddMinutes(-1 * serverOffset);
            DateTime lowerTimeLimit = DateTime.Now.AddMinutes(flightSetFrom).AddMinutes(-1 * serverOffset);

            List<FlightNode> poolSource = all;

            if (flttype == FlightType.Departure) {
                poolSource = dep;
            } else if (flttype == FlightType.Arrival) {
                poolSource = arr;
            }

            /*
             *  Check the correct set of flights are being used.
             */

            int notSetEvents = 0;
            int setEvents = 0;

            sourceLogger.Info($"Preparing Flights and Triggers for Line: {name}. Flight Set From = {flightSetFrom}, Flight Set To = {flightSetTo}, Server Offset = {serverOffset}  ");
            sourceLogger.Info($"Preparing Flights and Triggers for Line: {name}. Will be using flights with STO between {lowerTimeLimit} and {upperTimeLimit}");

            try {
                foreach (FlightNode flt in poolSource) {
                    // Skip any flight whose STO is outside the bounds specified
                    if (flt.dateTime < lowerTimeLimit || flt.dateTime > upperTimeLimit) {
                        sourceLogger.Warn($"Line:{name}. Flight STO Outside Line Range. {flt}");
                        continue;
                    }
                    sourceLogger.Trace($"Line:{name}. Flight STO In Range. {flt}");

                    if (expression != null && flt != null && filterTime == "pre") {
                        bool pass = expression.Pass(flt.FightXML);
                        if (!pass) {
                            sourceLogger.Trace("Pre Filter. Flight does not meet filter conditions");
                            continue;
                        }
                    }

                    if (topLevelFilter != null && flt != null && filterTime == "pre") {
                        bool pass = topLevelFilter.Pass(flt.FightXML);
                        if (!pass) {
                            sourceLogger.Trace("Pre Filter. Flight does not meet filter conditions");
                            continue;
                        }
                    }

                    int dataRecordIndex = 0;

                    foreach (XmlNode trigger in node.SelectNodes("trigger")) {
                        /*
                             *  Get a set of the required triggers from all the lines and only
                             *  set the triger if it is required
                             */

                        string triggerID = trigger.Attributes["id"].Value;
                        triggersThisLine.Add(triggerID);

                        if (!triggersInUse.Contains(triggerID)) {
                            continue;
                        }

                        DateTime triggerTime = flt.dateTime;

                        try {
                            if (trigger.Attributes["trigType"].Value == "externalName") {
                                triggerTime = DateTime.Parse(flt.externals[trigger.Attributes["externalName"].Value]);
                            }
                        } catch (Exception ex) {
                            sourceLogger.Error(ex,$"Error parsing trigger time for external name {flt.externals[trigger.Attributes["externalName"].Value]}");
                            break;
                        }

                        //Apply any offset to the trigger time

                        bool hasOffset = Int32.TryParse(trigger.Attributes["delta"]?.Value, out int offset);
                        if (hasOffset) {
                            triggerTime = triggerTime.AddMinutes(offset);
                        }

                        // Add the offset between the client and the server
                        triggerTime = triggerTime.AddMinutes(serverOffset);

                        Dictionary<string, string> dataRecord = null;
                        if (dataRecords.Count > 0) {
                            dataRecord = dataRecords[dataRecordIndex++ % dataRecords.Count];
                        }

                        Tuple<Dictionary<string, string>, FlightNode> triggerData = new Tuple<Dictionary<string, string>, FlightNode>(dataRecord, flt);

                        bool eventSet = false;
                        if (filterTime == "post") {
                            eventSet = eventDistributor.ScheduleEvent(new TriggerRecord(chainedController, triggerTime, null, false, triggerID, triggerData, uid, refreshFlight, topLevelFilter, expression));
                        } else {
                            eventSet = eventDistributor.ScheduleEvent(new TriggerRecord(chainedController, triggerTime, null, false, triggerID, triggerData, uid, refreshFlight));
                        }

                        if (eventSet) {
                            setEvents++;
                        } else {
                            notSetEvents++;
                        }
                    }
                }
            } catch (Exception ex) {
                sourceLogger.Error(ex,"Error preparing triggers for AMS");
            }

            foreach (XmlNode trigger in node.SelectNodes("trigger")) {
                string triggerID = trigger.Attributes["id"].Value;
                if (!triggersInUse.Contains(triggerID)) {
                    continue;
                }
                eventDistributor.AddDispatcher(triggerID);
                eventDistributor.AddMonitorHandler(triggerID, this);
            }

            SetSourceLineOutput($"{setEvents} events set for triggering.");

            sourceLogger.Info($"Complete Preparing Flights and Triggers for Line: {name}.\n");

            return true;
        }

        public void TriggerHandler(object sender, TriggerFiredEventArgs e) {
            foreach (TriggerRecord record in eventDistributor.triggerQueue) {
                if (triggersInUse.Contains(record.ID) && triggersThisLine.Contains(record.ID)) {
                    if (e.Flight != null) {
                        SetSourceLineOutput($"Trigger: {e.TriggerName}. Fired for {e.Flight.airlineCode}{e.Flight.fltNumber} at {DateTime.Now}. Next Event {record.ID} for {record.record.Item2.DisplayFlight} at {record.TIME}.");
                        return;
                    } else {
                        SetSourceLineOutput($"Trigger: {e.TriggerName} at {DateTime.Now}. Next Event {record.ID}  at {record.TIME}.");
                        return;
                    }
                }
            }

            SetSourceLineOutput($"Trigger: {e.TriggerName} at {DateTime.Now}. No further scheduled events");
            executionController.CheckForCompletion();
        }

        internal void Start(TriggerRecord first) {
            if (first == null) {
                SetSourceLineOutput("No triggers utilised from this line or triggers beyond test time");
                return;
            }

            Queue<TriggerRecord> q = eventDistributor.triggerQueue;
            Task.Run(() => {
                if (first.lineUid == uid) {
                    if (first.record.Item2 != null) {
                        SetSourceLineOutput($"Next Event {first.ID} for {first.record.Item2.DisplayFlight} at {first.TIME}.");
                    } else {
                        SetSourceLineOutput($"Next Event {first.ID} at {first.TIME}.");
                    }
                } else {
                    foreach (TriggerRecord record in q) {
                        if (triggersInUse.Contains(record.ID) && triggersThisLine.Contains(record.ID)) {
                            if (record.record.Item2 != null) {
                                SetSourceLineOutput($"Next Event {record.ID} for {record.record.Item2.DisplayFlight} at {record.TIME}.");
                                return;
                            } else {
                                SetSourceLineOutput($"Next Event {record.ID} at {record.TIME}.");
                            }
                        }
                    }
                }
            });
        }

        internal void Stop() {
            SetSourceLineOutput("Test Run Complete");
        }
    }
}