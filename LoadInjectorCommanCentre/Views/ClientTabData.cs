using System.ComponentModel;

namespace LoadInjectorCommanCentre.Views {

    public class ClientTabData : INotifyPropertyChanged {
        public string Header { get; set; }
        public bool IsSummary { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ClientTabData(string tabTitle) {
            this.Header = tabTitle;
        }
    }
}