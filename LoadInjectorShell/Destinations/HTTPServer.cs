using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class HttpServer : IDestinationType {
        public string name = "HTTPSERVER";
        public string description = "HTTP Server";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new HttpServerPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new HttpServerPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("HTTP Server Protocol Configuration")]
    public class HttpServerPropertyGrid : LoadInjectorGridBase {

        public HttpServerPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("HTTP Server URL"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The URL on the local server for clients to call. (including port and server name)")]
        public string ServerURL {
            get => GetAttribute("serverURL");
            set => SetAttribute("serverURL", value);
        }
    }
}