using LoadInjector.Common;
using LoadInjectorBase.Common;
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

        // public static MenuItem exportMenuItem;
        public static MenuItem aboutMenuItem;

        public static MenuItem executeMenuItem;

        public event EventHandler<DocumentLoadedEventArgs> DocumentLoaded;

        public event EventHandler SaveRequested;

        public event EventHandler ExportRequested;

        public event EventHandler LIExecuteRequested;

        public event EventHandler AboutRequested;

        public event EventHandler<SaveAsEventArgs> SaveAsRequested;

        public DocumentLoadedEventArgs loadedArgs;
        public SaveAsEventArgs saveAsArgs;

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

            //exportMenuItem = new MenuItem() { Header = "Export Archive" };
            //exportMenuItem.Click += new RoutedEventHandler(exportMenuItem_Click);
            //exportMenuItem.IsEnabled = false;
            //Path export = GetResourceCopy<Path>("export");
            //exportMenuItem.Icon = export;

            fileMenuItem.Items.Add(newMenuItem);
            fileMenuItem.Items.Add(openMenuItem);
            fileMenuItem.Items.Add(new Separator());
            fileMenuItem.Items.Add(saveMenuItem);
            fileMenuItem.Items.Add(saveAsMenuItem);
            fileMenuItem.Items.Add(new Separator());
            //fileMenuItem.Items.Add(exportMenuItem);
            //fileMenuItem.Items.Add(new Separator());
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

        private void openMenuItem_Click(object sender, RoutedEventArgs e) {
            OpenFileDialog open = new OpenFileDialog {
                Filter = "Config and Archives (*.xml, *.lia)|*.xml;*.lia",
                InitialDirectory = Directory.GetCurrentDirectory() + "\\Samples"
            };

            if (open.ShowDialog() == true) {
                XmlDocument document = new XmlDocument();
                if (open.FileName.EndsWith("xml") || open.FileName.EndsWith("XML")) {
                    try {
                        document.Load(open.FileName);
                        DocumentLoadedEventArgs args = new DocumentLoadedEventArgs() { Path = open.FileName, Document = document, FileName = open.SafeFileName };
                        saveAsArgs = null;
                        loadedArgs = args;
                        OnDocumentLoaded(this, args);
                    } catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                    }
                }
                if (open.FileName.EndsWith("lia") || open.FileName.EndsWith("LIA")) {
                    try {
                        string cwd = Utils.GetTemporaryDirectory();
                        document = LoadInjectorBase.Common.Utils.ExtractArchiveToDirectoryForEdit(open.FileName, cwd, "lia.lia");
                        DocumentLoadedEventArgs args = new DocumentLoadedEventArgs() { Path = open.FileName, Document = document, FileName = open.SafeFileName, ArchiveRoot = cwd };
                        saveAsArgs = null;
                        loadedArgs = args;
                        OnDocumentLoaded(this, args);
                    } catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
        }

        private void newMenuItem_Click(object sender, RoutedEventArgs e) {
            XmlDocument document = new XmlDocument();
            document.LoadXml(Parameters.Template);

            try {
                DocumentLoadedEventArgs args = new DocumentLoadedEventArgs() { Path = null, Document = document, FileName = "new.xml" };
                saveAsArgs = null;
                loadedArgs = args;
                OnDocumentLoaded(this, args);
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

        private void saveAsMenuItem_Click(object sender, RoutedEventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog {
                Filter = "Load Injector Archive File (*.lia)|*.lia|Load Injector Config File (*.xml)|*.xml"
            };
            if (dialog.ShowDialog() == true) {
                SaveAsEventArgs args = new SaveAsEventArgs { FileName = dialog.SafeFileName, Path = dialog.FileName };
                saveAsArgs = args;
                loadedArgs = null;
                SaveAsRequested?.Invoke(this, args);
            }
        }

        public void saveMenuItem_Click(object sender, RoutedEventArgs e) {
            SaveRequested?.Invoke(this, e);
        }

        public void exportMenuItem_Click(object sender, RoutedEventArgs e) {
            ExportRequested?.Invoke(this, e);
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e) {
            AboutRequested(this, e);
        }

        private void experimentalMenuItem_Click(object sender, RoutedEventArgs e) {
            // LIExecuteRequested?.Invoke(this, e);
            Process process = new Process();
            // Configure the process using the StartInfo properties.

            string lir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) + "/LoadInjectorRuntimeExecutive.exe";
            process.StartInfo.FileName = lir;
            string configfile = null;
            if (loadedArgs != null) {
                configfile = $"{loadedArgs.Path}/{loadedArgs.FileName}";
            } else if (saveAsArgs != null) {
                configfile = $"{saveAsArgs.Path}/{saveAsArgs.FileName}";
            }
            // process.StartInfo.Arguments = configfile;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;

            process.Start();
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