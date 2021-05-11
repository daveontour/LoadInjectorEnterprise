using System;
using System.Windows;
using System.Windows.Data;

namespace LoadInjector.RunTime {

    public class MarginConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            int depth = (int)value;

            return new Thickness(depth * 25, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            return null;
        }
    }
}