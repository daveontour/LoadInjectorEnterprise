using LoadInjector.Destinations;
using LoadInjectorBase;
using LoadInjectorDestinations;
using System;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.RunTime {

    public class DestinationEndPoint : IDestinationEndPointController {
        private readonly DestinationAbstract endPointDestination;
        private readonly NLog.Logger logger;

        public bool OK_TO_RUN;

        public DestinationEndPoint(XmlNode defn, NLog.Logger logger) {
            this.logger = logger;

            string protocol = defn.Attributes["protocol"].Value;
            endPointDestination = new DestinationFactory().GetSender(protocol);

            OK_TO_RUN = endPointDestination.Configure(defn, this, logger);
        }

        internal void ClearQueue() {
            endPointDestination.ClearQueue();
        }

        public void Prepare() {
            endPointDestination.Prepare();
        }

        public void Stop() {
            OK_TO_RUN = false;
            endPointDestination.OK_TO_RUN = false;
            endPointDestination.Stop();
        }

        public void ListenReset() {
            OK_TO_RUN = true;
        }

        public bool Send(string val, List<Variable> vars) {
            if (endPointDestination.USE_ASYNC_SEND) {
                return endPointDestination.SendAsync(val, vars).Result;
            } else {
                return endPointDestination.Send(val, vars);
            }
        }

        public string Listen() {
            return endPointDestination.Listen();
        }
    }
}