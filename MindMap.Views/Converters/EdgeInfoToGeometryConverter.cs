using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MindMap.Core.Models;
using System.Drawing;

namespace MindMap.Views.Converters;

public sealed class EdgeInfoToGeometryConverter : IValueConverter
{
    public object? Convert(object? value, Type t, object? p, CultureInfo c)
    {
        if (value is not EdgeInfo e) return null;

        var fig = new PathFigure { StartPoint = ToPt(e.Start), IsClosed = false };
        fig.Segments.Add(new BezierSegment(
            ToPt(e.Ctrl1), ToPt(e.Ctrl2), ToPt(e.End), true));

        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        return geo;

        static System.Windows.Point ToPt(PointF f) => new(f.X, f.Y);
    }

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}
