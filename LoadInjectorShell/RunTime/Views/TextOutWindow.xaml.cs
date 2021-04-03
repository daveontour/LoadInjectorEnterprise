using System;
using System.Windows;

namespace LoadInjector.RunTime.Views {

    public partial class TextOutWindow : Window {
        public bool myVar;
        private ControlWriter consoleWriter;

        public TextOutWindow() {
            InitializeComponent();
            DataContext = this;
        }

        public bool DisableScroll {
            get => myVar;
            set {
                myVar = value;
                consoleWriter.DisableScroll = value;
            }
        }

        public void WriteLine(string s) {
            consoleWriter.WriteLineText(s);
        }

        private void OutputConsole_Initialized(object sender, EventArgs e) {
            consoleWriter = new ControlWriter(outputConsole) {
                DisableScroll = DisableScroll
            };
        }
    }
}