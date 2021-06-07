using LoadInjectorBase;
using NLog;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LoadInjector.Destinations {

    //Class that implements the sending of the message
    public class DestinationRabbit : DestinationAbstract {
        private string queueName;
        private string connection;
        private string user;
        private string pass;
        private string vhost;
        private int port;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);

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

        public override bool Send(string val, List<Variable> vars) {
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
                            return false;
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
                            return false;
                        }
                    }
                }
            } catch (Exception ex) {
                logger.Error(ex.Message);
                logger.Error(ex.StackTrace);
                return false;
            }
            return true;
        }
    }
}