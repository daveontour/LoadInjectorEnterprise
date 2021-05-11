using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LoadInjector.RunTime {
    public class LineStyleSelector : IMultiValueConverter {
        public Style LineStyle { get; set; }

        public Style AMDDirectStyle { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {

            FrameworkElement targetElement = values[0] as FrameworkElement;
            string protocol = values[1] as string;

            if (protocol == null) {
                return null;
            }

            Style newStyle;

            if (protocol == "amsdirect") {
                newStyle = (Style)targetElement.TryFindResource("AMSDirectStyle");
            } else {
                newStyle = (Style)targetElement.TryFindResource("LineStyle");
            }

            return newStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
