using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace LoadInjector.Views {

    public partial class LIAbout : Window {

        public LIAbout() {
            InitializeComponent();
            DataContext = this;
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            VersionInfo = $"Last Build: - version {version}";
        }

        private string VersionInfo;

        public string VersionString {
            get => VersionInfo;
            set => VersionInfo = value;
        }

        public void ClickOK(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }

        private void ClickClear(object sender, RoutedEventArgs e) {
            File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\LoadInjectorConfig\\Config.xml");
        }
    }
}