using System;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace LoadInjector.GridDefinitions {

    public partial class FolderNameSelector : System.Windows.Controls.UserControl, ITypeEditor {

        public FolderNameSelector() {
            InitializeComponent();
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(string), typeof(FolderNameSelector),
                                                                                                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Value {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            try {
                if (Directory.Exists(Value)) {
                    dialog.SelectedPath = Value;
                } else {
                    dialog.SelectedPath = @"C:\";
                }
            } catch (Exception) {
                dialog.SelectedPath = @"C:\";
            }
            dialog.ShowNewFolderButton = true;

            DialogResult result = dialog.ShowDialog();
            if (result == DialogResult.OK) {
                Value = dialog.SelectedPath;
            }
        }

        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem) {
            System.Windows.Data.Binding binding = new System.Windows.Data.Binding("Value") {
                Source = propertyItem,
                Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay
            };
            BindingOperations.SetBinding(this, ValueProperty, binding);
            return this;
        }
    }
}