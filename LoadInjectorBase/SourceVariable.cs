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

    public class SourceVariable
    {
        public string token;

        private readonly string type;
        private readonly string sub;
        private readonly string format;
        private readonly bool dtRelative;
        private readonly int lowerOffset;
        private readonly int upperOffset;

        private int lowerLimit;
        private int upperLimit;

        private readonly Dictionary<string, string> subDict = new Dictionary<string, string>();

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

        public SourceVariable(XmlNode config)
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

            token = SetVar("token", null);
            field = SetVar("field", null);

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
                    normalInt = int.Parse(config.Attributes["normalInt"].Value);
                }
                catch (Exception)
                {
                    if (config.Attributes["normalInt"] != null && config.Attributes["normalInt"].Value.StartsWith("@@"))
                    {
                        varSubstitionRequired = true;
                        subDict.Add("normalInt", config.Attributes["normalInt"].Value);
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
                    normalDouble = double.Parse(config.Attributes["normalDouble"].Value);
                }
                catch (Exception)
                {
                    if (config.Attributes["normalDouble"] != null && config.Attributes["normalDouble"].Value.StartsWith("@@"))
                    {
                        varSubstitionRequired = true;
                        subDict.Add("normalDouble", config.Attributes["normalDouble"].Value);
                    }
                    else
                    {
                        normalDouble = 0.0;
                    }
                }
                stdDev = SetVar("stdDev", 1.0);
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

            ConfigOK = true;
        }

        public string GetValue()
        {
            if (type == "intRange")
            {
                return rand.Next(lowerLimit, upperLimit).ToString();
            }
            if (type == "intgaussian")
            {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                double randNormal = normalInt + stdDev * randStdNormal; //random normal(mean,stdDev^2)
                return Convert.ToInt32(randNormal).ToString();
            }
            if (type == "doublegaussian")
            {
                double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
                double u2 = 1.0 - rand.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
                double randNormal = normalDouble + stdDev * randStdNormal; //random normal(mean,stdDev^2)
                return randNormal.ToString(CultureInfo.CurrentCulture);
            }

            if (type == "fixed")
            {
                return fixedValue;
            }

            if (type == "timestamp")
            {
                return DateTime.Now.ToString(format);
            }

            if (type == "datetime")
            {
                int off = rand.Next(lowerLimit, upperLimit);
                DateTime now = DateTime.Now.AddMinutes(off);
                return now.ToString(format);
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
    }
}