using LoadInjector.Common;
using System.ComponentModel;
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

        public object GetConfigGrid(object dataModel, object view) {
            return new PowershellScriptPropertyGrid((XmlNode)dataModel, (IView)view);
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