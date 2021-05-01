using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class Oracle : IDestinationType {
        public string name = "ORACLE";
        public string description = "ORACLE Database Client";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new OraclePropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new OraclePropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("Oracle Protocol Configuration")]
    public class OraclePropertyGrid : LoadInjectorGridBase {

        public OraclePropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = "MYSQL";
        }

        [DisplayName("Connection String (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The connection  string to connect to the MySQL Server.")]
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