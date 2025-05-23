﻿// Converters/SelectionToBorderThicknessConverter.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MindMap.Converters
{
    internal class SelectionToBorderThicknessConverter : IValueConverter
    {
        private static readonly Thickness SelectedThickness = new Thickness(6); // 선택 시 더 두껍게
        private static readonly Thickness DefaultThickness = new Thickness(6); // 기본

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
