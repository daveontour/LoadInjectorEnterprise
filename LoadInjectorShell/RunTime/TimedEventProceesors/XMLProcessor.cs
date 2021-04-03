using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml;

namespace LoadInjector.RunTime.EngineComponents {

    public class XmlProcessor {

        public List<Dictionary<string, string>> GetRecords(string xmlFile, string xmlRestURL, string repeatingElement, string sourceType, List<string> xmlElementsInUse, bool xmlToString, XmlNode config) {
            List<Dictionary<string, string>> records = new List<Dictionary<string, string>>();

            XmlDocument xmldoc = GetXmlDocument(xmlFile, xmlRestURL, sourceType, config);

            try {
                foreach (XmlNode node in xmldoc.SelectNodes(repeatingElement)) {
                    Dictionary<string, string> record = new Dictionary<string, string>();

                    foreach (string element in xmlElementsInUse) {
                        if (record.ContainsKey(element)) {
                            record.Remove(element);
                        }
                        if (xmlToString) {
                            record.Add(element, node.SelectSingleNode(element).OuterXml);
                        } else {
                            string data = node.SelectSingleNode(element)?.InnerText;
                            record.Add(element, data);
                        }
                    }

                    records.Add(record);
                }
            } catch (Exception e) {
                Console.WriteLine($"XML XPath Problem {e.Message}");
            }

            return records;
        }

        private XmlDocument GetXmlDocument(string xmlFile, string xmlRestURL, string sourceType, XmlNode config) {
            XmlDocument xmldoc;
            if (sourceType == "file") {
                try {
                    xmldoc = new XmlDocument();
                    xmldoc.Load(xmlFile);
                    return xmldoc;
                } catch (Exception) {
                    Console.WriteLine($"Error reading XML File {xmlFile}");
                    return null;
                }
            }

            if (sourceType == "url") {
                try {
                    string url = xmlRestURL;
                    try {
                        using (var client = new HttpClient()) {
                            try {
                                foreach (XmlNode header in config.SelectNodes(".//header")) {
                                    string key = header.Attributes["name"]?.Value;
                                    string value = header.InnerText;
                                    client.DefaultRequestHeaders.Add(key, value);
                                }
                                string res = client.GetStringAsync(url).Result;

                                try {
                                    xmldoc = new XmlDocument();
                                    xmldoc.Load(res);
                                    return xmldoc;
                                } catch (Exception ex) {
                                    Console.WriteLine($"Error retrieving XML Document: {ex.Message}");
                                    return null;
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"Error retrieving XML Document: {ex.Message}");
                                return null;
                            }
                        }
                    } catch (Exception ex) {
                        Console.WriteLine($"Error retrieving XML Document: {ex.Message}");
                        return null;
                    }
                } catch (Exception) {
                    // NO-OP
                }

                return null;
            }

            return null;
        }
    }
}