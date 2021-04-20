﻿using LoadInjector.RunTime.Models;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Xml;
using static LoadInjector.RunTime.Models.ControllerStatusReport;

namespace LoadInjector.RunTime {

    public interface SourceUI {

        void SetOutput(string s);

        void SetActualRate(double s);

        void SeConfigRate(double s);

        void SetMessagesSent(double s);
    }

    public partial class TriggeredEventsUI : UserControl, SourceUI, INotifyPropertyChanged {
        public XmlNode node;
        private string executionNodeID;
        private string uuid;

        public string LineName { get; set; }
        public string Output { get; private set; }
        public double ActualRate { get; private set; }
        public double ConfigRate { get; private set; }
        public double MessagesSent { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public TriggeredEventsUI(XmlNode node) {
            LineName = node.Attributes["name"].Value;
            InitializeComponent();
            DataContext = this;
            this.node = node;
            this.executionNodeID = node.Attributes["executionNodeUuid"]?.Value;
            this.uuid = node.Attributes["uuid"]?.Value;
        }

        public void SetOutput(string s) {
            Output = s;
            OnPropertyChanged("Output");
        }

        public void SetActualRate(double s) {
            ActualRate = s;
            OnPropertyChanged("ActualRate");
        }

        public void SeConfigRate(double s) {
            ConfigRate = s;
            OnPropertyChanged("ConfigRate");
        }

        public void SetMessagesSent(double s) {
            MessagesSent = s;
            OnPropertyChanged("MessagesSent");
        }
    }
}