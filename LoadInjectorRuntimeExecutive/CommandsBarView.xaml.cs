using Microsoft.Win32;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;
using LoadInjector.Common;
using Path = System.Windows.Shapes.Path;
using System.IO.Compression;

namespace LoadInjector.Views {

    public class SearchRequestedEventArgs : EventArgs {
        public string XPath { get; set; }
    }

    public partial class CommandsBarView : UserControl {
        public static MenuItem aboutMenuItem = new MenuItem { Header = "About", IsEnabled = true };

        public static MenuItem executeMenuItem = new MenuItem { Header = "Execute", IsEnabled = true };
        public static MenuItem prepareMenuItem = new MenuItem { Header = "Prepare", IsEnabled = true };
        public static MenuItem stopMenuItem = new MenuItem { Header = "Stop", IsEnabled = true };

        public event EventHandler<DocumentLoadedEventArgs> DocumentLoaded;

        public event EventHandler LIExecuteRequested;

        public event EventHandler AboutRequested;

        public event EventHandler OpenClicked;

        public event EventHandler StopRequested;

        public event EventHandler PrepareRequested;

        public event EventHandler<SaveAsEventArgs> SaveAsRequested;

        public CommandsBarView() {
            InitializeComponent();
            MenuBar.Items.Clear();
            MenuItem fileMenuItem = new MenuItem { Header = "_File" };

            MenuItem openMenuItem = new MenuItem { Header = "_Open" };
            openMenuItem.Click += new RoutedEventHandler(openMenuItem_Click);
            Path open = GetResourceCopy<Path>("open");
            openMenuItem.Icon = open;

            aboutMenuItem.Click += new RoutedEventHandler(aboutMenuItem_Click);
            aboutMenuItem.IsEnabled = true;

            fileMenuItem.Items.Add(openMenuItem);
            fileMenuItem.Items.Add(new Separator());
            fileMenuItem.Items.Add(aboutMenuItem);

            MenuBar.Items.Add(fileMenuItem);

            prepareMenuItem.Icon = GetResourceCopy<Path>("prepare");
            prepareMenuItem.Click += new RoutedEventHandler(PrepareItem_Click);

            MenuBar.Items.Add(prepareMenuItem);

            executeMenuItem.Icon = GetResourceCopy<Path>("go");
            executeMenuItem.Click += new RoutedEventHandler(experimentalMenuItem_Click);

            MenuBar.Items.Add(executeMenuItem);

            stopMenuItem.Icon = GetResourceCopy<Path>("stop");
            stopMenuItem.Click += new RoutedEventHandler(StopItem_Click);

            MenuBar.Items.Add(stopMenuItem);
        }

        public void OnDocumentLoaded(object sender, DocumentLoadedEventArgs e) {
            DocumentLoaded?.Invoke(sender, e);
        }

        #region Menu Click Handlers

        private void openMenuItem_Click(object sender, RoutedEventArgs e) {
            OpenClicked?.Invoke(sender, e);
        }

        public void SetExecuteBtnEnabled(bool v) {
            executeMenuItem.IsEnabled = v;
            executeMenuItem.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetPrepareBtnEnabled(bool v) {
            prepareMenuItem.IsEnabled = v;
            prepareMenuItem.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetStopBtnEnabled(bool v) {
            stopMenuItem.IsEnabled = v;
            stopMenuItem.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e) {
            AboutRequested(this, e);
        }

        private void experimentalMenuItem_Click(object sender, RoutedEventArgs e) {
            LIExecuteRequested?.Invoke(this, e);
        }

        private void PrepareItem_Click(object sender, RoutedEventArgs e) {
            PrepareRequested?.Invoke(this, e);
        }

        private void StopItem_Click(object sender, RoutedEventArgs e) {
            StopRequested?.Invoke(this, e);
        }

        #endregion Menu Click Handlers

        private T GetResourceCopy<T>(string key) {
            T model = (T)FindResource(key);
            return ElementClone<T>(model);
        }

        /// <summary>
        /// Clones an element.
        /// </summary>
        public static T ElementClone<T>(T element) {
            T clone = default(T);
            MemoryStream memStream = ElementToStream(element);
            clone = ElementFromStream<T>(memStream);
            return clone;
        }

        /// <summary>
        /// Saves an element as MemoryStream.
        /// </summary>
        public static MemoryStream ElementToStream(object element) {
            MemoryStream memStream = new MemoryStream();
            XamlWriter.Save(element, memStream);
            return memStream;
        }

        /// <summary>
        /// Rebuilds an element from a MemoryStream.
        /// </summary>
        public static T ElementFromStream<T>(MemoryStream elementAsStream) {
            object reconstructedElement = null;

            if (elementAsStream.CanRead) {
                elementAsStream.Seek(0, SeekOrigin.Begin);
                reconstructedElement = XamlReader.Load(elementAsStream);
                elementAsStream.Close();
            }

            return (T)reconstructedElement;
        }
    }
}