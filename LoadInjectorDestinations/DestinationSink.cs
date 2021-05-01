using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class DestinationSink : SenderAbstract {

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);
            return true;
        }
    }
}