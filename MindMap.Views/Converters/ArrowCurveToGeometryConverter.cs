using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace MindMap.Views.Converters;

public class ArrowCurveToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ArrowCurvePoints c) return null;
        var fig = new PathFigure { StartPoint = new System.Windows.Point(c.Start.X, c.Start.Y), IsClosed = false };
        fig.Segments.Add(new QuadraticBezierSegment(
            new System.Windows.Point(c.Control.X, c.Control.Y),
            new System.Windows.Point(c.End.X, c.End.Y), true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        return geo;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
