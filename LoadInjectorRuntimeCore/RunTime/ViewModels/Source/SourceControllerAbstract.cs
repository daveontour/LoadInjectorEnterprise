using LoadInjector.RunTime.EngineComponents;

using System;
using System.Collections.Generic;

using System.Xml;
using LoadInjector.Filters;
using LoadInjectorBase;
using LoadInjectorBase.Interfaces;
using NLog;

namespace LoadInjector.RunTime.ViewModels
{
    public abstract class SourceControllerAbstract
    {
        public XmlNode node;
        public NgExecutionController executionController;

        //public IProgress<ControllerStatusReport> lineProgress;
        //public IProgress<ControllerStatusReport> controllerProgress;

        public TriggerEventDistributor eventDistributor;
        public string name;

        public int serverOffset;
        public double messagesPerMinute;

        public string dataFile;
        public string dataSourceFileOrURL;
        public string dataRestURL;
        public string repeatingElement;

        public bool xmlToString;

        public string connStr;
        public string sql;
        public string dbType;

        public string excelSheet;
        public string excelRowStart;
        public string excelRowEnd;

        public string dataSourceType;

        public List<string> triggersInUse;
        public bool lineInUse = false;

        public List<Dictionary<string, string>> dataRecords = new List<Dictionary<string, string>>();

        public IQueueFilter topLevelFilter;
        public Expression expression;
        public string filterTime;

        public string id;
        public bool refreshFlight;
        public List<IChainedSourceController> chainedController = new List<IChainedSourceController>();

        public ClientHub clientHub;

        public string executionNodeID;

        public string uuid;

        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
        public static readonly Logger destLogger = LogManager.GetLogger("destLogger");
        public static readonly Logger sourceLogger = LogManager.GetLogger("sourceLogger");

        protected SourceControllerAbstract(XmlNode node, int chainDepth, List<string> triggersInUse, int serverOffset, NgExecutionController executionController)
        {
            this.node = node;
            this.triggersInUse = triggersInUse;
            this.clientHub = executionController?.clientHub;
            eventDistributor = executionController?.eventDistributor;
            this.executionController = executionController;
            this.serverOffset = serverOffset;
            name = node.Attributes["name"]?.Value;
            id = node.Attributes["ID"]?.Value;

            dataSourceType = node.Attributes["dataSource"]?.Value;

            executionNodeID = node.Attributes["executionNodeUuid"]?.Value;
            uuid = node.Attributes["uuid"]?.Value;

            switch (dataSourceType)
            {
                case "CSV":
                    dataFile = node.Attributes["dataFile"]?.Value;
                    dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                    dataRestURL = node.Attributes["dataRestURL"]?.Value;
                    break;

                case "Excel":
                    dataFile = node.Attributes["dataFile"]?.Value;
                    excelSheet = node.Attributes["excelSheet"]?.Value;
                    excelRowStart = node.Attributes["excelRowStart"]?.Value;
                    excelRowEnd = node.Attributes["excelRowEnd"]?.Value;
                    break;

                case "XML":
                    dataFile = node.Attributes["dataFile"]?.Value;
                    dataRestURL = node.Attributes["dataRestURL"]?.Value;
                    repeatingElement = node.Attributes["repeatingElement"]?.Value;
                    dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                    try
                    {
                        xmlToString = bool.Parse(node.Attributes["xmlToString"]?.Value);
                    }
                    catch (Exception)
                    {
                        xmlToString = false;
                    }
                    break;

                case "JSON":
                    dataFile = node.Attributes["dataFile"]?.Value;
                    dataRestURL = node.Attributes["dataRestURL"]?.Value;
                    dataSourceFileOrURL = node.Attributes["sourceType"]?.Value;
                    repeatingElement = node.Attributes["repeatingElement"]?.Value;
                    break;

                case "DATABASE":
                case "MSSQL":
                case "MySQL":
                case "ORACLE":
                    connStr = node.Attributes["connStr"]?.Value;
                    sql = node.Attributes["sql"]?.Value;
                    dbType = node.Attributes["sourceType"]?.Value;
                    break;
            }

            dataSourceFileOrURL = dataSourceFileOrURL ?? "file";

            XmlNode filtersDefn = node.SelectSingleNode("./filter");

            if (filtersDefn != null)
            {
                /*
                 * At the top level, there is only one Expresssion, which it self can be a compound
                 * expression or a single data filter
                 *
                 * When the Expression itself is constucted, it recurssive creates all the Expression o
                 * filters configured under it
                 */

                // Cycle through the expressions types (and, or, not, xor) to see if any exist
                foreach (string eType in Expression.expressionTypes)
                {
                    XmlNode exprDefn = filtersDefn.SelectSingleNode($"./{eType}");
                    if (exprDefn != null)
                    {
                        expression = new Expression(exprDefn);
                    }
                }

                FilterFactory fact = new FilterFactory();
                // Cycle through the data filter types (and, or, not, xor) to see if any exist
                foreach (string fType in Expression.filterTypes)
                {
                    XmlNode filtDefn = filtersDefn.SelectSingleNode($"./{fType}");
                    if (filtDefn != null)
                    {
                        topLevelFilter = fact.GetFilter(filtDefn);
                    }
                }

                try
                {
                    filterTime = filtersDefn.Attributes["filterTime"]?.Value;
                }
                catch (Exception)
                {
                    filterTime = "post";
                }

                if (filterTime == null)
                {
                    filterTime = "post";
                }
            }

            // Add the chained controllers
            foreach (XmlNode chained in node.SelectNodes("./chained"))
            {
                chainedController.Add(new RateDrivenSourceController(chained, chainDepth++, triggersInUse, serverOffset, executionController));
            }
        }

