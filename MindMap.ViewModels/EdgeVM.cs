using MindMap.Core.Models;
using System.Drawing;                // PointF, SizeF
using CommunityToolkit.Mvvm.ComponentModel;

namespace MindMap.ViewModels;
public sealed class EdgeVM : ObservableObject
{
    private readonly NodeVM _parent;
    private readonly NodeVM _child;

    // 임시 고정 값 (Phase 7에 NodeView ActualSize 로 치환)
    private static readonly SizeF NodeSize = new(120, 48);

    public EdgeVM(NodeVM parent, NodeVM child)
    {
        _parent = parent;
        _child = child;
        Refresh();
    }

    private EdgeInfo _info;
    public EdgeInfo Info
    {
        get => _info;
        private set => SetProperty(ref _info, value);
    }

    public void Refresh()
    {
        bool right = _child.Model.Side == SideEnum.Right;
        var p = _parent.Model.Position;
        var c = _child.Model.Position;

        var start = new PointF(
            p.X + (right ? NodeSize.Width : 0),
            p.Y + NodeSize.Height / 2);

        var end = new PointF(
            c.X + (right ? 0 : NodeSize.Width),
            c.Y + NodeSize.Height / 2);

        float dx = 30 * (right ? 1 : -1);
        var c1 = new PointF(start.X + dx, start.Y);
        var c2 = new PointF(end.X - dx, end.Y);

        Info = new EdgeInfo(start, c1, c2, end);
    }
}