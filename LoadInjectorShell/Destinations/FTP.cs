using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class Ftp : IDestinationType {
        public string name = "FTP";
        public string description = "FTP";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new FtpPropertyGrid(dataModel, view);
        }

        public object GetConfigGrid(object dataModel, object view) {
            return new FtpPropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("FTP Protocol Configuration")]
    public class FtpPropertyGrid : LoadInjectorGridBase {

        public FtpPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = "FTP";
        }

        [DisplayName("FTP URL (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(31), DescriptionAttribute("The FTP Destination to send the message to, including filename. The URL can contain tokens which are substituted for the corresponding variable")]
        public string FTPURL {
            get => GetAttribute("ftpURL");
            set => SetAttribute("ftpURL", value);
        }

        [DisplayName("Enable FTP SSL"), ReadOnly(false), Browsable(true), PropertyOrder(33), DescriptionAttribute("Select to use SSL")]
        public bool FTPSSL {
            get => GetBoolDefaultFalseAttribute(_node, "ftpSSL");
            set => SetAttribute("ftpSSL", value);
        }

        [DisplayName("FTP User"), ReadOnly(false), Browsable(true), PropertyOrder(34), DescriptionAttribute("The FTP User")]
        public string FTPUser {
            get => GetAttribute("ftpUser");
            set => SetAttribute("ftpUser", value);
        }

        [DisplayName("FTP Pass"), ReadOnly(false), Browsable(true), PropertyOrder(35), DescriptionAttribute("The FTP Password")]
        public string FTPPass {
            get => GetAttribute("ftpPass");
            set => SetAttribute("ftpPass", value);
        }
    }
}