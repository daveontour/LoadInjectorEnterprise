using LoadInjectorCommandCentre;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LoadInjectorCommandCentre.Views {

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class LoadInjectorCommandCentreStatus : Window {
        public MainWindow MainWindow { get; }

        public LoadInjectorCommandCentreStatus() {
            InitializeComponent();
        }

        public LoadInjectorCommandCentreStatus(MainWindow mainWindow) {
            InitializeComponent();
            MainWindow = mainWindow;
        }

        private void okButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void SelectAutoArchive_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog open = new OpenFileDialog {
                Filter = "Load Injector Archive Files(*.lia)|*.lia",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                FileName = MainWindow.AutoAssignArchive
            };

            if (open.ShowDialog() == true) {
                MainWindow.AutoAssignArchive = open.FileName;
            } else {
                MainWindow.AutoAssignArchive = null;
            }
        }

        private void ClearAutoArchive_Click(object sender, RoutedEventArgs e) {
            MainWindow.AutoAssignArchive = null;
        }
    }
}