using LoadInjectorBase;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using NLog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml;

namespace LoadInjector.Destinations
{
    public class DestinationMqtt : DestinationAbstract
    {
        private string topic;
        private string mqttServer;
        private string mqttServerURL;
        private string serverType;
        private int mqttServerPort;

        private IMqttClientOptions options;
        private IMqttClient mqttClient;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log)
        {
            base.Configure(node, cont, log);

            MqttFactory factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();
            try
            {
                mqttServer = defn.Attributes["host"].Value;
            }
            catch (Exception)
            {
                mqttServer = null;
            }
            try
            {
                mqttServerURL = defn.Attributes["host"].Value;
            }
            catch (Exception)
            {
                mqttServerURL = null;
            }
            try
            {
                mqttServerPort = int.Parse(defn.Attributes["port"].Value);
            }
            catch (Exception)
            {
                mqttServerPort = -1;
            }
            try
            {
                topic = defn.Attributes["topic"].Value;
            }
            catch (Exception)
            {
                topic = null;
            }
            try
            {
                serverType = defn.Attributes["mqttServerType"].Value;
            }
            catch (Exception)
            {
                topic = null;
            }

            return true;
        }

        public override string GetDestinationDescription()
        {
            return $"Server URL: {mqttServerURL}, Topic: {topic}";
        }

        public override bool Send(string val, List<Variable> vars)
        {
            MqttApplicationMessage msg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(val)
            .WithExactlyOnceQoS()
            .WithRetainFlag()
            .Build();

            //"broker.hivemq.com", 1883)
            if (serverType == "tcp")
            {
                options = new MqttClientOptionsBuilder().WithTcpServer(mqttServer, mqttServerPort).Build();
            }

            //"broker.hivemq.com:8000/mqtt"
            if (serverType == "ws")
            {
                options = new MqttClientOptionsBuilder().WithWebSocketServer(mqttServerURL).Build();
            }
            if (!mqttClient.IsConnected)
            {
                mqttClient.ConnectAsync(options).Wait();
            }

            MqttClientPublishResult result = mqttClient.PublishAsync(msg, CancellationToken.None).Result;

            if (result.ReasonCode != MqttClientPublishReasonCode.Success)
            {
                Console.WriteLine($"MQTT Send Failure {result.ReasonString}");
                return false;
            }

            return true;
        }
    }
}