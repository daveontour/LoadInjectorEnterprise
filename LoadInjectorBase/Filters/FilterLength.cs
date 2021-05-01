using System;
using System.Xml;

namespace LoadInjector.Filters {

    internal class FilterLength : MustInitialize<XmlNode>, IQueueFilter {
        private readonly int length;

        public bool Pass(string message) {
            if (length == 0) {
                return true;
            }
            return message.Length >= length;
        }

        public FilterLength(XmlNode config) : base(config) {
            length = Int32.Parse(config.Attributes["value"]?.Value);
        }
    }
}