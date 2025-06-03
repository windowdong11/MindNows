using MindMap.Core.Models;
using System.Drawing;                // PointF, SizeF
using CommunityToolkit.Mvvm.ComponentModel;

namespace MindMap.ViewModels;
public sealed class EdgeVM : ObservableObject
{
    private readonly NodeVM _parent;
    private readonly NodeVM _child;
    private static readonly float Hoffset = 3.0f;

    public NodeVM Parent => _parent;
    public NodeVM Child => _child;


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
        var parentWidth = _parent.NodeWidth;
        var parentHeight = _parent.NodeHeight;
        var parentPos = _parent.Model.Position;
        var childWidth = _child.NodeWidth;
        var childHeight = _child.NodeHeight;
        var c = _child.Model.Position;

        var start = new PointF(
            parentPos.X + (right ? (float)parentWidth - Hoffset: Hoffset) ,
            parentPos.Y + (float)parentHeight);

        var end = new PointF(
            c.X + (right ? Hoffset : (float)childWidth - Hoffset),
            c.Y + (float)childHeight);

        float dx = 30 * (right ? 1 : -1);
        var c1 = new PointF(start.X + dx, start.Y);
        var c2 = new PointF(end.X - dx, end.Y);

        Info = new EdgeInfo(start, c1, c2, end);
    }
}