using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [DisplayName("Database Data Driven Source")]
    public class DataBaseDataDrivenGrid : DataDrivenPropertyBase {

        public DataBaseDataDrivenGrid(XmlNode dataModel, IView view) : base(dataModel, view) {
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
        [CategoryAttribute("Required"), DisplayName("Database Type"), PropertyOrder(2), Browsable(true), DescriptionAttribute("The Database Type"), ItemsSource(typeof(DataBaseList))]
        public string SourceType {
            get {
                string type = GetAttribute("sourceType");

                if (type == "mysql") {
                    return "MySQL";
                }
                if (type == "mssql") {
                    return "MS SQL";
                }
                if (type == "oracle") {
                    return "Oracle";
                }

                return null;
            }
            set {
                if (value == "Oracle") {
                    SetAttribute("sourceType", "oracle");
                }
                if (value == "MySQL") {
                    SetAttribute("sourceType", "mysql");
                }
                if (value == "MS SQL") {
                    SetAttribute("sourceType", "mssql");
                }
            }
        }

        [CategoryAttribute("Required"), DisplayName("Connection String"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The connection string to connect to the database")]
        public string Connection {
            get => GetAttribute("connStr");
            set => SetAttribute("connStr", value);
        }

        [CategoryAttribute("Required"), DisplayName("SQL Command"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The SQL command to retrieve the data")]
        public string SQL {
            get => GetAttribute("sql");
            set => SetAttribute("sql", value);
        }

        [CategoryAttribute("Required"), DisplayName("Time Element"), ReadOnly(false), Browsable(true), PropertyOrder(11), DescriptionAttribute("The Database Element that is the time element")]
        public string TimeElement {
            get => GetAttribute("timeElement");
            set => SetAttribute("timeElement", value);
        }

        [CategoryAttribute("Required"), DisplayName("Time Element Format"), ReadOnly(false), Browsable(true), PropertyOrder(13), DescriptionAttribute("The format of the Database Time Element")]
        public string TimeElementFormat {
            get => GetAttribute("timeElementFormat");
            set => SetAttribute("timeElementFormat", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Relative Time"), ReadOnly(false), Browsable(true), PropertyOrder(14), DescriptionAttribute("The time field is number of seconds relative to the start of the execution")]
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