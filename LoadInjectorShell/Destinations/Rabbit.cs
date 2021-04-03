using LoadInjector.Common;
using LoadInjector.RunTime;
using NLog;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class Rabbit : IDestinationType {
        public string name = "RABBITMQ";
        public string description = "Rabbit MQ";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public SenderAbstract GetDestinationSender() {
            return new DestinationRabbit();
        }

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new RabbitPropertyGrid(dataModel, view);
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

    //Class that implements the sending of the message
    public class DestinationRabbit : SenderAbstract {
        private string queueName;
        private string connection;
        private string user;
        private string pass;
        private string vhost;
        private int port;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);

            try {
                connection = defn.Attributes["connection"].Value;
            } catch (Exception) {
                return false;
            }
            try {
                queueName = defn.Attributes["queue"].Value;
            } catch (Exception) {
                queueName = null;
            }

            try {
                user = defn.Attributes["rabbitUser"].Value;
            } catch (Exception) {
                user = "guest";
            }
            try {
                pass = defn.Attributes["rabbitPass"].Value;
            } catch (Exception) {
                pass = "guest";
            }
            try {
                vhost = defn.Attributes["rabbitVHost"].Value;
            } catch (Exception) {
                vhost = "/";
            }
            try {
                port = int.Parse(defn.Attributes["rabbitPort"].Value);
            } catch (Exception) {
                port = 5672;
            }

            return true;
        }

        public override void Send(string val, List<Variable> vars) {
            foreach (Variable v in vars) {
                try {
                    queueName = queueName.Replace(v.token, v.value);
                } catch (Exception) {
                    // NO-OP
                }
            }

            try {
                var factory = new ConnectionFactory {
                    UserName = user,
                    Password = pass,
                    VirtualHost = vhost,
                    HostName = connection,
                    Port = port
                };

                using (var conn = factory.CreateConnection()) {
                    using (var channel = conn.CreateModel()) {
                        try {
                            channel.QueueDeclare(queue: queueName,
                                                                      durable: true,
                                                                      exclusive: false,
                                                                      autoDelete: false,
                                                                      arguments: null);
                        } catch (Exception ex) {
                            logger.Error(ex.Message);
                            logger.Error(ex.StackTrace);
                            return;
                        }
                        var body = Encoding.UTF8.GetBytes(val);

                        try {
                            channel.BasicPublish(exchange: "",
                                                 routingKey: queueName,
                                                 basicProperties: null,
                                                 body: body);
                        } catch (Exception ex) {
                            logger.Error(ex.Message);
                            logger.Error(ex.StackTrace);
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex.Message);
                logger.Error(ex.StackTrace);
            }
        }
    }
}