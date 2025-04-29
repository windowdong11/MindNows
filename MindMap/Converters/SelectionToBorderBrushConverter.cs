using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace MindMap.Converters
{
    internal class SelectionToBorderBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush SelectedBrush = new(Color.FromArgb(70, 0xE3, 0xE3, 0xF6)); // #E3E3F6
        private static readonly SolidColorBrush DefaultBrush = new(Colors.Gray);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return SelectedBrush;
            }
            return DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
