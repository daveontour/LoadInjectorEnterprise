using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Xml;

namespace LoadInjector.Destinations {

    internal class DestinationText : SenderAbstract {
        private string title;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);
            title = defn.Attributes["name"]?.Value;
            return true;
        }

        public override void Send(String val, List<Variable> vars) {
            Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss.ffff}] Output Message=======>\n\n");
            Console.WriteLine(val);
            Console.WriteLine("\n<======= Output Message\n");
        }
    }
}