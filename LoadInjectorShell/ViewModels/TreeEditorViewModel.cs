using LoadInjector.Common;
using LoadInjector.GridDefinitions;
using LoadInjector.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace LoadInjector.ViewModels {

    public class TreeEditorViewModel : BaseViewModel {
        private SelectedElementViewModel selectedElement = new SelectedElementViewModel(null);
        public static SelectedElementViewModel staticSelectedElement;

        public LoadInjectorGridBase MyGrid { get; set; }
        public LoadInjectorGridBase MyVariableGrid { get; set; }
        public LoadInjectorGridBase MyProtocolGrid { get; set; }
        public XmlDocument DataModel { get; private set; }
        public IView View { get; set; }
        public string Path { get; private set; }

        public string FileName { get; private set; }

        public Action<XmlNode> HighlightNodeInUI { get; set; }

        #region Commands

        public ICommand ViewAttributesCommand { get; }

        public ICommand AddAMSDirectCommand { get; }

        public ICommand AddAMSDataDrivenCommand { get; }

        public ICommand AddCSVDataDrivenCommand { get; }

        public ICommand AddExcelDataDrivenCommand { get; }

        public ICommand AddXMLDataDrivenCommand { get; }

        public ICommand AddJSONDataDrivenCommand { get; }

        public ICommand AddDataBaseDataDrivenCommand { get; }

        public ICommand AddRateDrivenCommand { get; }

        public ICommand AddDestinationCommand { get; }

        public ICommand AddVariableCommand { get; }

        public ICommand DeleteElementCommand { get; }

        public ICommand CloneElementCommand { get; }

        public ICommand AddChainedCommand { get; }

        public ICommand SaveDocumentCommand { get; }

        public ICommand ExportDocumentCommand { get; }

        public ICommand SaveAsDocumentCommand { get; }

        public ICommand LIExecuteCommand { get; }

        public ICommand AboutCommand { get; }

        public ICommand AddFilterCommand { get; }

        public ICommand AddExpressionCommand { get; }

        public ICommand AddDataFilterCommand { get; }

        #endregion Commands

        public TreeEditorViewModel(XmlDocument dataModel, string filePath, string fileName) {
            DataModel = dataModel;
            Path = filePath;
            this.FileName = fileName;

            ViewAttributesCommand = new RelayCommand<XmlNode>(TreeLeafSelected);
            AddAMSDirectCommand = new RelayCommand(p => { AddAMSDirect(); });
            AddAMSDataDrivenCommand = new RelayCommand(p => { AddAMSDataDriven(); });
            AddCSVDataDrivenCommand = new RelayCommand(p => { AddCSVDataDriven(); });
            AddExcelDataDrivenCommand = new RelayCommand(p => { AddExcelDataDriven(); });
            AddXMLDataDrivenCommand = new RelayCommand(p => { AddXMLDataDriven(); });
            AddJSONDataDrivenCommand = new RelayCommand(p => { AddJSONDataDriven(); });
            AddDataBaseDataDrivenCommand = new RelayCommand(p => { AddDataBaseDataDriven(); });
            AddRateDrivenCommand = new RelayCommand(p => { AddRateDriven(); });
            AddDestinationCommand = new RelayCommand(p => { AddDestination(); });
            AddVariableCommand = new RelayCommand(p => { AddVariable(); });

            AddExpressionCommand = new RelayCommand(p => { AddExpression(); }, p => { return CanAddExpression(); });
            AddDataFilterCommand = new RelayCommand(p => { AddDataFilter(); }, p => { return CanAddExpression(); });

            DeleteElementCommand = new RelayCommand<XmlNode>(p => { DeleteElement(SelectedElement.DataModel); }, p => { return CanDeleteElement(SelectedElement.DataModel); });
            CloneElementCommand = new RelayCommand<XmlNode>(p => { CloneElement(SelectedElement.DataModel); }, p => { return CanCloneElement(SelectedElement.DataModel); });
            AddChainedCommand = new RelayCommand<XmlNode>(p => { AddChained(SelectedElement.DataModel); }, p => { return CanChainElement(SelectedElement.DataModel); });
            AddFilterCommand = new RelayCommand<XmlNode>(p => { AddFilter(); }, p => { return CanAddFilter(); });

            SaveDocumentCommand = new RelayCommand(p => { Save(); });
            ExportDocumentCommand = new RelayCommand(p => { Export(); });
            SaveAsDocumentCommand = new RelayCommand<string>(SaveAs);
            LIExecuteCommand = new RelayCommand(p => { ExecuteLoadInjectorRuntime(); });

            AboutCommand = new RelayCommand(p => { AboutLoadInjector(); });

            PropertyChanged += ViewModel_PropertyChanged;

            //Check all the subscribed triggers exist is the source
            List<String> triggers = new List<string>();
            foreach (XmlNode trig in dataModel.SelectNodes("//trigger")) {
                triggers.Add(trig.Attributes["id"]?.Value);
            }
            foreach (XmlNode trig in dataModel.SelectNodes("//ratedriven")) {
                triggers.Add(trig.Attributes["ID"]?.Value);
            }
            foreach (XmlNode trig in dataModel.SelectNodes("//chained")) {
                triggers.Add(trig.Attributes["ID"]?.Value);
            }

            foreach (XmlNode trig in dataModel.SelectNodes("//subscribed")) {
                string value = trig.InnerText;
                if (!triggers.Contains(value)) {
                    var parent = trig.ParentNode;
                    parent.RemoveChild(trig);
                    MessageBox.Show($"Invalid Configuration File. Subscribed Trigger '{value}' doest not exist in any source. Subscription to trigger has been removed from destination '{parent.Attributes["name"]?.Value}'", "Invalid Config", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            XmlNode s = dataModel.SelectSingleNode("//settings");
            SelectedElement = new SelectedElementViewModel(s);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            try {
                switch (e.PropertyName) {
                    case "XMLText":
                        if (SelectedElement.DataModel.Name == "destination" || SelectedElement.DataModel.Name == "variable") {
                            View.UpdateTemplateBox();
                        }
                        if (!FileName.EndsWith("*")) {
                            FileName = FileName + "*";
                            OnPropertyChanged("FileName");
                        }
                        break;

                    case "SelectedElement":
                        if (SelectedElement.DataModel.Name == "destination" || SelectedElement.DataModel.Name == "variable") {
                            View.UpdateTemplateBox();
                        }
                        break;
                }
            } catch (Exception) {
                // Do nothing
            }
        }

        public SelectedElementViewModel SelectedElement {
            get => selectedElement;
            set {
                selectedElement = value;
                staticSelectedElement = value;

                //
                try {
                    if (View != null) {
                        if (selectedElement.NearestDestination != View.GetCurrentDestination) {
                            if (EnableTemplateSave) {
                                View.CheckSaveTemplate();
                            }
                            View.ResetCurrentDestination();
                            EnableTemplateSave = false;
                        }
                    }
                } catch (Exception) {
                    //
                }

                //Show the Configuration TAB if not a destination or variable selected
                if (View != null) {
                    if (selectedElement.NearestDestination == null) {
                        View.GetBottomTabControl.SelectedIndex = 0;
                    } else {
                        View.GetBottomTabControl.SelectedIndex = 2;
                    }
                }

                try {
                    UpdatePropertiesPanel(selectedElement.DataModel);
                } catch (Exception e) {
                    Debug.WriteLine(e.Message);
                }

                try {
                    TemplateFile = selectedElement.NearestDestination?.Attributes["templateFile"]?.Value;
                } catch (Exception e) {
                    Debug.WriteLine(e.Message);
                }

                OnPropertyChanged("TemplateFile");
                OnPropertyChanged("SelectedElement");
                OnPropertyChanged("TemplateFileVisibility");
            }
        }

        public string ProtocolLabel { get; set; } = "Protocol";

        public string Tab0Label { get; set; } = "Destination";

        private string templateFile;

        public string TemplateFile {
            get => templateFile;
            set {
                if (value != null) {
                    value = "File: " + value;
                }
                templateFile = value;
                OnPropertyChanged("TemplateFile");
            }
        }

        private bool enableTemplateSave;

        public bool EnableTemplateSave {
            get => enableTemplateSave;
            set {
                enableTemplateSave = value;
                OnPropertyChanged("EnableTemplateSave");
            }
        }

        public string Comments {
            get => DataModel.SelectSingleNode("//comments").InnerText;
            set {
                DataModel.SelectSingleNode("//comments").InnerText = value;
                OnPropertyChanged("XMLText");
            }
        }

        public LoadInjectorGridBase ProfilePropertyGrid { get; set; }

        public string XMLText {
            get {
                StringBuilder sb = new StringBuilder();
                TextWriter tr = new StringWriter(sb);
                XmlTextWriter wr = new XmlTextWriter(tr) {
                    Formatting = Formatting.Indented
                };
                DataModel.Save(wr);
                wr.Close();
                return sb.ToString();
            }
            set {
                XmlDocument document = new XmlDocument();
                document.LoadXml(value);
                DataModel = document;

                OnPropertyChanged("XMLText");
            }
        }

        internal void ChangeFilterType(string value) {
            //"Data Contains Value", "Data Matches Regex.", "Data Minimum Length", "XPath Exists","XPath Equals", "Xpath Date Within Offset"

            string name = "contains";
            if (value == "Data Contains Value") name = "contains";
            if (value == "Data Equals Value") name = "equals";
            if (value == "Data Matches Regex.") name = "matches";
            if (value == "Data Minimum Length") name = "length";
            if (value == "XPath Exists") name = "xpexists";
            if (value == "XPath Equals") name = "xpequals";
            if (value == "XPath Matches") name = "xpmatches";
            if (value == "XPath Date Within Offset") name = "dateRange";
            if (value == "Context Contains") name = "contextContains";

            XmlNode newNode = DataModel.CreateElement(name);
            SelectedElement.DataModel.ParentNode.InsertAfter(newNode, SelectedElement.DataModel);

            SelectedElement.DataModel.ParentNode.RemoveChild(SelectedElement.DataModel);

            ViewAttributesCommand.Execute(newNode);

            OnPropertyChanged("XMLText");
        }

        public bool LockExecution { get; set; }

        public List<SubscribedTriggerModel> SubscribedTriggers {
            get {
                TriggerTypeMap.SetTriggerMap(selectedElement.DataModel);

                List<string> currentSubID = new List<string>();
                foreach (XmlNode n in selectedElement.DataModel.SelectNodes("./subscribed")) {
                    currentSubID.Add(n.InnerText);
                }
                List<SubscribedTriggerModel> list = new List<SubscribedTriggerModel>();

                foreach (XmlNode n in selectedElement.DataModel.SelectNodes("//trigger")) {
                    SubscribedTriggerModel sub = new SubscribedTriggerModel(n, selectedElement.DataModel, DataModel, this, currentSubID);
                    if (sub.ID != null) {  //Only add fully defined triggers
                        list.Add(sub);
                    }
                }
                foreach (XmlNode n in selectedElement.DataModel.SelectNodes("//ratedriven")) {
                    SubscribedTriggerModel sub = new SubscribedTriggerModel(n.Attributes["name"]?.Value, n.Attributes["ID"]?.Value, selectedElement.DataModel, DataModel, this, currentSubID);
                    if (sub.ID != null) {
                        list.Add(sub);
                    }
                }
                foreach (XmlNode n in selectedElement.DataModel.SelectNodes("//chained")) {
                    SubscribedTriggerModel sub = new SubscribedTriggerModel(n.Attributes["name"]?.Value, n.Attributes["ID"]?.Value, selectedElement.DataModel, DataModel, this, currentSubID);
                    if (sub.ID != null) {
                        list.Add(sub);
                    }
                }
                return list;
            }
        }

        private void TreeLeafSelected(XmlNode newNode) {
            try {
                SelectedElement = new SelectedElementViewModel(newNode);
            } catch (Exception e) {
                Debug.WriteLine(e.Message);
            }

            View.HighlightCanvas(newNode);
        }

        public void UpdatePropertiesPanel(XmlNode selectedItem) {
            if (View == null) {
                return;
            }
            TabItem tab0 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(0);
            TabItem tab1 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(1);
            TabItem tab2 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(2);
            TabItem tab3 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(3);
            TabItem tab4 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(4);
            TabItem tab5 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(5);  // Variable Grid
            TabItem tab6 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(6);
            TabItem tab7 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(7);
            TabItem tab8 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(8);
            TabItem tab9 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(9);
            TabItem tab10 = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(10);
            tab0.Visibility = Visibility.Collapsed;
            tab1.Visibility = Visibility.Collapsed;
            tab2.Visibility = Visibility.Collapsed;
            tab3.Visibility = Visibility.Collapsed;
            tab4.Visibility = Visibility.Collapsed;
            tab5.Visibility = Visibility.Collapsed;
            tab6.Visibility = Visibility.Collapsed;
            tab7.Visibility = Visibility.Collapsed;
            tab8.Visibility = Visibility.Collapsed;
            tab9.Visibility = Visibility.Collapsed;
            tab10.Visibility = Visibility.Collapsed;

            int tabIndex = 0;

            switch (selectedItem.Name) {
                case "destination":
                    MyGrid = new DestinationPropertyGrid(selectedItem, View);
                    MyProtocolGrid = UpdateProtocolGrid(selectedItem.Attributes["protocol"].Value, selectedItem);
                    Tab0Label = "Destination";
                    tab1.Visibility = Visibility.Visible;
                    tab2.Visibility = Visibility.Visible;
                    if (selectedItem.Attributes["protocol"]?.Value == "TEXT") {
                        tab2.Visibility = Visibility.Collapsed;
                    }
                    tabIndex = 1;
                    break;

                case "amsdirect":
                    Tab0Label = "AMS Direct Destination";
                    MyGrid = new AmsDirectPropertyGrid(selectedItem, View);
                    tab1.Visibility = Visibility.Visible;
                    tabIndex = 1;
                    break;

                case "amsdatadriven":
                    MyGrid = new AmsDataDrivenGrid(selectedItem, View);
                    Tab0Label = "AMS Data Driven Source Destination";
                    tab4.Visibility = Visibility.Visible;
                    tabIndex = 4;
                    break;

                case "csvdatadriven":
                    MyGrid = new CsvDataDrivenGrid(selectedItem, View);
                    Tab0Label = "CSV Data Driven Source";
                    tab4.Visibility = Visibility.Visible;
                    tabIndex = 4;
                    break;

                case "exceldatadriven":
                    MyGrid = new ExcelDataDrivenGrid(selectedItem, View);
                    Tab0Label = "Excel Data Driven Source";
                    tab4.Visibility = Visibility.Visible;
                    tabIndex = 4;
                    break;

                case "jsondatadriven":
                    MyGrid = new JSONDataDrivenGrid(selectedItem, View);
                    Tab0Label = "JSON Data Driven Source";
                    tab4.Visibility = Visibility.Visible;
                    tabIndex = 4;
                    break;

                case "databasedatadriven":
                    MyGrid = new DataBaseDataDrivenGrid(selectedItem, View);
                    Tab0Label = "Database Data Driven Source";
                    tab4.Visibility = Visibility.Visible;
                    tabIndex = 4;
                    break;

                case "xmldatadriven":
                    MyGrid = new XMLDataDrivenGrid(selectedItem, View);
                    Tab0Label = "XML Data Driven Source";
                    tab4.Visibility = Visibility.Visible;
                    tabIndex = 4;
                    break;

                case "ratedriven":
                    Tab0Label = $"Rate Driven Source: {selectedElement.DataModel.Attributes["name"]?.Value}";
                    MyGrid = new RateSourcePropertyGrid(selectedItem, View);
                    tab0.Visibility = Visibility.Visible;
                    tabIndex = 0;
                    break;

                case "chained":
                    Tab0Label = $"Chained Driven Source: {selectedElement.DataModel.Attributes["name"]?.Value}";
                    MyGrid = new ChainedSourcePropertyGrid(selectedItem, View);
                    tab0.Visibility = Visibility.Visible;
                    tabIndex = 0;
                    break;

                case "settings":
                    Tab0Label = "Settings";
                    MyGrid = new SettingsGrid(selectedItem, View);
                    tab0.Visibility = Visibility.Visible;
                    break;

                case "header":
                    Tab0Label = "Header";
                    MyGrid = new HttpHeader(selectedItem, View);
                    tab0.Visibility = Visibility.Visible;
                    break;

                case "value":
                    Tab0Label = "Value";
                    MyGrid = new Value(selectedItem, View);
                    tab0.Visibility = Visibility.Visible;
                    break;

                case "variable":
                    Tab0Label = "Variable";
                    MyVariableGrid = new VariablePropertyGrid(selectedItem, View);
                    tab5.Visibility = Visibility.Visible;
                    tabIndex = 5;
                    break;

                case "eventdrivensources":
                    MyGrid = null;
                    tab6.Visibility = Visibility.Visible;
                    tabIndex = 6;
                    break;

                case "ratedrivensources":
                    MyGrid = null;
                    tab7.Visibility = Visibility.Visible;
                    tabIndex = 7;
                    break;

                case "lines":
                    MyGrid = null;
                    tab8.Visibility = Visibility.Visible;
                    tabIndex = 8;
                    break;

                case "filter":
                    MyGrid = new Filter(selectedItem, View);
                    break;

                case "and":
                case "or":
                case "xor":
                case "not":
                    MyGrid = new BooleanExpression(selectedItem, View);
                    break;

                case "contains":
                    MyGrid = new ContainsFilter(selectedItem, View);
                    break;

                case "equals":
                    MyGrid = new EqualsFilter(selectedItem, View);
                    break;

                case "matches":
                    MyGrid = new MatchesFilter(selectedItem, View);
                    break;

                case "length":
                    MyGrid = new LengthFilter(selectedItem, View);
                    break;

                case "xpexists":
                    MyGrid = new XPExistsFilter(selectedItem, View);
                    break;

                case "xpmatches":
                    MyGrid = new XPMatchesFilter(selectedItem, View);
                    break;

                case "xpequals":
                    MyGrid = new XPEqualsFilter(selectedItem, View);
                    break;

                case "dateRange":
                    MyGrid = new DateRangeFilter(selectedItem, View);
                    break;
            }

            View.GetDestinationTabControl.SelectedIndex = tabIndex;
            OnPropertyChanged("MyGrid");
            OnPropertyChanged("MyVariableGrid");
            OnPropertyChanged("MyProtocolGrid");
            OnPropertyChanged("ProfilePropertyGrid");
            OnPropertyChanged("Tab0Label");
            OnPropertyChanged("Triggers");
            OnPropertyChanged("SubscribedTriggers");
            OnPropertyChanged("HeaderGridProtocolVisibility");
            OnPropertyChanged("Headers");

            View.GetPropertyGrid().Update();
        }

        public List<TriggerModelEntry> Triggers {
            get {
                List<TriggerModelEntry> triggers = new List<TriggerModelEntry>();

                foreach (XmlNode node in SelectedElement.DataModel.SelectNodes(".//trigger")) {
                    TriggerModelEntry trig = new TriggerModelEntry(node, this);
                    triggers.Add(trig);
                }

                return triggers;
            }
        }

        public Visibility HeaderVisibility {
            get {
                if (SelectedElement.DataModel.Attributes["sourceType"]?.Value == "url") {
                    return Visibility.Visible;
                }
                if (SelectedElement.DataModel.Attributes["dataSource"]?.Value == "RESTXML") {
                    return Visibility.Visible;
                }
                if (SelectedElement.DataModel.Attributes["dataSource"]?.Value == "RESTJSON") {
                    return Visibility.Visible;
                }
                if (SelectedElement.DataModel.Attributes["dataSource"]?.Value == "RESTCSV") {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        public Visibility HeaderGridProtocolVisibility {
            get {
                Debug.WriteLine($"Protocol = {SelectedElement.DataModel.Attributes["protocol"]?.Value}");

                if (SelectedElement.DataModel.Attributes["protocol"]?.Value == "HTTP") {
                    return Visibility.Visible;
                }
                if (SelectedElement.DataModel.Attributes["protocol"]?.Value == "HTTPGET") {
                    return Visibility.Visible;
                }
                if (SelectedElement.DataModel.Attributes["protocol"]?.Value == "HTTPPUT") {
                    return Visibility.Visible;
                }
                if (SelectedElement.DataModel.Attributes["protocol"]?.Value == "HTTPPATCH") {
                    return Visibility.Visible;
                }
                if (SelectedElement.DataModel.Attributes["protocol"]?.Value == "HTTPDELETE") {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        public Visibility TemplateFileVisibility {
            get {
                if (SelectedElement.DataModel.Name == "variable" && SelectedElement.DataModel.ParentNode.Name == "destination") {
                    return Visibility.Visible;
                }
                if (SelectedElement.DataModel.Name == "destination") {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        public List<HeaderModelEntry> Headers {
            get {
                List<HeaderModelEntry> headers = new List<HeaderModelEntry>();

                foreach (XmlNode node in SelectedElement.DataModel.SelectNodes(".//header")) {
                    HeaderModelEntry head = new HeaderModelEntry(node, this);
                    headers.Add(head);
                }

                return headers;
            }
        }

        public void DeleteHeader(DataGrid grid) {
            HeaderModelEntry head = (HeaderModelEntry)grid.SelectedItem;
            foreach (XmlNode node in SelectedElement.DataModel.SelectNodes(".//header")) {
                HeaderModelEntry headde = new HeaderModelEntry(node, this);
                if (head.Equals(headde)) {
                    SelectedElement.DataModel.RemoveChild(node);
                    break;
                }
            }
            OnPropertyChanged("XMLText");
            OnPropertyChanged("Headers");
        }

        public void DeleteTrigger(DataGrid grid) {
            TriggerModelEntry trig = (TriggerModelEntry)grid.SelectedItem;
            foreach (XmlNode node in SelectedElement.DataModel.SelectNodes(".//trigger")) {
                TriggerModelEntry trigde = new TriggerModelEntry(node, this);
                if (trig.Equals(trigde)) {
                    SelectedElement.DataModel.RemoveChild(node);
                    break;
                }
            }

            // Delete any subcriptions to the trigger
            foreach (XmlNode node in SelectedElement.DataModel.SelectNodes("//subscribed")) {
                if (trig.ID == node.InnerText) {
                    node.ParentNode.RemoveChild(node);
                }
            }

            OnPropertyChanged("XMLText");
            OnPropertyChanged("Triggers");
        }

        public void DeleteValue(DataGrid grid) {
            string value = ((ValueModel)grid.SelectedItem)?.Value;
            if (value == null) {
                return;
            }

            foreach (XmlNode node in SelectedElement.DataModel.SelectNodes(".//value")) {
                if (value == node.InnerText) {
                    SelectedElement.DataModel.RemoveChild(node);
                    break;
                }
            }

            OnPropertyChanged("XMLText");
            OnPropertyChanged("MyVariableGrid");
        }

        private LoadInjectorGridBase UpdateProtocolGrid(string proto, XmlNode selectedItem) {
            ProtocolLabel = $"Protocol: {Parameters.protocolToDescription[proto]}";
            OnPropertyChanged("ProtocolLabel");
            return (LoadInjectorGridBase)Parameters.protocolDictionary[proto].GetConfigGrid(selectedItem, View);
        }

        // Called by changes in the UI.
        public void SetProtocolGrid(string value) {
            MyProtocolGrid = UpdateProtocolGrid(Parameters.descriptionToProtocol[value], SelectedElement.DataModel);
            TabItem tab = (TabItem)View.GetDestinationTabControl.Items.GetItemAt(2);
            if (SelectedElement.DataModel.Name == "destination") {
                tab.Visibility = Visibility.Visible;
                if (SelectedElement.DataModel.Attributes["protocol"]?.Value == "TEXT") {
                    tab.Visibility = Visibility.Hidden;
                }
            } else {
                tab.Visibility = Visibility.Hidden;
            }
            OnPropertyChanged("MyProtocolGrid");
        }

        #region action

        private void AddAttribute(XmlNode parent, string name, string value) {
            XmlAttribute newAttribute = DataModel.CreateAttribute(name);
            newAttribute.Value = value;
            parent.Attributes.Append(newAttribute);
        }

        private void AddAMSDirect() {
            SelectedElement = new SelectedElementViewModel(DataModel.SelectSingleNode("//lines"));

            XmlNode newNode = DataModel.CreateElement("amsdirect");
            AddAttribute(newNode, "name", "Direct Update of AMS");
            AddAttribute(newNode, "protocol", "WS");

            if (SelectedElement.DataModel.ChildNodes.Count == 0) {
                SelectedElement.DataModel.AppendChild(newNode);
            } else {
                XmlNode lastNode = null;
                foreach (XmlNode n in SelectedElement.DataModel.ChildNodes) {
                    if (n.Name == "destination") {
                        SelectedElement.DataModel.InsertBefore(newNode, n);
                        break;
                    } else {
                        if (lastNode == null) {
                            SelectedElement.DataModel.AppendChild(newNode);
                        } else {
                            lastNode = n;
                        }
                    }
                }
            }
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddCSVDataDriven() {
            SelectedElement = new SelectedElementViewModel(DataModel.SelectSingleNode("//eventdrivensources"));

            XmlNode newNode = DataModel.CreateElement("csvdatadriven");
            AddAttribute(newNode, "name", "Descriptive Name");
            AddAttribute(newNode, "sourceType", "file");
            SelectedElement.DataModel.AppendChild(newNode);
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddExcelDataDriven() {
            SelectedElement = new SelectedElementViewModel(DataModel.SelectSingleNode("//eventdrivensources"));

            XmlNode newNode = DataModel.CreateElement("exceldatadriven");
            AddAttribute(newNode, "name", "Descriptive Name");

            SelectedElement.DataModel.AppendChild(newNode);
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddXMLDataDriven() {
            SelectedElement = new SelectedElementViewModel(DataModel.SelectSingleNode("//eventdrivensources"));

            XmlNode newNode = DataModel.CreateElement("xmldatadriven");
            AddAttribute(newNode, "name", "Descriptive Name");
            AddAttribute(newNode, "sourceType", "file");

            SelectedElement.DataModel.AppendChild(newNode);
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddJSONDataDriven() {
            SelectedElement = new SelectedElementViewModel(DataModel.SelectSingleNode("//eventdrivensources"));

            XmlNode newNode = DataModel.CreateElement("jsondatadriven");
            AddAttribute(newNode, "name", "Descriptive Name");
            AddAttribute(newNode, "sourceType", "file");

            SelectedElement.DataModel.AppendChild(newNode);
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddRateDriven() {
            SelectedElement = new SelectedElementViewModel(DataModel.SelectSingleNode("//ratedrivensources"));

            XmlNode newNode = DataModel.CreateElement("ratedriven");
            AddAttribute(newNode, "name", "Descriptive Name");
            if (Parameters.SITAAMS) {
                AddAttribute(newNode, "flttype", "none");
            }
            SelectedElement.DataModel.AppendChild(newNode);
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddDataBaseDataDriven() {
            SelectedElement = new SelectedElementViewModel(DataModel.SelectSingleNode("//eventdrivensources"));

            XmlNode newNode = DataModel.CreateElement("databasedatadriven");
            AddAttribute(newNode, "name", "Descriptive Name");
            AddAttribute(newNode, "sourceType", "mssql");

            SelectedElement.DataModel.AppendChild(newNode);
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddAMSDataDriven() {
            SelectedElement = new SelectedElementViewModel(DataModel.SelectSingleNode("//eventdrivensources"));

            XmlNode newNode = DataModel.CreateElement("amsdatadriven");
            AddAttribute(newNode, "name", "Descriptive Name");
            AddAttribute(newNode, "flightSetFrom", "-180");
            AddAttribute(newNode, "flightSetTo", "540");
            AddAttribute(newNode, "flttype", "both");
            SelectedElement.DataModel.AppendChild(newNode);
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddDestination() {
            SelectedElement = new SelectedElementViewModel(DataModel.SelectSingleNode("//lines"));

            XmlNode newNode = DataModel.CreateElement("destination");
            AddAttribute(newNode, "name", "Descriptive Name");
            AddAttribute(newNode, "protocol", "TEXT");

            SelectedElement.DataModel.AppendChild(newNode);
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddVariable() {
            XmlNode newNode = DataModel.CreateElement("variable");
            SelectedElement.DataModel.AppendChild(newNode);

            if (newNode.ParentNode.Name == "destination") {
                AddAttribute(newNode, "name", "Descriptive Name");
                AddAttribute(newNode, "token", "@Unique Token");
                AddAttribute(newNode, "type", "fixed");
            } else {
                AddAttribute(newNode, "name", "Descriptive Name");
                AddAttribute(newNode, "externalName", "S__-ExternalName");
                AddAttribute(newNode, "type", "fixed");
            }

            SelectedElement.DataModel.AppendChild(newNode);
            SelectedElement = new SelectedElementViewModel(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        public void AddTrigger() {
            XmlNode newNode = DataModel.CreateElement("trigger");
            AddAttribute(newNode, "name", "New Trigger");
            AddAttribute(newNode, "delta", "0");

            if (Parameters.SITAAMS && SelectedElement.DataModel.Name == "amsdatadriven") {
                if (SelectedElement.DataModel.Attributes["trigType"]?.Value != null) {
                    AddAttribute(newNode, "trigType", SelectedElement.DataModel.Attributes["trigType"]?.Value);
                }
                if (SelectedElement.DataModel.Attributes["externalName"]?.Value != null) {
                    AddAttribute(newNode, "externalName", SelectedElement.DataModel.Attributes["externalName"]?.Value);
                }
            }

            SelectedElement.DataModel.AppendChild(newNode);

            OnPropertyChanged("XMLText");
            OnPropertyChanged("Triggers");
            View.DrawConfig();
        }

        public void AddValue() {
            XmlNode newNode = DataModel.CreateElement("value");
            newNode.InnerText = "- item value -";
            SelectedElement.DataModel.AppendChild(newNode);

            OnPropertyChanged("XMLText");
            View.UpdateParamBindings("MyVariableGrid");
            View.DrawConfig();
        }

        public void AddHeader() {
            XmlNode newNode = DataModel.CreateElement("header");
            AddAttribute(newNode, "name", "name");

            if (SelectedElement.DataModel.ChildNodes.Count == 0) {
                SelectedElement.DataModel.AppendChild(newNode);
            } else {
                XmlNode lastNode = null;
                foreach (XmlNode n in SelectedElement.DataModel.ChildNodes) {
                    if (n.Name == "variable") {
                        SelectedElement.DataModel.InsertBefore(newNode, n);
                        break;
                    } else {
                        if (lastNode == null) {
                            SelectedElement.DataModel.AppendChild(newNode);
                        } else {
                            lastNode = n;
                        }
                    }
                }
            }

            OnPropertyChanged("XMLText");
            OnPropertyChanged("Headers");
        }

        private void AddFilter() {
            XmlNode newNode = DataModel.CreateElement("filter");
            AddAttribute(newNode, "filterTime", "post");

            if (newNode == null)
                return;
            if (newNode.NodeType == XmlNodeType.Attribute) {
                SelectedElement.DataModel.Attributes.Append(newNode as XmlAttribute);
                TreeLeafSelected(SelectedElement.DataModel);
            } else {
                SelectedElement.DataModel.AppendChild(newNode);
            }

            OnPropertyChanged("XMLText");
        }

        private bool CanAddFilter() {
            if (SelectedElement == null || SelectedElement.DataModel == null) {
                return false;
            }

            if (SelectedElement.DataModel.HasChildNodes) {
                foreach (XmlNode n in SelectedElement.DataModel.ChildNodes) {
                    if (n.Name == "filter") {
                        return false;
                    }
                }
            }
            string flttype = SelectedElement.DataModel.Attributes["flttype"]?.Value;

            if (SelectedElement.DataModel.Name.Contains("driven")
                && flttype != null && flttype != "none"
                ) { return true; }

            if (SelectedElement.DataModel.Name == "amsdatadriven") {
                return true;
            } else {
                return false;
            }
        }

        private void AddDataFilter() {
            XmlNode newNode = DataModel.CreateElement("contains");
            SelectedElement.DataModel.AppendChild(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private void AddExpression() {
            XmlNode newNode = DataModel.CreateElement("and");
            SelectedElement.DataModel.AppendChild(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        private bool CanAddExpression() {
            if (SelectedElement == null || SelectedElement.DataModel == null) {
                return false;
            }
            if (SelectedElement.DataModel.NodeType != XmlNodeType.Element) {
                return false;
            }

            if (SelectedElement.DataModel.Name == "filter") {
                if (!SelectedElement.DataModel.HasChildNodes) {
                    return true;
                }
                if (SelectedElement.DataModel.ChildNodes.Count == 1 && SelectedElement.DataModel.ChildNodes.Item(0).Name == "altqueue") {
                    return true;
                }

                return false;
            }
            if (SelectedElement.DataModel.Name == "and" || SelectedElement.DataModel.Name == "or" || SelectedElement.DataModel.Name == "xor") {
                return true;
            }
            if (SelectedElement.DataModel.Name == "not" && !SelectedElement.DataModel.HasChildNodes) {
                return true;
            }

            return false;
        }

        private bool CanDeleteElement(XmlNode currentNode) {
            if (currentNode?.ParentNode == null) {
                return false;
            }
            if (SelectedElement.DataModel.Name == "lines"
                || SelectedElement.DataModel.Name == "settings"
                || SelectedElement.DataModel.Name == "datadrivenlines"
                || SelectedElement.DataModel.Name == "csvdatadrivenlines"
                || SelectedElement.DataModel.Name == "exceldatadrivenlines"
                || SelectedElement.DataModel.Name == "jsondatadrivenlines"
                || SelectedElement.DataModel.Name == "databasedatadrivenlines"
                || SelectedElement.DataModel.Name == "xmldatadrivenlines"
                || SelectedElement.DataModel.Name == "eventdrivensources"
                || SelectedElement.DataModel.Name == "ratedrivensources"
                ) {
                return false;
            } else if (currentNode.NodeType == XmlNodeType.Text || currentNode.NodeType == XmlNodeType.Element) {
                return true;
            }
            return false;
        }

        private void DeleteElement(XmlNode currentNode) {
            XmlNode parentNode = currentNode.ParentNode;
            parentNode.RemoveChild(currentNode);
            OnPropertyChanged("XMLText");
            View.DrawConfig();
            MyGrid = null;
            OnPropertyChanged("MyGrid");
        }

        private bool CanCloneElement(XmlNode currentNode) {
            if (currentNode?.ParentNode == null) {
                return false;
            }
            if (SelectedElement.DataModel.Name == "destination"
                || SelectedElement.DataModel.Name == "amsdirect"
                || SelectedElement.DataModel.Name == "chained"
                || SelectedElement.DataModel.Name == "amsdatadriven"
                || SelectedElement.DataModel.Name == "csvdatadriven"
                || SelectedElement.DataModel.Name == "exceldatadriven"
                || SelectedElement.DataModel.Name == "jsondatadriven"
                || SelectedElement.DataModel.Name == "databasedatadriven"
                || SelectedElement.DataModel.Name == "xmldatadrivens"
                || SelectedElement.DataModel.Name == "ratedriven"
                || SelectedElement.DataModel.Name == "header"
                || SelectedElement.DataModel.Name == "variable"
                ) {
                return true;
            } else {
                return false;
            }
        }

        private bool CanChainElement(XmlNode currentNode) {
            if (currentNode?.ParentNode == null) {
                return false;
            }
            if (SelectedElement.DataModel.Name == "chained"
                || SelectedElement.DataModel.Name == "ratedriven"
                || SelectedElement.DataModel.Name == "amsdatadriven"
                || SelectedElement.DataModel.Name == "csvdatadriven"
                || SelectedElement.DataModel.Name == "exceldatadriven"
                || SelectedElement.DataModel.Name == "jsondatadriven"
                || SelectedElement.DataModel.Name == "databasedatadriven"
                || SelectedElement.DataModel.Name == "xmldatadriven"

                ) {
                return true;
            } else {
                return false;
            }
        }

        private void CloneElement(XmlNode currentNode) {
            XmlNode parentNode = currentNode.ParentNode;
            XmlNode clone = currentNode.CloneNode(true);

            if (clone.Name == "ratedriven") {
                if (clone.Attributes["name"] != null) {
                    clone.Attributes["name"].Value = "Cloned - " + clone.Attributes["name"].Value;
                } else {
                    XmlAttribute newAttribute = DataModel.CreateAttribute("name");
                    newAttribute.Value = "Descriptive Name";
                    clone.Attributes.Append(newAttribute);
                }
                if (clone.Attributes["ID"] != null) {
                    clone.Attributes["ID"].Value = "Cloned - " + clone.Attributes["ID"].Value;
                }
            }
            if (clone.Name.Contains("datadriven") || clone.Name.Contains("databasedriven")) {
                if (clone.Attributes["name"] != null) {
                    clone.Attributes["name"].Value = "Cloned - " + clone.Attributes["name"].Value;
                } else {
                    XmlAttribute newAttribute = DataModel.CreateAttribute("name");
                    newAttribute.Value = "Descriptive Name";
                    clone.Attributes.Append(newAttribute);
                }

                foreach (XmlNode trig in clone.SelectNodes("./trigger")) {
                    if (trig.Attributes["id"] != null) {
                        trig.Attributes["id"].Value = "Cloned_" + trig.Attributes["id"].Value;
                    }
                }
            }
            if (clone.Name.Contains("destination")) {
                if (clone.Attributes["name"] != null) {
                    clone.Attributes["name"].Value = "Cloned - " + clone.Attributes["name"].Value;
                } else {
                    XmlAttribute newAttribute = DataModel.CreateAttribute("name");
                    newAttribute.Value = "Descriptive Name";
                    clone.Attributes.Append(newAttribute);
                }
            }
            parentNode.AppendChild(clone);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
            MyGrid = null;
            OnPropertyChanged("MyGrid");
        }

        private void AddChained(XmlNode currentNode) {
            XmlNode parentNode = currentNode;
            XmlNode nextLink = DataModel.CreateElement("chained");

            XmlAttribute newAttribute = DataModel.CreateAttribute("name");
            newAttribute.Value = "Chained Trigger";
            nextLink.Attributes.Append(newAttribute);

            XmlAttribute delay = DataModel.CreateAttribute("delay");
            delay.Value = "10";
            nextLink.Attributes.Append(delay);

            XmlAttribute useParentData = DataModel.CreateAttribute("useParentData");
            useParentData.Value = "true";
            nextLink.Attributes.Append(useParentData);

            parentNode.AppendChild(nextLink);

            SelectedElement.DataModel.AppendChild(nextLink);
            SelectedElement = new SelectedElementViewModel(nextLink);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        internal bool CanChangeElementType() {
            return true;
        }

        internal void ChangeElementType(string value) {
            if (value == "not" && SelectedElement.DataModel.ChildNodes.Count > 1) {
                MessageBox.Show("Cannot change to 'not' beacause a 'not' can only have one direct child", "QueueExchange Configuration");
                OnPropertyChanged("XMLText");
                View.DrawConfig();
                return;
            }

            XmlNode newNode = DataModel.CreateElement(value);
            SelectedElement.DataModel.ParentNode.InsertAfter(newNode, SelectedElement.DataModel);

            foreach (XmlNode child in SelectedElement.DataModel.ChildNodes) {
                XmlNode move = child.CloneNode(true);
                newNode.AppendChild(move);
            }

            SelectedElement.DataModel.ParentNode.RemoveChild(SelectedElement.DataModel);
            ViewAttributesCommand.Execute(newNode);

            OnPropertyChanged("XMLText");
            View.DrawConfig();
        }

        #endregion action

        public void Save() {
            if (Path == null) {
                SaveFileDialog dialog = new SaveFileDialog {
                    Filter = "XML Files (*.xml)|*.xml"
                };
                if (dialog.ShowDialog() == true) {
                    using (TextWriter sw = new StreamWriter(dialog.FileName, false, Encoding.UTF8)) {
                        DataModel.Save(sw);

                        Path = dialog.FileName;
                        string[] f = dialog.FileName.Split('\\');
                        FileName = f[f.Length - 1];

                        OnPropertyChanged("FileName");
                        OnPropertyChanged("Path");
                    }
                    return;
                } else {
                    return;
                }
            }

            using (TextWriter sw = new StreamWriter(Path, false, Encoding.UTF8)) {
                DataModel.Save(sw);
            }

            string[] f2 = Path.Split('\\');
            FileName = f2[f2.Length - 1];

            OnPropertyChanged("FileName");
            OnPropertyChanged("Path");
        }

        public void Export() {
            XmlDocument newDoc = DataModel.CloneNode(true) as XmlDocument;
            SaveFileDialog dialog = new SaveFileDialog {
                Filter = "Load Injector Archive Files (*.lia)|*.lia"
            };

            if (dialog.ShowDialog() == true) {
                bool success = ExportToFile(newDoc, dialog.FileName);
                if (!success) {
                    MessageBox.Show("Error creating archive. Possibly due to missing data or template file", "Archive Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool ExportToFile(XmlDocument doc, string file) {
            Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            Dictionary<string, string> fullfileName = new Dictionary<string, string>();

            foreach (XmlNode node in doc.SelectNodes(".//*")) {
                if (node.Attributes["dataFile"]?.Value != null) {
                    try {
                        string fullFile = node.Attributes["dataFile"]?.Value;
                        if (fullfileName.ContainsKey(fullFile)) {
                            node.Attributes["dataFile"].Value = $"./DATA/{fullfileName[fullFile]}";
                        } else {
                            string[] f2 = fullFile.Split('\\');
                            string filename = f2[f2.Length - 1];

                            files.Add($"DATA/{filename}", File.ReadAllBytes(fullFile));
                            node.Attributes["dataFile"].Value = $"./DATA/{filename}";
                            fullfileName.Add(fullFile, filename);
                        }
                    } catch (FileNotFoundException) {
                        MessageBox.Show("Data file not found", "Archive Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    } catch (Exception) {
                        return false;
                    }
                }
                if (node.Attributes["templateFile"]?.Value != null) {
                    try {
                        string fullFile = node.Attributes["templateFile"]?.Value;
                        if (fullfileName.ContainsKey(fullFile)) {
                            node.Attributes["templateFile"].Value = $"./TEMPLATES/{fullfileName[fullFile]}";
                        } else {
                            string[] f2 = fullFile.Split('\\');
                            string filename = f2[f2.Length - 1];

                            files.Add($"TEMPLATES/{filename}", File.ReadAllBytes(fullFile));
                            node.Attributes["templateFile"].Value = $"./TEMPLATES/{filename}";
                            fullfileName.Add(fullFile, filename);
                        }
                    } catch (FileNotFoundException) {
                        MessageBox.Show("Template file not found", "Archive Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    } catch (Exception) {
                        return false;
                    }
                }
            }

            //Pretty Print the XML config

            //StringBuilder sb = new StringBuilder();
            //TextWriter tr = new StringWriter(sb);
            //XmlTextWriter wr = new XmlTextWriter(tr, System.Text.Encoding.UTF8) {
            //    Formatting = Formatting.Indented,
            //};
            //doc.Save(wr);
            //wr.Close();

            files.Add("config.xml", Encoding.ASCII.GetBytes(doc.OuterXml));

            byte[] archiveBytes = ZipFiles(files);

            File.WriteAllBytes(file, archiveBytes);

            return true;
        }

        private static byte[] ZipFiles(Dictionary<string, byte[]> files) {
            using (MemoryStream ms = new MemoryStream()) {
                using (ZipArchive archive = new ZipArchive(ms, ZipArchiveMode.Update)) {
                    foreach (var file in files) {
                        ZipArchiveEntry orderEntry = archive.CreateEntry(file.Key); //create a file with this name
                        using (BinaryWriter writer = new BinaryWriter(orderEntry.Open())) {
                            writer.Write(file.Value); //write the binary data
                        }
                    }
                }
                //ZipArchive must be disposed before the MemoryStream has data
                return ms.ToArray();
            }
        }

        private void SaveAs(string path) {
            XmlDocument newDoc = DataModel.CloneNode(true) as XmlDocument;
            using (TextWriter sw = new StreamWriter(path, false, Encoding.UTF8)) {
                newDoc.Save(sw);
            }

            this.Path = path;
            string[] f = path.Split('\\');
            FileName = f[f.Length - 1];

            var dir = new FileInfo(this.Path).Directory;
            try {
                Directory.SetCurrentDirectory(dir.FullName);
            } catch (Exception ex) {
                Console.WriteLine("The specified directory does not exist. {0}", ex);
            }

            OnPropertyChanged("FileName");
            OnPropertyChanged("Path");
        }

        private void ExecuteLoadInjectorRuntime() {
            //Start Executeable here
        }

        private void AboutLoadInjector() {
            LIAbout dlg = new LIAbout();
            dlg.ShowDialog();
        }

        public static int GetAvailablePort(int startingPort) {
            if (startingPort > ushort.MaxValue) throw new ArgumentException($"Can't be greater than {ushort.MaxValue}", nameof(startingPort));
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();

            var connectionsEndpoints = ipGlobalProperties.GetActiveTcpConnections().Select(c => c.LocalEndPoint);
            var tcpListenersEndpoints = ipGlobalProperties.GetActiveTcpListeners();
            var udpListenersEndpoints = ipGlobalProperties.GetActiveUdpListeners();
            var portsInUse = connectionsEndpoints.Concat(tcpListenersEndpoints)
                .Concat(udpListenersEndpoints)
                .Select(e => e.Port);

            return Enumerable.Range(startingPort, ushort.MaxValue - startingPort + 1).Except(portsInUse).FirstOrDefault();
        }
    }
}