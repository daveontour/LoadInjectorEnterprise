using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjectorBase {

    public abstract class SenderAbstract {
        public bool OK_TO_RUN = false;
        public bool USE_ASYNC_SEND = false;
        public Logger logger;
        public XmlNode defn;
        public IDestinationEndPointController controller;

        public virtual bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            this.defn = defn;
            this.logger = logger;
            this.controller = controller;

            return true;
        }

        public virtual void Send(string val, List<Variable> vars) {
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

    public interface IDestinationEndPointController {
    }
}