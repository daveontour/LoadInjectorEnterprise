using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.GridDefinitions {

    public class DataDrivenPropertyBase : LoadInjectorGridBase {

        public DataDrivenPropertyBase(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
        }

        [RefreshProperties(RefreshProperties.All)]
        [CategoryAttribute("Iteration Flight Data"), DisplayName("Flight Type"), PropertyOrder(1), Browsable(true), DescriptionAttribute("Type of flights to use"), ItemsSource(typeof(FlightTypeListOut))]
        public string FlightType {
            get => GetFlightType();
            set {
                SetFlightType(value);
                if (value == null || value == "none" || value == "None") {
                    XmlNode filter = _node.SelectSingleNode("./filter");
                    if (filter != null) {
                        _node.RemoveChild(filter);
                        view.UpdateParamBindings("XMLText");
                    }
                }
            }
        }

        [CategoryAttribute("Iteration Flight Data"), DisplayName("Earliest Offset for Flights (mins)"), ReadOnly(false), Browsable(true), PropertyOrder(4), DescriptionAttribute("The offset in minutes of the earliest scheduled time of the flight to use")]
        public int FlightSetFrom {
            get {
                int val = GetIntAttribute("flightSetFrom");
                //if (val == -1) {
                //    val = -180;
                //}
                return val;
            }
            set {
                SetAttribute("flightSetFrom", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("Iteration Flight Data"), DisplayName("Latest Offset for Flights (mins)"), ReadOnly(false), Browsable(true), PropertyOrder(5), DescriptionAttribute("The offset in minutes of the letest scheduled time of the flight to use")]
        public int FlightSetTo {
            get {
                int val = GetIntAttribute("flightSetTo");
                //if (val == -1) {
                //    val = 540;
                //}
                return val;
            }
            set {
                SetAttribute("flightSetTo", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }

        [CategoryAttribute("Iteration Flight Data"), DisplayName("Sequential Flight"), ReadOnly(false), Browsable(true), PropertyOrder(6), DescriptionAttribute("Uses the flights in sequential order")]
        public bool Sequential {
            get => GetBoolAttribute("sequentialFlight");
            set => SetAttribute("sequentialFlight", value);
        }

        [CategoryAttribute("Iteration Flight Data"), DisplayName("Refresh Flight"), ReadOnly(false), Browsable(true), PropertyOrder(11), DescriptionAttribute("Refresh the flight from AMS at the time of use")]
        public bool RefreshFlight {
            get => GetBoolAttribute("refreshFlight");
            set => SetAttribute("refreshFlight", value);
        }

        [CategoryAttribute("Execution"), DisplayName("Disabled"), ReadOnly(false), Browsable(true), PropertyOrder(50), DescriptionAttribute("Disable this Source")]
        public bool Disable {
            get => GetBoolAttribute("disabled");
            set {
                SetAttribute("disabled", value);
                view.UpdateParamBindings("XMLText");
                view.UpdateDiagram();
            }
        }
    }
}