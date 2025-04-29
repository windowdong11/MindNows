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
    internal class SelectionToBorderThicknessConverter : IValueConverter
    {
        private static readonly Thickness SelectedThickness = new Thickness(6); // 선택 시 더 두껍게
        private static readonly Thickness DefaultThickness = new Thickness(0); // 기본

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return SelectedThickness;
            }
            return DefaultThickness;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
