namespace LoadInjectorBase.Interfaces {

    public interface IExcelProcessor {

        string Lookup(string value, string dataFile, string excelLookupSheet, string excelKeyColumn, string excelValueColumn);
    }
}