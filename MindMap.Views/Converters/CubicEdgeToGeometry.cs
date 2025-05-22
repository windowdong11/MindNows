using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MindMap.ViewModels;
using System.Windows;
using System.Drawing;
using Point = System.Windows.Point;

namespace MindMap.Views.Converters;

//public sealed class CubicEdgeToGeometry : IValueConverter
//{
//    public object? Convert(object? value, Type t, object? p, CultureInfo c)
//    {
//        if (value is not CubicEdge e) return null;

//        var fig = new PathFigure { StartPoint = PF(e.Start) };
//        fig.Segments.Add(new BezierSegment(PF(e.C1), PF(e.C2), PF(e.End), true));

//        return new PathGeometry(new[] { fig });
//    }

//    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
//        => throw new NotSupportedException();

//    static Point PF(PointF f) => new(f.X, f.Y);
//}
