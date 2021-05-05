using LoadInjectorBase;
using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LoadInjector.Destinations {

    public class DestinationOracle : DestinationAbstract {
        private string connStr;
        public bool showResults;

        public override bool Configure(XmlNode node, IDestinationEndPointController cont, Logger log) {
            base.Configure(node, cont, log);

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

            var result = SendData(val);

            if (showResults) {
                Console.WriteLine($"{result}\n");
            }
        }

        public string SendData(string sql) {
            OracleConnection cnn = new OracleConnection(connStr);

            try {
                cnn.Open();
                OracleCommand command = new OracleCommand(sql, cnn);
                OracleDataReader dataReader = command.ExecuteReader();

                StringBuilder sbld = new StringBuilder();
                while (dataReader.Read()) {
                    sbld.Append(dataReader.GetValue(0) + " - " + dataReader.GetValue(1) + "\n");
                }

                cnn.Close();

                return sbld.ToString();
            } catch (Exception ex) {
                Console.WriteLine($"Send Data Error: {ex.Message}");
                return $"Send Data Error: {ex.Message}";
            }
        }
    }
}