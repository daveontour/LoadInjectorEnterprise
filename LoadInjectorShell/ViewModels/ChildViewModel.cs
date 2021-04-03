using System.Xml;

namespace LoadInjector.ViewModels {

    public class ChildViewModel : BaseViewModel {
        public XmlNode DataModel { get; private set; }

        public ChildViewModel(XmlNode childNode) {
            DataModel = childNode;
        }

        private bool isSelected;

        public bool IsSelected {
            get => isSelected;
            set {
                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }
    }
}