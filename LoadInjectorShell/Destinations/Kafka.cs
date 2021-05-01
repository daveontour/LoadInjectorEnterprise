using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class Kafka : IDestinationType {
        public string name = "KAFKA";
        public string description = "Kafka Publisher";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new KafkaPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new KafkaPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("Kafka Protocol Configuration")]
    public class KafkaPropertyGrid : LoadInjectorGridBase {

        public KafkaPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("Kafka Server and Port"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The Kafka host and port")]
        public string KafkaServer {
            get => GetAttribute("connection");
            set => SetAttribute("connection", value);
        }

        [DisplayName("Kafka Topic (parameterizable)"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The Kafka topic")]
        public string Topic {
            get => GetAttribute("topic");
            set => SetAttribute("topic", value);
        }

        [DisplayName("Kafka Routing Key (parameterizable)"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The Kafka routing key")]
        public string Key {
            get => GetAttribute("key");
            set => SetAttribute("key", value);
        }
    }
}