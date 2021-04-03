using LoadInjector.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [DisplayName("Variable Defintion")]
    public class VariablePropertyGrid : LoadInjectorGridBase {

        public VariablePropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;

            if (_node.ParentNode.Name == "amsdirect") {
                Show("ExternalName");
                Hide("Token");
            } else {
                Hide("ExternalName");
                Show("Token");
            }

            if (!Parameters.SITAAMS) {
                Hide("Relative");
            }
        }

        private Visibility vis = Visibility.Collapsed;

        [Browsable(false)]
        public Visibility Visibility {
            get => vis;
            set {
                vis = value;
                view.UpdateParamBindings("Visibility");
                view.UpdateParamBindings("MyVariableGrid");
            }
        }

        [Browsable(false)]
        public List<ValueModel> Values {
            get {
                List<ValueModel> values = new List<ValueModel>();
                foreach (XmlNode v in _node.SelectNodes(".//value")) {
                    ValueModel val = new ValueModel(v, view);
                    values.Add(val);
                }
                return values;
            }
        }

        protected void DisplayParams() {
            Hide(new[] { "Visibility", "Format", "Seed", "NumPlaces", "FixedValue", "LowerOffset", "UpperOffset", "Relative", "Index", "JSONIndex", "Dir", "FileFilter", "FlightDataField", "Delta", "FlifoExternalName", "XPath", "ExcelColumn", "ExcelRowStart", "ExcelRowEnd", "ExcelDataType", "ExcelDataFormat", "ExcelUpdateFormulas", "XMLXPath", "NormalInt", "NormalDouble", "StdDev", "LowerLimit", "UpperLimit", "XMLToString" });

            switch (GetAttribute("type")) {
                case "sequence":
                    Show(new[] { "Seed", "NumPlaces" });
                    break;

                case "uuid":
                    break;

                case "intgaussian":
                    Show(new[] { "NormalInt", "StdDev" });
                    break;

                case "doublegaussian":
                    Show(new[] { "NormalDouble", "StdDev" });
                    break;

                case "fixed":
                    Show(new[] { "FixedValue" });
                    break;

                case "value":
                    break;

                case "valueSequence":
                    break;

                case "datetime":
                    if (Parameters.SITAAMS) {
                        TriggerTypeMap.SetTriggerMap(_node);
                        Tuple<string, string> t = TriggerTypeMap.GetDataAndFlightTypesForVariable(_node);
                        string flttype = t.Item2;
                        if (flttype != "none") {
                            Show(new[] { "Format", "UpperOffset", "LowerOffset", "Relative", "Delta" });
                        } else {
                            Show(new[] { "Format", "UpperOffset", "LowerOffset", "Relative", "Delta" });
                        }
                    } else {
                        Show(new[] { "Format", "UpperOffset", "LowerOffset" });
                    }
                    break;

                case "timestamp":
                    Show(new[] { "Format" });
                    break;

                case "csvfield":
                case "dbField":
                    Show(new[] { "Index" });
                    break;

                case "excelCol":
                    Show(new[] { "ExcelColumn", "ExcelRowStart", "ExcelRowEnd", "ExcelDataType", "ExcelDataFormat", "ExcelUpdateFormulas" });
                    break;

                case "excelTrigger":
                    Show(new[] { "ExcelColumn" });
                    break;

                case "xmlElement":
                    Show(new[] { "XMLXPath", "XMLToString" });
                    break;

                case "jsonElement":
                    Show(new[] { "JSONIndex" });
                    break;

                case "file":
                    Show(new[] { "Dir", "FileFilter" });
                    break;

                case "intRange":
                    Show(new[] { "LowerLimit", "UpperLimit" });
                    break;

                case "flightInfo":
                case "amsTrigger":
                    Show(new[] { "FlightDataField" });
                    break;
            }
        }

        protected string GetFLIFOField() {
            switch (GetAttribute("sub")) {
                case "airlineCode":
                    return "Airline Code - IATA";

                case "airlineCodeICAO":
                    return "Airline Code - ICAO";

                case "fltNumber":
                    return "Flight Number";

                case "arrivalAirport":
                    return "Arrival Airport - IATA";

                case "departureAirport":
                    return "Departure Airport - IATA";

                case "arrivalAirportICAO":
                    return "Arrival Airport - ICAO";

                case "departureAirportICAO":
                    return "Departure Airport - ICAO";

                case "schedDate":
                    return "Sched. Date of Operation";

                case "schedTime":
                    return "Sched. Time of Operation";

                case "kind":
                    return "Flight Kind (Arr/Dep)";

                case "csvfield":
                    return "CSV File Field";

                case "excelCol":
                    return "Excel Column";

                case "xmlElement":
                    return "XML Element";

                case "file":
                    return "File";

                case "flightInfo":
                    return "Flight ID Field";

                case "property":
                    return "Property";

                case "xpath":
                    return "XPath";

                case "intgaussian":
                    return "Random Integer (Gaussian)";

                case "doublegaussian":
                    return "Random Double (Gaussian)";
            }

            return null;
        }

        protected void SetFLIFOField(string value) {
            switch (value) {
                case "Airline Code - IATA":
                    SetAttribute("sub", "airlineCode");
                    break;

                case "Airline Code - ICAO":
                    SetAttribute("sub", "airlineCodeICAO");
                    break;

                case "Flight Number":
                    SetAttribute("sub", "fltNumber");
                    break;

                case "Arrival Airport - IATA":
                    SetAttribute("sub", "arrivalAirport");
                    break;

                case "Departure Airport - IATA":
                    SetAttribute("sub", "departureAirport");
                    break;

                case "Arrival Airport - ICAO":
                    SetAttribute("sub", "arrivalAirportICAO");
                    break;

                case "Departure Airport - ICAO":
                    SetAttribute("sub", "departureAirportICAO");
                    break;

                case "Sched. Date of Operation":
                    SetAttribute("sub", "schedDate");
                    break;

                case "Sched. Time of Operation":
                    SetAttribute("sub", "schedTime");
                    break;

                case "Flight Kind (Arr/Dep)":
                    SetAttribute("sub", "kind");
                    break;

                case "CSV File Field":
                    SetAttribute("sub", "csvfield");
                    break;

                case "Excel Column":
                    SetAttribute("sub", "excelCol");
                    break;

                case "XML Element":
                    SetAttribute("sub", "xmlElement");
                    break;

                case "File":
                    SetAttribute("sub", "file");
                    break;

                case "Property":
                    SetAttribute("sub", "property");
                    break;

                case "XPath":
                    SetAttribute("sub", "xpath");
                    break;

                case "Random Integer (Gaussian)":
                    SetAttribute("sub", "intgaussian");
                    break;

                case "Random Double (Gaussian)":
                    SetAttribute("sub", "doublegaussian");
                    break;
            }

            view.UpdateParamBindings("XMLText");
        }

        protected string GetVariableType() {
            switch (GetAttribute("type")) {
                case "sequence":
                    return "Sequence Number";

                case "uuid":
                    return "Unique Identifier";

                case "fixed":
                    return "Fixed Value";

                case "value":
                    return "Value From List (random)";

                case "valueSequence":
                    return "Value From List (sequential)";

                case "datetime":
                    return "Date Time";

                case "timestamp":
                    return "Timestamp";

                case "csvfield":
                    return "CSV File Field";

                case "excelCol":
                    return "Excel Column";

                case "xmlElement":
                    return "XML Element";

                case "jsonElement":
                    return "JSON Element";

                case "file":
                    return "File";

                case "flightInfo":
                    return "Flight ID Field";

                case "intgaussian":
                    return "Random Integer (Gaussian)";

                case "doublegaussian":
                    return "Random Double (Gaussian)";

                case "intRange":
                    return "Integer Range";

                case "dbField":
                    return "Database Field";
            }

            return null;
        }

        protected void SetVariable(string value) {
            string tok = Token;
            string ext = ExternalName;

            _node.Attributes.RemoveAll();

            if (value != "Value From List (random)" && value != "Value From List (sequential)") {
                _node.RemoveAll();
            } else {
                if (_node.ChildNodes.Count == 0) {
                    XmlNode newNode = view.GetViewModel().CreateElement("value");
                    newNode.InnerText = "- item value 1 -";
                    view.GetSelectedElement().DataModel.AppendChild(newNode);

                    XmlNode newNode2 = view.GetViewModel().CreateElement("value");
                    newNode2.InnerText = "- item value 2 -";
                    view.GetSelectedElement().DataModel.AppendChild(newNode2);
                }
            }

            SetAttribute("token", tok);
            SetAttribute("externalName", ext);

            switch (value) {
                case "Sequence Number":
                    SetAttribute("type", "sequence");
                    break;

                case "Unique Identifier":
                    SetAttribute("type", "uuid");
                    break;

                case "Fixed Value":
                    SetAttribute("type", "fixed");
                    break;

                case "Value From List (random)":
                    SetAttribute("type", "value");
                    break;

                case "Value From List (sequential)":
                    SetAttribute("type", "valueSequence");
                    break;

                case "Date Time":
                    SetAttribute("type", "datetime");
                    break;

                case "Timestamp":
                    SetAttribute("type", "timestamp");
                    break;

                case "File":
                    SetAttribute("type", "file");
                    break;

                case "CSV File Field":
                    SetAttribute("type", "csvfield");
                    break;

                case "Excel Column":
                    SetAttribute("type", "excelCol");
                    break;

                case "XML Element":
                    SetAttribute("type", "xmlElement");
                    break;

                case "JSON Element":
                    SetAttribute("type", "jsonElement");
                    break;

                case "Flight ID Field":
                    SetAttribute("type", "flightInfo");
                    break;

                case "Random Integer (Gaussian)":
                    SetAttribute("type", "intgaussian");
                    break;

                case "Random Double (Gaussian)":
                    SetAttribute("type", "doublegaussian");
                    break;

                case "Integer Range":
                    SetAttribute("type", "intRange");
                    break;

                case "CSV File Field from Trigger":
                    SetAttribute("type", "csvField");
                    break;

                case "Database Field from Trigger":
                    SetAttribute("type", "dbTrigger");
                    break;

                case "AMS Flight ID Field from Trigger":
                    SetAttribute("type", "amsTrigger");
                    break;

                case "Excel Column from Trigger":
                    SetAttribute("type", "excelTrigger");
                    break;

                case "XML Element from Trigger":
                    SetAttribute("type", "xmlTrigger");
                    break;

                case "JSON Element from Trigger":
                    SetAttribute("type", "jsonTrigger");
                    break;

                case "Database Field":
                    SetAttribute("type", "dbField");
                    break;
            }
            view.UpdateParamBindings("MyVariableGrid");
            view.UpdateParamBindings("XMLText");
        }

        [CategoryAttribute("Required"), DisplayName("Token"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The token in the template file that will be substituted by this variable")]
        public string Token {
            get => GetAttribute("token");
            set => SetAttribute("token", value);
        }

        [CategoryAttribute("Required"), DisplayName("AMS External Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The AMS ExternalName of the field to be update with by this variable")]
        public string ExternalName {
            get => GetAttribute("externalName");
            set => SetAttribute("externalName", value);
        }

        #region variable type

        [RefreshProperties(RefreshProperties.All)]
        [ItemsSource(typeof(VariableTypesExperimental))]
        [CategoryAttribute("Required"), DisplayName("Variable Type"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The type of variable")]
        public string Experiment {
            get {
                DisplayParams();
                string value = GetVariableType();
                if (value == "Value From List (random)" || value == "Value From List (sequential)") {
                    Visibility = Visibility.Visible;
                } else {
                    Visibility = Visibility.Collapsed;
                }
                return value;
            }
            set {
                SetVariable(value);
                DisplayParams();
                if (value == "value" || value == "valueSequence") {
                    Visibility = Visibility.Visible;
                } else {
                    Visibility = Visibility.Collapsed;
                }
            }
        }

        #endregion variable type

        #region Params

        [CategoryAttribute("Optional"), DisplayName("Delta"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("A Delta time in minutes to be applied to calculated date time")]
        public string Delta {
            get => GetAttribute("delta");
            set => SetAttribute("delta", value);
        }

        [CategoryAttribute("Optional"), DisplayName("Date Format"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The DateTime format string of the format to be outputted")]
        public string Format {
            get => GetAttribute("format");
            set => SetAttribute("format", value);
        }

        [CategoryAttribute("Required"), DisplayName("Fixed Value"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The fixed value that will be substituted in each time")]
        public string FixedValue {
            get => GetAttribute("fixedValue");
            set => SetAttribute("fixedValue", value);
        }

        [CategoryAttribute("Required"), DisplayName("Sequence Seed"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The first value of the sequence")]
        public int Seed {
            get => GetIntAttribute("seed");
            set => SetAttribute("seed", value);
        }

        [CategoryAttribute("Required"), DisplayName("Total Digits"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The total number of digits to include in sequence number. Leading zeros will be added. Non if set to -1")]
        public int NumPlaces {
            get => GetIntAttribute("digits");
            set => SetAttribute("digits", value);
        }

        [CategoryAttribute("Required"), DisplayName("Lower Time Offset (min)"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The lower offset in miuntes of the earliest time to use")]
        public int LowerOffset {
            get => GetIntAttribute("lowerOffset");
            set => SetAttribute("lowerOffset", value);
        }

        [CategoryAttribute("Required"), DisplayName("Upper Time Offset(min)"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The upper offset in miuntes of the earliest time to use")]
        public int UpperOffset {
            get => GetIntAttribute("upperOffset");
            set => SetAttribute("upperOffset", value);
        }

        [CategoryAttribute("Required"), DisplayName("Offset is relative to Sched. Time"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("Normally the offset is calculated from NOW, but if set, then the time will be relative the the scheduled time of the associated flight")]
        public bool Relative {
            get => GetBoolDefaultFalseAttribute(_node, "relative");
            set => SetAttribute("relative", value);
        }

        [Editor(typeof(FolderNameSelector), typeof(FolderNameSelector))]
        [CategoryAttribute("Required"), DisplayName("Directoy"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The full pathname of the directory containing the files whose content will be substituted in for this variable")]
        public string Dir {
            get => GetAttribute("srcDir");
            set => SetAttribute("srcDir", value);
        }

        [CategoryAttribute("Optional"), DisplayName("File Filter"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The file filter to apply to the files to be used")]
        public string FileFilter {
            get => GetAttribute("fileFilter");
            set => SetAttribute("fileFilter", value);
        }

        [CategoryAttribute("Required"), DisplayName("Excel Column"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The column in the Excel sheet to read the data from, e.g. A, B, AA, BF etc")]
        public string ExcelColumn {
            get => GetAttribute("excelCol");
            set => SetAttribute("excelCol", value);
        }

        //[CategoryAttribute("Required"), DisplayName("Starting Excel Row"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The row in the Excel sheet that the data starts on")]
        //public string ExcelRowStart {
        //    get { return GetAttribute("excelRowStart"); }
        //    set { SetAttribute("excelRowStart", value); }
        //}

        //[CategoryAttribute("Required"), DisplayName("Ending Excel Row"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The row in the Excel sheet that the data ends on")]
        //public string ExcelRowEnd {
        //    get { return GetAttribute("excelRowEnd"); }
        //    set { SetAttribute("excelRowEnd", value); }
        //}

        //[CategoryAttribute("Required"), DisplayName("Excel Data Type"), ReadOnly(false), Browsable(true), PropertyOrder(8), DescriptionAttribute("Excel Data Type"), ItemsSource(typeof(VariableExcelDataType))]
        //public string ExcelDataType {
        //    get { return GetAttribute("excelDataType"); }
        //    set { SetAttribute("excelDataType", value); }
        //}
        //[CategoryAttribute("Required"), DisplayName("Format"), ReadOnly(false), Browsable(true), PropertyOrder(9), DescriptionAttribute("C# format string")]
        //public string ExcelDataFormat {
        //    get { return GetAttribute("excelDataFormat"); }
        //    set { SetAttribute("excelDataFormat", value); }
        //}
        [CategoryAttribute("Required"), DisplayName("Update Time Formulas"), ReadOnly(false), Browsable(true), PropertyOrder(10), DescriptionAttribute("Update cells with time based formulas such as NOW() or TODAY()")]
        public bool ExcelUpdateFormulas {
            get => GetBoolAttribute("excelUpdateFormulas");
            set => SetAttribute("excelUpdateFormulas", value);
        }

        [CategoryAttribute("Required"), DisplayName("XML Element XPath"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The XPATH to the element")]
        public string XMLXPath {
            get => GetAttribute("xmlXPath");
            set => SetAttribute("xmlXPath", value);
        }

        [CategoryAttribute("Required"), DisplayName("XML Element To String"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The XML Element is converted to a string")]
        public bool XMLToString {
            get => GetBoolAttribute("xmlToString");
            set => SetAttribute("xmlToString", value);
        }

        [CategoryAttribute("Required"), DisplayName("Field Index"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The zero based index of the field in the data to use for substitution")]
        public int Index {
            get {
                int val = GetIntAttribute("field");
                if (val < 0) {
                    SetAttribute("field", 0);
                    return 0;
                }
                return val;
            }
            set {
                if (value < 0) {
                    value = 0;
                }
                SetAttribute("field", value);
            }
        }

        [CategoryAttribute("Required"), DisplayName("JSON Element (JSONPath)"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The JSONPath relative to the repeating element for the variable")]
        public string JSONIndex {
            get => GetAttribute("field");
            set => SetAttribute("field", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Flight ID Field"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The particular Flight ID element of the associated flight to use for this variable"), ItemsSource(typeof(FlifoTypes))]
        public string FlightDataField {
            get {
                string value = GetFLIFOField();
                if (value == "Property") {
                    Show(new[] { "FlifoExternalName" });
                }
                if (value == "XPath") {
                    Show(new[] { "XPath" });
                }
                return value;
            }
            set {
                Hide(new[] { "FlifoExternalName", "XPath" });
                SetFLIFOField(value);
                if (value == "Property") {
                    Show(new[] { "FlifoExternalName" });
                } else if (value == "XPath") {
                    Show(new[] { "XPath" });
                } else {
                    SetAttribute("externalName", null);
                }
            }
        }

        [CategoryAttribute("Required"), DisplayName("Property External Name"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The AMS External Name of the flight recoed value to substitute")]
        public string FlifoExternalName {
            get => GetAttribute("externalName");
            set => SetAttribute("externalName", value);
        }

        [CategoryAttribute("Required"), DisplayName("Flight Element XPath"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The XPath in the Flight Element of the required element")]
        public string XPath {
            get => GetAttribute("xpath");
            set => SetAttribute("xpath", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Optional - Variable Lookup"), DisplayName("Use variable as a lookup key"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The AMS External Name of the flight recoed value to substitute")]
        public bool LookupVariable {
            get {
                bool v = GetBoolAttribute("variableLookup");
                if (!v) {
                    Hide(new[] { "LookupDataSource", "DataFile", "ExcelSheet", "ExcelKeyColumn", "ExcelValueColumn", "CSVKeyField", "CSVValueField" });
                } else {
                    Show(new[] { "LookupDataSource" });
                }
                return v;
            }
            set {
                SetAttribute("variableLookup", value);
                if (!value) {
                    Hide(new[] { "LookupDataSource", "DataFile", "ExcelSheet", "ExcelKeyColumn", "ExcelValueColumn", "CSVKeyField", "CSVValueField" });
                    SetAttribute("lookupSource", null);
                    SetAttribute("dataFile", null);
                    SetAttribute("excelLookupSheet", null);
                    SetAttribute("excelKeyColumn", null);
                    SetAttribute("excelValueColumn", null);
                    SetAttribute("csvKeyField", null);
                    SetAttribute("csvValueField", null);
                } else {
                    Show(new[] { "LookupDataSource" });
                }
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Optional - Variable Lookup"), DisplayName("Lookup Source"), PropertyOrder(2), Browsable(true), DescriptionAttribute("The source of fligts to use for template data"), ItemsSource(typeof(VariableLookupTypes))]
        public string LookupDataSource {
            get {
                string v = GetAttribute("lookupSource");
                if (v == null) {
                    return null;
                }
                if (v == "csv") {
                    Show(new[] { "DataFile", "CSVKeyField", "CSVValueField" });
                    Hide(new[] { "ExcelSheet", "ExcelKeyColumn", "ExcelValueColumn" });
                    SetAttribute("excelLookupSheet", null);
                    SetAttribute("excelKeyColumn", null);
                    SetAttribute("excelValueColumn", null);
                    return "CSV File";
                } else if (v == "excel") {
                    Show(new[] { "DataFile", "ExcelSheet", "ExcelKeyColumn", "ExcelValueColumn" });
                    Hide(new[] { "CSVKeyField", "CSVValueField" });
                    SetAttribute("csvKeyField", null);
                    SetAttribute("csvValueField", null);
                    return "Excel File";
                }
                return null;
            }
            set {
                if (value == "CSV File") {
                    SetAttribute("lookupSource", "csv");
                    Show(new[] { "DataFile", "CSVKeyField", "CSVValueField" });
                    Hide(new[] { "ExcelSheet", "ExcelKeyColumn", "ExcelValueColumn" });
                    SetAttribute("dataFile", null);
                    SetAttribute("excelLookupSheet", null);
                    SetAttribute("excelKeyColumn", null);
                    SetAttribute("excelValueColumn", null);
                } else if (value == "Excel File") {
                    SetAttribute("lookupSource", "excel");
                    Show(new[] { "DataFile", "ExcelSheet", "ExcelKeyColumn", "ExcelValueColumn" });
                    Hide(new[] { "CSVKeyField", "CSVValueField" });
                    SetAttribute("dataFile", null);
                    SetAttribute("csvKeyField", null);
                    SetAttribute("csvValueField", null);
                }
            }
        }

        [Editor(typeof(FileNameSelector), typeof(FileNameSelector))]
        [CategoryAttribute("Optional - Variable Lookup"), DisplayName("Lookup File"), ReadOnly(false), Browsable(true), PropertyOrder(11), DescriptionAttribute("Full pathname of the lookup file")]
        public string DataFile {
            get => GetAttribute("dataFile");
            set => SetAttribute("dataFile", value);
        }

        [CategoryAttribute("Optional - Variable Lookup"), DisplayName("Excel Sheet"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The Excel sheet that the lookup data is on")]
        public string ExcelSheet {
            get => GetAttribute("excelLookupSheet");
            set => SetAttribute("excelLookupSheet", value);
        }

        [CategoryAttribute("Optional - Variable Lookup"), DisplayName("Excel Key Column"), ReadOnly(false), Browsable(true), PropertyOrder(13), DescriptionAttribute("The column in the Excel sheet that is the Key for the lookup")]
        public string ExcelKeyColumn {
            get => GetAttribute("excelKeyColumn");
            set => SetAttribute("excelKeyColumn", value);
        }

        [CategoryAttribute("Optional - Variable Lookup"), DisplayName("Excel Value Column"), ReadOnly(false), Browsable(true), PropertyOrder(14), DescriptionAttribute("The column in the Excel sheet that is the Value of the lookup")]
        public string ExcelValueColumn {
            get => GetAttribute("excelValueColumn");
            set => SetAttribute("excelValueColumn", value);
        }

        [CategoryAttribute("Optional - Variable Lookup"), DisplayName("CSV Key Field (0 index based)"), ReadOnly(false), Browsable(true), PropertyOrder(15), DescriptionAttribute("The Key field in the CSV record for the lookup")]
        public string CSVKeyField {
            get => GetAttribute("csvKeyField");
            set => SetAttribute("csvKeyField", value);
        }

        [CategoryAttribute("Optional - Variable Lookup"), DisplayName("CSV Value Field (0 index based)"), ReadOnly(false), Browsable(true), PropertyOrder(16), DescriptionAttribute("The Value field in the CSV record for the lookup")]
        public string CSVValueField {
            get => GetAttribute("csvValueField");
            set => SetAttribute("csvValueField", value);
        }

        [CategoryAttribute("Required"), DisplayName("Mean"), ReadOnly(false), Browsable(true), PropertyOrder(17), DescriptionAttribute("The mean value")]
        public string NormalInt {
            get => GetAttribute("normalInt");
            set => SetAttribute("normalInt", value);
        }

        [CategoryAttribute("Required"), DisplayName("Mean"), ReadOnly(false), Browsable(true), PropertyOrder(18), DescriptionAttribute("The Mean value")]
        public string NormalDouble {
            get => GetAttribute("normalDouble");
            set => SetAttribute("normalDouble", value);
        }

        [CategoryAttribute("Required"), DisplayName("Standard Deviation"), ReadOnly(false), Browsable(true), PropertyOrder(19), DescriptionAttribute("The Standard Variation")]
        public double StdDev {
            get => GetDoubleAttribute("stdDev");
            set => SetAttribute("stdDev", value);
        }

        [CategoryAttribute("Required"), DisplayName("Lower Limit"), ReadOnly(false), Browsable(true), PropertyOrder(20), DescriptionAttribute("The lower limit of the random variable")]
        public string LowerLimit {
            get => GetAttribute("lowerLimit");
            set => SetAttribute("lowerLimit", value);
        }

        [CategoryAttribute("Required"), DisplayName("Upper Limit"), ReadOnly(false), Browsable(true), PropertyOrder(21), DescriptionAttribute("The upper limit of tghe random variable")]
        public string UpperLimit {
            get => GetAttribute("upperLimit");
            set => SetAttribute("upperLimit", value);
        }

        #endregion Params
    }

    public class ValueModel {
        private readonly string value;
        private readonly XmlNode node;
        private readonly IView view;

        public ValueModel(string v) {
            Value = v;
        }

        public ValueModel(XmlNode v, IView view) {
            node = v;
            value = v.InnerText;
            this.view = view;
        }

        public string Value {
            get => value;
            set {
                node.InnerText = value;
                view.UpdateParamBindings("XMLText");
                view.UpdateParamBindings("MyVariableGrid");
                view.UpdateParamBindings("Values");
            }
        }
    }
}