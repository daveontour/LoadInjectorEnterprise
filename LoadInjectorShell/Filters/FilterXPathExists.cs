using System;
using System.Xml;

namespace LoadInjector.Filters {
    class FilterXPathExists : MustInitialize<XmlNode>, IQueueFilter {

        // Filter that evaluates whether the specified XPath elements exists in the message

        private readonly string nodePath;
        public bool Pass(string message) {
            try {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(message);
                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");

                XmlNodeList nodes = doc.SelectNodes(nodePath, ns);
                bool result = nodes.Count > 0;
                return result;
            } catch (Exception) {
                return false;
            }
        }

        public FilterXPathExists(XmlNode config) : base(config) {
            nodePath = config.Attributes["xpath"].Value;
        }
    }
}
