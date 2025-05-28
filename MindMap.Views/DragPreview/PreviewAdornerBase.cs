using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using Point = System.Windows.Point;

namespace MindMap.Views.DragPreview;

public abstract class PreviewAdornerBase : Adorner
{
    protected readonly IDragDropService DragSvc;
    protected readonly Canvas World;

    protected PreviewAdornerBase(Canvas adorned, IDragDropService svc)
        : base(adorned)
    {
        World = adorned;
        DragSvc = svc;
        DragSvc.StateChanged += (_, _) => InvalidateVisual();
        IsHitTestVisible = false;
    }

    protected Point ToVisual(PointF world)
    {
        return new Point(world.X, world.Y); // World 이미 뷰 좌표. Pan/Zoom 시 Transform 덮어씀
    }
}