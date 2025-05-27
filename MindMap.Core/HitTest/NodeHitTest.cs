using MindMap.Core.Models;
using System.Drawing;

namespace MindMap.Core.HitTest;

public static class NodeHitTest
{
    /// <summary>마우스 좌표(world) → 가장 위에 그려진 NodeModel 반환. 없으면 null.</summary>
    public static NodeModel? Pick(IReadOnlyList<NodeModel> all, PointF world)
    {
        var nodeSize = new SizeF(120, 48); // 임시 노드 크기
        for (int i = all.Count - 1; i >= 0; i--)            // Z-order: 뒤→앞
        {
            var n = all[i];
            var rect = new RectangleF(n.Position, nodeSize);   // Size = LayoutService 임시 120×48
            if (rect.Contains(world)) return n;
        }
        return null;
    }
}
