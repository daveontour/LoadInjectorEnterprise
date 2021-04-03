using LoadInjector.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Xml;

namespace LoadInjector.ViewModels {

    public class TriggerModelEntry : LoadInjectorGridBase {

        public String Name {
            get => GetAttribute("name");
            set {
                SetAttribute("name", value);
                treeEditorViewModel.OnPropertyChanged("XMLText");
            }
        }

        public String ID {
            get => GetAttribute("id");
            set {
                List<String> triggers = new List<string>();
                foreach (XmlNode trig in _node.SelectNodes("//trigger")) {
                    triggers.Add(trig.Attributes["id"]?.Value);
                }
                foreach (XmlNode trig in _node.SelectNodes("//ratedriven")) {
                    triggers.Add(trig.Attributes["ID"]?.Value);
                }

                if (triggers.Contains(value)) {
                    MessageBox.Show("Trigger ID already exists. The ID for the trigger already exists in this or one of the other source. Trigger IDs must be unique in the configuration", "Add Trigger", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string currentValue = GetAttribute("id");
                SetAttribute("id", value);

                if (!string.IsNullOrEmpty(currentValue)) {
                    var q = $"//subscribed[text() = '{currentValue}']";
                    foreach (XmlNode node in _node.SelectNodes(q)) {
                        node.InnerText = value;
                    }
                }

                treeEditorViewModel.OnPropertyChanged("XMLText");
            }
        }

        public String AMSTrigType {
            get => GetAttribute("trigType");
            set {
                SetAttribute("trigType", value);
                treeEditorViewModel.OnPropertyChanged("XMLText");
            }
        }

        public String AMSExternalName {
            get => GetAttribute("externalName");
            set {
                SetAttribute("externalName", value);
                treeEditorViewModel.OnPropertyChanged("XMLText");
            }
        }

        public String DateFormat {
            get => GetAttribute("format");
            set {
                SetAttribute("format", value);
                treeEditorViewModel.OnPropertyChanged("XMLText");
            }
        }

        public string Delta {
            get {
                string v = GetAttribute("delta");
                if (v == null) {
                    v = "0";
                }
                return v;
            }
            set {
                SetAttribute("delta", value);
                treeEditorViewModel.OnPropertyChanged("XMLText");
            }
        }

        private readonly TreeEditorViewModel treeEditorViewModel;

        public TriggerModelEntry(XmlNode node, TreeEditorViewModel treeEditorViewModel) {
            _node = node;
            this.treeEditorViewModel = treeEditorViewModel;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj is TriggerModelEntry) {
                TriggerModelEntry test = (TriggerModelEntry)obj;
                return (test.Name == Name && test.ID == ID && test.Delta == Delta);
            } else {
                return false;
            }
        }
    }
}