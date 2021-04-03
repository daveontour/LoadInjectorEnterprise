using LoadInjector.Common;
using LoadInjector.RunTime;
using LoadInjector.RunTime.Views;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace LoadInjector.Destinations {

    internal class MsSql : IDestinationType {
        public string name = "MSSQL";
        public string description = "MS SQL Client";

        public string ProtocolName => name;
        public string ProtocolDescription => description;

        public LoadInjectorGridBase GetConfigGrid(XmlNode dataModel, IView view) {
            return new MsSqlPropertyGrid(dataModel, view);
        }

        public SenderAbstract GetDestinationSender() {
            return new DestinationMsSql();
        }
    }

    public class DestinationMsSql : SenderAbstract {
        private string connStr;
        public bool showResults;
        private TextOutWindow win;

        public override bool Configure(XmlNode defn, IDestinationEndPointController controller, Logger logger) {
            base.Configure(defn, controller, logger);

            try {
                showResults = bool.Parse(defn.Attributes["showResults"].Value);
            } catch (Exception) {
                showResults = false;
            }
            try {
                connStr = defn.Attributes["connStr"].Value;
            } catch (Exception) {
                Console.WriteLine($"No Connection String defined for { defn.Attributes["name"].Value}");
                return false;
            }

            return true;
        }

        public override void Send(string val, List<Variable> vars) {
            foreach (Variable v in vars) {
                try {
                    connStr = connStr.Replace(v.token, v.value);
                } catch (Exception) {
                    //NO-OP
                }
            }

            string result = SendData(val);

            if (showResults) {
                win.WriteLine($"{result}\n");
            }
        }

        public string SendData(string sql) {
            SqlConnection cnn = new SqlConnection(connStr);

            try {
                cnn.Open();
                SqlCommand command = new SqlCommand(sql, cnn);
                SqlDataReader dataReader = command.ExecuteReader();

                StringBuilder bld = new StringBuilder();

                while (dataReader.Read()) {
                    bld.Append(dataReader.GetValue(0) + " - " + dataReader.GetValue(1) + "\n");
                }

                cnn.Close();

                return bld.ToString();
            } catch (Exception ex) {
                Console.WriteLine($"Send Data Error: {ex.Message}");
                return ($"Send Data Error: {ex.Message}");
            }
        }

        public override void Prepare() {
            base.Prepare();

            if (showResults) {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
                    win = new TextOutWindow {
                        Title = "SQL Command Output"
                    };
                    win.Show();
                }));
            }
        }
    }

    [RefreshProperties(RefreshProperties.All)]
    [DisplayName("MSSQL Protocol Configuration")]
    public class MsSqlPropertyGrid : LoadInjectorGridBase {

        public MsSqlPropertyGrid(XmlNode dataModel, IView view) {
            _node = dataModel;
            this.view = view;
            type = "MSSQL";
        }

        [DisplayName("Connection String (tokenizeable)"), ReadOnly(false), Browsable(true), PropertyOrder(1), DescriptionAttribute("The connection  string to connect to the SQL Server.")]
        public string ConnectionString {
            get => GetAttribute("connStr");
            set => SetAttribute("connStr", value);
        }

        [DisplayName("Show Output Window"), ReadOnly(false), Browsable(true), PropertyOrder(3), DescriptionAttribute("Show the output in a window")]
        public bool ShowResults {
            get => GetBoolAttribute("showResults");
            set => SetAttribute("showResults", value);
        }
    }
}