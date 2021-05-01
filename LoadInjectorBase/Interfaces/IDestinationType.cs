using System.Xml;
using LoadInjectorBase;

namespace LoadInjector.Destinations {

    public interface IDestinationType {
        string ProtocolName { get; }
        string ProtocolDescription { get; }

        object GetConfigGrid(object selectedItem, object View);
    }
}