using MindMap.Core.Models;
using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Size = System.Windows.Size;
using Point = System.Windows.Point;

namespace MindMap.Views.DragPreview;

public sealed class TooltipAdorner : PreviewAdornerBase
{
    private readonly FormattedText _text;

    public TooltipAdorner(Canvas adorned, IDragDropService svc)
        : base(adorned, svc)
    {
        _text = new FormattedText(
            "Attach",
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12, Brushes.White, 1.0);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (DragSvc.State.Phase is not Core.Services.DragDrop.DragPhase.Dragging) return;
        if (DragSvc.State.HoverAttachTarget is not NodeModel tgt) return;

        var pos = ToVisual(new PointF(tgt.Position.X, tgt.Position.Y - 20));
        var tooltipRect = new Rect(pos, new Size(_text.Width + 8, _text.Height + 4));
        dc.DrawRoundedRectangle(Brushes.Black, null, tooltipRect, 4, 4);
        dc.DrawText(_text, new Point(tooltipRect.Left + 4, tooltipRect.Top + 2));
    }
}
