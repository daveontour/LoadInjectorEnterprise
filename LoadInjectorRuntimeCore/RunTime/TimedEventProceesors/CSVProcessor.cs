using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.RunTime.EngineComponents
{
    public class CsvProcessor : ProcessorBase
    {
        private readonly string csvFile;
        private readonly string csvRestURL;
        private readonly string csvSourceType;
        private readonly List<string> fieldsInUse;
        private readonly XmlNode node;
        private readonly string postBodyText;

        public CsvProcessor()
        {
        }

        public CsvProcessor(string csvFile, string postBodyText, string csvRestURL, string csvSourceType, List<string> fieldsInUse, XmlNode node)
        {
            this.csvFile = csvFile;
            this.csvRestURL = csvRestURL;
            this.csvSourceType = csvSourceType;
            this.fieldsInUse = fieldsInUse;
            this.node = node;
            this.postBodyText = postBodyText;
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
            else if (csvSourceType.Contains("Post"))
            {
                parser = new TextFieldParser(GetDocumentFromPostSource(csvRestURL, postBodyText, node).Result);
            }
            else
            {
                parser = new TextFieldParser(GetDocumentFromGetSource(csvRestURL, node));
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
    }
}