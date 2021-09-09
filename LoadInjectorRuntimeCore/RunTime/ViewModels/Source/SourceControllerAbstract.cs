using LoadInjector.RunTime.EngineComponents;
using LoadInjectorBase.Interfaces;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.RunTime.ViewModels {
    public abstract class SourceControllerAbstract {
        public XmlNode node;
        public NgExecutionController executionController;

        public TriggerEventDistributor eventDistributor;
        public string name;

        public string postBodyText;

        public int serverOffset;
        public double messagesPerMinute;

        public string dataFile;
        public string dataSourceFileOrURL;
        public string datatURL;
        public string repeatingElement;

        public bool xmlToString;

        public string connStr;
        public string sql;
        public string dbType;

        public string excelSheet;
        public string excelStartRow;
        public string excelEndRow;

        public string dataSourceType;

        public List<string> triggersInUse;
        public bool lineInUse = false;

        public List<Dictionary<string, string>> dataRecords = new List<Dictionary<string, string>>();

        public string triggerID;
        public bool refreshFlight;
        public List<IChainedSourceController> chainedController = new List<IChainedSourceController>();

        public ClientHub clientHub;

        public string executionNodeID;

        public string uuid;

        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
        public static readonly Logger destLogger = LogManager.GetLogger("destLogger");
        public static readonly Logger sourceLogger = LogManager.GetLogger("sourceLogger");

        protected SourceControllerAbstract(XmlNode node, int chainDepth, List<string> triggersInUse, int serverOffset, NgExecutionController executionController) {
            this.node = node;
            this.triggersInUse = triggersInUse;
            this.clientHub = executionController?.clientHub;
            this.eventDistributor = executionController?.eventDistributor;
            this.executionController = executionController;
            this.serverOffset = serverOffset;
            this.name = node.Attributes["name"]?.Value;
            this.triggerID = node.Attributes["triggerID"]?.Value;

            this.dataSourceType = node.Attributes["dataType"]?.Value;
            this.postBodyText = node.SelectSingleNode(".//postBody")?.InnerText;
            this.executionNodeID = node.Attributes["executionNodeUuid"]?.Value;
            this.uuid = node.Attributes["uuid"]?.Value;

            string dfName = node.Attributes["dataFileID"]?.Value;
            if (dfName != null) {
                this.dataFile = executionController.GetFileName(dfName);
            }

            switch (dataSourceType) {
                case "csv":
                    this.dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                    this.datatURL = node.Attributes["dataURL"]?.Value;
                    break;

                case "excel":
                    this.excelSheet = node.Attributes["excelSheet"]?.Value;
                    this.excelStartRow = node.Attributes["excelStartRow"]?.Value;
                    this.excelEndRow = node.Attributes["excelEndRow"]?.Value;
                    break;

                case "xml":
                    this.datatURL = node.Attributes["dataURL"]?.Value;
                    this.repeatingElement = node.Attributes["repeatingElement"]?.Value;
                    this.dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                    try {
                        this.xmlToString = bool.Parse(node.Attributes["xmlToString"]?.Value);
                    } catch (Exception) {
                        this.xmlToString = false;
                    }
                    break;

                case "json":
                    this.datatURL = node.Attributes["dataURL"]?.Value;
                    this.dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                    this.repeatingElement = node.Attributes["repeatingElement"]?.Value;
                    break;

                case "database":
                case "mssql":
                case "mysql":
                case "oracle":
                    this.connStr = node.Attributes["connStr"]?.Value;
                    this.sql = node.Attributes["sql"]?.Value;
                    this.dbType = node.Attributes["sourceType"]?.Value;
                    break;
            }

            this.dataSourceFileOrURL = dataSourceFileOrURL ?? "file";

            // Add the chained controllers
            foreach (XmlNode chained in node.SelectNodes("./chained")) {
                chainedController.Add(new RateDrivenSourceController(chained, chainDepth++, triggersInUse, serverOffset, executionController));
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

        public bool InUse() {
            if (node.Attributes["disabled"]?.Value == "true") {
                return false;
            }

            if (triggerID != null && triggersInUse.Contains(triggerID)) {
                return true;
            }

            return lineInUse;
        }

        public List<string> GetDataPointsInUse(string triggerType, string attToAdd) {
            List<string> inUse = new List<string>();
            List<string> triggerIDS = new List<string> { triggerID };
            foreach (XmlNode chain in node.SelectNodes("./chained")) {
                if (chain.Attributes["useParentData"].Value.Equals("true", StringComparison.InvariantCultureIgnoreCase)) {
                    triggerIDS.Add(chain.Attributes["triggerID"].Value);
                }
            }

            foreach (string tID in triggerIDS) {
                foreach (XmlNode sub in node.SelectNodes($"//subscribed[text() = '{tID}']")) {
                    XmlNode destNode = sub.ParentNode;       // The destination nodes that use this triggering event
                    XmlNodeList vars = destNode.SelectNodes($".//variable[@type = '{triggerType}']");  //The variables of the required type
                    foreach (XmlNode v in vars) {
                        string dataPoint = v.Attributes[attToAdd]?.Value;
                        if (dataPoint != null && !inUse.Contains(dataPoint)) {
                            inUse.Add(dataPoint);
                        }
                    }
                }
            }

            return inUse;
        }

        public List<string> GetDataPointsInUse(List<string> inUse, string triggerType, string attToAdd) {
            try {
                List<string> triggerIDS = new List<string> {
                    triggerID
                };
                foreach (XmlNode chain in node.SelectNodes("./chained")) {
                    if (chain.Attributes["useParentData"].Value.Equals("true", StringComparison.InvariantCultureIgnoreCase)) {
                        triggerIDS.Add(chain.Attributes["triggerID"].Value);
                    }
                }
                foreach (XmlNode trigger in node.SelectNodes("trigger")) {
                    triggerIDS.Add(trigger.Attributes["triggerID"].Value);
                }
                foreach (string trig in triggerIDS) {
                    foreach (XmlNode sub in node.SelectNodes($"//subscribed[text() = '{trig}']")) {
                        XmlNode destNode = sub.ParentNode;       // The destination nodes that use this triggering event
                        XmlNodeList vars = destNode.SelectNodes($".//variable[@type = '{triggerType}']");  //The variables of the required type
                        foreach (XmlNode v in vars) {
                            string dataPoint = v.Attributes[attToAdd]?.Value;
                            if (dataPoint != null && !inUse.Contains(dataPoint)) {
                                inUse.Add(dataPoint);
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Error determining data requirements for trigger.");
            }

            return inUse;
        }

        public bool PrepareXML(string timeInUse = null) {
            List<string> dataPointsInUse;
            if (timeInUse != null) {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "xmlElement", "xmlXPath");
            } else {
                dataPointsInUse = GetDataPointsInUse("xmlElement", "element");
            }

            if (dataPointsInUse.Count == 0) {
                return true;
            }

            XmlProcessor xmlProcessor = new XmlProcessor();

            try {
                this.dataRecords = xmlProcessor.GetRecords(dataFile, postBodyText, datatURL, repeatingElement, dataSourceFileOrURL, dataPointsInUse, xmlToString, node).Result;
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Error retrieving XML data ");
                return false;
            }

            return true;
        }

        public bool PrepareJSON(string timeInUse = null) {
            List<string> dataPointsInUse;
            if (timeInUse != null) {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "jsonElement", "field");
            } else {
                dataPointsInUse = GetDataPointsInUse("jsonElement", "element");
            }
            if (dataPointsInUse.Count == 0) {
                return true;
            }
            JsonProcessor jsonProcessor = new JsonProcessor();

            try {
                this.dataRecords = jsonProcessor.GetRecords(dataFile, postBodyText, datatURL, repeatingElement, dataSourceFileOrURL, dataPointsInUse, node).Result;
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Error retrieving JSON data");
                return false;
            }

            return true;
        }

        public bool PrepareDB(string timeInUse = null) {
            List<string> dataPointsInUse;
            if (timeInUse != null) {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "dbField", "field");
            } else {
                dataPointsInUse = GetDataPointsInUse("dbField", "field");
            }
            if (dataPointsInUse.Count == 0) {
                return true;
            }
            DBProcessor dbProcessor = new DBProcessor();

            try {
                this.dataRecords = dbProcessor.GetRecords(connStr, sql, dbType, dataPointsInUse);
            } catch (Exception ex) {
                sourceLogger.Error(ex, $"Error retrieving Database data: ");
                return false;
            }

            return true;
        }

        public bool PrepareExcel(string timeInUse = null) {
            List<string> dataPointsInUse;
            if (timeInUse != null) {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "excelField", "excelColumn");
            } else {
                dataPointsInUse = GetDataPointsInUse("excelField", "excelColumn");
            }
            if (dataPointsInUse.Count == 0) {
                return true;
            }
            ExcelProcessor excelProcessor = new ExcelProcessor();

            try {
                this.dataRecords = excelProcessor.GetRecords(dataFile, excelSheet, dataPointsInUse, "Text", null, int.Parse(excelStartRow), int.Parse(excelEndRow), true);
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Error retrieving Excel data.");
                return false;
            }

            return true;
        }

        public bool PrepareCSV(string timeInUse = null) {
            List<string> dataPointsInUse;
            if (timeInUse != null) {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "csvField", "field");
            } else {
                dataPointsInUse = GetDataPointsInUse("csvField", "csvField");
            }
            if (dataPointsInUse.Count == 0) {
                return true;
            }
            CsvProcessor csvProcessor = new CsvProcessor(dataFile, postBodyText, datatURL, dataSourceFileOrURL, dataPointsInUse, node);

            try {
                this.dataRecords = csvProcessor.GetRecords();
            } catch (Exception ex) {
                sourceLogger.Error(ex, "Error retrieving CSV data.");
                return false;
            }

            return true;
        }

        public void SetMsgPerMin(String s) {
            //            clientHub.SetMsgPerMin(this.executionNodeID, this.uuid, s);
        }

        public void SetConfiguredMsgPerMin(String s) {
            //         clientHub.SetConfiguredMsgPerMin(this.executionNodeID, this.uuid, s);
        }

        public void SetSourceLineOutput(String s) {
            clientHub.SetSourceLineOutput(this.executionNodeID, this.uuid, s);
        }

        public void Report(string v, int messagesSent, double currentRate, double messagesPerMinute) {
            clientHub.SendSourceReport(this.executionNodeID, this.uuid, v, messagesSent, currentRate, messagesPerMinute);
        }

        public void ReportChain(string v, int messagesSent) {
            clientHub.SourceReportChain(this.executionNodeID, this.uuid, v, messagesSent, v, messagesSent);
        }
    }
}