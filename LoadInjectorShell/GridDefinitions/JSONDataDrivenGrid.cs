using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [DisplayName("JSON Data Driven Source")]
    public class JSONDataDrivenGrid : DataDrivenPropertyBase {

        public JSONDataDrivenGrid(XmlNode dataModel, IView view) : base(dataModel, view) {
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
        [CategoryAttribute("Required"), DisplayName("JSON Source Type"), PropertyOrder(2), Browsable(true), DescriptionAttribute("The Source Type"), ItemsSource(typeof(FileOrUrlList))]
        public string SourceType {
            get {
                string type = GetAttribute("sourceType");

                if (type == "file") {
                    Hide(new[] { "JSONRestURL" });
                    Show(new[] { "JSONFile" });
                    return "File";
                }
                if (type == "url") {
                    Hide(new[] { "JSONFile" });
                    Show(new[] { "JSONRestURL" });
                    return "URL";
                }
                return null;
            }
            set {
                if (value == "File") {
                    SetAttribute("sourceType", "file");
                    Hide(new[] { "JSONRestURL" });
                    Show(new[] { "JSONFile" });
                }
                if (value == "URL") {
                    SetAttribute("sourceType", "url");
                    Hide(new[] { "JSONFile" });
                    Show(new[] { "JSONRestURL" });
                }

                view.UpdateParamBindings("HeaderVisibility");
            }
        }

        [Editor(typeof(FileNameSelector), typeof(FileNameSelector))]
        [CategoryAttribute("Required"), DisplayName("JSON Data Source File"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The JSON File containing the test data")]
        public string JSONFile {
            get => GetAttribute("dataFile");
            set => SetAttribute("dataFile", value);
        }

        [CategoryAttribute("Required"), DisplayName("JSON Rest URL"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The URL to retrieve the JSON document")]
        public string JSONRestURL {
            get => GetAttribute("dataRestURL");
            set => SetAttribute("dataRestURL", value);
        }

        [CategoryAttribute("Required"), DisplayName("Repeating Element"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The JSON Element that is the repeating element for each iteration")]
        public string RepeatElement {
            get => GetAttribute("repeatingElement");
            set => SetAttribute("repeatingElement", value);
        }

        [CategoryAttribute("Required"), DisplayName("Time Element"), ReadOnly(false), Browsable(true), PropertyOrder(11), DescriptionAttribute("The JSON Element that is the time element")]
        public string TimeElement {
            get => GetAttribute("timeElement");
            set => SetAttribute("timeElement", value);
        }

        [CategoryAttribute("Required"), DisplayName("Time Element Format"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The format of the JSON Time Element")]
        public string TimeElementFormat {
            get => GetAttribute("timeElementFormat");
            set => SetAttribute("timeElementFormat", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Relative Time"), ReadOnly(false), Browsable(true), PropertyOrder(13), DescriptionAttribute("The time element is number of seconds relative to the start of the execution")]
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