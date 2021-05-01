using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    public class MQ : IDestinationType {
        private readonly string name = "MQ";
        private readonly string description = "IBM MQ";
        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new MQPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new MQPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("IBM MQ Protocol Configuration")]
    public class MQPropertyGrid : LoadInjectorGridBase {

        public MQPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("Host"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("Host name")]
        public string HostName {
            get => GetAttribute("host");
            set => SetAttribute("host", value);
        }

        [DisplayName("Port"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("TCP Port Number of Queue Manager")]
        public string Port {
            get => GetAttribute("port");
            set => SetAttribute("port", value);
        }

        [DisplayName("Queue"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("Queue Name")]
        public string Queue {
            get => GetAttribute("queue");
            set => SetAttribute("queue", value);
        }

        [DisplayName("Queue Manager"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("IBM MQ Queue Manager Name")]
        public string QManager {
            get => GetAttribute("queueMgr");
            set => SetAttribute("queueMgr", value);
        }

        [DisplayName("Channel"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("IBM MQ Server Connection Channel name")]
        public string Channel {
            get => GetAttribute("channel");
            set => SetAttribute("channel", value);
        }

        [DisplayName("Message Put Timeout"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("Timeout for the message put")]
        public int PutTimeout {
            get => GetIntAttribute("putTimeout");
            set => SetAttribute("putTimeout", value);
        }

        [DisplayName("Use Send Locking"), ReadOnly(false), Browsable(true), PropertyOrder(7), DescriptionAttribute("Serialization lock on sending messages - required for high  volumes")]
        public bool SendLocking {
            get => GetBoolAttribute("useSendLocking");
            set => SetAttribute("useSendLocking", value);
        }

        [DisplayName("User Name"), ReadOnly(false), Browsable(true), PropertyOrder(16), DescriptionAttribute("MQ User Name - if rquired for the connection")]
        public string UserName {
            get => GetAttribute("username");
            set => SetAttribute("username", value);
        }

        [DisplayName("User Password"), ReadOnly(false), Browsable(true), PropertyOrder(17), DescriptionAttribute("Connection password for this user - if required for the connection")]
        public string UserPass {
            get => GetAttribute("password");
            set => SetAttribute("password", value);
        }
    }
}