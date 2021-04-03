using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LoadInjector.Common {

    public class ContextMenuProvider {
        public readonly Dictionary<ContextMenuType, MenuItem> contextMenus = new Dictionary<ContextMenuType, MenuItem>();

        public ContextMenuProvider() {
            contextMenus.Add(ContextMenuType.AddAMSDataDriven, new MenuItem { Header = "Add AMS Data Event Driven Injector", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddCSVDataDriven, new MenuItem { Header = "Add CSV Data Event Driven Injector", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddExcelDataDriven, new MenuItem { Header = "Add Excel Data Event Driven Injector", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddXMLDataDriven, new MenuItem { Header = "Add XML Data Event Driven Injector", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddJSONDataDriven, new MenuItem { Header = "Add JSON Data Event Driven Injector", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddDataBaseDataDriven, new MenuItem { Header = "Add Database Data Event Driven Injector", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddRateDriven, new MenuItem { Header = "Add Rate Driven Injector", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddAMSDirect, new MenuItem { Header = "Add AMS Direct Flight Update", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddDestination, new MenuItem { Header = "Add Destination", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.Delete, new MenuItem { Header = "Delete", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.Clone, new MenuItem { Header = "Clone", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddChained, new MenuItem { Header = "Add Chained Injector", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddVariable, new MenuItem { Header = "Add Variable", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddValue, new MenuItem { Header = "Add Value Item", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddServiceSettings, new MenuItem { Header = "Add Service Settings", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });

            contextMenus.Add(ContextMenuType.AddFilter, new MenuItem { Header = "Add Flight Filter", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddExpression, new MenuItem { Header = "Add Boolean Expression", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
            contextMenus.Add(ContextMenuType.AddDataFilter, new MenuItem { Header = "Add Data Filter", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center });
        }
    }
}