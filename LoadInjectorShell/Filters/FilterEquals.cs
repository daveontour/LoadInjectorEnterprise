
using System.Xml;

namespace LoadInjector.Filters {
    class FilterEquals : MustInitialize<XmlNode>, IQueueFilter {

        private readonly string value;

        public bool Pass(string message) {
            if (value == null) {
                return true;
            }
            return message.Equals(value);
        }

        public FilterEquals(XmlNode config) : base(config) {
            value = config.Attributes["value"]?.Value;
        }
    }
}
