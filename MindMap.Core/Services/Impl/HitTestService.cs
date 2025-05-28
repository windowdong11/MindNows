using MindMap.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services.Impl;
public sealed class HitTestService : IHitTestService
{
    private readonly Dictionary<NodeModel, RectangleF> _bounds = new();
    private readonly Dictionary<NodeModel, NodeModel?> _parent = new();
    private const float NodeW = 120f, NodeH = 48f;         // Phase 7 에서 동적 값으로 교체
    private const float GapHalf = 12f;                     // 위·아래 Gap 높이

    public void BuildIndex(IEnumerable<NodeModel> roots)
    {
        _bounds.Clear(); _parent.Clear();
        foreach (var r in roots) DFS(r, null);
        void DFS(NodeModel n, NodeModel? p)
        {
            _bounds[n] = new RectangleF(n.Position.X, n.Position.Y, NodeW, NodeH);
            _parent[n] = p;
            foreach (var c in n.Children) DFS(c, n);
        }
    }

    public NodeModel? HitNode(PointF pos)
        => _bounds.FirstOrDefault(kv => kv.Value.Contains(pos)).Key;

    public (NodeModel parent, int insertIndex)? HitSiblingGap(
        NodeModel dragging, PointF pos)
    {
        if (!_parent.TryGetValue(dragging, out var parent) || parent is null)
            return null;
            //throw new InvalidOperationException("HitSiblingGap: No parent found for the dragging between sibling node.");

        var siblings = parent.Children;

        var topY = _bounds[siblings[0]].Bottom - GapHalf;
        if (pos.Y <= topY)
            return (parent, 0);  // 첫 번째 형제 위에 위치
        var bottomY = _bounds[siblings[^1]].Bottom;
        if (pos.Y >= bottomY)
            return (parent, siblings.Count);  // 마지막 형제 아래에 위치


        for (int i = 0; i <= siblings.Count; i++)
        {
            var upperY = i == 0
                ? _bounds[siblings[0]].Bottom - GapHalf
                : _bounds[siblings[i - 1]].Bottom;

            var lowerY = i == siblings.Count
                ? _bounds[siblings[^1]].Bottom
                : _bounds[siblings[i]].Bottom - GapHalf;

            //var left = siblings[0].Side == SideEnum.Left
            //           ? _bounds[siblings[0]].Right - NodeW   // 정렬 보조
            //           : _bounds[siblings[0]].Left;

            //var gapRect = new RectangleF(left, upperY,
            //    NodeW, lowerY - upperY);
            if (upperY <= pos.Y && pos.Y <= lowerY) return (parent, i);

            //if (gapRect.Contains(pos)) return (parent, i);
        }
        return null;
    }

    public NodeModel? HitAttachTarget(NodeModel dragging, PointF pos)
    {
        var hit = HitNode(pos);
        if (hit is null) return null;

        // 자기 자신·하위 트리면 attach 불가
        for (var n = _parent[hit]; n != null; n = _parent[n])
            if (n == dragging) return null;

        return hit;
    }
}