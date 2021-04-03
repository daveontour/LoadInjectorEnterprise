using System.Xml;

namespace LoadInjector.ViewModels {

    public class SelectedElementViewModel : BaseViewModel {
        private XmlNode dataModel;
        private XmlNode currentNode;

        public SelectedElementViewModel(XmlNode dataModel) {
            DataModel = dataModel;
        }

        public XmlNode DataModel {
            get => dataModel;
            private set { dataModel = value; OnPropertyChanged("DataModel"); }
        }

        public XmlNode CurrentNode {
            get => currentNode;
            set {
                currentNode = value;
                OnPropertyChanged("CurrentNode");
            }
        }

        public XmlNode NearestDestination {
            get {
                XmlNode node = dataModel;

                if (node.Name == "destination") {
                    return node;
                }

                while (node.ParentNode != null) {
                    if (node.ParentNode.Name == "destination") {
                        return node.ParentNode;
                    }
                    node = node.ParentNode;
                }
                return null;
            }
        }
    }
}