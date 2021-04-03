using LoadInjector.Common;
using System.ComponentModel;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.ViewModels {

    [DisplayName("Message Sending Rate Profile")]
    public class RateModel : LoadInjectorGridBase {

        public RateModel(XmlNode dataModel) {
            _node = dataModel;
        }

        [DisplayName("Initial Message Rate (msgs/min)"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The initial rate for sending messages in messages/minute")]
        public string Rate {
            get => GetAttribute("messagesPerMinute");
            set => SetAttribute("messagesPerMinute", value);
        }

        private int timeFromStart;

        [DisplayName("Defered Start Time (sec)"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The time in seconds from the start of the test to wait before executing")]
        public int TimeFromStart {
            get => timeFromStart;
            set => timeFromStart = value;
        }

        [DisplayName("Maximum Number of Messages"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The time in seconds from the start of the test to wait before executing")]
        public int MaxMessages {
            get => GetIntAttribute("maxNumMessages");
            set => SetAttribute("maxNumMessages", value);
        }

        [DisplayName("Maximum Execution Time (sec)"), ReadOnly(false), Browsable(true), PropertyOrder(2), DescriptionAttribute("The time in seconds from the start of the test to wait before executing")]
        public int MaxTime {
            get => GetIntAttribute("maxRunTime");
            set => SetAttribute("maxRunTime", value);
        }
    }
}