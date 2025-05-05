using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace MindMap.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    internal class BoolToVisibilityConverter : IValueConverter
    {
        public bool Inverted { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;

            if (Inverted)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                return Inverted ? vis != Visibility.Visible : vis == Visibility.Visible;
            }
            return false;
        }
    }
}
