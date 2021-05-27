using System.ComponentModel;

namespace LoadInjectorCommanCentre.Views {

    public class ClientTabControl : INotifyPropertyChanged {
        public string Header { get; set; }
        public bool IsSummary { get; set; }

        public string ConnectionID { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ClientTabControl(string tabTitle) {
            this.Header = tabTitle;
        }
    }
}