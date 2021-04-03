using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [DisplayName("Excel Data Driven Source")]
    public class ExcelDataDrivenGrid : DataDrivenPropertyBase {

        public ExcelDataDrivenGrid(XmlNode dataModel, IView view) : base(dataModel, view) {
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

        [Editor(typeof(FileNameSelector), typeof(FileNameSelector))]
        [CategoryAttribute("Required"), DisplayName("Excel Data Source File"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The Excel File containing the test data")]
        public string ExcelFile {
            get => GetAttribute("dataFile");
            set => SetAttribute("dataFile", value);
        }

        [CategoryAttribute("Required"), DisplayName("Excel Sheet"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The Excel sheet in the workbook to use")]
        public string ExcelSheet {
            get => GetAttribute("excelSheet");
            set => SetAttribute("excelSheet", value);
        }

        [CategoryAttribute("Required"), DisplayName("Excel Time Column"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The Excel Column containing the time of the trigger")]
        public string ExcelTimeCol {
            get => GetAttribute("timeElement");
            set => SetAttribute("timeElement", value);
        }

        [CategoryAttribute("Required"), DisplayName("Starting Excel Row"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The row in the Excel sheet that the data starts on")]
        public string ExcelRowStart {
            get => GetAttribute("excelRowStart");
            set => SetAttribute("excelRowStart", value);
        }

        [CategoryAttribute("Required"), DisplayName("Ending Excel Row"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The row in the Excel sheet that the data ends on")]
        public string ExcelRowEnd {
            get => GetAttribute("excelRowEnd");
            set => SetAttribute("excelRowEnd", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Relative Time"), ReadOnly(false), Browsable(true), PropertyOrder(9), DescriptionAttribute("The time column is number of seconds relative to the start of the execution")]
        public bool RelativeTime {
            get {
                bool v = GetBoolDefaultFalseAttribute(_node, "relativeTime");
                if (v) {
                    Hide("ExcelTimeColFormat");
                } else {
                    Show("ExcelTimeColFormat");
                }
                return v;
            }
            set {
                if (value) {
                    Hide("ExcelTimeColFormat");
                } else {
                    Show("ExcelTimeColFormat");
                }
                SetAttribute("relativeTime", value);
            }
        }

        [CategoryAttribute("Required"), DisplayName("Excel Time Column Format"), ReadOnly(false), Browsable(true), PropertyOrder(8), DescriptionAttribute("The Excel Time Column format")]
        public string ExcelTimeColFormat {
            get => GetAttribute("timeElementFormat");
            set => SetAttribute("timeElementFormat", value);
        }
    }
}