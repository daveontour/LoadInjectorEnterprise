using System.Xml;

namespace LoadInjector.Common {
    public interface ILoadInjectorGridBase {
        void ClearAttributes();
        string GetAttribute(string attribName);
        bool GetBoolAttribute(string attribName);
        bool GetBoolDefaultFalseAttribute(XmlNode _node, string attribName);
        bool GetBoolDefaultTrueAttribute(string attribName);
        double GetDoubleAttribute(string attribName);
        double GetDoubleAttributeDefaultZero(string attribName);
        double GetDoubleAttributeZeroDefault(string attribName);
        string GetFlightType();
        int GetIntAttribute(string attribName);
        int GetIntAttribute(string attribName, int def);
        int GetIntDefaultZeroAttribute(string attribName);
        XmlNode GetNode(string name);
        void Hide(string field);
        void Hide(string[] fields);
        void SetAbsoluteAttribute(string attribName, bool value);
        void SetAttribute(string attribName, bool value);
        void SetAttribute(string attribName, double value);
        void SetAttribute(string attribName, int value);
        void SetAttribute(string attribName, string value);
        void SetAttribute(string attribName, string value, XmlNode node);
        void SetAttributeAbs(string attribName, bool value);
        void SetFlightType(string value);
        void Show(string field);
        void Show(string[] fields);
        void ShowHide(string field, bool value);
    }
}