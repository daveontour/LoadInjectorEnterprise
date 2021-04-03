using LoadInjector.Common;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("Destination Line Configuration")]
    public class DestinationPropertyGrid : LoadInjectorGridBase {
        private readonly string[] protocolFields = new[] { "QManager", "Channel", "HostName", "Port", "UserName", "UserPass", "Queue", "TCPServerIP", "TCPServerPort", "CloseConnection", "ServerURL", "PostURL", "GetURL", "HTTPLogPath", "MaxRetry", "DestinationFileName", "AppendFile", "FTPURL", "FTPUser", "FTPPass", "FTPSSL", "Template", "SMTPHost", "SMTPPort", "SMTPUser", "SMTPPass", "SMTPSSL", "SMTPAttachment", "SMTPSubj", "SMTPFromUser", "SMTPFromEmail", "SMTPToUser", "SMTPToEmail", "SMTPAttachmentName", "Key", "Topic", "ConsumerGroup", "KafkaServer", "RabbitQueue", "RabbitServer", "RabbitServerPort", "RabbitServerUser", "RabbitServerPass", "RabbitVirtualHost" };

        //protected void DisplayParams(string protocol) {
        //}
        protected string GetProtocol() {
            //DisplayParams(GetAttribute("protocol"));
            return Parameters.protocolToDescription[GetAttribute("protocol")];
        }

        protected void SetProtocol(string value) {
            //DisplayParams(value);

            ////Clear out all previous protocol parameters
            List<string> protoFieldsList = protocolFields.ToList();
            Dictionary<string, string> atts = new Dictionary<string, string>();
            foreach (XmlAttribute att in _node.Attributes) {
                string key = att.Name;
                string v = att.Value;

                atts.Add(key, v);
            }
            _node.Attributes.RemoveAll();

            foreach (XmlNode child in _node.SelectNodes("./header")) {
                _node.RemoveChild(child);
            }

            foreach (string key in atts.Keys) {
                if (protoFieldsList.Contains(key)) {
                    continue;
                }
                SetAttribute(key, atts[key]);
            }

            SetAttribute("protocol", Parameters.descriptionToProtocol[value]);

            view.UpdateParamBindings("XMLText");
            view.UpdateParamBindings("HeaderGridProtocolVisibility");
        }

        public DestinationPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        #region required

        [CategoryAttribute("Line Configuration"), DisplayName("Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("Name of the Destination")]
        public string Name {
            get => GetAttribute("name");
            set {
                SetAttribute("name", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        [Editor(typeof(FileNameSelector), typeof(FileNameSelector))]
        [CategoryAttribute("Line Configuration"), DisplayName("Template File"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The pathname of the template file")]
        public string Template {
            get => GetAttribute("templateFile");
            set => SetAttribute("templateFile", value);
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Line Configuration"), DisplayName("Protocol"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The communications protocol of the destination"), ItemsSource(typeof(ProtocolTypes))]
        public string Protocol {
            get => GetProtocol();
            set {
                SetProtocol(value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
                view.SetProtocolGrid(value);
            }
        }

        #endregion required

        [CategoryAttribute("Execution"), DisplayName("Disabled"), ReadOnly(false), Browsable(true), PropertyOrder(50), DescriptionAttribute("Disable this Destination")]
        public bool Disable {
            get => GetBoolAttribute("disabled");
            set {
                SetAttribute("disabled", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }
    }
}