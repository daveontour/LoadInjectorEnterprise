using LoadInjector.Common;
using LoadInjector.GridDefinitions;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class HttpPost : IDestinationType {
        public string name = "HTTPPOST";
        public string description = "HTTP Post";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new HttpPostPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new HttpPostPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("HTTP Post Protocol Configuration")]
    public class HttpPostPropertyGrid : LoadInjectorGridBase {

        public HttpPostPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("Post URL (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(41), DescriptionAttribute("The full URL of the endpoint to post to. The URL can contain tokens which are substituted for the corresponding variable")]
        public string PostURL {
            get => GetAttribute("postURL");
            set => SetAttribute("postURL", value);
        }

        [Editor(typeof(FolderNameSelector), typeof(FolderNameSelector))]
        [DisplayName("HTTP Log Path"), ReadOnly(false), Browsable(true), PropertyOrder(42), DescriptionAttribute("If not null, then the local *directory* path to write the request and response")]
        public string HTTPLogPath {
            get => GetAttribute("httpLogPath");
            set => SetAttribute("httpLogPath", value);
        }

        [DisplayName("Request Timeout (seconds)"), ReadOnly(false), Browsable(true), PropertyOrder(43), DescriptionAttribute("The request timeout in seconds")]
        public int Timeout {
            get {
                int val = GetIntAttribute("timeout");
                if (val == -1) {
                    SetAttribute("timeout", 5);
                }
                return val;
            }
            set => SetAttribute("timeout", value);
        }

        [DisplayName("Number of Retries"), ReadOnly(false), Browsable(true), PropertyOrder(43), DescriptionAttribute("The number of retries to send the message if there is a failure")]
        public int MaxRetry {
            get => GetIntAttribute("maxRetry");
            set => SetAttribute("maxRetry", value);
        }
    }
}