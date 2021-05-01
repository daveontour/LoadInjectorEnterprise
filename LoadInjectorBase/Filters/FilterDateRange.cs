using System;
using System.Xml;

namespace LoadInjector.Filters {
    class FilterDateRange : MustInitialize<XmlNode>, IQueueFilter {

        // Filters out messages that do not fall withing the specified window
        // xpath specifies the date under test in YYYY-MM-DD format
        // The fromOffset and toOffset are the number of days relative to NOW() (can be negative)

        private readonly string nodePath;
        private readonly int fromOffset;
        private readonly int toOffset;

        public bool Pass(string message) {
            try {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(message);
                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");

                XmlNode node = doc.SelectSingleNode(nodePath, ns);

                DateTime check = DateTime.Parse(node.InnerText);
                DateTime from = DateTime.Now.AddDays(fromOffset);
                from = new DateTime(from.Year, from.Month, from.Day, 0, 0, 0);
                DateTime to = DateTime.Now.AddDays(toOffset);
                to = new DateTime(to.Year, to.Month, to.Day, 23, 59, 59);

                return from <= check && check <= to;

            } catch (Exception) {
                // Could occur if the document is not XML or does not have the configured path and data in the correct format.
                return false;
            }
        }

        public FilterDateRange(XmlNode config) : base(config) {
            fromOffset = Int32.Parse(config.Attributes["fromOffset"].Value);
            toOffset = Int32.Parse(config.Attributes["toOffset"].Value);
            nodePath = config.Attributes["xpath"].Value;
        }
    }
}
