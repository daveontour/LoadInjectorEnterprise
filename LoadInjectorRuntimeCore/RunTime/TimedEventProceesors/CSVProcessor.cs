using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml;

namespace LoadInjector.RunTime.EngineComponents
{
    public class CsvProcessor
    {
        private readonly string csvFile;
        private readonly string csvRestURL;
        private readonly string csvSourceType;
        private readonly List<string> fieldsInUse;
        private readonly XmlNode node;

        public CsvProcessor()
        {
        }

        public CsvProcessor(string csvFile, string csvRestURL, string csvSourceType, List<string> fieldsInUse, XmlNode node)
        {
            this.csvFile = csvFile;
            this.csvRestURL = csvRestURL;
            this.csvSourceType = csvSourceType;
            this.fieldsInUse = fieldsInUse;
            this.node = node;
        }

        public List<Dictionary<string, string>> GetRecords()
        {
            List<Dictionary<String, String>> records = new List<Dictionary<String, String>>();

            bool hasHeaders;

            try
            {
                var v = node.Attributes["csvHasHeaders"]?.Value;
                hasHeaders = (v == null) || bool.Parse(v);
            }
            catch (Exception)
            {
                hasHeaders = true;
            }

            TextFieldParser parser;
            if (csvSourceType.Contains("File"))
            {
                parser = new TextFieldParser(csvFile);
            }
            else
            {
                parser = new TextFieldParser(GetCSVDocument(csvRestURL, node));
            }

            try
            {
                using (parser)
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    bool firstLine = true;
                    while (!parser.EndOfData)
                    {
                        if (firstLine && hasHeaders)
                        {
                            firstLine = false;
                            parser.ReadFields();
                            continue;
                        }

                        firstLine = false;

                        Dictionary<string, string> record = new Dictionary<string, string>();

                        try
                        {
                            string[] fields = parser.ReadFields();

                            foreach (string field in fieldsInUse)
                            {
                                if (record.ContainsKey(field))
                                {
                                    continue;
                                }

                                int index = int.Parse(field);

                                record.Add(field, fields[index]);
                            }

                            records.Add(record);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Error reading CSV Source File {csvFile}");
            }
            return records;
        }

        private string GetCSVDocument(string url, XmlNode config)
        {
            try
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        try
                        {
                            foreach (XmlNode header in config.SelectNodes(".//header"))
                            {
                                string key = header.Attributes["name"]?.Value;
                                string value = header.InnerText;
                                client.DefaultRequestHeaders.Add(key, value);
                            }
                            string res = client.GetStringAsync(url).Result;
                            return res;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error retrieving CSV Document: {ex.Message}");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving CSV Document: {ex.Message}");
                    return null;
                }
            }
            catch (Exception)
            {
                // NO-OP
            }

            return null;
        }
    }
}