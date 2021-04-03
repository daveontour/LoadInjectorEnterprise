using LoadInjector.ViewModels;
using System.Windows.Controls;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace LoadInjector.Common {

    public enum ContextMenuType {
        AddAMSDirect,
        AddAMSDataDriven,
        AddCSVDataDriven,
        AddExcelDataDriven,
        AddXMLDataDriven,
        AddJSONDataDriven,
        AddDataBaseDataDriven,
        AddRateDriven,
        AddDestination,
        Clone,
        AddChained,
        AddVariable,
        AddFilter,
        AddExpression,
        AddDataFilter,
        AddValue,
        AddServiceSettings,
        Delete
    }

    public interface IView {

        void DrawConfig();

        void UpdateParamBindings(string param);

        void UpdateDiagram();

        void ChangeElementType(string value);

        bool CanChangeElementType(string value);

        void ChangeFilterType(string value);

        void RefreshDraw();

        void SetProtocolGrid(string value);

        XmlDocument GetViewModel();

        SelectedElementViewModel GetSelectedElement();

        void UpdateTemplateBox();

        TabControl GetDestinationTabControl { get; }

        TabControl GetBottomTabControl { get; }

        XmlNode GetCurrentDestination { get; }

        void ResetCurrentDestination();

        void CheckSaveTemplate();

        PropertyGrid GetPropertyGrid();

        void HighlightCanvas(XmlNode node);
    }
}