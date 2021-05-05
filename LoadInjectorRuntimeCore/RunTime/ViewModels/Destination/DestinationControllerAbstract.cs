using LoadInjector.RunTime.EngineComponents;
using LoadInjector.RunTime.ViewModels;
using LoadInjector.RuntimeCore;
using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjector.RunTime {

    public abstract class DestinationControllerAbstract {
        public DestinationEndPoint destinationEndPoint;
        public readonly object _locker = new object();
        public XmlNode config;

        //protected IProgress<ControllerStatusReport> lineProgress;
        public string name;

        public List<string> values = new List<string>();
        public List<string[]> variableCSVValues = new List<string[]>();
        public ClientHub clientHub;

        internal bool PrePrepare() {
            return true;
        }

        public int excelRowStart;
        public readonly List<FlightNode> flightPool = new List<FlightNode>();
        public readonly List<Variable> vars = new List<Variable>();
        public int maxMessages;
        public int deferTime;
        public int maxTime;

        public string[] triggerIDs;

        public string amsHost;
        public string amsToken;
        public string amsAptCode;
        public string amsTimeout;

        public double interval;
        public string templateFile;
        public string template;
        public string dataSource;
        public string dataFile;
        public Random rand = new Random();
        public int messagesSent;
        public double avg;
        public static readonly Logger logger = LogManager.GetLogger("consoleLogger");
        public static readonly Logger destLogger = LogManager.GetLogger("destLogger");
        public static readonly Logger sourceLogger = LogManager.GetLogger("sourceLogger");

        public bool ConfigOK;
        public bool EXECUTE_TEST;
        protected Thread thread;

        public TriggerEventDistributor eventDistributor;
        public string executionNodeID;
        public string uuid;

        public string LineName { get; set; }
        public string LineType { get; set; }

        public NgExecutionController executionController;

        protected string saveMessageFile;

        public abstract void Execute();

        public abstract Task<bool> ProcessIteration(Tuple<Dictionary<string, string>, FlightNode> record);

        //public void SetLineProgress(IProgress<ControllerStatusReport> lineProgress) {
        //    this.lineProgress = lineProgress;
        //}

        protected DestinationControllerAbstract(XmlNode node, NgExecutionController executionController) {
            this.config = node;
            this.executionController = executionController;

            this.eventDistributor = executionController.eventDistributor;
            this.clientHub = executionController.clientHub;

            this.executionNodeID = node.Attributes["executionNodeUuid"]?.Value;
            this.uuid = node.Attributes["uuid"]?.Value;

            try {
                name = config.Attributes["name"].Value;
            } catch (Exception) {
                Console.WriteLine("Error: No Name define <destination>");
                return;
            }

            this.triggerIDs = SetTriggerIDS();
            this.saveMessageFile = SetVar("saveMessageFile", null);
            this.dataSource = SetVar("dataSource", null);
            this.dataFile = SetVar("dataFile", null);
            this.excelRowStart = SetVar("excelRowStart", -1);

            this.amsHost = SetVar("amshost", null);
            this.amsToken = SetVar("amstoken", null);
            this.amsAptCode = SetVar("aptcode", null);
            this.amsTimeout = SetVar("amstimeout", null);

            this.ConfigOK = true;
            //SetOutput($"Configured OK ({config.SelectNodes(".//variable").Count} variables defined)");
        }

        public bool Prepare() {
            // Some preparation for the end point if required.

            vars.Clear();
            foreach (XmlNode variableConfig in config.SelectNodes(".//variable")) {
                Variable var = new Variable(variableConfig);
                if (!var.ConfigOK) {
                    this.ConfigOK = false;
                    Debug.WriteLine("Error in variable config");
                    SetOutput("Error: Variable Configuration Error - " + var.ErrorMsg);

                    return false;
                } else {
                    vars.Add(var);
                }
            }
            return true;
        }

        public void Start() {
            EXECUTE_TEST = true;
            thread = new Thread(Execute) {
                IsBackground = false,
                Priority = ThreadPriority.AboveNormal
            };
            thread.Start();
        }

        public void Stop() {
            lock (_locker) {
                EXECUTE_TEST = false;
                destinationEndPoint?.Stop();
                Monitor.Pulse(_locker);
            }
            Report(messagesSent, 0);
            SetOutput("Test Run Complete");
        }

        public void Report(int messagesSent, double rate) {
            if (rate < Parameters.MAXREPORTRATE || messagesSent % Parameters.REPORTEPOCH == 0) {
                clientHub.SendDestinationReport(executionNodeID, uuid, messagesSent, rate);
            }
        }

        #region Helpers

        /*
         * Helper Methods
         */

        public double RoundToSignificantDigits(double d, int digits) {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, digits);

            double dd = d * scale;
            double di = Math.Round(dd);
            return di / scale;
        }

        public void SetOutput(String s) {
            clientHub.SetDestinationOutput(executionNodeID, uuid, s);
        }

        public void ConsoleMsg(String s) {
            clientHub.ConsoleMsg(executionNodeID, uuid, s);
        }

        //public void SetRate(double s) {
        //    clientHub.SetDestinationRate(executionNodeID, uuid, s);
        //}

        //public void Sent(int s) {
        //    clientHub.SetDestinationSent(executionNodeID, uuid, s);
        //}

        public bool SetVar(string attrib, bool defaultValue) {
            bool value;
            try {
                if (config.Attributes[attrib] == null) return defaultValue;
                value = bool.Parse(config.Attributes[attrib].Value);
            } catch (Exception) {
                value = defaultValue;
            }
            return value;
        }

        public string SetVar(string attrib, string defaultValue) {
            string value;
            try {
                if (config.Attributes[attrib] == null) return defaultValue;
                value = config.Attributes[attrib].Value;
            } catch (Exception) {
                value = defaultValue;
            }
            return value;
        }

        public double SetVar(string attrib, double defaultValue) {
            double value;
            try {
                if (config.Attributes[attrib] == null) return defaultValue;
                value = double.Parse(config.Attributes[attrib].Value);
            } catch (Exception) {
                value = defaultValue;
            }
            return value;
        }

        public int SetVar(string attrib, int defaultValue) {
            int value;
            try {
                if (config.Attributes[attrib] == null) return defaultValue;
                value = int.Parse(config.Attributes[attrib].Value);
            } catch (Exception) {
                value = defaultValue;
            }
            return value;
        }

        public string[] SetTriggerIDS() {
            List<string> ids = new List<string>();
            foreach (XmlNode sub in config.SelectNodes("./subscribed")) {
                ids.Add(sub.InnerText);
            }
            return ids.ToArray();
        }

        #endregion Helpers
    }
}