using LoadInjector.Common;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;
using Path = System.Windows.Shapes.Path;

namespace LoadInjector.Views {

    public partial class CommandsBarView : UserControl {

        public readonly ICommand Save;
        public static MenuItem saveMenuItem;
        public static MenuItem saveAsMenuItem;
        public static MenuItem aboutMenuItem;
        public static MenuItem executeMenuItem;

        public event EventHandler<DocumentLoadedEventArgs> DocumentLoaded;
        public event EventHandler SaveRequested;
        public event EventHandler LIExecuteRequested;
        public event EventHandler AboutRequested;
        public event EventHandler<SaveAsEventArgs> SaveAsRequested;
        public CommandsBarView() {
            InitializeComponent();
            MenuBar.Items.Clear();
            MenuItem fileMenuItem = new MenuItem { Header = "_File" };
            MenuItem newMenuItem = new MenuItem { Header = "_New" };
            newMenuItem.Click += new RoutedEventHandler(newMenuItem_Click);
            Path newI = GetResourceCopy<Path>("new");
            newMenuItem.Icon = newI;

            MenuItem openMenuItem = new MenuItem { Header = "_Open" };
            openMenuItem.Click += new RoutedEventHandler(openMenuItem_Click);
            Path open = GetResourceCopy<Path>("open");
            openMenuItem.Icon = open;

            saveMenuItem = new MenuItem { Header = "_Save", InputGestureText = "Ctrl+S" };

            saveMenuItem.Click += new RoutedEventHandler(saveMenuItem_Click);
            saveMenuItem.IsEnabled = false;
            Path save = GetResourceCopy<Path>("save");
            saveMenuItem.Icon = save;

            saveAsMenuItem = new MenuItem { Header = "Save _As" };
            saveAsMenuItem.Click += new RoutedEventHandler(saveAsMenuItem_Click);
            saveAsMenuItem.IsEnabled = false;
            Path saveas = GetResourceCopy<Path>("save");
            saveAsMenuItem.Icon = saveas;

            aboutMenuItem = new MenuItem { Header = "About" };
            aboutMenuItem.Click += new RoutedEventHandler(aboutMenuItem_Click);
            aboutMenuItem.IsEnabled = true;

            fileMenuItem.Items.Add(newMenuItem);
            fileMenuItem.Items.Add(openMenuItem);
            fileMenuItem.Items.Add(new Separator());
            fileMenuItem.Items.Add(saveMenuItem);
            fileMenuItem.Items.Add(saveAsMenuItem);
            fileMenuItem.Items.Add(new Separator());
            fileMenuItem.Items.Add(aboutMenuItem);

            MenuBar.Items.Add(fileMenuItem);

            executeMenuItem = new MenuItem { Header = "_Execute..." };
            executeMenuItem.IsEnabled = false;
            executeMenuItem.Icon = GetResourceCopy<Path>("go");
            executeMenuItem.Click += new RoutedEventHandler(experimentalMenuItem_Click);

            MenuBar.Items.Add(executeMenuItem);
        }

        public void OnDocumentLoaded(object sender, DocumentLoadedEventArgs e) {
            DocumentLoaded?.Invoke(sender, e);
        }

        #region Menu Click Handlers

        void openMenuItem_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog open = new OpenFileDialog {
                Filter = "XML Files (*.xml)|*.xml",
                InitialDirectory = Directory.GetCurrentDirectory() + "\\Samples"
            };

            if (open.ShowDialog() == true) {
                XmlDocument document = new XmlDocument();
                try {
                    document.Load(open.FileName);
                    DocumentLoadedEventArgs args = new DocumentLoadedEventArgs() { Path = open.FileName, Document = document, FileName = open.SafeFileName };
                    OnDocumentLoaded(this, args);
                } catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                }

            }
        }

        void newMenuItem_Click(object sender, RoutedEventArgs e) {

            XmlDocument document = new XmlDocument();
            document.LoadXml(Parameters.Template);

            try {

                DocumentLoadedEventArgs args = new DocumentLoadedEventArgs() { Path = null, Document = document, FileName = "new.xml" };
                OnDocumentLoaded(this, args);

            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }

        }

        void saveAsMenuItem_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog {
                Filter = "XML Files (*.xml)|*.xml"
            };
            if (dialog.ShowDialog() == true) {
                SaveAsEventArgs args = new SaveAsEventArgs { FileName = dialog.SafeFileName, Path = dialog.FileName };
                SaveAsRequested?.Invoke(this, args);
            }
        }

        public void saveMenuItem_Click(object sender, RoutedEventArgs e) {
            SaveRequested?.Invoke(this, e);
        }

        void aboutMenuItem_Click(object sender, RoutedEventArgs e) {
            AboutRequested(this, e);
        }

        void experimentalMenuItem_Click(object sender, RoutedEventArgs e) {
            LIExecuteRequested?.Invoke(this, e);
        }
        #endregion


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
