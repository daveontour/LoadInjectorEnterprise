using LoadInjector.Common;
using LoadInjector.RunTime;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class Sink : IDestinationType {
        public string name = "SINK";
        public string description = "SINK";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public SenderAbstract GetDestinationSender() {
            return new SinkText();
        }

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return null;
        }
    }

    internal class SinkText : SenderAbstract {

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);
            return true;
        }

        public override void Send(String mess, List<Variable> vars) {
        }

        public override void Prepare() {
            base.Prepare();
        }

        public override void Stop() {
            base.Stop();
        }
    }
}