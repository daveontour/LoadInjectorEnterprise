using LoadInjector.Common;
using LoadInjector.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace LoadInjector.ViewModels {

    public class TreeEditorsViewModel : BaseViewModel {

        // private int activeTabIndex;
        private ObservableCollection<TreeEditorViewModel> treeEditors = new ObservableCollection<TreeEditorViewModel>();

        private TreeEditorViewModel activeEditor;

        public TreeEditorViewModel ActiveEditor {
            get => activeEditor;
            set {
                activeEditor = value;
                OnPropertyChanged("ActiveEditor");
            }
        }

        private readonly ICommand saveDocumentCommand;
        public ICommand SaveDocumentCommand => saveDocumentCommand;

        public TreeEditorsViewModel() {
            saveDocumentCommand = new RelayCommand(p => { Save(); });
        }

        private void Save() {
            activeEditor.Save();
        }

        public int activeEditorIndex;

        public int ActiveEditorIndex {
            get => activeEditorIndex;
            set {
                activeEditorIndex = value;
                OnPropertyChanged("ActiveEditorIndex");
                if (activeEditorIndex > -1 && activeEditorIndex < TreeEditors.Count) {
                    CommandsBarView.executeMenuItem.IsEnabled = true;
                    CommandsBarView.saveMenuItem.IsEnabled = true;
                    CommandsBarView.saveAsMenuItem.IsEnabled = true;
                    CommandsBarView.exportMenuItem.IsEnabled = true;
                    ActiveEditor = TreeEditors[ActiveEditorIndex];
                    try {
                        var dir = new FileInfo(ActiveEditor.Path).Directory;
                        try {
                            Directory.SetCurrentDirectory(dir.FullName);
                        } catch (Exception ex) {
                            Console.WriteLine("The specified directory does not exist. {0}", ex);
                        }
                    } catch (Exception) {
                        var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
                        var dir = new FileInfo(location.AbsolutePath).Directory;
                        try {
                            Directory.SetCurrentDirectory(dir.FullName);
                        } catch (Exception ex) {
                            Console.WriteLine("The specified directory does not exist. {0}", ex);
                        }
                    }
                } else {
                    CommandsBarView.executeMenuItem.IsEnabled = false;
                    CommandsBarView.saveMenuItem.IsEnabled = false;
                    CommandsBarView.saveAsMenuItem.IsEnabled = false;
                }
            }
        }

        public ObservableCollection<TreeEditorViewModel> TreeEditors {
            get => treeEditors;
            set {
                treeEditors = value;
                OnPropertyChanged("TreeEditors");
            }
        }

        public void Add(TreeEditorViewModel treeEditor) {
            TreeEditors.Add(treeEditor);
            ActiveEditorIndex = TreeEditors.Count - 1;
        }

        public void Remove(TreeEditorViewModel treeEditor) {
            try {
                TreeEditors.Remove(treeEditor);
            } catch (Exception ex) {
                Debug.WriteLine(ex);
            }
            ActiveEditorIndex = TreeEditors.Count - 1;
        }
    }
}