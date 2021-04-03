using LoadInjector.Common;
using System;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    public class TriggerSourceTypeListNoNoneOut : IItemsSource {

        public ItemCollection GetValues() {
            var types = new ItemCollection {
                "Scheduled Time of Operation", "External Name"
            };
            return types;
        }
    }

    [DisplayName("Trigger Defintion")]
    public class TriggerPropertyGrid : LoadInjectorGridBase {

        public TriggerPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;

            Hide(new[] { "VariCSVNoFlight", "VariCSVFlight", "VariXMLNoFlight", "VariXMLFlight", "VariExcelNoFlight", "VariExcelFlight", "VariNoFlight", "VariFlight" });

            string flttype;
            try {
                flttype = dataModel.ParentNode.Attributes["flttype"].Value;
            } catch (Exception) {
                flttype = null;
            }
            string dataSource;
            try {
                dataSource = dataModel.ParentNode.Attributes["dataSource"].Value;
            } catch (Exception) {
                dataSource = null;
            }

            if (flttype == null || flttype == "none") {
                if (dataSource == "CSV") {
                    Show("VariCSVNoFlight");
                }
                if (dataSource == "Excel") {
                    Show("VariExcelNoFlight");
                }
                if (dataSource == "XML" || dataSource == "RESTXML") {
                    Show("VariXMLNoFlight");
                }
                if (dataSource == null) {
                    Show("VariNoFlight");
                }
            } else {
                if (dataSource == "CSV") {
                    Show("VariCSVFlight");
                }
                if (dataSource == "Excel") {
                    Show("VariExcelFlight");
                }
                if (dataSource == "XML" || dataSource == "RESTXML") {
                    Show("VariXMLFlight");
                }
                if (dataSource == null) {
                    Show("VariFlight");
                }
            }

            if (_node.ParentNode.Name == "amsdirect") {
                Show("ExternalName");
                Hide("Token");
            } else {
                Hide("ExternalName");
                Show("Token");
            }
        }

        [CategoryAttribute("Required"), DisplayName("Trigger Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The Name of this trigger")]
        public string Name {
            get => GetAttribute("name");
            set => SetAttribute("name", value);
        }

        [CategoryAttribute("Required"), DisplayName("Trigger ID"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The ID of this trigger")]
        public string Token {
            get => GetAttribute("id");
            set => SetAttribute("id", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Trigger Source Type"), PropertyOrder(5), Browsable(true), DescriptionAttribute("Trigger Source Type"), ItemsSource(typeof(TriggerSourceTypeListNoNoneOut))]
        public string TriggerSourceType {
            get {
                string type = GetAttribute("trigType");
                if (type == "sto") {
                    Hide(new[] { "FlifoExternalName", "XMLXPath", "Format" });
                    return "Scheduled Time of Operation";
                }
                if (type == "externalName") {
                    Hide(new[] { "XMLXPath" });
                    Show(new[] { "FlifoExternalName", "Format" });
                    return "External Name";
                }
                if (type == "xpath") {
                    Hide(new[] { "FlifoExternalName", "Format" });
                    Show(new[] { "XMLXPath" });
                    return "XPath";
                }

                return null;
            }
            set {
                if (value == "Scheduled Time of Operation") {
                    SetAttribute("trigType", "sto");
                    Hide(new[] { "FlifoExternalName", "XMLXPath" });
                }
                if (value == "External Name") {
                    SetAttribute("trigType", "externalName");
                    Hide(new[] { "XMLXPath" });
                    Show(new[] { "FlifoExternalName", "Format" });
                }
                if (value == "XPath") {
                    SetAttribute("trigType", "xpath");
                    Hide(new[] { "FlifoExternalName", "Format" });
                    Show(new[] { "XMLXPath" });
                }
            }
        }

        [CategoryAttribute("Required"), DisplayName("AMS External Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The AMS ExternalName of the field to be update with by this variable")]
        public string ExternalName {
            get => GetAttribute("externalName");
            set => SetAttribute("externalName", value);
        }

        [CategoryAttribute("Required"), DisplayName("XML Element XPath"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The XPATH to the element")]
        public string XMLXPath {
            get => GetAttribute("xmlXPath");
            set => SetAttribute("xmlXPath", value);
        }

        [CategoryAttribute("Required"), DisplayName("Property External Name"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The AMS External Name of the flight recoed value to substitute")]
        public string FlifoExternalName {
            get => GetAttribute("externalName");
            set => SetAttribute("externalName", value);
        }

        [Editor(typeof(DoubleUpDown), typeof(DoubleUpDown))]
        [CategoryAttribute("Optional"), DisplayName("Time Offset"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("A offset in minutes to be applied to trigger source")]
        public double Delta {
            get {
                double val = GetDoubleAttributeDefaultZero("delta");
                return val;
            }
            set {
                SetAttribute("delta", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("Optional"), DisplayName("DateTime Format"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The DateTime format of the trigger source")]
        public string Format {
            get => GetAttribute("format");
            set => SetAttribute("format", value);
        }
    }
}