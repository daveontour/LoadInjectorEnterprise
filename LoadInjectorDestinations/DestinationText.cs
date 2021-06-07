using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class DestinationText : DestinationAbstract {

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);
            return true;
        }

        public override bool Send(String val, List<Variable> vars) {
            destLogger.Info($"\nOutput Message=======>\n{val}\n<=======Output Message");
            return true;
        }
    }
}