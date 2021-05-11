using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Media3D;
using System.Windows.Shell;
using System.Windows.Threading;
using System.Xml;

namespace LoadInjector.RunTime {

    public interface IExecutionUI {
        XmlDocument DataModel { get; set; }
        ObservableCollection<TriggerRecord> SchedTriggers { get; set; }
        ObservableCollection<TriggerRecord> FiredTriggers { get; set; }
        bool DisableScroll { get; set; }
        int PercentComplete { get; set; }
        string ElapsedString { get; set; }
        string StatusLabel { get; set; }
        string TriggerLabel { get; set; }
        Dispatcher Dispatcher { get; }

        Dictionary<string, LineUserControl> DestUIMap { get; }
        Dictionary<string, SourceUI> SourceUIMap { get; }
        LoadInjector.RunTime.Views.ControlWriter ConsoleWriter { get; }

        void OnPropertyChanged(string propName);

        void SetExecuteBtnEnabled(bool v);

        void SetPrepareBtnEnabled(bool v);

        void SetStopBtnEnabled(bool v);
    }
}