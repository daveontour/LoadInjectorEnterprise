using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class MsSql : IDestinationType {
        public string name = "MSSQL";
        public string description = "MS SQL Client";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new MsSqlPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new MsSqlPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("MSSQL Protocol Configuration")]
    public class MsSqlPropertyGrid : LoadInjectorGridBase {

        public MsSqlPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = "MSSQL";
        }

        [DisplayName("Connection String (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The connection  string to connect to the SQL Server.")]
        public string ConnectionString {
            get => GetAttribute("connStr");
            set => SetAttribute("connStr", value);
        }

        [DisplayName("Show Output Window"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("Show the output in a window")]
        public bool ShowResults {
            get => GetBoolAttribute("showResults");
            set => SetAttribute("showResults", value);
        }
    }
}