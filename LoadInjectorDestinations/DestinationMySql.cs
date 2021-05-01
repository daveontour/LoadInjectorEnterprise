using LoadInjectorBase;
using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Xml;

namespace LoadInjector.Destinations {

    public class DestinationMySql : SenderAbstract {
        private string connStr;
        private bool showResults;

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
                    // NO-OP
                }
            }

            string result = SendData(val);

            if (showResults) {
                Console.WriteLine($"{result}\n");
            }
        }

        public string SendData(string sql) {
            MySqlConnection cnn = new MySqlConnection(connStr);

            try {
                cnn.Open();
                MySqlCommand command = new MySqlCommand(sql, cnn);
                MySqlDataReader dataReader = command.ExecuteReader();

                StringBuilder sbld = new StringBuilder();
                while (dataReader.Read()) {
                    sbld.Append(dataReader.GetValue(0) + " - " + dataReader.GetValue(1) + "\n");
                }

                cnn.Close();

                return sbld.ToString();
            } catch (Exception ex) {
                Console.WriteLine($"Send Data Error: {ex.Message}");
                return ($"Send Data Error: {ex.Message}");
            }
        }

        public override void Prepare() {
            base.Prepare();
        }
    }
}