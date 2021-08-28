using LoadInjector.Destinations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LoadInjector.RuntimeCore
{
    /*
     * Class to make the configuration parameters available.
     * The static constructor makes sure the parameters are initialised the first time the
     * class is accessed
     *
     */

    public static class Parameters
    {
        public static readonly Dictionary<string, string> descriptionToProtocol = new Dictionary<string, string>();

        public static readonly int MAXREPORTRATE = InitMaxReportRate();

        public static readonly int PROGRESSEPOCH = InitProgressEpoch();

        public static readonly Dictionary<string, IDestinationType> protocolDescriptionDictionary = new Dictionary<string, IDestinationType>();

        public static readonly Dictionary<string, IDestinationType> protocolDictionary = new Dictionary<string, IDestinationType>();

        public static readonly Dictionary<string, string> protocolToDescription = new Dictionary<string, string>();

        public static readonly int REPORTEPOCH = InitReportEpoch();

        public static string Template
        {
            get
            {
                return ReadResource("LoadInjector.assets.Template.xml");
            }
        }

        static Parameters()
        {
            var type = typeof(IDestinationType);
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(s => s.GetTypes())
                            .Where(p => type.IsAssignableFrom(p));
            foreach (Type t in types)
            {
                if (!t.IsAbstract && !t.IsInterface)
                {
                    IDestinationType dest = (IDestinationType)Activator.CreateInstance(t);
                    protocolDictionary.Add(dest.ProtocolName, dest);
                    protocolDescriptionDictionary.Add(dest.ProtocolDescription, dest);
                    protocolToDescription.Add(dest.ProtocolName, dest.ProtocolDescription);
                    descriptionToProtocol.Add(dest.ProtocolDescription, dest.ProtocolName);
                }
            }
        }

        public static string ReadResource(string name)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static int InitMaxReportRate()
        {
            try
            {
                return int.Parse(ConfigurationManager.AppSettings["MAXREPORTRATE"]);
            }
            catch (Exception)
            {
                return 1000;
            }
        }

        private static int InitProgressEpoch()
        {
            try
            {
                return int.Parse(ConfigurationManager.AppSettings["PROGRESSEPOCH"]);
            }
            catch (Exception)
            {
                return 5;
            }
        }

        private static int InitReportEpoch()
        {
            try
            {
                return int.Parse(ConfigurationManager.AppSettings["REPORTEPOCH"]);
            }
            catch (Exception)
            {
                return 100;
            }
        }
    }
}