using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class TCPClient : IDestinationType {
        public string name = "TCPCLIENT";
        public string description = "TCP Client";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new TCPClientPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new TCPClientPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("TCP Client Protocol Configuration")]
    public class TCPClientPropertyGrid : LoadInjectorGridBase {

        public TCPClientPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("TCP Server IP"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The IP addres of the TCP Server")]
        public string TCPServerIP {
            get => GetAttribute("tcpServerIP");
            set => SetAttribute("tcpServerIP", value);
        }

        [DisplayName("TCP Server Port"), ReadOnly(false), Browsable(true), PropertyOrder(13), DescriptionAttribute("The Port number of the TCP Server")]
        public string TCPServerPort {
            get => GetAttribute("tcpServerPort");
            set => SetAttribute("tcpServerPort", value);
        }
    }
}