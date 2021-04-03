using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace LoadInjector.GridDefinitions {

    public partial class FileNameSelector : UserControl, ITypeEditor {

        public FileNameSelector() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(string), typeof(FileNameSelector),
                                                                                                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Value {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog dialog = new OpenFileDialog {
                Filter = "All Files |*.*"
            };

            try {
                FileInfo file_info = new FileInfo(Value);
                string parent_directory_path = file_info.DirectoryName;

                if (Directory.Exists(parent_directory_path)) {
                    dialog.InitialDirectory = parent_directory_path;
                } else {
                    dialog.InitialDirectory = @"C:\";
                }
            } catch (Exception) {
                dialog.InitialDirectory = @"C:\";
            }
            if (dialog.ShowDialog() == true) {
                Value = dialog.FileName;
            }
        }

        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem) {
            Binding binding = new Binding("Value") {
                Source = propertyItem,
                Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
            };
            BindingOperations.SetBinding(this, ValueProperty, binding);
            return this;
        }
    }
}