using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [DisplayName("CSV Data Driven Source")]
    public class CsvDataDrivenGrid : DataDrivenPropertyBase {

        public CsvDataDrivenGrid(XmlNode dataModel, IView view) : base(dataModel, view) {
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
        [CategoryAttribute("Required"), DisplayName("CSV Source Type"), PropertyOrder(2), Browsable(true), DescriptionAttribute("The Source Type"), ItemsSource(typeof(FileOrUrlList))]
        public string SourceType {
            get {
                string type = GetAttribute("sourceType");

                if (type == null) {
                    SetAttribute("sourceType", "file");
                    type = "file";
                }

                if (type == "file") {
                    Hide(new[] { "CSVRestURL" });
                    Show(new[] { "CSVFile" });
                    return "File";
                }
                if (type == "url") {
                    Hide(new[] { "CSVFile" });
                    Show(new[] { "CSVRestURL" });
                    return "URL";
                }

                return null;
            }
            set {
                if (value == "File") {
                    SetAttribute("sourceType", "file");
                    Hide(new[] { "CSVRestURL" });
                    Show(new[] { "CSVFile" });
                }
                if (value == "URL") {
                    SetAttribute("sourceType", "url");
                    Hide(new[] { "CSVFile" });
                    Show(new[] { "CSVRestURL" });
                }
                view.UpdateParamBindings("HeaderVisibility");
            }
        }

        [Editor(typeof(FileNameSelector), typeof(FileNameSelector))]
        [CategoryAttribute("Required"), DisplayName("CSV Data Source File"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The CSV File containing the test data")]
        public string CSVFile {
            get => GetAttribute("dataFile");
            set => SetAttribute("dataFile", value);
        }

        [CategoryAttribute("Required"), DisplayName("CSV Rest URL"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The URL to retrieve the XML document")]
        public string CSVRestURL {
            get => GetAttribute("csvRestURL");
            set => SetAttribute("csvRestURL", value);
        }

        [CategoryAttribute("Required"), DisplayName("CSV Time Field Number"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The 0 based index of the field in the CSV file containing the time to trigger the event on")]
        public int CSVTimeFieldNumber {
            get {
                int val = GetIntAttribute("timeElement");
                if (val < 0) {
                    val = 0;
                    CSVTimeFieldNumber = 0;
                }
                return val;
            }
            set {
                if (value < 0) {
                    value = 0;
                }
                SetAttribute("timeElement", value);
            }
        }

        [CategoryAttribute("Required"), DisplayName("CSV Time Field Format"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The time format of the time field")]
        public string CSVTimeFieldFormat {
            get => GetAttribute("timeElementFormat");
            set => SetAttribute("timeElementFormat", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Relative Time"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The time field is number of seconds relative to the start of the execution")]
        public bool RelativeTime {
            get {
                bool v = GetBoolDefaultFalseAttribute(_node, "relativeTime");
                if (v) {
                    Hide("CSVTimeFieldFormat");
                } else {
                    Show("CSVTimeFieldFormat");
                }
                return v;
            }
            set {
                if (value) {
                    Hide("CSVTimeFieldFormat");
                } else {
                    Show("CSVTimeFieldFormat");
                }
                SetAttribute("relativeTime", value);
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("CSV Header Line"), ReadOnly(false), Browsable(true), PropertyOrder(8), DescriptionAttribute("The first row of the source is a header line")]
        public bool CSVHeaders {
            get => GetBoolDefaultTrueAttribute("csvHasHeaders");
            set => SetAttributeAbs("csvHasHeaders", value);
        }
    }
}