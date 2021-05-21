using System;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace LoadInjector.RunTime.Views {

    public class ControlWriter : TextWriter {
        private readonly TextBox textbox;
        private bool lastNewLine = true;
        public bool DisableScroll { get; set; }

        public ControlWriter(TextBox textbox) {
            DisableScroll = false;
            this.textbox = textbox;
        }

        public void Clear() {
            textbox.Dispatcher.Invoke(
                () => {
                    textbox.Text = "";
                    textbox.ScrollToEnd();
                });
        }

        public override void Write(char value) {
            textbox.Dispatcher.Invoke(
                () => {
                    try {
                        if (lastNewLine) {
                            textbox.Text += $"[{DateTime.Now:HH:mm:ss.ffff}] ";
                        }

                        if (value == '\n') {
                            lastNewLine = true;
                        } else {
                            lastNewLine = false;
                        }

                        textbox.Text += value;

                        if (!DisableScroll) {
                            textbox.ScrollToEnd();
                        }
                    } catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                    }
                });
        }

        public override void Write(string value) {
            textbox.Dispatcher.Invoke(
                () => {
                    if (textbox.Text.Length > 10000) {
                        textbox.Text = textbox.Text.Substring(2000);
                    }
                    if (lastNewLine) {
                        textbox.Text += $"[{DateTime.Now:HH:mm:ss.ffff}] ";
                    }

                    if (value == "\n") {
                        lastNewLine = true;
                    } else {
                        lastNewLine = false;
                    }

                    textbox.Text += value;
                    if (!DisableScroll) {
                        textbox.ScrollToEnd();
                    }
                });
        }

        public override void WriteLine(string value) {
            textbox.Dispatcher.Invoke(
                () => {
                    if (textbox.Text.Length > 10000) {
                        textbox.Text = textbox.Text.Substring(2000);
                    }
                    textbox.Text += $"[{DateTime.Now:HH:mm:ss.ffff}] {value}\n";
                    if (!DisableScroll) {
                        textbox.ScrollToEnd();
                    }
                });
        }

        public void WriteLineText(string value) {
            textbox.Dispatcher.Invoke(
                () => {
                    if (textbox.Text.Length > 10000) {
                        textbox.Text = textbox.Text.Substring(2000);
                    }
                    textbox.Text += value;
                    if (!DisableScroll) {
                        textbox.ScrollToEnd();
                    }
                });
        }

        public override Encoding Encoding => Encoding.ASCII;
    }
}