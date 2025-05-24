using MindMap.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services.Impl;

public sealed class NodeMutationService : INodeMutationService
{
    private readonly ISelectionService _sel;

    public NodeMutationService(ISelectionService sel) => _sel = sel;

    public bool MoveWithinSiblings(NodeModel node, int delta)
    {
        var parent = _sel.GetParent(node);
        if (parent is null) return false;                     // 루트는 패스

        var list = parent.Children;
        var idx = list.IndexOf(node);
        var tgt = idx + delta;
        if (tgt < 0 || tgt >= list.Count) return false;

        // 이동
        list.RemoveAt(idx);
        list.Insert(tgt, node);
        return true;
    }

    public bool Reparent(NodeModel node, ReparentAction dir)
    {
        var parent = _sel.GetParent(node);
        if (parent is null) return false;              // 루트는 재부모 불가

        bool isLeft = node.Side == SideEnum.Left;

        // 명세-기준으로 '승진|강등' 판정
        bool promote = (!isLeft && dir == ReparentAction.Left) ||
                       (isLeft && dir == ReparentAction.Right);

        bool demote = (!isLeft && dir == ReparentAction.Right) ||
                       (isLeft && dir == ReparentAction.Left);

        if (promote) return PromoteAfterParent(node, parent);
        if (demote) return DemoteToPrevSibling(node, parent);
        return false;
    }

    // ───── Helper : 승진 (부모 다음 형제로) ─────
    private bool PromoteAfterParent(NodeModel node, NodeModel parent)
    {
        var grand = _sel.GetParent(parent);

        // ── 1) 일반 케이스 (grand 존재) ──
        if (grand is not null)
        {
            parent.Children.Remove(node);

            var list = grand.Children;
            int insert = list.IndexOf(parent) + 1;
            list.Insert(insert, node);

            _sel.RegisterParent(node, grand);
            node.Side = parent.Side;
            return true;
        }

        // ── 2) 특수: parent 가 루트 ──
        return PromoteRootChild(node, parent);
    }

    private bool PromoteRootChild(NodeModel node, NodeModel root)
    {
        // 반대쪽 Side 그룹 마지막 위치 탐색
        var destSide = node.Side == SideEnum.Right ? SideEnum.Left : SideEnum.Right;
        var siblings = root.Children;

        int destIndex = siblings
            .Select((n, i) => (n, i))
            .Where(t => t.n.Side == destSide)
            .Select(t => t.i)
            .DefaultIfEmpty(-1)
            .Max() + 1;                      // 그룹 마지막 뒤

        if (destIndex < 0) return false;

        siblings.Remove(node);
        siblings.Insert(destIndex, node);
        node.Side = destSide;                // 방향 전환
        return true;
    }

    // ───── Helper : 강등 (앞 형제의 마지막 자식) ─────
    private bool DemoteToPrevSibling(NodeModel node, NodeModel parent)
    {
        var siblings = parent.Children;
        int idx = siblings.IndexOf(node);
        if (idx <= 0) return false;          // 앞 형제 없음

        var prev = siblings[idx - 1];

        siblings.RemoveAt(idx);
        prev.Children.Add(node);

        _sel.RegisterParent(node, prev);
        node.Side = prev.Side;
        return true;
    }
}
