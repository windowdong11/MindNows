using MindMap.Core.Models;
using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace MindMap.Views.DragPreview;

public sealed class PlaceholderAdorner : PreviewAdornerBase
{
    private static readonly Pen DashPen = new(new SolidColorBrush(Colors.DodgerBlue), 2)
    { DashStyle = new DashStyle(new double[] { 4, 4 }, 0) };

    public PlaceholderAdorner(Canvas adorned, IDragDropService svc)
        : base(adorned, svc) { }

    protected override void OnRender(DrawingContext dc)
    {
        if (DragSvc.State.Phase is not Core.Services.DragDrop.DragPhase.Dragging) return;
        if (DragSvc.State.HoverAttachTarget is NodeModel tgt) return;
        if (DragSvc.State.HoverGap is not { } g) return;

        var current = DragSvc.State.Selection;
        if (current is null)
        {
            throw new InvalidOperationException("Selection is null during placeholder rendering.");
        }
        var currentIdx = g.parent.Children.IndexOf(current);
        var placeholderIndex = g.index > currentIdx ? g.index - 1 : g.index;
        var yTop = g.parent.Children.Count > 0 ?
            g.parent.Children[placeholderIndex].Position.Y
            : g.parent.Position.Y;
        var x = g.parent.Side == SideEnum.Right
              ? g.parent.Position.X + 120 + 48
              : g.parent.Position.X - 16;

        var rect = new Rect(x, yTop, 120, 48);
        dc.DrawRectangle(null, DashPen, rect);
    }
}
