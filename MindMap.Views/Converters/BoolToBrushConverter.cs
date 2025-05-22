using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MindMap.Views.Converters;

public sealed class BoolToBrushConverter : IValueConverter
{
    public Brush TrueBrush { get; set; } = Brushes.Gray;      // 선택됨
    public Brush FalseBrush { get; set; } = Brushes.Transparent; // 기본

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? TrueBrush : FalseBrush;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
