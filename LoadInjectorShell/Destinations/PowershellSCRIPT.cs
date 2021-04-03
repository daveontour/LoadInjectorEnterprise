using LoadInjector.Common;
using LoadInjector.RunTime;
using LoadInjector.RunTime.Views;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class PowershellScript : IDestinationType {
        public string name = "POWERSHELLLSCRIPT";
        public string description = "Windows Powershell Script";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new PowershellScriptPropertyGrid(dataModel, view);
        }

        public SenderAbstract GetDestinationSender() {
            return new DestinationPowershellScript();
        }
    }

    public class DestinationPowershellScript : SenderAbstract {
        private TextOutWindow win;
        private bool showResults;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);

            try {
                showResults = bool.Parse(defn.Attributes["showResults"].Value);
            } catch (Exception) {
                showResults = false;
            }

            return true;
        }

        public override void Send(string val, List<Variable> vars) {
            string result = RunScript(val);
            if (showResults) {
                win.WriteLine(result);
            }
        }

        public string RunScript(string scriptText) {
            // create Powershell runspace

            Runspace runspace = RunspaceFactory.CreateRunspace();

            // open it

            runspace.Open();

            // create a pipeline and feed it the script text

            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(scriptText);
            pipeline.Commands.Add("Out-String");

            Collection<PSObject> results = pipeline.Invoke();

            // close the runspace

            runspace.Close();

            // convert the script result into a single string

            StringBuilder stringBuilder = new StringBuilder();
            foreach (PSObject obj in results) {
                stringBuilder.AppendLine(obj.ToString());
            }

            return stringBuilder.ToString();
        }

        public override void Prepare() {
            base.Prepare();

            if (showResults) {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
                    win = new TextOutWindow {
                        Title = "PowerShell Command Output"
                    };
                    win.Show();
                }));
            }
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("PowerShell Script Protocol Configuration")]
    public class PowershellScriptPropertyGrid : LoadInjectorGridBase {

        public PowershellScriptPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = "POWERSHELLSCRIPT";
        }

        [DisplayName("Show Output Window"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("Show the output in a window")]
        public bool ShowResults {
            get => GetBoolAttribute("showResults");
            set => SetAttribute("showResults", value);
        }
    }
}