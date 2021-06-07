using LoadInjectorBase;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Xml;

namespace LoadInjector.Destinations {

    public class DestinationMsSql : DestinationAbstract {
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

        public override bool Send(string val, List<Variable> vars) {
            foreach (Variable v in vars) {
                try {
                    connStr = connStr.Replace(v.token, v.value);
                } catch (Exception) {
                    //NO-OP
                }
            }

            return SendData(val);
        }

        public bool SendData(string sql) {
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

                if (showResults) {
                    Console.WriteLine(bld.ToString());
                }
                return true;
            } catch (Exception ex) {
                Console.WriteLine($"Send Data Error: {ex.Message}");
                return false;
            }
        }
    }
}