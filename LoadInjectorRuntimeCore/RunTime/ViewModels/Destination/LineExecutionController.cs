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
                    Console.WriteLine($"Error: End Point Configuration Problem for {name}");
                    ConfigOK = false;
                    return;
                }
            } catch (Exception) {
                Console.WriteLine($"Error: No End Point defined for {name}");
                SetOutput("Error: No End Point Defined");
                ConfigOK = false;
                return;
            }

            try {
                templateFile = config.Attributes["templateFile"].Value;
            } catch (Exception) {
                if (LineType != "HTTPGET") {
                    Console.WriteLine($"Template File not defined for {name}");
                    SetOutput("Error: No Template File Defined");
                    ConfigOK = false;
                    return;
                }
            }
            try {
                template = File.ReadAllText(templateFile);
            } catch (Exception ex) {
                if (LineType != "HTTPGET") {
                    Console.WriteLine($"Template File {templateFile} cannot be read for {name}. {ex.Message}");
                    SetOutput("Error: Template File Could Not Be Read");
                    return;
                } else {
                    template = "";
                }
            }
            Console.WriteLine($"Controller and View Created for {LineName}");
        }

        public new bool PrePrepare() {
            messagesSent = 0;
            Report(0, 0);
            return base.PrePrepare();
        }

        public new bool Prepare() {
            messagesSent = 0;
            Report(0, 0);
            stopwatch.Stop();
            destinationEndPoint.Prepare();
            return base.Prepare();
        }

        public override void Execute() {
            messagesSent = 0;
            Report(0, 0);
            foreach (string trigger in triggerIDs) {
                eventDistributor.AddLineHandler(trigger, this);
            }
            stopwatch.Restart();
            SetOutput("Waiting for next triggering event");
        }

        internal void TriggerHandler(object sender, TriggerFiredEventArgs e) {
            ProcessIteration(e.record).Wait();
        }

        public override async Task<bool> ProcessIteration(Tuple<Dictionary<string, string>, FlightNode> record) {
            string message = (string)template.Clone();
            FlightNode fl = record.Item2;
            Dictionary<string, string> dict = record.Item1;

            // Process the data records
            foreach (Variable v in vars) {
                try {
                    if (v.varSubstitionRequired) {
                        v.value = v.GetValue(fl, dict, vars);
                        message = message.Replace(v.token, v.value);
                    } else {
                        v.value = v.GetValue(fl, dict);    //Special veriosn of GetValues which returns the correct field from the supplied values, if it is a CSV field
                        message = message.Replace(v.token, v.value);
                    }
                } catch (ArgumentException) {
                    break;
                } catch (Exception ex) {
                    logger.Warn($"Token Substitution Exception  {ex.Message}");
                    Console.WriteLine("Token Substitution Exception");
                }
            }

            logger.Trace($"Message to be sent: \n{message}\n");

            destinationEndPoint.Send(message, vars);

            messagesSent += 1;

            avg = stopwatch.Elapsed.TotalMilliseconds / messagesSent;
            double rate = RoundToSignificantDigits(60000 / avg, 2);

            Report(messagesSent, rate);

            if (saveMessageFile != null) {
                string fullPath = saveMessageFile.Replace(".", $"{messagesSent}.");
                try {
                    File.WriteAllText(fullPath, message);
                } catch (Exception ex) {
                    Console.WriteLine($"Unable to record message to file ${fullPath}. {ex.Message}");
                }
            }

            return true;
        }
    }
}