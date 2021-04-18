using LoadInjector.Common;
using System.Xml;
using LoadInjectorBase;

namespace LoadInjector.Destinations {

    public interface IDestinationType {
        string ProtocolName { get; }
        string ProtocolDescription { get; }

        LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view);

        SenderAbstract GetDestinationSender();
    }
}