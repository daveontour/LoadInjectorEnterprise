using System.Xml;

namespace LoadInjector.Filters {
    class FilterContains : MustInitialize<XmlNode>, IQueueFilter {

        private readonly string value;

        public bool Pass(string message) {
            if (value == null) {
                return true;
            }
            return message.Contains(value);
        }

        public FilterContains(XmlNode config) : base(config) {
            value = config.Attributes["value"]?.Value;
        }
    }
}
