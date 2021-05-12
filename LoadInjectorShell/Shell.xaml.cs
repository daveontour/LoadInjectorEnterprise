using LoadInjector.Common;
using LoadInjector.RunTime;
using LoadInjector.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;

namespace LoadInjector {

    public partial class MainWindow : Window {
        private readonly TreeEditorsViewModel editorsVM;

        private readonly string[] args;

        public MainWindow(string[] args = null) {
            this.args = args;

            InitializeComponent();
            commandBarView.DocumentLoaded += new EventHandler<DocumentLoadedEventArgs>(CommandBarView_DocumentLoaded);

            commandBarView.SaveRequested += new EventHandler(CommandBarView_SaveRequested);
            commandBarView.ExportRequested += new EventHandler(CommandBarView_ExportRequested);
            commandBarView.LIExecuteRequested += new EventHandler(CommandBarView_LIExecuteRequested);
            commandBarView.AboutRequested += new EventHandler(CommandBarView_AboutRequested);
            commandBarView.SaveAsRequested += new EventHandler<SaveAsEventArgs>(CommandBarView_SaveAsRequested);

            editorsVM = new TreeEditorsViewModel();

            editorsView.ViewModel = editorsVM;
            StartUp();
        }

        private void Window_ContentRendered(object sender, EventArgs e) {
            // NO-OP
        }

        private void StartUp() {
            if (args == null) {
                XmlDocument document = new XmlDocument();
                document.LoadXml(Parameters.Template);

                var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
                var dir = new FileInfo(location.AbsolutePath).Directory;
                try {
                    Directory.SetCurrentDirectory(dir.FullName);
                } catch (Exception ex) {
                    Console.WriteLine("The specified directory does not exist. {0}", ex);
                }
                var xmlTreeViewModel = new TreeEditorViewModel(document, dir.ToString(), "new.xml");
                editorsVM.Add(xmlTreeViewModel);
            }
        }

        private void CommandBarView_DocumentLoaded(object sender, DocumentLoadedEventArgs e) {
            var xmlTreeViewModel = new TreeEditorViewModel(e.Document, e.Path, e.FileName);
            editorsVM.Add(xmlTreeViewModel);
        }

        private void CommandBarView_SaveAsRequested(object sender, SaveAsEventArgs e) {
            editorsVM.ActiveEditor.SaveAsDocumentCommand.Execute(e.Path);
        }

        private void CommandBarView_SaveRequested(object sender, EventArgs e) {
            editorsVM.ActiveEditor.SaveDocumentCommand.Execute(null);
        }

        private void CommandBarView_ExportRequested(object sender, EventArgs e) {
            editorsVM.ActiveEditor.ExportDocumentCommand.Execute(null);
        }

        private void CommandBarView_LIExecuteRequested(object sender, EventArgs e) {
            editorsVM.ActiveEditor.LIExecuteCommand.Execute(null);
        }

        private void CommandBarView_AboutRequested(object sender, EventArgs e) {
            editorsVM.ActiveEditor.AboutCommand.Execute(null);
        }

        private void EditorsView_Loaded(object sender, RoutedEventArgs e) {
            // NO-OP
        }

        public void Window_Closing(object sender, CancelEventArgs e) {
        }
    }
}