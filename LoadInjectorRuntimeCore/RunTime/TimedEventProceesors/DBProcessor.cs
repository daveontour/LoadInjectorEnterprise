using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace LoadInjector.RunTime.EngineComponents
{
    public class DBProcessor
    {
        internal List<Dictionary<string, string>> GetRecords(string connStr, string sql, string dbType, List<string> dbElementsInUse)
        {
            var table = new DataTable();

            if (dbType == "mssql")
            {
                using (var da = new SqlDataAdapter(sql, connStr))
                {
                    da.Fill(table);
                }
            }
            if (dbType == "mysql")
            {
                using (var da = new MySqlDataAdapter(sql, connStr))
                {
                    da.Fill(table);
                }
            }
            if (dbType == "oracle")
            {
                using (var da = new OracleDataAdapter(sql, connStr))
                {
                    da.Fill(table);
                }
            }

            List<Dictionary<String, String>> records = new List<Dictionary<String, String>>();

            foreach (DataRow row in table.Rows)
            {
                Dictionary<string, string> record = new Dictionary<string, string>();

                foreach (string field in dbElementsInUse)
                {
                    if (record.ContainsKey(field))
                    {
                        continue;
                    }

                    record.Add(field, row.ItemArray[int.Parse(field)]?.ToString());
                }

                records.Add(record);
            }

            return records;
        }

        internal DataTable GetDataTable(XmlNode config)
        {
            string dbType = config.Attributes["sourceType"]?.Value;
            string sql = config.Attributes["sql"]?.Value;
            string connStr = config.Attributes["connStr"]?.Value;

            var table = new DataTable();

            if (dbType == "databaseMSSQL")
            {
                using (var da = new SqlDataAdapter(sql, connStr))
                {
                    da.Fill(table);
                }
            }
            if (dbType == "databaseMySQL")
            {
                using (var da = new MySqlDataAdapter(sql, connStr))
                {
                    da.Fill(table);
                }
            }
            if (dbType == "databaseOracle")
            {
                using (var da = new OracleDataAdapter(sql, connStr))
                {
                    da.Fill(table);
                }
            }
            return table;
        }
    }
}