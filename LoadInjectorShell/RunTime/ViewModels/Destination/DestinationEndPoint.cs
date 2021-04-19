using LoadInjector.Destinations;
using LoadInjector.RunTime.Models;
using System;
using System.Collections.Generic;
using System.Xml;
using static LoadInjector.RunTime.Models.ControllerStatusReport;

namespace LoadInjector.RunTime {

    public class DestinationEndPoint : IDestinationEndPointController {
        private readonly SenderAbstract endPointDestination;
        private readonly NLog.Logger logger;

        public bool OK_TO_RUN;

        public DestinationEndPoint(XmlNode defn, NLog.Logger logger) {
            this.logger = logger;

            string protocol = defn.Attributes["protocol"].Value;
            endPointDestination = Common.Parameters.protocolDictionary[protocol].GetDestinationSender();

            OK_TO_RUN = endPointDestination.Configure(defn, this, logger);
        }

        internal void ClearQueue() {
            endPointDestination.ClearQueue();
        }

        public void Prepare() {
            endPointDestination.Prepare();
        }

        public void Stop() {
            OK_TO_RUN = false;
            endPointDestination.OK_TO_RUN = false;
            endPointDestination.Stop();
        }

        public void ListenReset() {
            OK_TO_RUN = true;
        }

        public void Send(string val, List<Variable> vars) {
            if (endPointDestination.USE_ASYNC_SEND) {
                _ = endPointDestination.SendAsync(val, vars);
            } else {
                endPointDestination.Send(val, vars);
            }
        }

        public string Listen() {
            return endPointDestination.Listen();
        }

        public void ConsoleOutLine(String s) {
            ConsoleOut(s + "\n");
        }

        public void ConsoleOut(String s) {
            ControllerStatusReport controllerStatusReport = new ControllerStatusReport {
                OutputString = s,
                Type = Operation.Console
            };
            // controllerProgress.Report(controllerStatusReport);
        }
    }
}