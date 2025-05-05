using System;
using System.Globalization;
using System.Windows.Data;

namespace MindMap.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    internal class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return value;
        }
    }
}
