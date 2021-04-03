using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [DisplayName("XML Data Driven Source")]
    public class XMLDataDrivenGrid : DataDrivenPropertyBase {

        public XMLDataDrivenGrid(XmlNode dataModel, IView view) : base(dataModel, view) {
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
        [CategoryAttribute("Required"), DisplayName("XML Source Type"), PropertyOrder(2), Browsable(true), DescriptionAttribute("The Source Type"), ItemsSource(typeof(FileOrUrlList))]
        public string SourceType {
            get {
                string type = GetAttribute("sourceType");

                if (type == "file") {
                    Hide(new[] { "XMLRestURL" });
                    Show(new[] { "XMLFile" });
                    return "File";
                }
                if (type == "url") {
                    Hide(new[] { "XMLFile" });
                    Show(new[] { "XMLRestURL" });
                    return "URL";
                }
                return null;
            }
            set {
                if (value == "File") {
                    SetAttribute("sourceType", "file");
                    Hide(new[] { "XMLRestURL" });
                    Show(new[] { "XMLFile" });
                }
                if (value == "URL") {
                    SetAttribute("sourceType", "url");
                    Hide(new[] { "XMLFile" });
                    Show(new[] { "XMLRestURL" });
                }

                view.UpdateParamBindings("HeaderVisibility");
            }
        }

        [Editor(typeof(FileNameSelector), typeof(FileNameSelector))]
        [CategoryAttribute("Required"), DisplayName("XML Data Source File"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The XML File containing the test data")]
        public string XMLFile {
            get => GetAttribute("dataFile");
            set => SetAttribute("dataFile", value);
        }

        [CategoryAttribute("Required"), DisplayName("XML Rest URL"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The URL to retrieve the XML document")]
        public string XMLRestURL {
            get => GetAttribute("dataRestURL");
            set => SetAttribute("dataRestURL", value);
        }

        [CategoryAttribute("Required"), DisplayName("Repeating Element"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The XML Element that is the repeating element for each iteration")]
        public string RepeatElement {
            get => GetAttribute("repeatingElement");
            set => SetAttribute("repeatingElement", value);
        }

        [CategoryAttribute("Required"), DisplayName("Time Element"), ReadOnly(false), Browsable(true), PropertyOrder(11), DescriptionAttribute("The XML Element that is the time element")]
        public string TimeElement {
            get => GetAttribute("timeElement");
            set => SetAttribute("timeElement", value);
        }

        [CategoryAttribute("Required"), DisplayName("Time Element Format"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The format of the XML Time Element")]
        public string TimeElementFormat {
            get => GetAttribute("timeElementFormat");
            set => SetAttribute("timeElementFormat", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Relative Time"), ReadOnly(false), Browsable(true), PropertyOrder(14), DescriptionAttribute("The time element is number of seconds relative to the start of the execution")]
        public bool RelativeTime {
            get {
                bool v = GetBoolDefaultFalseAttribute(_node, "relativeTime");
                if (v) {
                    Hide("TimeElementFormat");
                } else {
                    Show("TimeElementFormat");
                }
                return v;
            }
            set {
                if (value) {
                    Hide("TimeElementFormat");
                } else {
                    Show("TimeElementFormat");
                }
                SetAttribute("relativeTime", value);
            }
        }
    }
}