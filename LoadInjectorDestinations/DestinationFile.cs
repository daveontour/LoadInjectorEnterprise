using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace LoadInjector.Destinations {

    public class DestinationFile : SenderAbstract {
        private string destinationFile;
        private bool appendFile;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);

            try {
                destinationFile = defn.Attributes["destinationFile"].Value;
            } catch (Exception) {
                Console.WriteLine($"No Destination Filename defined for {defn.Attributes["name"].Value}");
                return false;
            }

            try {
                appendFile = bool.Parse(defn.Attributes["appendFile"].Value);
            } catch (Exception) {
                appendFile = false;
            }

            return true;
        }

        public override void Send(string val, List<Variable> vars) {
            /*
             *  Check behaviour if directory does not exist. Create it if it does not exist.
             */
            string fullPath = string.Copy(destinationFile);

            try {
                if (appendFile) {
                    val += Environment.NewLine;
                    File.AppendAllText(fullPath, val);
                } else {
                    foreach (Variable v in vars) {
                        try {
                            fullPath = fullPath.Replace(v.token, v.value);
                        } catch (Exception) {
                            // NO-OP
                        }
                    }

                    try {
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        File.WriteAllText(fullPath, val);
                    } catch (DirectoryNotFoundException ex) {
                        Console.WriteLine($"Directory not found  {ex.Message}");
                    }
                }
            } catch (Exception ex) {
                logger.Error($"Unable to write file to {fullPath}");
                logger.Error(ex.Message);
            }
        }
    }
}