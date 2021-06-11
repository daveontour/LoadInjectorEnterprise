using NLog;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjectorBase {

    public abstract class DestinationAbstract {
        public bool OK_TO_RUN = false;
        public bool USE_ASYNC_SEND = false;
        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
        public static readonly Logger destLogger = LogManager.GetLogger("destLogger");

        public XmlNode defn;
        public IDestinationEndPointController controller;

        public virtual bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            this.defn = node;
            this.controller = cont;

            return true;
        }

        public abstract string GetDestinationDescription();

        public virtual bool Send(string val, List<Variable> vars) {
            return false;
        }

        public virtual async Task<bool> SendAsync(string message, List<Variable> vars) {
            return true;
        }

        public virtual string Listen() {
            return null;
        }

        public virtual void Stop() {
        }

        public virtual void Prepare() {
        }

        public virtual void ClearQueue() {
        }
    }
}