using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class Mqtt : IDestinationType {
        public string name = "MQTT";
        public string description = "MQTT Publisher";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new MqttPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new MqttPropertyGrid((XmlNode)dataModel, (IView)view);
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
}