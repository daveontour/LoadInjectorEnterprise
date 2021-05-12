using LoadInjector.Common;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class TextWindow : IDestinationType {
        public string name = "TEXT";
        public string description = "Text Window";

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