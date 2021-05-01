using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    public class Msmq : IDestinationType {
        public string name = "MSMQ";
        public string description = "Microsoft MQ";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new MsmqPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new MsmqPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("Microsoft MQ Protocol Configuration")]
    public class MsmqPropertyGrid : LoadInjectorGridBase {

        public MsmqPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("Queue"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("Queue Name")]
        public string Queue {
            get => GetAttribute("queue");
            set => SetAttribute("queue", value);
        }
    }
}