using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MindMap.Views.Converters;

public sealed class ImagePathToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type t, object? p, CultureInfo c)
    {
        if (value is not string path || path.Length == 0) return null;
        return new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
    }

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}
