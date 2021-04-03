using LoadInjector.Common;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    public class FilterTypeList : IItemsSource {

        public ItemCollection GetValues() {
            var types = new ItemCollection {
                "Data Contains Value", "Data Equals Value", "Data Matches Regex.", "Data Minimum Length", "XPath Exists","XPath Equals","XPath Matches", "XPath Date Within Offset"
            };
            return types;
        }
    }

    public class BooleanTypeList : IItemsSource {

        public ItemCollection GetValues() {
            var types = new ItemCollection {
                "and", "or", "xor", "not"
            };
            return types;
        }
    }

    public class PrePostTypeList : IItemsSource {

        public ItemCollection GetValues() {
            var types = new ItemCollection {
                "Pre Filtering", "Post Filtering"
            };
            return types;
        }
    }

    public class Filter : LoadInjectorGridBase {

        public Filter(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = dataModel.Name;
        }

        [CategoryAttribute("Required"), DisplayName("Time of Filtering"), Browsable(true), PropertyOrder(1), DescriptionAttribute("Select whether flight are pre filtered at the time of preparation or post filtered at the time of triggering"), ItemsSource(typeof(PrePostTypeList))]
        public string FilterNature {
            get {
                string v = GetAttribute("filterTime");
                if (v == null) {
                    SetAttribute("filterTime", "post");
                    return "Post Filtering";
                }
                if (v == "post") {
                    return "Post Filtering";
                } else {
                    return "Pre Filtering";
                }
            }
            set {
                if (value == "Post Filtering") {
                    SetAttribute("filterTime", "post");
                } else {
                    SetAttribute("filterTime", "pre");
                }
            }
        }
    }

    [DisplayName("Boolean Expression of Child Nodes")]
    public class BooleanExpression : LoadInjectorGridBase {

        public BooleanExpression(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = dataModel.Name;
        }

        [CategoryAttribute("Required"), DisplayName("Boolean Type"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The selected boolean operator is applied to all the child nodes to produce a result"), ItemsSource(typeof(BooleanTypeList))]
        public string Type {
            get => _node.Name;
            set {
                if (view.CanChangeElementType(value)) {
                    view.ChangeElementType(value);
                }
            }
        }
    }

    [DisplayName("Data Contains Filter")]
    public class ContainsFilter : LoadInjectorGridBase {

        public ContainsFilter(XmlNode dataModel, IView view) {
            try {
                _node = dataModel;
                this.view = view;
                type = dataModel.Name;
            } catch (Exception e) {
                Debug.WriteLine("ERROR IN FILTER GRID CONSTRUCTOR");
                Debug.WriteLine(e.Message);
            }
        }

        [CategoryAttribute("Required"), DisplayName("Data Filter Type"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The type of data filter"), ItemsSource(typeof(FilterTypeList))]
        public string Type {
            get => "Data Contains Value";
            set => view.ChangeFilterType(value);
        }

        [CategoryAttribute("Required"), DisplayName("Value"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The string the data must contain for the filter to pass")]
        public string Value {
            get => GetAttribute("value");
            set => SetAttribute("value", value);
        }
    }

    [DisplayName("Data Equals Filter")]
    public class EqualsFilter : LoadInjectorGridBase {

        public EqualsFilter(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = dataModel.Name;
        }

        [CategoryAttribute("Required"), DisplayName("Data Filter Type"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The type of data filter"), ItemsSource(typeof(FilterTypeList))]
        public string Type {
            get => "Data Equals Value";
            set => view.ChangeFilterType(value);
        }

        [CategoryAttribute("Required"), DisplayName("Value"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The string the data must equal for the filter to pass")]
        public string Value {
            get => GetAttribute("value");
            set => SetAttribute("value", value);
        }
    }

    [DisplayName("Data Matched RegEx Filter")]
    public class MatchesFilter : LoadInjectorGridBase {

        public MatchesFilter(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = dataModel.Name;
        }

        [CategoryAttribute("Required"), DisplayName("Data Filter Type"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The type of data filter"), ItemsSource(typeof(FilterTypeList))]
        public string Type {
            get => "Data Equals Value";
            set => view.ChangeFilterType(value);
        }

        [CategoryAttribute("Required"), DisplayName("Value"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The Regex that the data must match")]
        public string Value {
            get => GetAttribute("value");
            set => SetAttribute("value", value);
        }
    }

    [DisplayName("Data Minimum Length Filter")]
    public class LengthFilter : LoadInjectorGridBase {

        public LengthFilter(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = dataModel.Name;
        }

        [CategoryAttribute("Required"), DisplayName("Data Filter Type"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The type of data filter"), ItemsSource(typeof(FilterTypeList))]
        public string Type {
            get => "Data Minimum Length";
            set => view.ChangeFilterType(value);
        }

        [CategoryAttribute("Required"), DisplayName("Minimum Length"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The Minimum Length of the Data")]
        public int Value {
            get => GetIntAttribute("value");
            set => SetAttribute("value", value);
        }
    }

    [DisplayName("XPath Exists Filter")]
    public class XPExistsFilter : LoadInjectorGridBase {

        public XPExistsFilter(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = dataModel.Name;
        }

        [CategoryAttribute("Required"), DisplayName("Data Filter Type"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The type of data filter"), ItemsSource(typeof(FilterTypeList))]
        public string Type {
            get => "XPath Exists";
            set => view.ChangeFilterType(value);
        }

        [CategoryAttribute("Required"), DisplayName("XPath"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The XPath that must exist for the filter to pass")]
        public string Value {
            get => GetAttribute("xpath");
            set => SetAttribute("xpath", value);
        }
    }

    [DisplayName("Value at XPath Matches RegEx Filter")]
    public class XPMatchesFilter : LoadInjectorGridBase {

        public XPMatchesFilter(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = dataModel.Name;
        }

        [CategoryAttribute("Required"), DisplayName("Data Filter Type"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The type of data filter"), ItemsSource(typeof(FilterTypeList))]
        public string Type {
            get => "XPath Matches";
            set => view.ChangeFilterType(value);
        }

        [CategoryAttribute("Required"), DisplayName("XPath"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The XPath of the data element")]
        public string XPath {
            get => GetAttribute("xpath");
            set => SetAttribute("xpath", value);
        }

        [CategoryAttribute("Required"), DisplayName("Value"), Browsable(true), PropertyOrder(1), DescriptionAttribute("The RegEx that the data at the specified XPath must match")]
        public string Value {
            get => GetAttribute("value");
            set => SetAttribute("value", value);
        }
    }

    [DisplayName("Value at XPath Equals Filter")]
    public class XPEqualsFilter : LoadInjectorGridBase {

        public XPEqualsFilter(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = dataModel.Name;
        }

        [CategoryAttribute("Required"), DisplayName("Data Filter Type"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The type of data filter"), ItemsSource(typeof(FilterTypeList))]
        public string Type {
            get => "XPath Equals";
            set => view.ChangeFilterType(value);
        }

        [CategoryAttribute("Required"), DisplayName("XPath"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The XPath of the data element")]
        public string XPath {
            get => GetAttribute("xpath");
            set => SetAttribute("xpath", value);
        }

        [CategoryAttribute("Required"), DisplayName("Value"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The value at the specified XPath must equal")]
        public string Value {
            get => GetAttribute("value");
            set => SetAttribute("value", value);
        }
    }

    [DisplayName("Date at XPath fall within Offset Filter")]
    public class DateRangeFilter : LoadInjectorGridBase {

        public DateRangeFilter(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = dataModel.Name;
        }

        [CategoryAttribute("Required"), DisplayName("Data Filter Type"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The type of data filter"), ItemsSource(typeof(FilterTypeList))]
        public string Type {
            get => "XPath Date Within Offset";
            set => view.ChangeFilterType(value);
        }

        [CategoryAttribute("Required"), DisplayName("XPath"), PropertyOrder(1), Browsable(true), DescriptionAttribute("The XPath Date Element to test")]
        public string Value {
            get => GetAttribute("xpath");
            set => SetAttribute("xpath", value);
        }

        [CategoryAttribute("Required"), DisplayName("From Offset"), PropertyOrder(1), Browsable(true), DescriptionAttribute("From Date Offset in Days from Now")]
        public int From {
            get => GetIntAttribute("fromOffset");
            set => SetAttribute("fromOffset", value);
        }

        [CategoryAttribute("Required"), DisplayName("To Offset"), PropertyOrder(1), Browsable(true), DescriptionAttribute("To Date Offset in Days from Now")]
        public int To {
            get => GetIntAttribute("toOffset");
            set => SetAttribute("toOffset", value);
        }
    }
}