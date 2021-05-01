using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class Rabbit : IDestinationType {
        public string name = "RABBITMQ";
        public string description = "Rabbit MQ";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new RabbitPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new RabbitPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    // Class that defines the configuration grid for this type
    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("Rabbit MQ Protocol Configuration")]
    public class RabbitPropertyGrid : LoadInjectorGridBase {

        public RabbitPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("Rabbit Server"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The Rabbit MQ Server")]
        public string RabbitServer {
            get => GetAttribute("connection");
            set => SetAttribute("connection", value);
        }

        [DisplayName("Rabbit Server Port"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The Rabbit MQ Server Port")]
        public int RabbitServerPort {
            get => GetIntAttribute("rabbitPort");
            set => SetAttribute("rabbitPort", value);
        }

        [DisplayName("Rabbit User"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The Rabbit MQ Username")]
        public string RabbitUser {
            get => GetAttribute("rabbitUser");
            set => SetAttribute("rabbitUser", value);
        }

        [DisplayName("Rabbit Password"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The Rabbit MQ User Possword")]
        public string RabbitPass {
            get => GetAttribute("rabbitPass");
            set => SetAttribute("rabbitPass", value);
        }

        [DisplayName("Rabbit Virtual Server"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("The Rabbit Virtual Host. For example \"/\", \"ams\"")]
        public string RabbitVirtualHost {
            get => GetAttribute("rabbitVHost");
            set => SetAttribute("rabbitVHost", value);
        }

        [DisplayName("Rabbit Queue (parameterizable)"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("The Rabbit MQ Queue")]
        public string RabbitQueue {
            get => GetAttribute("queue");
            set => SetAttribute("queue", value);
        }
    }
}