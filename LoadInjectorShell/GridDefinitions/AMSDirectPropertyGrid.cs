using LoadInjector.Common;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [DisplayName("Direct AMS Updates")]
    public class AmsUpdateModes : IItemsSource {

        public ItemCollection GetValues() {
            var types = new ItemCollection {
                "Web Service", "MS MQ Request Queue", "IBM MQ Request Queue"
            };
            return types;
        }
    }

    [DisplayName("Direct AMS  Line Settings")]
    public class AmsDirectPropertyGrid : LoadInjectorGridBase {

        protected void DisplayParams(string protocol) {
            Hide(new[] { "QManager", "Channel", "HostName", "Port", "UserName", "UserPass", "Queue" });

            switch (protocol) {
                case "MQ":
                    Show(new[] { "QManager", "Channel", "HostName", "Port", "UserName", "UserPass", "Queue" });
                    Hide(new[] { "AMSHost", "AMSTimeout" });
                    break;

                case "MSMQ":
                    Show(new[] { "Queue" });
                    Hide(new[] { "AMSHost", "AMSTimeout" });
                    break;

                case "WS":
                    Show(new[] { "AMSHost", "AMSTimeout" });
                    break;
            }
        }

        protected string GetProtocol() {
            DisplayParams(GetAttribute("protocol"));

            switch (GetAttribute("protocol")) {
                case "MQ":
                    return "IBM MQ Request Queue";

                case "MSMQ":
                    return "MS MQ Request Queue";

                case "WS":
                    return "Web Service";

                default:
                    return "Web Service";
            }
        }

        protected void SetProtocol(string value) {
            switch (value) {
                case "IBM MQ Request Queue":
                    SetAttribute("protocol", "MQ");
                    Hide(new[] { "AMSHost", "AMSTimeout" });
                    break;

                case "MS MQ Request Queue":
                    SetAttribute("protocol", "MSMQ");
                    Hide(new[] { "AMSHost", "AMSTimeout" });
                    break;

                default:
                    SetAttribute("protocol", "WS");
                    Show(new[] { "AMSHost", "AMSTimeout" });
                    break;
            }
            DisplayParams(GetAttribute("protocol"));
            view.UpdateParamBindings("XMLText");
        }

        public AmsDirectPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [CategoryAttribute("Required"), DisplayName("Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("Name of the Destination")]
        public string Name {
            get => GetAttribute("name");
            set {
                SetAttribute("name", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Required"), DisplayName("Update Protocol"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The communications protocol for the AMS Update"), ItemsSource(typeof(AmsUpdateModes))]
        public string Protocol {
            get => GetProtocol();
            set {
                SetProtocol(value);
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("Required"), DisplayName("Queue Manager"), ReadOnly(false), Browsable(true), PropertyOrder(12), DescriptionAttribute("IBM MQ Queue Manager Name")]
        public string QManager {
            get => GetAttribute("queueMgr");
            set => SetAttribute("queueMgr", value);
        }

        [CategoryAttribute("Required"), DisplayName("Queue"), ReadOnly(false), Browsable(true), PropertyOrder(13), DescriptionAttribute("Queue Name")]
        public string Queue {
            get => GetAttribute("queue");
            set => SetAttribute("queue", value);
        }

        [CategoryAttribute("Required"), DisplayName("Channel"), ReadOnly(false), Browsable(true), PropertyOrder(14), DescriptionAttribute("IBM MQ Server Connection Channel name")]
        public string Channel {
            get => GetAttribute("channel");
            set => SetAttribute("channel", value);
        }

        [CategoryAttribute("Required"), DisplayName("Host"), ReadOnly(false), Browsable(true), PropertyOrder(15), DescriptionAttribute("Host name")]
        public string HostName {
            get => GetAttribute("host");
            set => SetAttribute("host", value);
        }

        [CategoryAttribute("Required"), DisplayName("Port"), ReadOnly(false), Browsable(true), PropertyOrder(16), DescriptionAttribute("TCP Port Number of Queue Manager")]
        public string Port {
            get => GetAttribute("port");
            set => SetAttribute("port", value);
        }

        [CategoryAttribute("Optional - Connection"), DisplayName("User Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("MQ User Name - if required")]
        public string UserName {
            get => GetAttribute("username");
            set => SetAttribute("username", value);
        }

        [CategoryAttribute("Optional - Connection"), DisplayName("User Password"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("Connection password for this user - if required")]
        public string UserPass {
            get => GetAttribute("password");
            set => SetAttribute("password", value);
        }

        [Editor(typeof(FolderNameSelector), typeof(FolderNameSelector))]
        [CategoryAttribute("Optional"), DisplayName("Response Logging Directory"), ReadOnly(false), Browsable(true), PropertyOrder(10), DescriptionAttribute("If set, then the local directory file path to write the response message to")]
        public string DestinationDirectory {
            get => GetAttribute("destinationDirectory");
            set => SetAttribute("destinationDirectory", value);
        }

        [CategoryAttribute("Execution"), DisplayName("Disabled"), ReadOnly(false), Browsable(true), PropertyOrder(50), DescriptionAttribute("Disable this Destination")]
        public bool Disable {
            get => GetBoolAttribute("disabled");
            set {
                SetAttribute("disabled", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("AMS Connection"), DisplayName("AMS Host"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("Host name that AMS is running on (if different to source instance)")]
        public string AMSHost {
            get => GetAttribute("amshost");
            set {
                SetAttribute("amshost", value);
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("AMS Connection"), DisplayName("AMS Token"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("Security token to access AMS  (if different to source instance)")]
        public string AMSToken {
            get => GetAttribute("amstoken");
            set {
                SetAttribute("amstoken", value);
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("AMS Connection"), DisplayName("AMS Timeout"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("The timeout for  AMS  (if different to source instance)")]
        public int AMSTimeout {
            get {
                string t = GetAttribute("amstimeout");
                int tt;
                try {
                    tt = int.Parse(t);
                } catch (Exception) {
                    tt = 60;
                }
                return tt;
            }
            set {
                try {
                    SetAttribute("amstimeout", value);
                } catch (Exception) {
                    Debug.WriteLine("Old File Format Warning");
                }
                view.UpdateParamBindings("XMLText");
            }
        }

        [CategoryAttribute("AMS Connection"), DisplayName("IATA Airport Code"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("IATA Airport Code  (if different to source instance)")]
        public string IATA {
            get => GetAttribute("aptcode");
            set {
                SetAttribute("aptcode", value);
                view.UpdateParamBindings("XMLText");
            }
        }
    }
}