using LoadInjector.Common;
using System.Collections.Generic;
using System.Windows;
using System.Xml;

namespace LoadInjector.ViewModels {

    public class SubscribedTriggerModel {
        private readonly XmlDocument dataModel;
        private readonly XmlNode dest;
        private readonly TreeEditorViewModel treeEditorViewModel;

        public string Name { get; set; }
        public string ID { get; set; }
        public string Delta { get; set; }

        private bool sub;

        public bool Subscribed {
            get => sub;
            set {
                sub = value;
                UpdateDestinationSub(value);
            }
        }

        private void UpdateDestinationSub(bool value) {
            if (value) {
                // Add the subscirption
                if (TriggerTypeMap.CheckCompatible(ID, dest)) {
                    XmlNode newNode = dataModel.CreateElement("subscribed");
                    newNode.InnerText = ID;
                    dest.AppendChild(newNode);
                } else {
                    MessageBox.Show("The selected trigger is incompatible with the types of triggers already selected", "Incompatible Triggers", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            } else {
                // Remove the subscription
                if (dest != null)
                    foreach (XmlNode n in dest.SelectNodes(".//subscribed")) {
                        if (n.InnerText == ID) {
                            dest.RemoveChild(n);
                            break;
                        }
                    }
            }

            treeEditorViewModel.OnPropertyChanged("XMLText");
            treeEditorViewModel.OnPropertyChanged("SubscribedTriggers");
        }

        public SubscribedTriggerModel(XmlNode triggerNode, XmlNode dest, XmlDocument dataModel, TreeEditorViewModel treeEditorViewModel, List<string> currIDs) {
            this.dataModel = dataModel;
            this.dest = dest;
            this.treeEditorViewModel = treeEditorViewModel;
            if (triggerNode.Attributes != null) {
                Name = triggerNode.Attributes["name"]?.Value;
                ID = triggerNode.Attributes["id"]?.Value;
                Delta = triggerNode.Attributes["delta"]?.Value;
            }

            sub = currIDs.Contains(ID);
        }

        public SubscribedTriggerModel(string name, string id, XmlNode dest, XmlDocument dataModel, TreeEditorViewModel treeEditorViewModel, List<string> currIDs) {
            this.dataModel = dataModel;
            this.dest = dest;
            this.treeEditorViewModel = treeEditorViewModel;
            Name = name;
            ID = id;
            Delta = "Rate";
            sub = currIDs.Contains(id);
        }
    }
}