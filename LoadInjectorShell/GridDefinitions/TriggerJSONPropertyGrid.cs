using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    [DisplayName("Trigger Defintion")]
    public class TriggerJsonPropertyGrid : LoadInjectorGridBase {

        public TriggerJsonPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [CategoryAttribute("Required"), DisplayName("Trigger Name"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The Name of this trigger")]
        public string Name {
            get => GetAttribute("name");
            set => SetAttribute("name", value);
        }

        [CategoryAttribute("Required"), DisplayName("Trigger ID"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The ID of this trigger")]
        public string Token {
            get => GetAttribute("id");
            set => SetAttribute("id", value);
        }

        [Editor(typeof(DoubleUpDown), typeof(DoubleUpDown))]
        [CategoryAttribute("Optional"), DisplayName("Time Offset"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("A offset in minutes to be applied to trigger source")]
        public double Delta {
            get {
                double val = GetDoubleAttributeDefaultZero("delta");
                return val;
            }
            set {
                SetAttribute("delta", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }
    }
}