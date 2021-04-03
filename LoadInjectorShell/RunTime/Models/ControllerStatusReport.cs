using System;
using System.Windows;

namespace LoadInjector.RunTime.Models {
    public class ControllerStatusReport {
        public bool Prepare { get; set; }
        public bool Stop { get; set; }
        public bool PrepareExecute { get; set; }
        public string SchedStart { get; set; }
        public bool Execute { get; set; }
        public int PercentComplete { get; set; }
        public bool ClearConsole { get; set; }
        public string Timestr { get; set; }
        public string OutputString { get; set; }
        public int OutputInt { get; set; }
        public double OutputDouble { get; set; }
        public UIElement UiElement { get; set; }
        public double Sent { get; set; }
        public double Config { get; set; }
        public double Actual { get; set; }
        public string Consolestr { get; set; }
        public string Label { get; set; }
        public Operation Type { get; set; }

        [Flags]
        public enum Operation {
            None = 0b_0000_0000_0000_0000,  // 0
            Console = 0b_0000_0000_0000_0001,  // 1
            ExecuteBtn = 0b_0000_0000_0000_0010,  // 2
            PrepareBtn = 0b_0000_0000_0000_0100,  // 4
            StopBtn = 0b_0000_0000_0000_1000,  // 8
            SchedStart = 0b_0000_0000_0001_0000,  // 16

            Percent = 0b_0000_0000_0010_0000,  // 32
            ClearConsole = 0b_0000_0000_0100_0000,  // 64
            TimeStr = 0b_0000_0000_1000_0000,  // 128
            AddUILineElement = 0b_0000_0001_0000_0000,  // 128
            AddUILabel = 0b_0000_0010_0000_0000,
            LineSent = 0b_0000_0100_0000_0000,
            LineRate = 0b_0000_1000_0000_0000,
            LineMsgPerMin = 0b_0001_0000_0000_0000,
            LineConfiguredMsgPerMin = 0b_0010_0000_0000_0000,
            PrepareExecuteBtn = 0b_0100_0000_0000_0000,
            LineConfigRate = 0b_1000_0000_0000_0000,
            AllButtons = ExecuteBtn | PrepareBtn | StopBtn | PrepareExecuteBtn,
            LineReport = Console | LineSent | LineConfigRate | LineRate,
            DestinataionSendReport = LineSent | LineRate

        }
    }
}
