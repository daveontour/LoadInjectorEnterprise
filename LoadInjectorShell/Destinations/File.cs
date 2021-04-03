using LoadInjector.Common;
using LoadInjector.RunTime;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class FileDestination : IDestinationType {
        private readonly string name = "FILE";
        private readonly string description = "File";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public SenderAbstract GetDestinationSender() {
            return new DestinationFile();
        }

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new FilePropertyGrid(dataModel, view);
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("File Protocol Configuration")]
    public class FilePropertyGrid : LoadInjectorGridBase {

        public FilePropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [DisplayName("Destination Filename (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(31), DescriptionAttribute("The full pathname of the file to write to. The pathname/filename can contain tokens which are substituted for the corresponding variable")]
        public string DestinationFileName {
            get => GetAttribute("destinationFile");
            set => SetAttribute("destinationFile", value);
        }

        [DisplayName("Append to Destination File"), ReadOnly(false), Browsable(true), PropertyOrder(32), DescriptionAttribute("Rather than creating a new file for each message, append the new message to the file")]
        public bool AppendFile {
            get => GetBoolDefaultFalseAttribute(_node, "appendFile");
            set => SetAttribute("appendFile", value);
        }
    }

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