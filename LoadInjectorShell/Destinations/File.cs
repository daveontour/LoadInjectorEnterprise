using LoadInjector.Common;
using System;
using System.ComponentModel;
using System.Linq;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class FileDestination : IDestinationType {
        private readonly string name = "FILE";
        private readonly string description = "File";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public string SenderClassName => throw new NotImplementedException();

        public object GetConfigGrid(object dataModel, object view) {
            return new FilePropertyGrid((XmlNode)dataModel, (IView)view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("File Protocol Configuration")]
    public class FilePropertyGrid : LoadInjectorGridBase {

        public FilePropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("Destination Filename (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(31), DescriptionAttribute("The full pathname of the file to write to. The pathname/filename can contain tokens which are substituted for the corresponding variable")]
        public string DestinationFileName {
            get => GetAttribute("destinationFile");
            set => SetAttribute("destinationFile", value);
        }

        [DisplayName("Append to Destination File"), ReadOnly(false), Browsable(true), PropertyOrder(32), DescriptionAttribute("Rather than creating a new file for each message, append the new message to the file")]
        public bool AppendFile {
            get => GetBoolDefaultFalseAttribute(_node, "appendFile");
            set => SetAttribute("appendFile", value);
        }
    }
}