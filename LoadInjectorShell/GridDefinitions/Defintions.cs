using LoadInjector.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Xml;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace LoadInjector.Common {

    public class FileOrUrlList : IItemsSource {

        public ItemCollection GetValues() {
            return new ItemCollection {
                "File", "URL"
            };
        }
    }

    public class DataBaseList : IItemsSource {

        public ItemCollection GetValues() {
            return new ItemCollection {
                "MS SQL", "MySQL","Oracle"
            };
        }
    }

    public class VariableLookupTypes : IItemsSource {

        public ItemCollection GetValues() {
            return new ItemCollection {
                "CSV File", "Excel File"
            };
        }
    }

    public class MqttServerTypeList : IItemsSource {

        public ItemCollection GetValues() {
            return new ItemCollection {
                "Web Services Server", "TCP Server"
            };
        }
    }

    public class DataSourceList : IItemsSource {

        public ItemCollection GetValues() {
            return new ItemCollection {
                   "None", "Pulsar Only", "CSV File", "Excel File", "XML File", "XML File via Rest", "JSON File", "JSON File via Rest", "MS SQL Database", "MySQL Database", "Oracle Database"
            };
        }
    }

    public class FlightTypeListOut : IItemsSource {

        public ItemCollection GetValues() {
            return new ItemCollection {
                "None", "Arrivals", "Departures","Arrivals & Departures"
            };
        }
    }

    public class FlightTypeListNoNoneOut : IItemsSource {

        public ItemCollection GetValues() {
            return new ItemCollection {
                "Arrivals", "Departures","Arrivals & Departures"
            };
        }
    }

    public class LogLevelList : IItemsSource {

        public ItemCollection GetValues() {
            return new ItemCollection {
                "Info", "Trace"
            };
        }
    }

    public class VariableTypesExperimental : IItemsSource {

        // Hacky version that uses a static variable to work out the current configuration so the correct options are displayed
        public ItemCollection GetValues() {
            SelectedElementViewModel node = TreeEditorViewModel.staticSelectedElement;

            var types = new ItemCollection();

            TriggerTypeMap.SetTriggerMap(node.DataModel);
            Tuple<string, string> t = TriggerTypeMap.GetDataAndFlightTypesForVariable(node.DataModel);

            string dataSource = t.Item1;
            string flttype = t.Item2;

            if (dataSource == "CSV") {
                types.Add("CSV File Field");
            }
            if (dataSource == "XML" || dataSource == "RESTXML") {
                types.Add("XML Element");
            }
            if (dataSource == "Excel") {
                types.Add("Excel Column");
            }
            if (dataSource == "JSON" || dataSource == "RESTJSON") {
                types.Add("JSON Element");
            }
            if (dataSource == "MSSQL" || dataSource == "MYSQL" || dataSource == "ORACLE") {
                types.Add("Database Field");
            }
            if (flttype != "none") {
                types.Add("Flight ID Field");
            }

            types.Add("Sequence Number");
            types.Add("Unique Identifier");
            types.Add("Fixed Value");
            types.Add("Value From List (random)");
            types.Add("Value From List (sequential)");
            types.Add("Date Time");
            types.Add("Timestamp");
            types.Add("File");
            types.Add("Random Integer (Gaussian)");
            types.Add("Random Double (Gaussian)");
            types.Add("Integer Range");

            return types;
        }
    }

    public class ProtocolTypes : IItemsSource {

        public ItemCollection GetValues() {
            var types = new ItemCollection();

            List<string> l = new List<string>();
            foreach (string s in Parameters.protocolDescriptionDictionary.Keys) {
                l.Add(s);
            }
            l.Sort();
            foreach (string s in l) {
                types.Add(s);
            }

            return types;
        }
    }

    public class FlifoTypes : IItemsSource {

        public ItemCollection GetValues() {
            return new ItemCollection {
                "Airline Code - IATA","Airline Code - ICAO", "Flight Number", "Arrival Airport - IATA","Arrival Airport - ICAO","Departure Airport - IATA","Departure Airport - ICAO", "Sched. Date of Operation", "Sched. Time of Operation", "Flight Kind (Arr/Dep)", "Property", "XPath"
            };
        }
    }

    public class TimePickerEditor : TimePicker, ITypeEditor {

        public TimePickerEditor() {
            Format = DateTimeFormat.Custom;
            FormatString = "HH:mm:ss";
            DefaultValue = null;
            DisplayDefaultValueOnEmptyText = true;
            ShowButtonSpinner = true;
            AllowSpin = true;
        }

        public FrameworkElement ResolveEditor(PropertyItem propertyItem) {
            Binding binding = new Binding("Value") {
                Source = propertyItem,
                Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
            };

            BindingOperations.SetBinding(this, ValueProperty, binding);
            return this;
        }
    }

    [CategoryOrder("Line Configuration", 1)]
    [CategoryOrder("Source Configuration", 1)]
    [CategoryOrder("Destination Configuration", 2)]
    [CategoryOrder("SMTP - Optional", 3)]
    [CategoryOrder("Iteration Flight Data", 4)]
    [CategoryOrder("Iteration Data", 5)]
    [CategoryOrder("Message Send Profile", 6)]
    [CategoryOrder("Required", 1)]
    [CategoryOrder("Optional - Variable Lookup", 2)]
    [CategoryOrder("AMS Connection", 2)]
    [CategoryOrder("Test Execution", 1)]
    [CategoryOrder("Execution", 10)]
    [RefreshProperties(RefreshProperties.All)]
    public class LoadInjectorGridBase {
        public string type = "MSMQ";
        public XmlNode _node;
        public IView view;

        protected void Show(string[] fields) {
            foreach (string field in fields) {
                ShowHide(field, true);
            }
        }

        protected void Show(string field) {
            ShowHide(field, true);
        }

        protected void Hide(string[] fields) {
            foreach (string field in fields) {
                ShowHide(field, false);
            }
        }

        protected void Hide(string field) {
            ShowHide(field, false);
        }

        protected void ShowHide(string field, bool value) {
            try {
                PropertyDescriptor descriptor = TypeDescriptor.GetProperties(GetType())[field];
                BrowsableAttribute theDescriptorBrowsableAttribute = (BrowsableAttribute)descriptor?.Attributes[typeof(BrowsableAttribute)];
                if (theDescriptorBrowsableAttribute == null) {
                    return;
                }
                FieldInfo isBrowsable = theDescriptorBrowsableAttribute.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);
                isBrowsable.SetValue(theDescriptorBrowsableAttribute, value);
            } catch (Exception ex) {
                Debug.WriteLine($"Show/Hide Issue for {field}. {ex.Message}");
            }

            if (!Parameters.SITAAMS) {
                try {
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(GetType())["FlightType"];
                    BrowsableAttribute theDescriptorBrowsableAttribute = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
                    FieldInfo isBrowsable = theDescriptorBrowsableAttribute.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);
                    isBrowsable.SetValue(theDescriptorBrowsableAttribute, false);
                } catch (Exception) {
                    // NO-OP
                }
                try {
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(GetType())["FlightDataSource"];
                    BrowsableAttribute theDescriptorBrowsableAttribute = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
                    FieldInfo isBrowsable = theDescriptorBrowsableAttribute.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);
                    isBrowsable.SetValue(theDescriptorBrowsableAttribute, false);
                } catch (Exception) {
                    // NO-OP
                }
                try {
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(GetType())["FlightSetFrom"];
                    BrowsableAttribute theDescriptorBrowsableAttribute = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
                    FieldInfo isBrowsable = theDescriptorBrowsableAttribute.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);
                    isBrowsable.SetValue(theDescriptorBrowsableAttribute, false);
                } catch (Exception) {
                    // NO-OP
                }
                try {
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(GetType())["FlightSetTo"];
                    BrowsableAttribute theDescriptorBrowsableAttribute = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
                    FieldInfo isBrowsable = theDescriptorBrowsableAttribute.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);
                    isBrowsable.SetValue(theDescriptorBrowsableAttribute, false);
                } catch (Exception) {
                    // NO-OP
                }
                try {
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(GetType())["Sequential"];
                    BrowsableAttribute theDescriptorBrowsableAttribute = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
                    FieldInfo isBrowsable = theDescriptorBrowsableAttribute.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);
                    isBrowsable.SetValue(theDescriptorBrowsableAttribute, false);
                } catch (Exception) {
                    // NO-OP
                }
                try {
                    PropertyDescriptor descriptor = TypeDescriptor.GetProperties(GetType())["RefreshFlight"];
                    BrowsableAttribute theDescriptorBrowsableAttribute = (BrowsableAttribute)descriptor.Attributes[typeof(BrowsableAttribute)];
                    FieldInfo isBrowsable = theDescriptorBrowsableAttribute.GetType().GetField("Browsable", BindingFlags.IgnoreCase | BindingFlags.NonPublic | BindingFlags.Instance);
                    isBrowsable.SetValue(theDescriptorBrowsableAttribute, false);
                } catch (Exception) {
                    // NO-OP
                }
            }
        }

        protected string GetAttribute(string attribName) {
            if (_node.Attributes[attribName] != null) {
                return _node.Attributes[attribName].Value;
            } else {
                return "";
            }
        }

        protected bool GetBoolAttribute(string attribName) {
            if (_node.Attributes[attribName] != null) {
                return bool.Parse(_node.Attributes[attribName].Value);
            } else {
                return false;
            }
        }

        protected bool GetBoolDefaultTrueAttribute(string attribName) {
            if (_node.Attributes[attribName] != null) {
                return bool.Parse(_node.Attributes[attribName].Value);
            } else {
                if (_node.Attributes[attribName] == null) {
                    XmlAttribute newAttribute = _node.OwnerDocument.CreateAttribute(attribName);
                    newAttribute.Value = "True";
                    _node.Attributes.Append(newAttribute);
                } else {
                    _node.Attributes[attribName].Value = "True";
                }
                view.UpdateParamBindings("XMLText");
                return true;
            }
        }

        protected bool GetBoolDefaultFalseAttribute(XmlNode _node, string attribName) {
            if (_node.Attributes[attribName] != null) {
                return bool.Parse(_node.Attributes[attribName].Value);
            } else {
                if (_node.Attributes[attribName] == null) {
                    XmlAttribute newAttribute = _node.OwnerDocument.CreateAttribute(attribName);
                    newAttribute.Value = "False";
                    _node.Attributes.Append(newAttribute);
                } else {
                    _node.Attributes[attribName].Value = "False";
                }
                view.UpdateParamBindings("XMLText");
                return false;
            }
        }

        protected int GetIntDefaultZeroAttribute(string attribName) {
            if (_node.Attributes[attribName] != null) {
                int value;
                try {
                    value = int.Parse(_node.Attributes[attribName].Value);
                } catch (Exception) {
                    value = 0;
                }

                return value;
            } else {
                return 0;
            }
        }

        protected double GetDoubleAttribute(string attribName) {
            if (_node.Attributes[attribName] != null) {
                double value;
                try {
                    value = double.Parse(_node.Attributes[attribName].Value);
                } catch (Exception) {
                    value = -1.0;
                }

                return value;
            } else {
                return -1.0;
            }
        }

        protected double GetDoubleAttributeDefaultZero(string attribName) {
            if (_node.Attributes[attribName] != null) {
                double value;
                try {
                    value = double.Parse(_node.Attributes[attribName].Value);
                } catch (Exception) {
                    value = 0.0;
                }

                return value;
            } else {
                return 0.0;
            }
        }

        protected double GetDoubleAttributeZeroDefault(string attribName) {
            if (_node.Attributes[attribName] != null) {
                double value;
                try {
                    value = double.Parse(_node.Attributes[attribName].Value);
                } catch (Exception) {
                    value = 0.0;
                }

                return value;
            } else {
                return 0.0;
            }
        }

        protected int GetIntAttribute(string attribName) {
            if (_node.Attributes[attribName] != null) {
                int value;
                try {
                    value = int.Parse(_node.Attributes[attribName].Value);
                } catch (Exception) {
                    value = -1;
                }

                return value;
            } else {
                return -1;
            }
        }

        protected int GetIntAttribute(string attribName, int def) {
            if (_node.Attributes[attribName] != null) {
                return int.Parse(_node.Attributes[attribName].Value);
            } else {
                return def;
            }
        }

        protected void ClearAttributes() {
            _node.Attributes.RemoveAll();
            view.UpdateParamBindings("XMLText");
        }

        protected void SetAttribute(string attribName, string value) {
            if (string.IsNullOrEmpty(value)) {
                try {
                    _node.Attributes.Remove(_node.Attributes[attribName]);
                } catch {
                    // NO-OP
                }
            } else {
                if (_node.Attributes[attribName] == null) {
                    XmlAttribute newAttribute = _node.OwnerDocument.CreateAttribute(attribName);
                    newAttribute.Value = value;
                    _node.Attributes.Append(newAttribute);
                } else {
                    _node.Attributes[attribName].Value = value;
                }
            }

            view?.UpdateParamBindings("XMLText");
        }

        protected void SetAttribute(string attribName, string value, XmlNode node) {
            if (string.IsNullOrEmpty(value)) {
                try {
                    node.Attributes.Remove(_node.Attributes[attribName]);
                } catch {
                    // NO-OP
                }
            } else {
                if (node.Attributes[attribName] == null) {
                    XmlAttribute newAttribute = node.OwnerDocument.CreateAttribute(attribName);
                    newAttribute.Value = value;
                    node.Attributes.Append(newAttribute);
                } else {
                    node.Attributes[attribName].Value = value;
                }
            }

            view?.UpdateParamBindings("XMLText");
        }

        protected void SetAttribute(string attribName, double value) {
            if ((value == -1) && _node.Attributes[attribName] != null) {
                _node.Attributes.Remove(_node.Attributes[attribName]);
            } else {
                if (_node.Attributes[attribName] == null) {
                    XmlAttribute newAttribute = _node.OwnerDocument.CreateAttribute(attribName);
                    newAttribute.Value = value.ToString(CultureInfo.CurrentCulture);
                    _node.Attributes.Append(newAttribute);
                } else {
                    _node.Attributes[attribName].Value = value.ToString(CultureInfo.CurrentCulture);
                }
            }

            view.UpdateParamBindings("XMLText");
        }

        protected void SetAbsoluteAttribute(string attribName, bool value) {
            if (_node.Attributes[attribName] != null) {
                _node.Attributes[attribName].Value = value.ToString();
            } else {
                if (_node.Attributes[attribName] == null) {
                    XmlAttribute newAttribute = _node.OwnerDocument.CreateAttribute(attribName);
                    newAttribute.Value = value.ToString();
                    _node.Attributes.Append(newAttribute);
                } else {
                    _node.Attributes[attribName].Value = value.ToString();
                }
            }

            view.UpdateParamBindings("XMLText");
        }

        protected void SetAttribute(string attribName, bool value) {
            if ((!value) && _node.Attributes[attribName] != null) {
                _node.Attributes.Remove(_node.Attributes[attribName]);
            } else {
                if (_node.Attributes[attribName] == null) {
                    XmlAttribute newAttribute = _node.OwnerDocument.CreateAttribute(attribName);
                    newAttribute.Value = value.ToString();
                    _node.Attributes.Append(newAttribute);
                } else {
                    _node.Attributes[attribName].Value = value.ToString();
                }
            }

            view.UpdateParamBindings("XMLText");
        }

        protected void SetAttributeAbs(string attribName, bool value) {
            if (_node.Attributes[attribName] != null) {
                _node.Attributes[attribName].Value = value.ToString();
            } else {
                if (_node.Attributes[attribName] == null) {
                    XmlAttribute newAttribute = _node.OwnerDocument.CreateAttribute(attribName);
                    newAttribute.Value = value.ToString();
                    _node.Attributes.Append(newAttribute);
                } else {
                    _node.Attributes[attribName].Value = value.ToString();
                }
            }

            view.UpdateParamBindings("XMLText");
        }

        protected void SetAttribute(string attribName, int value) {
            if ((value == -1) && _node.Attributes[attribName] != null) {
                _node.Attributes.Remove(_node.Attributes[attribName]);
            } else {
                if (_node.Attributes[attribName] == null) {
                    XmlAttribute newAttribute = _node.OwnerDocument.CreateAttribute(attribName);
                    newAttribute.Value = value.ToString();
                    _node.Attributes.Append(newAttribute);
                } else {
                    _node.Attributes[attribName].Value = value.ToString();
                }
            }

            view?.UpdateParamBindings("XMLText");
        }

        protected XmlNode GetNode(string name) {
            foreach (XmlNode node in _node.ChildNodes) {
                if (node.Name == name) {
                    return node;
                }
            }

            return null;
        }

        protected string GetFlightType() {
            switch (GetAttribute("flttype")) {
                case "":
                case "none":
                    try {
                        SetAttribute("flightSetFrom", null);
                        SetAttribute("flightSetTo", null);
                        SetAttribute("sequentialFlight", null);
                        SetAttribute("refreshFlight", false);
                        Hide(new[] { "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight" });
                        SetAttribute("flightSource", null);
                    } catch (Exception) {
                        // NO-OP
                    }
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

        protected void SetFlightType(string value) {
            switch (value) {
                case "None":
                    SetAttribute("flttype", "none");
                    try {
                        SetAttribute("flightSetFrom", null);
                        SetAttribute("flightSetTo", null);
                        SetAttribute("sequentialFlight", null);
                        SetAttribute("refreshFlight", false);
                        SetAttribute("flightSource", null);
                        Hide(new[] { "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
                    } catch (Exception) {
                        // NO-OP
                    }
                    break;

                case "Arrivals":
                    SetAttribute("flttype", "arr");
                    try {
                        Show(new[] { "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
                    } catch (Exception) {
                        // NO-OP
                    }
                    break;

                case "Departures":
                    SetAttribute("flttype", "dep");
                    try {
                        Show(new[] { "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
                    } catch (Exception) {
                        // NO-OP
                    }
                    break;

                default:
                    SetAttribute("flttype", "both");
                    try {
                        Show(new[] { "FlightSetFrom", "FlightSetTo", "Sequential", "RefreshFlight", "FlightRepeatData" });
                    } catch (Exception) {
                        // NO-OP
                    }
                    break;
            }
            view.UpdateDiagram();
            view.UpdateParamBindings("XMLText");
        }
    }

    [DisplayName("Value")]
    public class Value : LoadInjectorGridBase {

        public Value(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [CategoryAttribute("Required"), DisplayName("Value"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The value for this item")]
        public string ValueItem {
            get => _node.InnerText;
            set {
                _node.InnerText = value;
                view.UpdateParamBindings("XMLText");
            }
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("Test Run Settings")]
    public class SettingsGrid : LoadInjectorGridBase {

        public SettingsGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            if (!Parameters.SITAAMS) {
                Hide(new[] { "AMSHost", "AMSToken", "IATA", "ICAO", "ServerDiff", "AMSTimeout" });
            }
        }

        [CategoryAttribute("Test Execution"), DisplayName("Duration of Test (sec)"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("Duration of Test Run in Seconds")]
        public int Duration {
            get {
                int d;
                try {
                    d = int.Parse(GetNode("duration").InnerText);
                } catch (Exception) {
                    Duration = 60;
                    d = 60;
                }
                return d;
            }
            set {
                GetNode("duration").InnerText = value.ToString();
                view.UpdateParamBindings("XMLText");
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Test Execution"), DisplayName("Enable Scheduled Start"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("Enable the test run to start at a predefined time")]
        public bool StartAtEnabled {
            get {
                bool value = GetBoolDefaultFalseAttribute(GetNode("startAt"), "enabled");
                ShowHide("StartAt", value);
                return value;
            }
            set {
                try {
                    GetNode("startAt").Attributes["enabled"].Value = value.ToString();
                } catch (Exception) {
                    XmlAttribute newAttribute = GetNode("startAt").OwnerDocument.CreateAttribute("enabled");
                    newAttribute.Value = value.ToString();
                    GetNode("startAt").Attributes.Append(newAttribute);
                }

                ShowHide("StartAt", value);
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("Test Execution"), DisplayName("Scheduled Test Start"), ReadOnly(false), Browsable(false), PropertyOrder(4), DescriptionAttribute("Time to start the test run if Enable Scheduled Start is true")]
        [Editor(typeof(TimePickerEditor), typeof(TimePicker))]
        public DateTime StartAt {
            get => DateTime.Parse(GetNode("startAt").InnerText);
            set {
                try {
                    GetNode("startAt").InnerText = value.ToString("HH:mm:ss");
                } catch (Exception) {
                    GetNode("startAt").InnerText = "";
                }
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("Test Execution"), DisplayName("Logging Level"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("Logging Level"), ItemsSource(typeof(LogLevelList))]
        public string LogLevel {
            get => GetNode("logLevel").InnerText;
            set {
                GetNode("logLevel").InnerText = value;
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("Test Execution"), DisplayName("Repetitions"), ReadOnly(false), Browsable(true), PropertyOrder(10), DescriptionAttribute("Number of test execution repetitions")]
        public int Repetions {
            get {
                var n = GetNode("repeats");
                int v = int.Parse(n?.InnerText);
                return v;
            }
            set {
                GetNode("repeats").InnerText = value.ToString();
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("Test Execution"), DisplayName("Repetition Interval"), ReadOnly(false), Browsable(true), PropertyOrder(11), DescriptionAttribute("Time between successive exection repetitions in seconds")]
        public int RepttionResr {
            get => int.Parse(GetNode("repeatRest")?.InnerText);
            set {
                GetNode("repeatRest").InnerText = value.ToString();
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("AMS Connection"), DisplayName("AMS Host"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("Host name that AMS is running on")]
        public string AMSHost {
            get {
                try {
                    return GetNode("amshost").InnerText;
                } catch (Exception) {
                    XmlNode newNode = _node.OwnerDocument.CreateNode("element", "amshost", "");
                    newNode.InnerText = "localhost";

                    _node.AppendChild(newNode);
                    view.UpdateParamBindings("XMLText");
                    return "localhost";
                }
            }
            set {
                GetNode("amshost").InnerText = value;
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("AMS Connection"), DisplayName("AMS Token"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("Security token to access AMS")]
        public string AMSToken {
            get => GetNode("amstoken").InnerText;
            set {
                GetNode("amstoken").InnerText = value;
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("AMS Connection"), DisplayName("AMS Timeout"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The timeout in seconds for requests to get flights from  AMS")]
        public int AMSTimeout {
            get {
                string t = GetNode("amstimeout")?.InnerText;
                int tt;
                try {
                    tt = int.Parse(t);
                } catch (Exception) {
                    tt = 60;
                }
                return tt;
            }
            set {
                try {
                    GetNode("amstimeout").InnerText = value.ToString();
                } catch (Exception) {
                    Debug.WriteLine("Old File Format Warning");
                }
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("AMS Connection"), DisplayName("IATA Airport Code"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("IATA Airport Code")]
        public string IATA {
            get => GetNode("aptcode").InnerText;
            set {
                GetNode("aptcode").InnerText = value;
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("AMS Connection"), DisplayName("ICAO Airport Code"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("ICAO Airport Code")]
        public string ICAO {
            get => GetNode("aptcodeicao").InnerText;
            set {
                GetNode("aptcodeicao").InnerText = value;
                view.UpdateParamBindings("XMLText");
            }
        }

        [Editor(typeof(DoubleUpDown), typeof(DoubleUpDown))]
        [CategoryAttribute("AMS Connection"), DisplayName("Server Time Offset"), Browsable(true), PropertyOrder(18), DescriptionAttribute("The time difference (in half hour increments e.g. 1.0, 1.5 or 2.0 etc..) between the server and the client running the test. Positive value if the client is ahead of the server, negative if the client is behind server")]
        public double ServerDiff {
            get {
                try {
                    return double.Parse(GetNode("serverDiff").InnerText);
                } catch (Exception) {
                    GetNode("serverDiff").InnerText = "0";
                    return 0.0;
                }
            }
            set {
                GetNode("serverDiff").InnerText = value.ToString(CultureInfo.CurrentCulture);
                view.UpdateParamBindings("XMLText");
            }
        }
    }

    [DisplayName("HTTP Header")]
    public class HttpHeader : LoadInjectorGridBase {

        public HttpHeader(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [CategoryAttribute("Required"), DisplayName("Header Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The name of the header item")]
        public string Name {
            get => GetAttribute("name");
            set => SetAttribute("name", value);
        }

        [CategoryAttribute("Required"), DisplayName("Header Value"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The value of the header item")]
        public string Value {
            get => _node.InnerText;
            set {
                _node.InnerText = value;
                view.UpdateParamBindings("XMLText");
            }
        }
    }
}