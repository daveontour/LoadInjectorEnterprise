using System;
using System.Text.RegularExpressions;
using System.Xml;

namespace LoadInjector.Filters {

    internal class FilterXPathEquals : MustInitialize<XmlNode>, IQueueFilter {
        private readonly string nodePath;
        private readonly bool equals;
        private readonly bool matches;
        private readonly string value;

        public bool Pass(string message) {
            try {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(message);
                XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                ns.AddNamespace("ams", "http://www.sita.aero/ams6-xml-api-datatypes");

                XmlNode node = doc.SelectSingleNode(nodePath, ns);
                string nodeValue = node.InnerText;

                if (equals) {
                    return (nodeValue == value);
                }

                if (matches) {
                    Regex reg = new Regex(value, RegexOptions.Compiled);
                    Match match = reg.Match(nodeValue);
                    return match.Success;
                }
            } catch (Exception) {
                return false;
            }

            return false;
        }

        public FilterXPathEquals(XmlNode config) : base(config) {
            equals = config.Name == "xpequals";
            matches = config.Name == "xpmatches";

            nodePath = config.Attributes["xpath"]?.Value;
            value = config.Attributes["value"]?.Value;
        }
    }
}