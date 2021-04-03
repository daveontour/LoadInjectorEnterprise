using LoadInjector.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LoadInjector.Views {

    public partial class TreeEditorsView : UserControl {
        private TreeEditorsViewModel viewModel;

        public TreeEditorsViewModel ViewModel {
            get => viewModel;
            set => DataContext = viewModel = value;
        }

        public TreeEditorsView() {
            InitializeComponent();
        }

        private void CloseTab_Handler(object sender, RoutedEventArgs e) {
            try {
                ViewModel.Remove((sender as CloseableTabItem).Content as TreeEditorViewModel);
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

        private void SaveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            viewModel.SaveDocumentCommand.Execute(null);
        }
    }
}