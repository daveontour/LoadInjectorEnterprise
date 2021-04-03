using System.Xml;

namespace LoadInjector.Filters {

    internal class FilterBool : MustInitialize<XmlNode>, IQueueFilter {
        // Simple Filter that return the boolean value set, regardless of message content
        // (used for testing Expressions)

        private readonly bool value = true;

        public bool Pass(string message) {
            return value;
        }

        public FilterBool(XmlNode config) : base(config) {
            value = bool.Parse(config.SelectSingleNode("./value").InnerText);
        }
    }
}