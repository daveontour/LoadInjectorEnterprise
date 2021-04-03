using LoadInjector.Common;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("Rate Driven Source Configuration")]
    public class ChainedSourcePropertyGrid : LoadInjectorGridBase {

        public ChainedSourcePropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        #region required

        [CategoryAttribute("Source Configuration"), DisplayName("Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("Name of the Source")]
        public string Name {
            get => GetAttribute("name");
            set {
                SetAttribute("name", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("Source Configuration"), DisplayName("Trigger ID"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The ID used to identify the triggering of the Source")]
        public string ID {
            get => GetAttribute("ID");
            set {
                string currentValue = GetAttribute("ID");

                List<string> triggers = new List<string>();
                foreach (XmlNode trig in _node.SelectNodes("//trigger")) {
                    triggers.Add(trig.Attributes["id"]?.Value);
                }
                foreach (XmlNode trig in _node.SelectNodes("//ratedriven")) {
                    triggers.Add(trig.Attributes["ID"]?.Value);
                }

                if (triggers.Contains(value)) {
                    MessageBox.Show("ID already exists. The ID for the source is already defined by another source or trigger. Source IDs must be unique amongst Source ID and Trigger IDs of the configuration", "Set Source ID", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                SetAttribute("ID", value);

                if (!string.IsNullOrEmpty(currentValue)) {
                    var q = $"//subscribed[text() = '{currentValue}']";
                    foreach (XmlNode node in _node.SelectNodes(q)) {
                        node.InnerText = value;
                    }
                }

                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Source Configuration"), DisplayName("Chained Delay"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("Delay after the parent fires to the firing of this trigger")]
        public int ChainDelay {
            get {
                int v = GetIntAttribute("delay");
                if (v < 0) {
                    v = 0;
                }
                return v;
            }
            set {
                if (value < 0) {
                    value = 0;
                }
                SetAttribute("delay", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Source Configuration"), DisplayName("Use Parent Data"), ReadOnly(false), Browsable(true), PropertyOrder(50), DescriptionAttribute("Use the value produced by the parent as the data for this trigger")]
        public bool UseParentData {
            get {
                bool v = GetBoolAttribute("useParentData");
                if (v) {
                    Hide("DataDataSource");
                    Hide("DataFile");
                    Hide("CSVFileHeaders");
                    Hide("ExcelSheet");
                    Hide("ExcelRowStart");
                    Hide("ExcelRowEnd");
                    Hide("XMLRestURL");
                    Hide("JSONRestURL");
                    Hide("RepeatData");
                    Hide("RepeatElement");
                    Hide("RepeatElementJSON");
                    Hide("Connection");
                    Hide("SQL");
                    Hide(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
                } else {
                    Show("DataDataSource");
                }
                return GetBoolAttribute("useParentData");
            }
            set {
                SetAttribute("useParentData", value);
                if (value) {
                    Hide("DataDataSource");
                    Hide("DataFile");
                    Hide("CSVFileHeaders");
                    Hide("ExcelSheet");
                    Hide("ExcelRowStart");
                    Hide("ExcelRowEnd");
                    Hide("XMLRestURL");
                    Hide("JSONRestURL");
                    Hide("RepeatData");
                    Hide("RepeatElement");
                    Hide("RepeatElementJSON");
                    Hide("Connection");
                    Hide("SQL");
                    Hide(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
                } else {
                    Show("DataDataSource");
                }
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        #endregion required

        #region Iteration Flight Data

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Iteration Flight Data"), DisplayName("Flight Type"), PropertyOrder(1), Browsable(true), DescriptionAttribute("Type of flights to use"), ItemsSource(typeof(FlightTypeListOut))]
        public string FlightType {
            get {
                switch (GetAttribute("flttype")) {
                    case "":
                    case "none":
                        Hide(new[] { "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
                        SetAttribute("flightSource", null);
                        return "None";

                    case "arr":
                        Show(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight" });
                        return "Arrivals";

                    case "dep":
                        Show(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight" });
                        return "Departures";

                    case "both":
                        Show(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight" });
                        return "Arrivals & Departures";

                    default:
                        Show(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight" });
                        return "Arrivals & Departures";
                }
            }
            set {
                SetFlightType(value);
                if (value == null || value == "None" || value == "none") {
                    XmlNode filter = _node.SelectSingleNode("./filter");
                    if (filter != null) {
                        _node.RemoveChild(filter);
                        view.UpdateParamBindings("XMLText");
                    }
                }
            }
        }

        [CategoryAttribute("Iteration Flight Data"), DisplayName("Earliest Offset for Flights (mins)"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The offset in minutes of the earliest scheduled time of the flight to use")]
        public int FlightSetFrom {
            get {
                int val = GetIntAttribute("flightSetFrom");
                //if (val == -1) {
                //    val = -180;
                //}
                return val;
            }
            set {
                SetAttribute("flightSetFrom", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("Iteration Flight Data"), DisplayName("Latest Offset for Flights (mins)"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The offset in minutes of the letest scheduled time of the flight to use")]
        public int FlightSetTo {
            get {
                int val = GetIntAttribute("flightSetTo");
                //if (val == -1) {
                //    val = 540;
                //}
                return val;
            }
            set {
                SetAttribute("flightSetTo", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("Iteration Flight Data"), DisplayName("Sequential Flight"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("Uses the flights in sequential order")]
        public bool Sequential {
            get => GetBoolAttribute("sequentialFlight");
            set => SetAttribute("sequentialFlight", value);
        }

        [CategoryAttribute("Iteration Flight Data"), DisplayName("Refresh Flight"), ReadOnly(false), Browsable(true), PropertyOrder(11), DescriptionAttribute("Refresh the flight from AMS at the time of use")]
        public bool RefreshFlight {
            get => GetBoolAttribute("refreshFlight");
            set => SetAttribute("refreshFlight", value);
        }

        [CategoryAttribute("Iteration Flight Data"), DisplayName("Stop at End of Flight Data"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("Stop the line when the limit of the iteration data is reached. Otherwise, repeat the data from the start")]
        public bool FlightRepeatData {
            get => GetBoolAttribute("stopOnFlightDataEnd");
            set => SetAttribute("stopOnFlightDataEnd", value);
        }

        #endregion Iteration Flight Data

        #region Iteration Data

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Iteration Data"), DisplayName("Data Source"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The source of data to be substituted into the template"), ItemsSource(typeof(DataSourceList))]
        public string DataDataSource {
            get {
                Hide("DataFile");
                Hide("CSVFileHeaders");
                Hide("ExcelSheet");
                Hide("ExcelRowStart");
                Hide("ExcelRowEnd");
                Hide("XMLRestURL");
                Hide("JSONRestURL");
                Hide("RepeatData");
                Hide("RepeatElement");
                Hide("RepeatElementJSON");
                Hide("Connection");
                Hide("SQL");
                if (!Parameters.SITAAMS) {
                    Hide(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
                } else {
                    Show("FlightType");
                }

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
                            Show("ExcelRowStart");
                            Show("ExcelRowEnd");
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

                        case "PULSAR":
                            Hide(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
                            return "Pulsar Only";
                    }
                    return "None";
                }
            }
            set {
                Hide("DataFile");
                Hide("CSVFileHeaders");
                Hide("ExcelSheet");
                Hide("ExcelRowStart");
                Hide("ExcelRowEnd");
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

                Show(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });

                switch (value) {
                    case "CSV File":
                        Show("DataFile");
                        Show("CSVFileHeaders");
                        SetAttribute("dataSource", "CSV");
                        break;

                    case "Excel File":
                        Show("DataFile");
                        Show("ExcelSheet");
                        Show("ExcelRowStart");
                        Show("ExcelRowEnd");
                        SetAttribute("dataSource", "Excel");
                        SetAttribute("excelRowStart", 1);
                        SetAttribute("excelRowEnd", 100);
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

                    case "Pulsar Only":
                        SetAttribute("dataSource", "PULSAR");
                        Hide(new[] { "FlightType", "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
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

        [CategoryAttribute("Iteration Data"), DisplayName("Starting Excel Row"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The row in the Excel sheet that the data starts on")]
        public string ExcelRowStart {
            get => GetAttribute("excelRowStart");
            set => SetAttribute("excelRowStart", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("Ending Excel Row"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The row in the Excel sheet that the data ends on")]
        public string ExcelRowEnd {
            get => GetAttribute("excelRowEnd");
            set => SetAttribute("excelRowEnd", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("XML Rest URL"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The URL to retrieve the XML document")]
        public string XMLRestURL {
            get => GetAttribute("dataRestURL");
            set => SetAttribute("dataRestURL", value);
        }

        [CategoryAttribute("Iteration Data"), DisplayName("JSON Rest URL"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The URL to retrieve the JSON document")]
        public string JSONRestURL {
            get => GetAttribute("dataRestURL");
            set => SetAttribute("dataRestURL", value);
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