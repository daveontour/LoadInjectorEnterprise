using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace LoadInjector.Common {

    public class TaskListDataTemplateSelector : DataTemplateSelector {

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            FrameworkElement element = container as FrameworkElement;
            XmlElement el = item as XmlElement;

            if (el == null) {
                return element?.FindResource("NodeTemplate") as DataTemplate;
            }

            if (el.Name == "config") {
                return element?.FindResource("ConfigNodeTemplate") as DataTemplate;
            }

            if (el.Name == "settings") {
                return element?.FindResource("SettingsNodeTemplate") as DataTemplate;
            }
            if (el.Name == "lines") {
                return element?.FindResource("LinesNodeTemplate") as DataTemplate;
            }
            if (el.Name == "eventdrivensources") {
                return element?.FindResource("EventDrivenLinesNodeTemplate") as DataTemplate;
            }
            if (el.Name == "ratedrivensources") {
                return element?.FindResource("RateDrivenLinesNodeTemplate") as DataTemplate;
            }
            if (el.Name == "ratedriven") {
                return element?.FindResource("RateDrivenNodeTemplate") as DataTemplate;
            }
            if (el.Name == "chained") {
                return element?.FindResource("ChainedDrivenNodeTemplate") as DataTemplate;
            }
            if (el.Name == "amsdirect") {
                return element?.FindResource("AMSDirectNodeTemplate") as DataTemplate;
            }
            if (el.Name == "amsdatadriven") {
                return element?.FindResource("AMSDataDrivenNodeTemplate") as DataTemplate;
            }
            if (el.Name == "csvdatadriven") {
                return element?.FindResource("CSVDataDrivenNodeTemplate") as DataTemplate;
            }
            if (el.Name == "exceldatadriven") {
                return element?.FindResource("ExcelDataDrivenNodeTemplate") as DataTemplate;
            }
            if (el.Name == "xmldatadriven") {
                return element?.FindResource("XMLDataDrivenNodeTemplate") as DataTemplate;
            }
            if (el.Name == "jsondatadriven") {
                return element?.FindResource("JSONDataDrivenNodeTemplate") as DataTemplate;
            }
            if (el.Name == "databasedatadriven") {
                return element?.FindResource("DataBaseDataDrivenNodeTemplate") as DataTemplate;
            }
            if (el.Name == "variable") {
                if (el?.ParentNode?.Name == "amsdirect") {
                    return element?.FindResource("AMSVariableNodeTemplate") as DataTemplate;
                }

                return element?.FindResource("VariableNodeTemplate") as DataTemplate;
            }
            if (el.Name == "value") {
                return element?.FindResource("ValueNodeTemplate") as DataTemplate;
            }
            if (el.Name == "destination") {
                return element?.FindResource("DestinationNodeTemplate") as DataTemplate;
            }
            if (el.Name == "header") {
                return element?.FindResource("HeaderNodeTemplate") as DataTemplate;
            }
            if (el.Name == "subscribed") {
                return element?.FindResource("HiddenNodeTemplate") as DataTemplate;
            }
            if (el.Name == "filter") {
                return element?.FindResource("FilterNodeTemplate") as DataTemplate;
            }
            if (el.Name == "or") {
                return element?.FindResource("ORExpression") as DataTemplate;
            }
            if (el.Name == "xor") {
                return element?.FindResource("XORExpression") as DataTemplate;
            }
            if (el.Name == "and") {
                return element?.FindResource("ANDExpression") as DataTemplate;
            }
            if (el.Name == "not") {
                return element?.FindResource("NOTExpression") as DataTemplate;
            }
            return element?.FindResource("NodeTemplate") as DataTemplate;
        }
    }
}