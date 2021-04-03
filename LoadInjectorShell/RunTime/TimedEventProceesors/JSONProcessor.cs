using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Xml;

namespace LoadInjector.RunTime.EngineComponents {

    public class JsonProcessor {

        public List<Dictionary<string, string>> GetRecords(string jsonFile, string jsonRestURL, string repeatingElement, string sourceType, List<string> jsonFieldInUse, XmlNode config) {
            List<Dictionary<String, String>> records = new List<Dictionary<String, String>>();

            string json;

            try {
                if (sourceType == "file") {
                    json = File.ReadAllText(jsonFile);
                } else {
                    json = GetJSONDocument(jsonRestURL, config);
                }
            } catch (Exception ex) {
                Console.WriteLine($"Retriveving raw JSON Problem {ex.Message}");
                return records;
            }

            try {
                foreach (JToken token in JToken.Parse(json).SelectTokens(repeatingElement)) {
                    Dictionary<string, string> record = new Dictionary<string, string>();

                    foreach (string element in jsonFieldInUse) {
                        if (record.ContainsKey(element)) {
                            record.Remove(element);
                        }
                        string val = (string)token.SelectToken(element);
                        record.Add(element, val);
                    }

                    records.Add(record);
                }
            } catch (Exception e) {
                Console.WriteLine($"JSON XPath Problem {e.Message}");
            }

            return records;
        }

        private string GetJSONDocument(string url, XmlNode config) {
            try {
                using (var client = new HttpClient()) {
                    try {
                        foreach (XmlNode header in config.SelectNodes(".//header")) {
                            string key = header.Attributes["name"]?.Value;
                            string value = header.InnerText;
                            client.DefaultRequestHeaders.Add(key, value);
                        }
                        string res = client.GetStringAsync(url).Result;
                        return res;
                    } catch (Exception ex) {
                        Console.WriteLine($"Error retrieving JSON Document.{ex.Message}");
                        return null;
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error retrieving JSON Document: {ex.Message}");
                return null;
            }
        }
    }
}