using Confluent.Kafka;
using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.Destinations
{
    public class DestinationKafka : DestinationAbstract
    {
        private string bootStrapServers = "localhost:9092";

        public string Topic { get; set; }
        public string Key { get; set; }

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log)
        {
            base.Configure(node, cont, log);

            try
            {
                bootStrapServers = defn.Attributes["host"].Value;
            }
            catch (Exception)
            {
                return false;
            }
            try
            {
                Key = defn.Attributes["key"].Value;
            }
            catch (Exception)
            {
                Key = null;
            }

            try
            {
                Topic = defn.Attributes["topic"].Value;
            }
            catch (Exception)
            {
                Topic = "my_topic";
            }

            if (Key == null)
            {
                Key = Topic;
            }

            return true;
        }

        public override string GetDestinationDescription()
        {
            return $"Server: {bootStrapServers}, Key: {Key}, Topic: {Topic}";
        }

        public override bool Send(string val, List<Variable> vars)
        {
            var config = new ProducerConfig { BootstrapServers = bootStrapServers };
            foreach (Variable v in vars)
            {
                try
                {
                    Key = Key.Replace(v.token, v.value);
                }
                catch (Exception)
                {
                    // NO-OP
                }
                try
                {
                    Topic = Topic.Replace(v.token, v.value);
                }
                catch (Exception)
                {
                    // NO-OP
                }

                using (var p = new ProducerBuilder<string, string>(config).Build())
                {
                    try
                    {
                        p.Produce(Topic, new Message<string, string> { Key = Key, Value = val });
                    }
                    catch (Exception e)
                    {
                        logger.Error(e.Message);
                        logger.Error(e);
                        logger.Error($"Unable to deliver to Kafka Server on topic {Topic}");
                        return false;
                    }
                    logger.Info($"Kafka sent message to {Topic}");
                }
            }
            return true;
        }
    }
}