using LoadInjectorBase.Interfaces;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace LoadInjectorBase
{
    /*
     * Supplies the variables that will be substituted in for the defined tokens or external name
     */

    public class Variable
    {
        public string token;
        public string externalName;

        private readonly string type;
        private readonly string sub;
        private readonly string format;
        private readonly bool dtRelative;
        private readonly int lowerOffset;
        private readonly int upperOffset;

        private int lowerLimit;
        private int upperLimit;

        private readonly Dictionary<string, string> subDict = new Dictionary<string, string>();

        private readonly bool abortOnListEnd;

        private int normalInt;
        private double normalDouble;
        private readonly double stdDev;

        public bool varSubstitionRequired;

        private readonly XmlNode config;

        // Stores the current value for type="sequence"
        private int seqNum;

        // Random number generator for random selections
        private static readonly Random rand = new Random();

        // List of supplied values, or values populated from a CSV file
        private readonly List<string> values = new List<string>();

        private readonly int listLength;

        private readonly List<string> fileEntries = new List<string>();

        // Used to hold the current index for type=csvField or type=valueSequence
        private int seq;

        private readonly int delta;
        private readonly string srcDir;

        // Holds value for type=fixed
        private readonly string fixedValue;

        public bool ConfigOK;
        public string ErrorMsg;

        public string value;
        private readonly int digits;

        private readonly string field;
        private readonly bool variableLookup;
        private readonly string lookupSource;
        private string dataFile;
        private readonly string excelLookupSheet;
        private readonly string excelKeyColumn;
        private readonly string excelValueColumn;
        private readonly int csvKeyField;
        private readonly int csvValueField;
        private readonly string xpath;

        public Variable(XmlNode config)
        {
            this.config = config;

            try
            {
                type = config.Attributes["type"].Value;
            }
            catch (Exception)
            {
                Console.WriteLine("Variable defined without a \"type\" attribute");
                ConfigOK = false;
                return;
            }

            variableLookup = SetVar("variableLookup", false);
            if (variableLookup)
            {
                lookupSource = SetVar("lookupSource", null);
            }

            dataFile = SetVar("dataFile", null);

            token = SetVar("token", null);
            externalName = SetVar("externalName", null);
            field = SetVar("field", null);
            excelLookupSheet = SetVar("excelLookupSheet", null);
            excelKeyColumn = SetVar("excelKeyColumn", null);
            excelValueColumn = SetVar("excelValueColumn", null);
            csvKeyField = SetVar("csvKeyField", -1);
            csvValueField = SetVar("csvValueField", -1);

            if (type == "intRange")
            {
                try
                {
                    lowerLimit = int.Parse(config.Attributes["lowerLimit"].Value);
                }
                catch (Exception)
                {
                    if (config.Attributes["lowerLimit"] != null && config.Attributes["lowerLimit"].Value.StartsWith("@@"))
                    {
                        varSubstitionRequired = true;
                        subDict.Add("lowerLimit", config.Attributes["lowerLimit"].Value);
                    }
                    else
                    {
                        lowerLimit = 0;
                    }
                }
                digits = SetVar("digits", -1);
                try
                {
                    upperLimit = int.Parse(config.Attributes["upperLimit"].Value);
                }
                catch (Exception)
                {
                    if (config.Attributes["upperLimit"] != null && config.Attributes["upperLimit"].Value.StartsWith("@@"))
                    {
                        varSubstitionRequired = true;
                        subDict.Add("upperLimit", config.Attributes["upperLimit"].Value);
                    }
                    else
                    {
                        upperLimit = 0;
                    }
                }
            }

            if (type == "intgaussian")
            {
                try
                {
                    normalInt = int.Parse(config.Attributes["mean"].Value);
                }
                catch (Exception)
                {
                    if (config.Attributes["mean"] != null && config.Attributes["mean"].Value.StartsWith("@@"))
                    {
                        varSubstitionRequired = true;
                        subDict.Add("mean", config.Attributes["mean"].Value);
                    }
                    else
                    {
                        normalInt = 0;
                    }
                }
                stdDev = SetVar("stdDev", 1.0);
                digits = SetVar("digits", -1);
            }

            if (type == "doublegaussian")
            {
                try
                {
                    normalDouble = double.Parse(config.Attributes["mean"].Value);
                }
                catch (Exception)
                {
                    if (config.Attributes["mean"] != null && config.Attributes["mean"].Value.StartsWith("@@"))
                    {
                        varSubstitionRequired = true;
                        subDict.Add("mean", config.Attributes["mean"].Value);
                    }
                    else
                    {
                        normalDouble = 0.0;
                    }
                }
                stdDev = SetVar("stdDev", 1.0);
            }

            if (token == null && externalName == null)
            {
                ErrorMsg = "Variable defined without a token or externalName value defined";
                ConfigOK = false;
                return;
            }

            try
            {
                if (config.ParentNode.Attributes["stopOnDataEnd"] != null)
                {
                    abortOnListEnd = bool.Parse(config.ParentNode.Attributes["stopOnDataEnd"].Value);
                }
                else
                {
                    abortOnListEnd = false;
                }
            }
            catch (Exception)
            {
                abortOnListEnd = false;
            }

            if (type == "flightInfo")
            {
                xpath = SetVar("xpath", null);
                sub = SetVar("sub", null);
            }

            if (type == "sequence")
            {
                seqNum = SetVar("seed", 1);
                digits = SetVar("digits", -1);
            }

            if (type == "datetime")
            {
                lowerOffset = SetVar("lowerOffset", 0);
                upperOffset = SetVar("upperOffset", 0);
                format = SetVar("format", "yyyy-MM-ddTHH:mm:ss.fffK");
                dtRelative = SetVar("relative", false);
                delta = SetVar("delta", 0);
            }

            if (type == "timestamp")
            {
                format = SetVar("format", "yyyy-MM-ddTHH:mm:ss.fffK");
            }

            if (type == "fixed")
            {
                fixedValue = SetVar("fixedValue", "");
            }

            if (type == "value" || type == "valueSequence")
            {
                XElement xElem = XElement.Load(config.CreateNavigator().ReadSubtree());
                IEnumerable<XElement> valueDefn = from value in xElem.Descendants("value") select value;
                foreach (XElement v in valueDefn)
                {
                    values.Add(v.Value);
                }
                listLength = values.Count;
            }

            if (type == "file")
            {
                try
                {
                    srcDir = config.Attributes["srcDir"]?.Value;
                    if (srcDir == null)
                    {
                        Console.WriteLine("File type defined without a \"srcDir\" attribute");
                        return;
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("File type defined without a \"srcDir\" attribute");
                    return;
                }

                if (!Directory.Exists(srcDir))
                {
                    Console.WriteLine($"Source Directory {srcDir} not valid ");
                    return;
                }

                string fileFilter = "*.*";

                try
                {
                    fileFilter = config.Attributes["fileFilter"].Value;
                }
                catch (Exception)
                {
                    fileFilter = "*.*";
                }

                Regex regex = FindFilesPatternToRegex.Convert(fileFilter);

                foreach (string file in Directory.GetFiles(srcDir))
                {
                    if (File.Exists(file))
                    {
                        if (regex.IsMatch(file))
                        {
                            fileEntries.Add(file);
                        }
                    }
                }

                listLength = fileEntries.Count;
            }

            if (type == "csvField")
            {
                field = SetVar("csvField", null);
            }

            if (type == "excelField")
            {
                field = SetVar("excelColumn", null);
            }

            if (type == "xmlElement")
            {
                field = SetVar("element", null);
            }
            if (type == "jsonElement")
            {
                field = SetVar("element", null);
            }

            if (type == "dbField")
            {
                field = SetVar("field", null);
            }
            ConfigOK = true;
        }

        public string GetValue(Dictionary<string, string> dict, List<Variable> vars = null)
        {
            if (type == "csvField"
                || type == "jsonElement"
                || type == "xmlElement"
                || type == "excelField"
                || type == "dbField"
                || type == "excelCol")
            {
                return dict[field];
            }
            else
            {
                return GetValue(vars);
            }
        }

        private string GetValue(List<Variable> vars = null)
        {
            subDict.TryGetValue("lowerLimit", out string ll);
            subDict.TryGetValue("upperLimit", out string ul);
            subDict.TryGetValue("normalInt", out string ni);
            subDict.TryGetValue("normalDouble", out string nd);

            if (ll != null)
            {
                string toke = ll.Substring(2);
                foreach (Variable v in vars)
                {
                    if (v.token == toke)
                    {
                        try
                        {
                            lowerLimit = int.Parse(v.value);
                        }
                        catch (Exception)
                        {
                            lowerLimit = Int32.MinValue;
                        }
                        break;
                    }
                }
            }
            if (ul != null)
            {
                string toke = ul.Substring(2);
                foreach (Variable v in vars)
                {
                    if (v.token == toke)
                    {
                        try
                        {
                            upperLimit = int.Parse(v.value);
                        }
                        catch (Exception)
                        {
                            upperLimit = Int32.MaxValue;
                        }
                        break;
                    }
                }
            }
            if (ni != null)
            {
                string toke = ni.Substring(2);
                foreach (Variable v in vars)
                {
                    if (v.token == toke)
                    {
                        try
                        {
                            normalInt = int.Parse(v.value);
                        }
                        catch (Exception)
                        {
                            normalInt = 0;
                        }
                        break;
                    }
                }
            }
            if (nd != null)
            {
                string toke = nd.Substring(2);
                foreach (Variable v in vars)
                {
                    if (v.token == toke)
                    {
                        try
                        {
                            normalDouble = int.Parse(v.value);
                        }
                        catch (Exception)
                        {
                            normalDouble = 0.0;
                        }
                        break;
                    }
                }
            }
            if (type == "intRange")
            {
                if (digits == -1)
                {
                    return ProcessValue(rand.Next(lowerLimit, upperLimit).ToString());
                }

                string digitsStr = rand.Next(lowerLimit, upperLimit).ToString().ToString();
                while (digitsStr.Length < digits)
                {
                    digitsStr = "0" + digitsStr;
                }

                return ProcessValue(digitsStr);
            }

            if (type == "sequence")
            {
                if (digits == -1)
                {
                    seqNum++;
                    return ProcessValue(seqNum.ToString());
                }

                string digitsStr = seqNum.ToString();
                while (digitsStr.Length < digits)
                {
                    digitsStr = "0" + digitsStr;
                }

                seqNum++;
                return ProcessValue(digitsStr);
            }

            if (type == "intgaussian")
            {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                double randNormal =
                             normalInt + stdDev * randStdNormal; //random normal(mean,stdDev^2)

                string rtn = Convert.ToInt32(randNormal).ToString();

                if (digits == -1)
                {
                    return ProcessValue(rtn);
                }

                while (rtn.Length < digits)
                {
                    rtn = "0" + rtn;
                }

                return ProcessValue(rtn);
            }
            if (type == "doublegaussian")
            {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                double randNormal =
                             normalDouble + stdDev * randStdNormal; //random normal(mean,stdDev^2)

                return ProcessValue(randNormal.ToString(CultureInfo.CurrentCulture));
            }

            if (type == "uuid")
            {
                return ProcessValue(Guid.NewGuid().ToString());
            }

            if (type == "datetime")
            {
                int offset = rand.Next(lowerOffset, upperOffset);
                if (dtRelative)
                {
                    return ProcessValue(DateTime.Now.AddMinutes(offset).AddMinutes(delta).ToString(format));
                }
                else
                {
                    return ProcessValue(DateTime.Now.ToString(format));
                }
            }

            if (type == "fixed")
            {
                return ProcessValue(fixedValue);
            }

            if (type == "timestamp")
            {
                return ProcessValue(DateTime.Now.ToString(format));
            }

            if (type == "value")
            {
                return ProcessValue(values[rand.Next(0, values.Count)]);
            }

            if (type == "valueSequence")
            {
                return ProcessValue(values[seq++ % values.Count]);
            }

            if (type == "file")
            {
                if (seq >= listLength && abortOnListEnd)
                {
                    throw new ArgumentException("Ran out of iteration data");
                }
                return ProcessValue(File.ReadAllText(fileEntries[seq++ % listLength]));
            }

            return null;
        }

        private string GetValue()
        {
            if (type == "intRange")
            {
                if (digits == -1)
                {
                    return ProcessValue(rand.Next(lowerLimit, upperLimit).ToString());
                }

                string digitsStr = rand.Next(lowerLimit, upperLimit).ToString().ToString();
                while (digitsStr.Length < digits)
                {
                    digitsStr = "0" + digitsStr;
                }

                return ProcessValue(digitsStr);
            }

            if (type == "sequence")
            {
                if (digits == -1)
                {
                    seqNum++;
                    return ProcessValue(seqNum.ToString());
                }

                string digitsStr = seqNum.ToString();
                while (digitsStr.Length < digits)
                {
                    digitsStr = "0" + digitsStr;
                }

                seqNum++;
                return ProcessValue(digitsStr);
            }

            if (type == "intgaussian")
            {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                double randNormal =
                             normalInt + stdDev * randStdNormal; //random normal(mean,stdDev^2)

                string rtn = Convert.ToInt32(randNormal).ToString();

                if (digits == -1)
                {
                    return ProcessValue(rtn);
                }

                while (rtn.Length < digits)
                {
                    rtn = "0" + rtn;
                }

                return ProcessValue(rtn);
            }
            if (type == "doublegaussian")
            {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                             Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                double randNormal =
                             normalDouble + stdDev * randStdNormal; //random normal(mean,stdDev^2)

                return ProcessValue(randNormal.ToString(CultureInfo.CurrentCulture));
            }

            if (type == "uuid")
            {
                return ProcessValue(Guid.NewGuid().ToString());
            }

            if (type == "datetime")
            {
                int offset = rand.Next(lowerOffset, upperOffset);
                return ProcessValue(DateTime.Now.AddMinutes(offset).AddMinutes(delta).ToString(format));
            }

            if (type == "fixed")
            {
                return ProcessValue(fixedValue);
            }

            if (type == "timestamp")
            {
                return ProcessValue(DateTime.Now.ToString(format));
            }

            if (type == "value")
            {
                return ProcessValue(values[rand.Next(0, values.Count)]);
            }

            if (type == "file")
            {
                if (seq >= listLength && abortOnListEnd)
                {
                    throw new ArgumentException("Ran out of iteration data");
                }
                return ProcessValue(File.ReadAllText(fileEntries[seq++ % listLength]));
            }

            return null;
        }

        private bool SetVar(string attrib, bool defaultValue)
        {
            bool value;
            try
            {
                if (config.Attributes[attrib] == null) return defaultValue;
                value = bool.Parse(config.Attributes[attrib].Value);
            }
            catch (Exception)
            {
                value = defaultValue;
            }
            return value;
        }

        private string SetVar(string attrib, string defaultValue)
        {
            string value;
            try
            {
                if (config.Attributes[attrib] == null) return defaultValue;
                value = config.Attributes[attrib].Value;
            }
            catch (Exception)
            {
                value = defaultValue;
            }
            return value;
        }

        private double SetVar(string attrib, double defaultValue)
        {
            double value;
            try
            {
                if (config.Attributes[attrib] == null) return defaultValue;
                value = double.Parse(config.Attributes[attrib].Value);
            }
            catch (Exception)
            {
                value = defaultValue;
            }
            return value;
        }

        private int SetVar(string attrib, int defaultValue)
        {
            int value;
            try
            {
                if (config.Attributes[attrib] == null) return defaultValue;
                value = int.Parse(config.Attributes[attrib].Value);
            }
            catch (Exception)
            {
                value = defaultValue;
            }
            return value;
        }

        private string ProcessValue(string value)
        {
            //Returns the lookup value if the variable has been configured to use a lookup, otherwise, just return the value;

            if (!variableLookup)
            {
                return value;
            }
            dataFile = SetVar("dataFile", null);

            if (lookupSource == "csv")
            {
                return LookupCSV(value);
            }
            if (lookupSource == "excel")
            {
                return LookupExcel(value);
            }
            return value;
        }

        private string LookupExcel(string value)
        {
            IExcelProcessor xlProcessor = null;

            try
            {
                var type = typeof(IExcelProcessor);

                IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(s => s.GetTypes())
                                .Where(p => type.IsAssignableFrom(p));
                foreach (Type t in types)
                {
                    if (!t.IsAbstract && !t.IsInterface)
                    {
                        xlProcessor = (IExcelProcessor)Activator.CreateInstance(t);
                    }
                }

                if (xlProcessor == null)
                {
                    return value;
                }

                return xlProcessor.Lookup(value, dataFile, excelLookupSheet, excelKeyColumn, excelValueColumn);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return value;
            }
        }

        private string LookupCSV(string value)
        {
            if (csvKeyField == -1 || csvValueField == -1)
            {
                return value;
            }
            try
            {
                using (TextFieldParser parser = new TextFieldParser(dataFile))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    while (!parser.EndOfData)
                    {
                        try
                        {
                            string[] fields = parser.ReadFields();
                            if (fields[csvKeyField] == value)
                            {
                                return fields[csvValueField];
                            }
                        }
                        catch
                        {
                            // Do nothing, try the next line
                        }
                    }

                    return value;
                }
            }
            catch (Exception)
            {
                Console.WriteLine($"Error reading CSV Lookup File {dataFile}");
                return value;
            }
        }

        internal static class FindFilesPatternToRegex
        {
            private static readonly Regex HasQuestionMarkRegEx = new Regex(@"\?", RegexOptions.Compiled);
            private static readonly Regex IllegalCharactersRegex = new Regex("[" + @"\/:<>|" + "\"]", RegexOptions.Compiled);
            private static readonly Regex CatchExtentionRegex = new Regex(@"^\s*.+\.([^\.]+)\s*$", RegexOptions.Compiled);
            private static readonly string NonDotCharacters = @"[^.]*";

            public static Regex Convert(string pattern)
            {
                if (pattern == null)
                {
                    throw new ArgumentNullException();
                }
                pattern = pattern.Trim();
                if (pattern.Length == 0)
                {
                    throw new ArgumentException("Pattern is empty.");
                }
                if (IllegalCharactersRegex.IsMatch(pattern))
                {
                    throw new ArgumentException("Pattern contains illegal characters.");
                }
                bool hasExtension = CatchExtentionRegex.IsMatch(pattern);
                bool matchExact = false;
                if (HasQuestionMarkRegEx.IsMatch(pattern))
                {
                    matchExact = true;
                }
                else if (hasExtension)
                {
                    matchExact = CatchExtentionRegex.Match(pattern).Groups[1].Length != 3;
                }
                string regexString = Regex.Escape(pattern);
                regexString = "^" + Regex.Replace(regexString, @"\\\*", ".*");
                regexString = Regex.Replace(regexString, @"\\\?", ".");
                if (!matchExact && hasExtension)
                {
                    regexString += NonDotCharacters;
                }
                regexString += "$";
                Regex regex = new Regex(regexString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                return regex;
            }
        }
    }
}