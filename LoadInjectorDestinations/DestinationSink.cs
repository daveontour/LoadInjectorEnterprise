using LoadInjectorBase;
using NLog;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class DestinationSink : DestinationAbstract {

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);
            return true;
        }
    }
}