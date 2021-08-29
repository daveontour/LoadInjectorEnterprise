using LoadInjector.RunTime.ViewModels;
using LoadInjectorBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjector.RunTime
{
    public class DataDrivenSourceController : SourceControllerAbstract
    {
        internal bool ConfigOK = true;
        public int messagesSent;
        private readonly List<string> triggersThisLine = new List<string>();
        private readonly string uid = Guid.NewGuid().ToString();

        private readonly string timeElement;
        private readonly string timeElementFormat;
        private readonly bool relativeTime;

        public DataDrivenSourceController(XmlNode node, List<string> triggersInUse, int serverOffset, NgExecutionController nGExecutionController) : base(node, 1, triggersInUse, serverOffset, nGExecutionController)
        {
            this.node = node;
            this.triggersInUse = triggersInUse;
            this.serverOffset = serverOffset;

            try
            {
                var v = node.Attributes["relativeTime"]?.Value;
                if (v != null)
                {
                    relativeTime = bool.Parse(v);
                }
                else
                {
                    relativeTime = false;
                }
            }
            catch (Exception)
            {
                relativeTime = false;
            }

            foreach (XmlNode trigger in this.node.SelectNodes("./trigger"))
            {
                string triggerID = trigger.Attributes["triggerID"]?.Value;
                if (triggerID == null)
                {
                    continue;
                }
                if (triggersInUse.Contains(triggerID))
                {
                    lineInUse = true;
                }
            }

            if (node.Name == "csvdatadriven")
            {
                dataFile = nGExecutionController.GetFileName(node.Attributes["dataFile"]?.Value);
                dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                dataRestURL = node.Attributes["dataRestURL"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
            }
            if (node.Name == "exceldatadriven")
            {
                dataFile = nGExecutionController.GetFileName(node.Attributes["dataFile"]?.Value);
                excelSheet = node.Attributes["excelSheet"]?.Value;
                excelRowStart = node.Attributes["excelRowStart"]?.Value;
                excelRowEnd = node.Attributes["excelRowEnd"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
            }
            if (node.Name == "xmldatadriven")
            {
                dataFile = nGExecutionController.GetFileName(node.Attributes["dataFile"]?.Value);
                dataRestURL = node.Attributes["dataRestURL"]?.Value;
                repeatingElement = node.Attributes["repeatingElement"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
                dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                try
                {
                    xmlToString = bool.Parse(node.Attributes["xmlToString"]?.Value);
                }
                catch (Exception)
                {
                    xmlToString = false;
                }
            }
            if (node.Name == "jsondatadriven")
            {
                dataFile = nGExecutionController.GetFileName(node.Attributes["dataFile"]?.Value);
                dataRestURL = node.Attributes["dataRestURL"]?.Value;
                dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                repeatingElement = node.Attributes["repeatingElement"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
            }
            if (node.Name == "databasedatadriven")
            {
                connStr = node.Attributes["connStr"]?.Value;
                sql = node.Attributes["sql"]?.Value;
                dbType = node.Attributes["sourceType"]?.Value;
                timeElement = node.Attributes["timeElement"]?.Value;
                timeElementFormat = node.Attributes["timeElementFormat"]?.Value;
            }
        }

        private bool SetTriggers(List<Dictionary<String, String>> records, string timeStrElement, string timeFormat)
        {
            if (records == null || records.Count == 0)
            {
                SetSourceLineOutput("No data records available to process");
                return true;
            }

            int notSetEvents = 0;
            int setEvents = 0;

            int index = 0;

            foreach (Dictionary<String, String> record in records)
            {
                Tuple<Dictionary<string, string>> triggerData = new Tuple<Dictionary<string, string>>(record);

                foreach (XmlNode trigger in node.SelectNodes("trigger"))
                {
                    string triggerID = trigger.Attributes["triggerID"].Value;
                    triggersThisLine.Add(triggerID);

                    if (!triggersInUse.Contains(triggerID))
                    {
                        continue;
                    }

                    if (!relativeTime)
                    {
                        CultureInfo provider = CultureInfo.InvariantCulture;
                        string timeStr = record[timeStrElement];
                        DateTime triggerTime;
                        try
                        {
                            triggerTime = DateTime.Parse(timeStr);
                        }
                        catch (Exception)
                        {
                            try
                            {
                                triggerTime = DateTime.ParseExact(timeStr, timeFormat, provider);
                            }
                            catch (Exception ex)
                            {
                                sourceLogger.Error(ex, $"Error parsing triggerTime. Unparsed string: {timeStr}");
                                break;
                            }
                        }
                        //Apply any offset to the trigger time

                        bool hasOffset = Int32.TryParse(trigger.Attributes["delta"]?.Value, out int offset);
                        if (hasOffset)
                        {
                            triggerTime = triggerTime.AddMinutes(offset);
                        }

                        // Add the offset between the client and the server
                        triggerTime = triggerTime.AddMinutes(serverOffset);

                        bool eventSet = eventDistributor.ScheduleEvent(new TriggerRecord(chainedController, triggerTime, timeStr, relativeTime, triggerID, triggerData, uid, refreshFlight));
                        if (eventSet)
                        {
                            setEvents++;
                        }
                        else
                        {
                            notSetEvents++;
                        }
                    }
                    else
                    {
                        // Time Relative to start
                        CultureInfo provider = CultureInfo.InvariantCulture;
                        string timeStr = record[timeStrElement];
                        DateTime triggerTime = DateTime.Now;

                        //Apply any offset to the trigger time

                        bool hasOffset = double.TryParse(timeStr, out double offset);
                        if (hasOffset)
                        {
                            triggerTime = triggerTime.AddMilliseconds(offset * 1000);
                        }

                        if (offset < 5)
                        {
                            sourceLogger.Warn("Triggering Event Rejected. Time Relative to start must be > 5 seconds");
                            continue;
                        }

                        bool eventSet = eventDistributor.ScheduleEvent(new TriggerRecord(chainedController, triggerTime, timeStr, relativeTime, triggerID, triggerData, uid, refreshFlight));
                        if (eventSet)
                        {
                            setEvents++;
                        }
                        else
                        {
                            notSetEvents++;
                        }
                    }
                }
            }

            foreach (XmlNode trigger in node.SelectNodes("trigger"))
            {
                string triggerID = trigger.Attributes["triggerID"].Value;
                if (!triggersInUse.Contains(triggerID))
                {
                    continue;
                }
                eventDistributor.AddDispatcher(triggerID);
                eventDistributor.AddMonitorHandler(triggerID, this);
            }

            SetSourceLineOutput($"{setEvents} events set for triggering.");

            foreach (RateDrivenSourceController chain in chainedController)
            {
                chain.Prepare();
            }

            return true;
        }

        public bool PrepareXML()
        {
            messagesSent = 0;
            if (PrepareXML(timeElement))
            {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            }
            else
            {
                return false;
            }
        }

        public bool PrepareJSON()
        {
            messagesSent = 0;
            if (PrepareJSON(timeElement))
            {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            }
            else
            {
                return false;
            }
        }

        public bool PrepareDB()
        {
            messagesSent = 0;
            if (PrepareDB(timeElement))
            {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            }
            else
            {
                return false;
            }
        }

        public bool PrepareExcel()
        {
            messagesSent = 0;
            if (PrepareExcel(timeElement))
            {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            }
            else
            {
                return false;
            }
        }

        public bool PrepareCSV()
        {
            messagesSent = 0;
            if (PrepareCSV(timeElement))
            {
                return SetTriggers(dataRecords, timeElement, timeElementFormat);
            }
            else
            {
                return false;
            }
        }

        public void TriggerHandler(object sender, TriggerFiredEventArgs e)
        {
            foreach (TriggerRecord record in eventDistributor.triggerQueue)
            {
                if (triggersInUse.Contains(record.ID) && triggersThisLine.Contains(record.ID))
                {
                    messagesSent++;
                    Report(null, messagesSent, 0, 0);

                    Report($"Next Event {record.ID}  at {record.TIME}.", messagesSent, 0, 0);
                    SetSourceLineOutput($"Trigger: {e.TriggerName} at {DateTime.Now}. Next Event {record.ID}  at {record.TIME}.");
                    return;
                }
            }
            messagesSent++;
            Report($"Trigger: {e.TriggerName} at {DateTime.Now}. No further scheduled events", messagesSent, 0, 0);
            SetSourceLineOutput($"Trigger: {e.TriggerName} at {DateTime.Now}. No further scheduled events");
            executionController.CheckForCompletion();
        }

        internal void Start(TriggerRecord first)
        {
            messagesSent = 0;
            if (first == null)
            {
                SetSourceLineOutput("No triggers utilised from this line or triggers beyond test time");
                return;
            }

            Queue<TriggerRecord> q = eventDistributor.triggerQueue;
            Task.Run(() =>
            {
                if (first.lineUid == uid)
                {
                    SetSourceLineOutput($"Next Event {first.ID} at {first.TIME}.");
                }
                else
                {
                    foreach (TriggerRecord record in q)
                    {
                        if (triggersInUse.Contains(record.ID) && triggersThisLine.Contains(record.ID))
                        {
                            messagesSent++;
                            SetSourceLineOutput($"Next Event {record.ID} at {record.TIME}.");
                        }
                    }
                }
            });
        }

        internal void Stop()
        {
            SetSourceLineOutput("Test Run Complete");
        }
    }
}