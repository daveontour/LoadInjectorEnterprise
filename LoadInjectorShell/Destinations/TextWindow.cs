using LoadInjector.Common;
using LoadInjector.RunTime;
using LoadInjector.RunTime.Views;
using NLog;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class TextWindow : IDestinationType {
        public string name = "TEXT";
        public string description = "Text Window";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public SenderAbstract GetDestinationSender() {
            return new DestinationText();
        }

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return null;
        }
    }

    internal class DestinationText : SenderAbstract {
        private TextOutWindow win;
        private string title;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);
            title = defn.Attributes["name"]?.Value;
            return true;
        }

        public override void Send(String val, List<Variable> vars) {
            win.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
                win.WriteLine($"\n[{DateTime.Now:HH:mm:ss.ffff}] Output Message=======>\n\n");
                win.WriteLine(val);
                win.WriteLine("\n<======= Output Message\n");
            }));
        }

        public override void Prepare() {
            base.Prepare();

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
                win = new TextOutWindow {
                    Title = $"TEXT Destination- {title}"
                };
                win.Show();
            }));
        }

        public override void Stop() {
            base.Stop();
            try {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                    try {
                        win?.Close();
                    } catch (Exception) {
                        // NO-OP
                    }
                }));
            } catch (Exception) {
                // NO-OP
            }
        }
    }
}