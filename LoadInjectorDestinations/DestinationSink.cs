using LoadInjectorBase;
using NLog;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class DestinationSink : DestinationAbstract {

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);
            return true;
        }

        public override bool Send(string val, List<Variable> vars) {
            return true;
        }
    }
}