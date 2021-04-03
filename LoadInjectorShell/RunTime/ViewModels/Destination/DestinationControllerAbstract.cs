using LoadInjector.Common;
using LoadInjector.RunTime.Models;
using LoadInjector.RunTime.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using static LoadInjector.RunTime.Models.ControllerStatusReport;

namespace LoadInjector.RunTime {

    public abstract class DestinationControllerAbstract {
        public DestinationEndPoint destinationEndPoint;
        public readonly object _locker = new object();
        public XmlNode config;
        protected readonly IProgress<ControllerStatusReport> controllerProgress;
        protected IProgress<ControllerStatusReport> lineProgress;
        public string name;
        public List<string> values = new List<string>();
        public List<string[]> variableCSVValues = new List<string[]>();

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
        public NLog.Logger logger;

        public bool ConfigOK;
        public bool EXECUTE_TEST;
        protected Thread thread;

        public TriggerEventDistributor eventDistributor;

        public string LineName { get; set; }
        public string LineType { get; set; }

        public NgExecutionController executionController;

        protected string saveMessageFile;

        public abstract void Execute();

        public abstract Task<bool> ProcessIteration(Tuple<Dictionary<string, string>, FlightNode> record);

        public void SetLineProgress(IProgress<ControllerStatusReport> lineProgress) {
            this.lineProgress = lineProgress;
        }

        protected DestinationControllerAbstract(XmlNode node, IProgress<ControllerStatusReport> controllerProgress, NgExecutionController executionController) {
            config = node;
            this.controllerProgress = controllerProgress;

            this.executionController = executionController;
            logger = executionController.logger;
            eventDistributor = executionController.eventDistributor;

            try {
                name = config.Attributes["name"].Value;
            } catch (Exception) {
                Console.WriteLine("Error: No Name define <destination>");
                return;
            }

            triggerIDs = SetTriggerIDS();
            saveMessageFile = SetVar("saveMessageFile", null);
            dataSource = SetVar("dataSource", null);
            dataFile = SetVar("dataFile", null);
            excelRowStart = SetVar("excelRowStart", -1);

            amsHost = SetVar("amshost", null);
            amsToken = SetVar("amstoken", null);
            amsAptCode = SetVar("aptcode", null);
            amsTimeout = SetVar("amstimeout", null);

            ConfigOK = true;
            SetOutput($"Configured OK ({config.SelectNodes(".//variable").Count} variables defined)");
        }

        public bool Prepare() {
            // Some preparation for the end point if required.

            vars.Clear();
            foreach (XmlNode variableConfig in config.SelectNodes(".//variable")) {
                Variable var = new Variable(variableConfig);
                if (!var.ConfigOK) {
                    ConfigOK = false;
                    Console.WriteLine("Error in variable config");
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
                ControllerStatusReport report = new ControllerStatusReport() {
                    Sent = messagesSent,
                    Actual = rate,
                    Type = Operation.DestinataionSendReport
                };
                lineProgress?.Report(report);
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
            ControllerStatusReport controllerStatusReport = new ControllerStatusReport {
                OutputString = s,
                Type = Operation.Console
            };
            lineProgress?.Report(controllerStatusReport);
        }

        public void SetRate(double s) {
            ControllerStatusReport controllerStatusReport = new ControllerStatusReport {
                OutputDouble = s,
                Type = Operation.LineRate
            };
            lineProgress?.Report(controllerStatusReport);
        }

        public void Sent(int s) {
            ControllerStatusReport controllerStatusReport = new ControllerStatusReport {
                OutputInt = s,
                Type = Operation.LineSent
            };
            lineProgress?.Report(controllerStatusReport);
        }

        public void SetMsgPerMin(String s) {
            ControllerStatusReport controllerStatusReport = new ControllerStatusReport {
                OutputString = s,
                Type = Operation.LineMsgPerMin
            };
            lineProgress?.Report(controllerStatusReport);
        }

        public void SetConfiguredMsgPerMin(String s) {
            ControllerStatusReport controllerStatusReport = new ControllerStatusReport {
                OutputString = s,
                Type = Operation.LineConfiguredMsgPerMin
            };
            lineProgress?.Report(controllerStatusReport);
        }

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