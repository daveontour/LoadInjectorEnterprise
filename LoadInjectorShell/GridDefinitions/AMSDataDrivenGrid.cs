using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [DisplayName("AMS Data Driven Source")]
    public class AmsDataDrivenGrid : LoadInjectorGridBase {

        public AmsDataDrivenGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [CategoryAttribute("Required"), DisplayName("Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("Name of the Destination")]
        public string Name {
            get => GetAttribute("name");
            set {
                SetAttribute("name", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Flight Type"), PropertyOrder(5), Browsable(true), DescriptionAttribute("Type of flights to use"), ItemsSource(typeof(FlightTypeListNoNoneOut))]
        public string FlightType {
            get => GetFlightType();
            set {
                SetFlightType(value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("Required"), DisplayName("Earliest Offset for Flights (mins)"), ReadOnly(false), Browsable(true), PropertyOrder(9), DescriptionAttribute("The offset in minutes of the earliest scheduled time of the flight to use")]
        public int FlightSetFrom {
            get {
                int val = GetIntAttribute("flightSetFrom");
                if (val == -1) {
                    val = -180;
                    SetAttribute("flightSetFrom", -180);
                    view.UpdateParamBindings("XMLText");
                    view.UpdateDiagram();
                }
                return val;
            }
            set {
                SetAttribute("flightSetFrom", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("Required"), DisplayName("Latest Offset for Flights (mins)"), ReadOnly(false), Browsable(true), PropertyOrder(10), DescriptionAttribute("The offset in minutes of the latest scheduled time of the flight to use")]
        public int FlightSetTo {
            get {
                int val = GetIntAttribute("flightSetTo");
                if (val == -1) {
                    val = 540;
                    SetAttribute("flightSetTo", 540);
                    view.UpdateParamBindings("XMLText");
                    view.UpdateDiagram();
                }
                return val;
            }
            set {
                SetAttribute("flightSetTo", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("Required"), DisplayName("Refresh Flight"), ReadOnly(false), Browsable(true), PropertyOrder(11), DescriptionAttribute("Refresh the flight from AMS at the time of use")]
        public bool RefreshFlight {
            get => GetBoolAttribute("refreshFlight");
            set => SetAttribute("refreshFlight", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Trigger Source Type"), PropertyOrder(15), Browsable(true), DescriptionAttribute("Trigger Source Type"), ItemsSource(typeof(TriggerSourceTypeListNoNoneOut))]
        public string TriggerSourceType {
            get {
                string type = GetAttribute("trigType");
                if (type == "sto") {
                    Hide(new[] { "ExternalName", "XMLXPath", "Format" });
                    return "Scheduled Time of Operation";
                } else if (type == "externalName") {
                    Hide(new[] { "XMLXPath" });
                    Show(new[] { "ExternalName", "Format" });
                    return "External Name";
                } else if (type == "xpath") {
                    Hide(new[] { "ExternalName", "Format" });
                    Show(new[] { "XMLXPath" });
                    return "XPath";
                } else {
                    Hide(new[] { "ExternalName", "Format" });
                    return null;
                }
            }
            set {
                if (value == "Scheduled Time of Operation") {
                    SetAttribute("trigType", "sto");
                    SetAttribute("externalName", null);
                    Hide(new[] { "ExternalName", "XMLXPath" });
                    foreach (XmlNode node in _node.SelectNodes("./trigger")) {
                        SetAttribute("trigType", "sto", node);
                        SetAttribute("externalName", null, node);
                    }
                }
                if (value == "External Name") {
                    SetAttribute("trigType", "externalName");
                    foreach (XmlNode node in _node.SelectNodes("./trigger")) {
                        SetAttribute("trigType", "externalName", node);
                    }
                    Hide(new[] { "XMLXPath" });
                    Show(new[] { "ExternalName", "Format" });
                }
                if (value == "XPath") {
                    SetAttribute("trigType", "xpath");
                    Hide(new[] { "FlifoExternalName", "Format" });
                    Show(new[] { "XMLXPath" });
                }
            }
        }

        [CategoryAttribute("Required"), DisplayName("AMS External Name"), ReadOnly(false), Browsable(true), PropertyOrder(16), DescriptionAttribute("The AMS ExternalName which will be the source of the triggering time")]
        public string ExternalName {
            get => GetAttribute("externalName");
            set {
                SetAttribute("externalName", value);
                foreach (XmlNode node in _node.SelectNodes("./trigger")) {
                    SetAttribute("externalName", value, node);
                }
            }
        }

        #region Iteration Data

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Iteration Data"), DisplayName("Data Source"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The source of data to be substituted into the template"), ItemsSource(typeof(DataSourceList))]
        public string DataDataSource {
            get {
                Hide("DataFile");
                Hide("CSVFileHeaders");
                Hide("ExcelSheet");
                Hide("XMLRestURL");
                Hide("JSONRestURL");
                Hide("RepeatData");
                Hide("RepeatElement");
                Hide("RepeatElementJSON");
                Hide("Connection");
                Hide("SQL");

                string ds = GetAttribute("dataSource");
                if (ds == null || ds == "none") {
                    return "None";
                } else {
                    switch (ds) {
                        case "CSV":
                            Show("DataFile");
                            Show("CSVFileHeaders");
                            Show("RepeatData");
                            return "CSV File";

                        case "Excel":
                            Show("DataFile");
                            Show("ExcelSheet");
                            Show("RepeatData");
                            return "Excel File";

                        case "ODBC":
                            return "ODBC Database";

                        case "JDBC":
                            return "JDBC Database";

                        case "RESTXML":
                            Show("RepeatData");
                            Show("XMLRestURL");
                            Show("RepeatElement");
                            return "XML File via Rest";

                        case "XML":
                            Show("RepeatData");
                            Show("DataFile");
                            Show("RepeatElement");
                            return "XML File";

                        case "RESTJSON":
                            Show("RepeatData");
                            Show("JSONRestURL");
                            Show("RepeatElementJSON");
                            return "JSON File via Rest";

                        case "JSON":
                            Show("RepeatData");
                            Show("DataFile");
                            Show("RepeatElementJSON");
                            return "JSON File";

                        case "MSSQL":
                            Show("Connection");
                            Show("SQL");
                            return "MS SQL Database";

                        case "MYSQL":
                            Show("Connection");
                            Show("SQL");
                            return "MySQL Database";

                        case "ORACLE":
                            Show("Connection");
                            Show("SQL");
                            return "Oracle Database";
                    }
                    return "None";
                }
            }
            set {
                Hide("DataFile");
                Hide("CSVFileHeaders");
                Hide("ExcelSheet");
                Hide("XMLRestURL");
                Hide("JSONRestURL");
                Hide("RepeatData");
                Hide("RepeatElement");
                Hide("RepeatElementJSON");
                Hide("Connection");
                Hide("SQL");

                SetAttribute("excelSheet", null);
                SetAttribute("excelRowStart", null);
                SetAttribute("excelRowEnd", null);
                SetAttribute("dataFile", null);

                switch (value) {
                    case "CSV File":
                        Show("DataFile");
                        Show("CSVFileHeaders");
                        SetAttribute("dataSource", "CSV");
                        break;

                    case "Excel File":
                        Show("DataFile");
                        Show("ExcelSheet");
                        SetAttribute("dataSource", "Excel");
                        break;

                    case "ODBC Database":
                        SetAttribute("dataSource", "ODBC");
                        break;

                    case "JDBC Database":
                        SetAttribute("dataSource", "JDBC");
                        break;

                    case "XML File via Rest":
                        Show("XMLRestURL");
                        Show("RepeatElement");
                        SetAttribute("dataSource", "RESTXML");
                        break;

                    case "XML File":
                        Show("RepeatElement");
                        Show("DataFile");
                        SetAttribute("dataSource", "XML");
                        break;

                    case "JSON File via Rest":
                        Show("JSONRestURL");
                        Show("RepeatElementJSON");
                        Show("RepeatData");
                        SetAttribute("dataSource", "RESTJSON");
                        break;

                    case "JSON File":
                        Show("RepeatElementJSON");
                        Show("DataFile");
                        Show("RepeatData");
                        SetAttribute("dataSource", "JSON");
                        break;

                    case "MS SQL Database":
                        Show("Connection");
                        Show("SQL");
                        SetAttribute("dataSource", "MSSQL");
                        break;

                    case "MySQL Database":
                        Show("Connection");
                        Show("SQL");
                        SetAttribute("dataSource", "MYSQL");
                        break;

                    case "Oracle Database":
                        Show("Connection");
                        Show("SQL");
                        SetAttribute("dataSource", "ORACLE");
                        break;

                    default:
                        Hide("DataFile");
                        SetAttribute("dataSource", null);
                        break;
                }

                view.UpdateParamBindings("HeaderVisibility");
            }
        }

        [Editor(typeof(FileNameSelector), typeof(FileNameSelector))]
        [CategoryAttribute("Iteration Data"), DisplayName("Data File"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("Full pathname of the data file")]
        public string DataFile {
            get => GetAttribute("dataFile");
            set => SetAttribute("dataFile", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("CSV File Headers"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("Select if the CSV file has headers on the first line")]
        public bool CSVFileHeaders {
            get => GetBoolAttribute("csvFileHeaders");
            set => SetAttribute("csvFileHeaders", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("Excel Sheet"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The sheet in the Excel file")]
        public string ExcelSheet {
            get => GetAttribute("excelSheet");
            set => SetAttribute("excelSheet", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("XML Rest URL"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The URL to retrieve the XML document")]
        public string XMLRestURL {
            get => GetAttribute("xmlRestURL");
            set => SetAttribute("xmlRestURL", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("JSON Rest URL"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The URL to retrieve the JSON document")]
        public string JSONRestURL {
            get => GetAttribute("jsonRestURL");
            set => SetAttribute("jsonRestURL", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("Repeating Element"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The XML Element that is the repeating element for each iteration")]
        public string RepeatElement {
            get => GetAttribute("repeatingElement");
            set => SetAttribute("repeatingElement", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("Repeating Element"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The JSON Element that is the repeating element for each iteration (JSONPath notation)")]
        public string RepeatElementJSON {
            get => GetAttribute("repeatingElement");
            set => SetAttribute("repeatingElement", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("Stop at End of Data"), ReadOnly(false), Browsable(true), PropertyOrder(8), DescriptionAttribute("Stop the line when the limit of the iteration data is reached. Otherwise, repeat the data from the start")]
        public bool RepeatData {
            get => GetBoolAttribute("stopOnDataEnd");
            set => SetAttribute("stopOnDataEnd", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("Connection String"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The connection string to connect to the database")]
        public string Connection {
            get => GetAttribute("connStr");
            set => SetAttribute("connStr", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("SQL Command"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The SQL command to retrieve the data")]
        public string SQL {
            get => GetAttribute("sql");
            set => SetAttribute("sql", value);
        }

        #endregion Iteration Data

        [CategoryAttribute("Execution"), DisplayName("Disabled"), ReadOnly(false), Browsable(true), PropertyOrder(50), DescriptionAttribute("Disable this Source")]
        public bool Disable {
            get => GetBoolAttribute("disabled");
            set {
                SetAttribute("disabled", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }
    }
}