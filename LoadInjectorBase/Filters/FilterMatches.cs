using System;
using System.Text.RegularExpressions;
using System.Xml;

namespace LoadInjector.Filters {
    class FilterMatches : MustInitialize<XmlNode>, IQueueFilter {

        private readonly string value;

        public bool Pass(string message) {
            try {
                Regex reg = new Regex(value, RegexOptions.Compiled);
                Match match = reg.Match(message);
                return match.Success;
            } catch (Exception) {
                return false;
            }
        }

        public FilterMatches(XmlNode config) : base(config) {
            value = config.Attributes["value"]?.Value;
        }
    }
}
