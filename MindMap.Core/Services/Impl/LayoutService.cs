using MindMap.Core.Layout;
using MindMap.Core.Models;
using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed class LayoutService : ILayoutService
{
    private const int HGap = 48;
    private const int VGap = 24;

    private readonly Dictionary<NodeModel, NodeLayout> _cache = new();

    public void Arrange(NodeModel root)
    {
        _cache.Clear();
        Measure(root);
        ArrangeInternal(root, new PointF(0, 0));
    }

    /// <summary>재귀적으로 서브트리 크기 계산</summary>
    private SizeF Measure(NodeModel node)
    {
        var textWidth = 120f; // 임시 노드 최소폭
        var textHeight = 48f;  // 임시 노드 높이

        var ownSize = new SizeF(textWidth, textHeight);

        // 자식 Measure
        var leftChildren = node.Children.Where(c => c.Side == SideEnum.Left).ToList();
        var rightChildren = node.Children.Where(c => c.Side == SideEnum.Right).ToList();

        var leftSize = PackVertical(leftChildren);
        var rightSize = PackVertical(rightChildren);

        // 전체 너비 = 왼쪽 서브트리 + HGap + ownSize + HGap + 오른쪽 서브트리
        var totalWidth = leftSize.Width + (leftChildren.Count > 0 ? HGap : 0)
                        + ownSize.Width
                        + (rightChildren.Count > 0 ? HGap : 0) + rightSize.Width;

        // 전체 높이 = max(왼쪽 높이, ownSize.Height, 오른쪽 높이)
        var totalHeight = MathF.Max(MathF.Max(leftSize.Height, rightSize.Height), ownSize.Height);

        var subtree = new SizeF(totalWidth, totalHeight);
        _cache[node] = new NodeLayout(subtree, Point.Empty);
        return subtree;

        SizeF PackVertical(IEnumerable<NodeModel> nodes)
        {
            float w = 0, h = 0;
            foreach (var c in nodes)
            {
                var sz = Measure(c);
                w = MathF.Max(w, sz.Width);
                if (h > 0) h += VGap;
                h += sz.Height;
            }
            return new SizeF(w, h);
        }
    }

    /// <summary>재귀 Arrange – 이미 Measure 정보가 _cache 에 있음</summary>
    private void ArrangeInternal(NodeModel node, PointF origin)
    {
        // 1) 자신의 레이아웃 정보
        var info = _cache[node];
        var ownSize = new SizeF(120, 48);

        // 부모 중앙 y 계산용
        var leftChildren = node.Children.Where(c => c.Side == SideEnum.Left).ToList();
        var rightChildren = node.Children.Where(c => c.Side == SideEnum.Right).ToList();

        float leftW = leftChildren.Count > 0 ? _cache[leftChildren[0]].SubtreeSize.Width : 0;
        float rightW = rightChildren.Count > 0 ? _cache[rightChildren[0]].SubtreeSize.Width : 0;

        // 자신의 좌표 = origin + (leftWidth + optional HGap, centeredY)
        var x = origin.X + leftW + (leftChildren.Count > 0 ? HGap : 0);
        var y = origin.Y + (info.SubtreeSize.Height - ownSize.Height) / 2;

        node.Position = new Point((int)x, (int)y);

        // 2) 왼쪽 자식들 Arrange 수직 스택
        float currentY = origin.Y;
        foreach (var c in leftChildren)
        {
            ArrangeInternal(c, new PointF(origin.X, currentY));
            currentY += _cache[c].SubtreeSize.Height + VGap;
        }

        // 3) 오른쪽 자식들 Arrange
        currentY = origin.Y;
        float rightOriginX = x + ownSize.Width + HGap;
        foreach (var c in rightChildren)
        {
            ArrangeInternal(c, new PointF(rightOriginX, currentY));
            currentY += _cache[c].SubtreeSize.Height + VGap;
        }
    }
}

