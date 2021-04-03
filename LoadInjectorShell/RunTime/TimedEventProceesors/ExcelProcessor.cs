using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;

namespace LoadInjector.RunTime.EngineComponents {

    public class ExcelProcessor {

        public List<Dictionary<String, String>> GetRecords(string file, string sheet, List<string> columns, string type, string format, int start, int end, bool updateFormulas) {
            List<Dictionary<String, String>> records = new List<Dictionary<String, String>>();
            if (format == null) {
                format = "G";
            }
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            try {
                using (var package = new ExcelPackage(new FileInfo(file))) {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[sheet];

                    for (int i = start; i <= end; i++) {
                        Dictionary<string, string> record = new Dictionary<string, string>();

                        foreach (string col in columns) {
                            try {
                                string cellID = col + i;
                                ExcelRange cell = worksheet.Cells[cellID];
                                if ((cell.Formula.Contains("NOW()") || cell.Formula.Contains("TODAY()")) && updateFormulas && type == "DateTime") {
                                    cell.Formula = cell.Formula?.Replace("YYYY", "yyyy");
                                    cell.Formula = cell.Formula?.Replace("DD", "dd");
                                    worksheet.Cells[cellID].Calculate();
                                }

                                if (record.ContainsKey(col)) {
                                    record.Remove(col);
                                    // Console.WriteLine("Column is being reused");
                                }
                                record.Add(col, worksheet.Cells[cellID].Text);
                            } catch (Exception ex) {
                                Console.WriteLine($"Error retrieving Excel records 1: {ex.Message}");
                            }
                        }

                        records.Add(record);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Error retrieving Excel records: {ex.Message}");
            }
            return records;
        }

        public List<string> GetList(string file, string sheet, string column, string type, string format, int start, int end, bool updateFormulas) {
            List<string> values = new List<string>();
            if (format == null) {
                format = "G";
            }
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage(new FileInfo(file))) {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheet];

                for (int i = start; i <= end; i++) {
                    string cellID = column + i;
                    ExcelRange cell = worksheet.Cells[cellID];
                    if ((cell.Formula.Contains("NOW()") || cell.Formula.Contains("TODAY()")) && updateFormulas && type == "DateTime") {
                        cell.Formula = cell.Formula?.Replace("YYYY", "yyyy");
                        cell.Formula = cell.Formula?.Replace("DD", "dd");
                        worksheet.Cells[cellID].Calculate();
                    }

                    object cellValue = worksheet.Cells[cellID].Value;
                    string value = null;

                    if (cellValue != null) {
                        switch (type) {
                            case "DateTime":
                                if (cellValue is double @double) {
                                    DateTime t = DateTime.FromOADate(@double);
                                    value = t.ToString(format);
                                } else if (cellValue is DateTime time) {
                                    value = time.ToString(format);
                                } else if (cellValue is String @string) {
                                    value = @string;
                                }
                                break;

                            case "Number":
                                value = Convert.ToDouble(cellValue).ToString(format);
                                break;

                            case "Text":
                                value = worksheet.Cells[cellID].Value.ToString();
                                break;

                            default:
                                value = worksheet.Cells[cellID].Value.ToString();
                                break;
                        }
                    }

                    values.Add(value);
                }
                return values;
            }
        }

        public string Lookup(string value, string dataFile, string excelLookupSheet, string excelKeyColumn, string excelValueColumn) {
            var file = new FileInfo(dataFile);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage(file)) {
                var sheet = package.Workbook.Worksheets[excelLookupSheet];
                sheet.Calculate();

                for (int i = 1; i <= 10000; i++) {
                    string cell = $"{excelKeyColumn}{i}";
                    object cellvalue;

                    try {
                        cellvalue = sheet.Cells[cell].Value;
                    } catch (Exception ex) {
                        Console.WriteLine(ex.Message);
                        continue;
                    }

                    if (cellvalue.ToString() == value) {
                        try {
                            return sheet.Cells[$"{excelValueColumn}{i}"].Value.ToString();
                        } catch (Exception ex) {
                            Console.WriteLine(ex.Message);
                            return value;
                        }
                    }
                }
            }

            return value;
        }
    }
}