        private bool CheckDisabled(XmlNode node)
        {
            bool disabled = false;
            if (node.Attributes["disabled"] != null)
            {
                try
                {
                    disabled = bool.Parse(node.Attributes["disabled"].Value);
                }
                catch (Exception)
                {
                    disabled = false;
                }
            }
            return disabled;
        }

        public bool InUse()
        {
            if (node.Attributes["disabled"]?.Value == "true")
            {
                return false;
            }

            if (id != null && triggersInUse.Contains(id))
            {
                return true;
            }

            return lineInUse;
        }

        public List<string> GetDataPointsInUse(string triggerType, string attToAdd)
        {
            List<string> inUse = new List<string>();
            List<string> triggerIDS = new List<string> {
                id
            };
            foreach (XmlNode chain in node.SelectNodes("./chained"))
            {
                if (chain.Attributes["useParentData"].Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                {
                    triggerIDS.Add(chain.Attributes["ID"].Value);
                }
            }

            foreach (string tID in triggerIDS)
            {
                foreach (XmlNode sub in node.SelectNodes($"//subscribed[text() = '{tID}']"))
                {
                    XmlNode destNode = sub.ParentNode;       // The destination nodes that use this triggering event
                    XmlNodeList vars = destNode.SelectNodes($".//variable[@type = '{triggerType}']");  //The variables of the required type
                    foreach (XmlNode v in vars)
                    {
                        string dataPoint = v.Attributes[attToAdd]?.Value;
                        if (dataPoint != null && !inUse.Contains(dataPoint))
                        {
                            inUse.Add(dataPoint);
                        }
                    }
                }
            }

            return inUse;
        }

        public List<string> GetDataPointsInUse(List<string> inUse, string triggerType, string attToAdd)
        {
            try
            {
                List<string> triggerIDS = new List<string> {
                    id
                };
                foreach (XmlNode chain in node.SelectNodes("./chained"))
                {
                    if (chain.Attributes["useParentData"].Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    {
                        triggerIDS.Add(chain.Attributes["ID"].Value);
                    }
                }
                foreach (XmlNode trigger in node.SelectNodes("trigger"))
                {
                    triggerIDS.Add(trigger.Attributes["id"].Value);
                }
                foreach (string triggerID in triggerIDS)
                {
                    foreach (XmlNode sub in node.SelectNodes($"//subscribed[text() = '{triggerID}']"))
                    {
                        XmlNode destNode = sub.ParentNode;       // The destination nodes that use this triggering event
                        XmlNodeList vars = destNode.SelectNodes($".//variable[@type = '{triggerType}']");  //The variables of the required type
                        foreach (XmlNode v in vars)
                        {
                            string dataPoint = v.Attributes[attToAdd]?.Value;
                            if (dataPoint != null && !inUse.Contains(dataPoint))
                            {
                                inUse.Add(dataPoint);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sourceLogger.Error(ex, "Error determining data requirements for trigger.");
            }

            return inUse;
        }

        public bool PrepareXML(string timeInUse = null)
        {
            List<string> dataPointsInUse;
            if (timeInUse != null)
            {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "xmlElement", "xmlXPath");
            }
            else
            {
                dataPointsInUse = GetDataPointsInUse("xmlElement", "xmlXPath");
            }

            if (dataPointsInUse.Count == 0)
            {
                return true;
            }

            XmlProcessor xmlProcessor = new XmlProcessor();

            try
            {
                dataRecords = xmlProcessor.GetRecords(dataFile, dataRestURL, repeatingElement, dataSourceFileOrURL, dataPointsInUse, xmlToString, node);
            }
            catch (Exception ex)
            {
                sourceLogger.Error(ex, "Error retrieving XML data ");
                return false;
            }

            return true;
        }

        public bool PrepareJSON(string timeInUse = null)
        {
            List<string> dataPointsInUse;
            if (timeInUse != null)
            {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "jsonElement", "field");
            }
            else
            {
                dataPointsInUse = GetDataPointsInUse("jsonElement", "field");
            }
            if (dataPointsInUse.Count == 0)
            {
                return true;
            }
            JsonProcessor jsonProcessor = new JsonProcessor();

            try
            {
                dataRecords = jsonProcessor.GetRecords(dataFile, dataRestURL, repeatingElement, dataSourceFileOrURL, dataPointsInUse, node);
            }
            catch (Exception ex)
            {
                sourceLogger.Error(ex, "Error retrieving JSON data");
                return false;
            }

            return true;
        }

        public bool PrepareDB(string timeInUse = null)
        {
            List<string> dataPointsInUse;
            if (timeInUse != null)
            {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "dbField", "field");
            }
            else
            {
                dataPointsInUse = GetDataPointsInUse("dbField", "field");
            }
            if (dataPointsInUse.Count == 0)
            {
                return true;
            }
            DBProcessor dbProcessor = new DBProcessor();

            try
            {
                dataRecords = dbProcessor.GetRecords(connStr, sql, dbType, dataPointsInUse);
            }
            catch (Exception ex)
            {
                sourceLogger.Error(ex, $"Error retrieving Database data: ");
                return false;
            }

            return true;
        }

        public bool PrepareExcel(string timeInUse = null)
        {
            List<string> dataPointsInUse;
            if (timeInUse != null)
            {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "excelCol", "excelCol");
            }
            else
            {
                dataPointsInUse = GetDataPointsInUse("excelCol", "excelCol");
            }
            if (dataPointsInUse.Count == 0)
            {
                return true;
            }
            ExcelProcessor excelProcessor = new ExcelProcessor();

            try
            {
                dataRecords = excelProcessor.GetRecords(dataFile, excelSheet, dataPointsInUse, "Text", null, int.Parse(excelRowStart), int.Parse(excelRowEnd), true);
            }
            catch (Exception ex)
            {
                sourceLogger.Error(ex, "Error retrieving Excel data.");
                return false;
            }

            return true;
        }

        public bool PrepareCSV(string timeInUse = null)
        {
            List<string> dataPointsInUse;
            if (timeInUse != null)
            {
                dataPointsInUse = new List<string> {
                    timeInUse
                };
                dataPointsInUse = GetDataPointsInUse(dataPointsInUse, "csvfield", "field");
            }
            else
            {
                dataPointsInUse = GetDataPointsInUse("csvfield", "field");
            }
            if (dataPointsInUse.Count == 0)
            {
                return true;
            }
            CsvProcessor csvProcessor = new CsvProcessor(dataFile, dataRestURL, dataSourceFileOrURL, dataPointsInUse, node);

            try
            {
                dataRecords = csvProcessor.GetRecords();
            }
            catch (Exception ex)
            {
                sourceLogger.Error(ex, "Error retrieving CSV data.");
                return false;
            }

            return true;
        }

        public void SetMsgPerMin(String s)
        {
            //            clientHub.SetMsgPerMin(this.executionNodeID, this.uuid, s);
        }

        public void SetConfiguredMsgPerMin(String s)
        {
            //         clientHub.SetConfiguredMsgPerMin(this.executionNodeID, this.uuid, s);
        }

        public void SetSourceLineOutput(String s)
        {
            clientHub.SetSourceLineOutput(this.executionNodeID, this.uuid, s);
        }

        public void Report(string v, int messagesSent, double currentRate, double messagesPerMinute)
        {
            clientHub.SendSourceReport(this.executionNodeID, this.uuid, v, messagesSent, currentRate, messagesPerMinute);
        }

        public void ReportChain(string v, int messagesSent)
        {
            clientHub.SourceReportChain(this.executionNodeID, this.uuid, v, messagesSent, v, messagesSent);
        }
    }
}