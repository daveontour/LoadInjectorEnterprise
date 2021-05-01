using LoadInjector.Common;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class Sink : IDestinationType {
        public string name = "SINK";
        public string description = "SINK";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return null;
        }

        public object GetConfigGrid(object dataModel, object view) {
            return null;
        }
    }
}