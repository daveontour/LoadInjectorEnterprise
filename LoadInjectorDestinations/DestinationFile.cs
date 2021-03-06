using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Xml;

namespace LoadInjector.Destinations {

    public class DestinationFile : DestinationAbstract {
        private string destinationFile;
        private bool appendFile;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);

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

        public override string GetDestinationDescription() {
            return $"File Name: {destinationFile}";
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override bool Send(string val, List<Variable> vars) {
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
                        return false;
                    }
                }
            } catch (Exception ex) {
                logger.Error($"Unable to write file to {fullPath}");
                logger.Error(ex.Message);
                return false;
            }

            return true;
        }
    }
}