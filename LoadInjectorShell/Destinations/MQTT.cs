using LoadInjector.Common;
using LoadInjector.RunTime;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class Mqtt : IDestinationType {
        public string name = "MQTT";
        public string description = "MQTT Publisher";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public SenderAbstract GetDestinationSender() {
            return new DestinationMqtt();
        }

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new MqttPropertyGrid(dataModel, view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("MQTT Protocol Configuration")]
    public class MqttPropertyGrid : LoadInjectorGridBase {

        public MqttPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = "MQTT";
        }

        [RefreshProperties(RefreshProperties.All)]
        [DisplayName("MQTT Server Type"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("The MQTT Server Type"), ItemsSource(typeof(MqttServerTypeList))]
        public string MQTTServerType {
            get {
                if (GetAttribute("protocol") == "MQTT") {
                    string t = GetAttribute("mqttServerType");
                    if (t == "ws") {
                        Hide(new[] { "MQTTServer", "MQTTServerPort" });
                        Show(new[] { "MQTTServerURL" });
                        return "Web Services Server";
                    } else if (t == "tcp") {
                        Show(new[] { "MQTTServer", "MQTTServerPort" });
                        Hide(new[] { "MQTTServerURL" });
                        return "TCP Server";
                    } else {
                        Hide(new[] { "MQTTServer", "MQTTServerPort" });
                        Show(new[] { "MQTTServerURL" });
                        SetAttribute("mqttServerType", "ws");
                        return "Web Services Server";
                    }
                } else {
                    return "Web Services Server";
                }
            }
            set {
                if (value == "Web Services Server") {
                    Hide(new[] { "MQTTServer", "MQTTServerPort" });
                    Show(new[] { "MQTTServerURL" });
                    SetAttribute("mqttServerType", "ws");
                } else if (value == "TCP Server") {
                    Show(new[] { "MQTTServer", "MQTTServerPort" });
                    Hide(new[] { "MQTTServerURL" });
                    SetAttribute("mqttServerType", "tcp");
                }
            }
        }

        [DisplayName("MQTT Server"), ReadOnly(false), Browsable(true), PropertyOrder(13), DescriptionAttribute("The connection string for the MQTT host, eg \"broker.hivemq.com:8000/mqtt\"")]
        public string MQTTServerURL {
            get => GetAttribute("mqttServer");
            set => SetAttribute("mqttServer", value);
        }

        [DisplayName("MQTT Server"), ReadOnly(false), Browsable(true), PropertyOrder(13), DescriptionAttribute("The MQTT Server Host Name")]
        public string MQTTServer {
            get => GetAttribute("mqttServer");
            set => SetAttribute("mqttServer", value);
        }

        [DisplayName("MQTT Server Port"), ReadOnly(false), Browsable(true), PropertyOrder(14), DescriptionAttribute("The MQTT Server Port")]
        public int MQTTServerPort {
            get => GetIntAttribute("mqttPort");
            set => SetAttribute("mqttPort", value);
        }

        [DisplayName("MQTT Topic"), ReadOnly(false), Browsable(true), PropertyOrder(15), DescriptionAttribute("The MQTT Topic")]
        public string MQTTTopic {
            get => GetAttribute("mqttTopic");
            set => SetAttribute("mqttTopic", value);
        }
    }

    public class DestinationMqtt : SenderAbstract {
        private string topic;
        private string mqttServer;
        private string mqttServerURL;
        private string serverType;
        private int mqttServerPort;

        private IMqttClientOptions options;
        private IMqttClient mqttClient;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);

            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();
            try {
                mqttServer = defn.Attributes["mqttServer"].Value;
            } catch (Exception) {
                mqttServer = null;
            }
            try {
                mqttServerURL = defn.Attributes["mqttServerURL"].Value;
            } catch (Exception) {
                mqttServerURL = null;
            }
            try {
                mqttServerPort = int.Parse(defn.Attributes["mqttPort"].Value);
            } catch (Exception) {
                mqttServerPort = -1;
            }
            try {
                topic = defn.Attributes["mqttTopic"].Value;
            } catch (Exception) {
                topic = null;
            }
            try {
                serverType = defn.Attributes["mqttServerType"].Value;
            } catch (Exception) {
                topic = null;
            }

            return true;
        }

        public override void Send(string val, List<Variable> vars) {
            var msg = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(val)
            .WithExactlyOnceQoS()
            .WithRetainFlag()
            .Build();

            //"broker.hivemq.com", 1883)
            if (serverType == "tcp") {
                options = new MqttClientOptionsBuilder().WithTcpServer(mqttServer, mqttServerPort).Build();
            }

            //"broker.hivemq.com:8000/mqtt"
            if (serverType == "ws") {
                options = new MqttClientOptionsBuilder().WithWebSocketServer(mqttServerURL).Build();
            }
            if (!mqttClient.IsConnected) {
                mqttClient.ConnectAsync(options).Wait();
            }

            MqttClientPublishResult result = mqttClient.PublishAsync(msg, CancellationToken.None).Result;

            if (result.ReasonCode != MqttClientPublishReasonCode.Success) {
                Console.WriteLine($"MQTT Send Failure {result.ReasonString}");
            }
        }
    }
}