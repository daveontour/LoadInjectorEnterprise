using LoadInjector.Common;
using System;
using System.Xml;

namespace LoadInjector.ViewModels {

    public class HeaderModelEntry : LoadInjectorGridBase {

        public String Name {
            get => GetAttribute("name");
            set {
                SetAttribute("name", value);
                treeEditorViewModel.OnPropertyChanged("XMLText");
            }
        }

        public String Value {
            get => GetAttribute("value");
            set {
                SetAttribute("value", value);
                treeEditorViewModel.OnPropertyChanged("XMLText");
            }
        }

        private readonly TreeEditorViewModel treeEditorViewModel;

        public HeaderModelEntry(XmlNode node, TreeEditorViewModel treeEditorViewModel) {
            _node = node;
            this.treeEditorViewModel = treeEditorViewModel;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj is HeaderModelEntry) {
                HeaderModelEntry test = (HeaderModelEntry)obj;
                return (test.Name == Name && test.Value == Value);
            } else {
                return false;
            }
        }
    }
}