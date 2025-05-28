using MindMap.Core.Services.DragDrop;
using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Size = System.Windows.Size;
using Color = System.Windows.Media.Color;

namespace MindMap.Views.DragPreview;

public sealed class GhostAdorner : PreviewAdornerBase
{
    private static readonly Brush Fill = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));

    public GhostAdorner(Canvas adorned, IDragDropService svc)
        : base(adorned, svc) { }

    protected override void OnRender(DrawingContext dc)
    {
        if (DragSvc.State.Phase != DragPhase.Dragging) return;
        var sel = DragSvc.State.Selection;
        var offset = DragSvc.State.CursorOffset;
        var pos = DragSvc.State.CurrentWorldPos;

        var w = new PointF(pos.X + offset.X, pos.Y + offset.Y);
        var rect = new Rect(ToVisual(w), new Size(120, 48));
        dc.DrawRoundedRectangle(Fill, null, rect, 6, 6);
        //foreach (var n in sel)
        //{
        //}
    }
}
