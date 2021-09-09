using LoadInjector.RunTime.ViewModels;
using LoadInjectorBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace LoadInjector.RunTime {
    public class LineExecutionController : DestinationControllerAbstract {
        public Stopwatch stopwatch = new Stopwatch();

        public LineExecutionController(XmlNode node, NgExecutionController executionController) : base(node, executionController) {
            if (!ConfigOK) {
                return;
            }
            LineName = config.Attributes["name"].Value;
            LineType = config.Attributes["protocol"].Value;

            try {
                destinationEndPoint = new DestinationEndPoint(config, logger);
                if (!destinationEndPoint.OK_TO_RUN) {
                    destLogger.Error($"Error: End Point Configuration Problem for {name}");
                    ConfigOK = false;
                    return;
                }
            } catch (Exception ex) {
                destLogger.Error(ex, $"Error: No End Point defined for {name}");
                SetOutput("Error: No End Point Defined " + ex.Message);
                ConfigOK = false;
                return;
            }

            try {
                templateFileID = config.Attributes["templateFileID"].Value;
            } catch (Exception ex) {
                if (LineType != "HTTPGET") {
                    destLogger.Error(ex, $"Template File not defined for {name}");
                    SetOutput("Error: No Template File Defined");
                    ConfigOK = false;
                    return;
                }
            }
            try {
                template = executionController.GetFileContent(templateFileID);
            } catch (Exception ex) {
                if (LineType != "HTTPGET") {
                    destLogger.Error(ex, $"Template File {templateFileID} cannot be read for {name}.");
                    SetOutput("Error: Template File Could Not Be Read");
                    return;
                } else {
                    template = "";
                }
            }
            destLogger.Info($"Controller and View Created for {LineName}");
        }

        public new bool PrePrepare() {
            messagesSent = 0;
            messagesFail = 0;
            Report(0, 0, 0);
            return base.PrePrepare();
        }

        public new bool Prepare() {
            messagesSent = 0;
            messagesFail = 0;
            Report(0, 0, 0);
            stopwatch.Stop();
            destinationEndPoint.Prepare();
            return base.Prepare();
        }

        public override void Execute() {
            messagesSent = 0;
            messagesFail = 0;
            Report(0, 0, 0);
            foreach (string trigger in triggerIDs) {
                eventDistributor.AddLineHandler(trigger, this);
            }
            stopwatch.Restart();
            SetOutput("Waiting for next triggering event");
        }

        internal void TriggerHandler(object sender, TriggerFiredEventArgs e) {
            ProcessIteration(e.record).Wait();
        }

        public override async Task<bool> ProcessIteration(Tuple<Dictionary<string, string>> record) {
            string message = (string)template.Clone();

            Dictionary<string, string> dict = record.Item1;

            // Process the data records
            foreach (Variable v in vars) {
                try {
                    if (v.varSubstitionRequired) {
                        v.value = v.GetValue(dict, vars);
                        message = message.Replace(v.token, v.value);
                    } else {
                        v.value = v.GetValue(dict);    //Special veriosn of GetValues which returns the correct field from the supplied values, if it is a CSV field
                        message = message.Replace(v.token, v.value);
                    }
                } catch (ArgumentException) {
                    break;
                } catch (Exception ex) {
                    destLogger.Error(ex, $"Token Substitution Exception");
                }
            }

            logger.Trace($"Message to be sent: \n{message}\n");

            if (destinationEndPoint.Send(message, vars)) {
                messagesSent += 1;
                destLogger.Info($"Destination {LineName}. Messages Sent = {messagesSent}");
            } else {
                messagesFail += 1;
                destLogger.Error($"Destination {LineName}. Messages Failed = {messagesFail}");
            }

            avg = stopwatch.Elapsed.TotalMilliseconds / messagesSent;
            double rate = RoundToSignificantDigits(60000 / avg, 2);

            Report(messagesSent, messagesFail, rate);

            if (saveMessageFile != null) {
                string fullPath = saveMessageFile.Replace(".", $"{messagesSent}.");
                try {
                    File.WriteAllText(fullPath, message);
                } catch (Exception ex) {
                    destLogger.Warn($"Unable to record message to file ${fullPath}. {ex.Message}");
                }
            }

            return true;
        }
    }
}