using LoadInjector.Runtime.EngineComponents;
using LoadInjectorBase.Common;

using LoadInjectorBase.Common;

using Microsoft.Win32;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LoadInjectorCommandCentre.Views {

    public partial class ClientControl : UserControl, INotifyPropertyChanged {
        public string ConnectionID { get; set; }
        public CentralMessagingHub MessageHub { get; }

        private string executionID;

        public CompletionReport iterationRecords;

        public string ExecutionNodeID {
            get {
                return executionID;
            }
            set {
                executionID = value;
                OnPropertyChanged("ExecutionNodeID");
            }
        }

        private string osversion;

        public string OSVersion {
            get {
                return $"{osversion}";
            }
            set {
                osversion = value;
                OnPropertyChanged("OSVersion");
            }
        }

        private string statusText = ClientState.UnAssigned.Value;

        public string StatusText {
            get {
                return $"{statusText}";
            }
            set {
                statusText = value;
                Application.Current.Dispatcher.Invoke((Action)delegate {
                    if (statusText == ClientState.UnAssigned.Value) {
                        assignBtn.IsEnabled = true;
                        prepBtn.IsEnabled = false;
                        execBtn.IsEnabled = false;
                        stopBtn.IsEnabled = false;
                        viewBtn.IsEnabled = false;
                    }
                    if (statusText == ClientState.Assigned.Value) {
                        assignBtn.IsEnabled = true;
                        prepBtn.IsEnabled = true;
                        execBtn.IsEnabled = false;
                        stopBtn.IsEnabled = false;
                        viewBtn.IsEnabled = true;
                    }
                    if (statusText == ClientState.Ready.Value) {
                        assignBtn.IsEnabled = true;
                        prepBtn.IsEnabled = true;
                        execBtn.IsEnabled = true;
                        stopBtn.IsEnabled = false;
                        viewBtn.IsEnabled = true;
                    }
                    if (statusText == ClientState.Executing.Value || statusText == ClientState.WaitingNextIteration.Value || statusText == ClientState.ExecutionPending.Value) {
                        assignBtn.IsEnabled = false;
                        prepBtn.IsEnabled = false;
                        execBtn.IsEnabled = false;
                        stopBtn.IsEnabled = true;
                        viewBtn.IsEnabled = true;
                    }
                    if (statusText == ClientState.Stopped.Value) {
                        assignBtn.IsEnabled = true;
                        prepBtn.IsEnabled = true;
                        execBtn.IsEnabled = false;
                        stopBtn.IsEnabled = false;
                        viewBtn.IsEnabled = true;
                    }
                    OnPropertyChanged("StatusText");
                });
            }
        }

        private CompletionReport completionReport;

        public CompletionReport CompletionReport {
            get { return completionReport; }
            set {
                completionReport = value;
                OnPropertyChanged("CompletionReport");
                Console.WriteLine(completionReport.ToString());
                OnPropertyChanged("CompletionReportString");
            }
        }

        public string CompletionReportString {
            get { return CompletionReport.ToString(); }
        }

        private string ip;

        public string IP {
            get {
                return $"Client IP: {ip}";
            }
            set {
                ip = value;
                OnPropertyChanged("IP");
            }
        }

        private string processID;

        public string ProcessID {
            get {
                return $"Process ID: {processID}";
            }
            set {
                processID = value;
                OnPropertyChanged("ProcessID");
            }
        }

        public string XML { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private MainCommandCenterController cCController;

        public void OnPropertyChanged(string propName) {
            try {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            } catch (Exception) {
                // NO-OP
            }
        }

        public ClientControl(string connectionID, CentralMessagingHub messageHub, MainCommandCenterController cCController) {
            InitializeComponent();
            DataContext = this;
            ConnectionID = connectionID;
            MessageHub = messageHub;
            this.cCController = cCController;
        }

        private void Prep_OnClick(object sender, RoutedEventArgs e) {
            cCController.View.nodeTabHolder.SelectedItem = cCController.clientTabControls[ConnectionID];
            MessageHub.Hub.Clients.Client(ConnectionID).ClearAndPrepare();
        }

        private void Exec_OnClick(object sender, RoutedEventArgs e) {
            cCController.View.nodeTabHolder.SelectedItem = cCController.clientTabControls[ConnectionID];
            this.cCController.ExecuteClient(ConnectionID);
        }

        private void Stop_OnClick(object sender, RoutedEventArgs e) {
            MessageHub.Hub.Clients.Client(ConnectionID).Stop();
        }

        private void Status_OnClick(object sender, RoutedEventArgs e) {
            this.cCController.ShowTab(ConnectionID);
        }

        private void Disconnect_OnClick(object sender, RoutedEventArgs e) {
            this.cCController.DisconnectClient(ConnectionID);
        }

        private void Assign_OnClick(object sender, RoutedEventArgs e) {
            string archiveRoot = cCController.ArchiveRoot;
            Directory.CreateDirectory(archiveRoot);

            OpenFileDialog open = new OpenFileDialog {
                Filter = "Load Injector Archive Files(*.lia)|*.lia",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (open.ShowDialog() == true) {
                cCController.ClearGridData(ConnectionID);
                cCController.View.nodeTabHolder.SelectedItem = cCController.clientTabControls[ConnectionID];
                File.Copy(open.FileName, archiveRoot + "\\" + open.SafeFileName, true);

                ClientTabControl tabControl = cCController.clientTabControls[ConnectionID];
                tabControl.WorkPackage = open.SafeFileName;
                tabControl.OnPropertyChanged("WorkPackage");

                MessageHub.Hub.Clients.Client(ConnectionID).RetrieveArchive(cCController.WebServerURL + "/" + open.SafeFileName);
            }
        }

        public void SetStatusText(string message) {
            StatusText = message;
        }

        internal void SetCompletionReportText(CompletionReport report) {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                CompletionReport = report;
            });
        }

        public void SaveExcelCompletionReport() {
            if (iterationRecords == null) {
                MessageBox.Show("The Completion Report has not been retrieved yet. Please use the \"Completion Report\" button to retrieve", "Completion Report Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            ExcelPackage excel = new ExcelPackage();

            foreach (IterationRecord itRec in iterationRecords.Records) {
                var workSheet = excel.Workbook.Worksheets.Add($"Iteration {itRec.IterationNumber}");

                workSheet.Cells[1, 1].Value = "Execution Node IP";
                workSheet.Cells[2, 1].Value = "Execution Process";
                workSheet.Cells[3, 1].Value = "Work Package";

                workSheet.Cells[5, 1].Value = "Execution Start";
                workSheet.Cells[6, 1].Value = "Execution End";

                workSheet.Column(1).AutoFit();
                workSheet.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                workSheet.Column(1).Style.Font.Bold = true;

                workSheet.Cells[1, 2].Value = iterationRecords.IPAddress;
                workSheet.Cells[2, 2].Value = iterationRecords.ProcessID;
                workSheet.Cells[3, 2].Value = iterationRecords.WorkPacakage;

                workSheet.Cells[5, 2].Value = itRec.ExecutionStart.ToString("yyyy-MM-dd  HH:mm:ss");
                workSheet.Cells[6, 2].Value = itRec.ExecutionEnd.ToString("yyyy-MM-dd  HH:mm:ss");
                workSheet.Column(2).AutoFit();

                workSheet.Cells[7, 3].Value = "Sources:";
                workSheet.Row(7).Style.Font.Bold = true;
                workSheet.Cells[8, 4].Value = "Type";
                workSheet.Cells[8, 5].Value = "Description";
                workSheet.Column(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                workSheet.Column(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                workSheet.Cells[8, 6].Value = "Triggers Fired";
                workSheet.Column(6).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Cells[8, 8].Value = "Source";
                workSheet.Row(8).Style.Font.Bold = true;

                int row = 9;
                foreach (LineRecord rec in itRec.SourceLineRecords) {
                    workSheet.Cells[row, 4].Value = rec.SourceType;
                    workSheet.Cells[row, 5].Value = rec.Name;
                    workSheet.Cells[row, 6].Value = rec.MessagesSent;
                    workSheet.Cells[row, 8].Value = rec.Description;
                    row++;
                }

                workSheet.Column(4).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                workSheet.Column(5).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                workSheet.Column(6).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Column(7).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Column(8).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                row++;

                workSheet.Cells[row, 3].Value = "Destinations:";
                workSheet.Row(row).Style.Font.Bold = true;
                row++;
                workSheet.Cells[row, 4].Value = "Type";
                workSheet.Cells[row, 5].Value = "Description";
                workSheet.Cells[row, 6].Value = "Messages Sent";
                workSheet.Cells[row, 7].Value = "Messages Fail";
                workSheet.Cells[row, 8].Value = "Destination";
                workSheet.Row(row).Style.Font.Bold = true;
                row++;
                foreach (LineRecord rec in itRec.DestinationLineRecords) {
                    workSheet.Cells[row, 4].Value = rec.DestinationType;
                    workSheet.Cells[row, 5].Value = rec.Name;
                    workSheet.Cells[row, 6].Value = rec.MessagesSent;
                    workSheet.Cells[row, 7].Value = rec.MessagesFailed;
                    workSheet.Cells[row, 8].Value = rec.Description;
                    row++;
                }

                workSheet.Column(3).AutoFit();
                workSheet.Column(4).AutoFit();
                workSheet.Column(5).AutoFit();
                workSheet.Column(6).AutoFit();
                workSheet.Column(7).AutoFit();
                workSheet.Column(8).AutoFit();
            }

            SaveFileDialog dialog = new SaveFileDialog {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };
            if (dialog.ShowDialog() == true) {
                if (File.Exists(dialog.FileName))
                    File.Delete(dialog.FileName);

                // Create excel file on physical disk
                FileStream objFileStrm = File.Create(dialog.FileName);
                objFileStrm.Close();

                // Write content to excel file
                File.WriteAllBytes(dialog.FileName, excel.GetAsByteArray());
                //Close Excel package
            }

            excel.Dispose();
        }
    }
}