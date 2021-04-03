using Confluent.Kafka;
using LoadInjector.Common;
using LoadInjector.RunTime;
using NLog;
using System;
using System.Collections.Generic;
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

        public SenderAbstract GetDestinationSender() {
            return new DestinationKafka();
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

    public class DestinationKafka : SenderAbstract {
        private string bootStrapServers = "localhost:9092";

        public string Topic { get; set; }
        public string Key { get; set; }

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);

            try {
                bootStrapServers = defn.Attributes["connection"].Value;
            } catch (Exception) {
                return false;
            }
            try {
                Key = defn.Attributes["key"].Value;
            } catch (Exception) {
                Key = null;
            }

            try {
                Topic = defn.Attributes["topic"].Value;
            } catch (Exception) {
                Topic = "my_topic";
            }

            if (Key == null) {
                Key = Topic;
            }

            return true;
        }

        public override void Send(string val, List<Variable> vars) {
            var config = new ProducerConfig { BootstrapServers = bootStrapServers };
            foreach (Variable v in vars) {
                try {
                    Key = Key.Replace(v.token, v.value);
                } catch (Exception) {
                    // NO-OP
                }
                try {
                    Topic = Topic.Replace(v.token, v.value);
                } catch (Exception) {
                    // NO-OP
                }

                using (var p = new ProducerBuilder<string, string>(config).Build()) {
                    try {
                        p.Produce(Topic, new Message<string, string> { Key = Key, Value = val });
                    } catch (Exception e) {
                        logger.Error(e.Message);
                        logger.Error(e);
                        logger.Error($"Unable to deliver to Kafka Server on topic {Topic}");
                    }
                    logger.Info($"Kafka sent message to {Topic}");
                }
            }
        }
    }
}