using System;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.Common
{
    public static class TriggerTypeMap
    {
        public static readonly Dictionary<string, string> triggerDataTypeMap = new Dictionary<string, string>();
        public static readonly Dictionary<string, string> triggerFlightTypeMap = new Dictionary<string, string>();

        public static Dictionary<string, string> SetTriggerMap(XmlNode node)
        {
            triggerDataTypeMap.Clear();
            triggerFlightTypeMap.Clear();

            // First deal with the Data Driven type
            foreach (XmlNode tNode in node.SelectNodes("//trigger"))
            {
                XmlNode pNode = tNode.ParentNode;
                string pType = pNode.Name;
                string id = tNode.Attributes["id"]?.Value;
                if (id == null)
                {
                    continue;
                }

                try
                {
                    // Populate the data type
                    switch (pType)
                    {
                        case "jsondatadriven":
                            triggerDataTypeMap.Add(id, "JSON");
                            break;

                        case "csvdatadriven":
                            triggerDataTypeMap.Add(id, "CSV");
                            break;

                        case "exceldatadriven":
                            triggerDataTypeMap.Add(id, "Excel");
                            break;

                        case "xmldatadriven":
                            triggerDataTypeMap.Add(id, "XML");
                            break;

                        case "databasedatadriven":
                            triggerDataTypeMap.Add(id, "DATABASE");
                            break;
                    }
                    // Populate the flight type
                    string flttype = pNode.Attributes["flttype"]?.Value;
                    flttype = flttype ?? "none";

                    triggerFlightTypeMap.Add(id, flttype);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // Rate driven types
            foreach (XmlNode pNode in node.SelectNodes("//ratedrivensources/*"))
            {
                try
                {
                    string id = pNode.Attributes["ID"]?.Value;
                    if (id == null)
                    {
                        continue;
                    }

                    string flttype = pNode.Attributes["flttype"]?.Value;
                    flttype = flttype ?? "none";

                    triggerFlightTypeMap.Add(pNode.Attributes["ID"]?.Value, flttype);

                    switch (pNode.Attributes["dataSource"]?.Value)
                    {
                        case "MSSQL":
                        case "MySQL":
                        case "ORACLE":
                            triggerDataTypeMap.Add(id, "DATABASE");
                            break;

                        default:
                            triggerDataTypeMap.Add(id, pNode.Attributes["dataSource"]?.Value);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            // Chained driven types
            foreach (XmlNode pNode in node.SelectNodes("//chained"))
            {
                string triggerID = pNode.Attributes["ID"]?.Value;

                if (triggerID == null)
                {
                    continue;
                }
                SetTriggerDataSourceType(pNode, triggerID);
            }

            return triggerDataTypeMap;
        }

        public static void SetTriggerDataSourceType(XmlNode node, string ogID)
        {
            if (node.Attributes["useParentData"] != null && node.Attributes["useParentData"].Value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
            {
                SetTriggerDataSourceType(node.ParentNode, ogID);
                return;
            }

            try
            {
                if (node.Attributes["ID"]?.Value == null)
                {
                    return;
                }

                string flttype = node.Attributes["flttype"]?.Value ?? "none";
                triggerFlightTypeMap.Add(ogID, flttype);

                switch (node.Attributes["dataSource"]?.Value)
                {
                    case "MSSQL":
                    case "MySQL":
                    case "ORACLE":
                        triggerDataTypeMap.Add(ogID, "DATABASE");
                        break;

                    default:
                        triggerDataTypeMap.Add(ogID, node.Attributes["dataSource"]?.Value);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static bool CheckCompatible(string trigID, XmlNode des)
        {
            XmlNodeList currentSubTrigger = des.SelectNodes(".//subscribed");

            // Case where not previous trigger selected
            if (currentSubTrigger == null || currentSubTrigger.Count == 0)
            {
                return true;
            }

            string newTrigData = triggerDataTypeMap[trigID];
            string newTrigFlight = triggerFlightTypeMap[trigID];

            foreach (XmlNode sub in currentSubTrigger)
            {
                string subTrigData = triggerDataTypeMap[sub.InnerText];
                string subTrigFlight = triggerFlightTypeMap[sub.InnerText];

                if (newTrigData != subTrigData)
                {
                    return false;
                }

                if (newTrigFlight == "none" || newTrigFlight == subTrigFlight)
                {
                    return true;
                }
            }
            return false;
        }

        public static Tuple<string, string> GetDataAndFlightTypesForVariable(XmlNode var)
        {
            string dataType = null;
            string flttype = "none";

            XmlNode dest = var.ParentNode;
            XmlNodeList triggers = dest.SelectNodes("./subscribed");

            foreach (XmlNode trig in triggers)
            {
                try
                {
                    string trigDataType = triggerDataTypeMap[trig.InnerText];
                    string trigFltType = triggerFlightTypeMap[trig.InnerText];

                    dataType = trigDataType;
                    flttype = trigFltType != "none" ? trigFltType : flttype;
                }
                catch (Exception)
                {
                    // NO-OP
                }
            }

            Tuple<string, string> tuple = new Tuple<string, string>(dataType, flttype);
            return tuple;
        }
    }
}