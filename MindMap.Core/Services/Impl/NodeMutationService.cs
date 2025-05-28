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

        // 같은 방향을 가진 앞쪽 형제들 중 마지막 노드 찾기
        var result = false;
        while (delta < 0)
        {
            result |= SwapWithPrevSibling(node, parent);
            ++delta;
        }
        while (delta > 0)
        {
            result |= SwapWithNextSibling(node, parent);
            --delta;
        }
        return result;
    }

    private bool SwapWithPrevSibling(NodeModel node, NodeModel parent)
    {
        var siblings = parent.Children;
        int idx = siblings.IndexOf(node);
        if (idx <= 0) return false; // 첫 번째면 swap 불가
        int prevIdx = -1;
        for (int i = idx - 1; i >= 0; i--)
        {
            if (siblings[i].Side == node.Side)
            {
                prevIdx = i;
                break;
            }
        }
        if (prevIdx == -1) return false; // 같은 방향의 앞 형제 없음
                                         // swap
        siblings.Move(idx, prevIdx);
        return true;
    }

    private bool SwapWithNextSibling(NodeModel node, NodeModel parent)
    {
        var siblings = parent.Children;
        int idx = siblings.IndexOf(node);
        if (idx >= siblings.Count - 1) return false; // 마지막이면 swap 불가
        int nextIdx = -1;
        for (int i = idx + 1; i < siblings.Count; i++)
        {
            if (siblings[i].Side == node.Side)
            {
                nextIdx = i;
                break;
            }
        }
        if (nextIdx == -1) return false; // 같은 방향의 뒤 형제 없음
                                         // swap
        siblings.Move(idx, nextIdx);
        return true;
    }


    public void MoveRootChildSide(NodeModel node)
    {
        if (node is null) return;  // 루트가 아니면 패스
        var parent = _sel.GetParent(node);
        if (parent is null) return;  // 루트가 아니면 패스
        var grand = _sel.GetParent(parent);
        if (grand is not null) return;    // 루트가 아니면 패스

        // 마지막 노드로 이동
        var siblings = parent.Children;
        siblings.Remove(node);           // 현재 위치에서 제거
        var destSide = node.Side == SideEnum.Right ? SideEnum.Left : SideEnum.Right;
        siblings.Add(node);             // 마지막에 추가 (Side 변경은 필요 없음)
        SetSide(node, destSide);         // Side 변경
    }

    public bool SetSide(NodeModel n, SideEnum side)
    {
        if (n.Side == side) return false; // 변경 필요 없음
        var result = false;
        n.Side = side;
        foreach (var child in n.Children)
            result |= SetSide(child, side);       // 자식도 동일하게 설정
        return result;
    }

    //public bool Reparent(NodeModel node, ReparentAction dir)
    //{
    //    var parent = _sel.GetParent(node);
    //    if (parent is null) return false;              // 루트는 재부모 불가

    //    bool isLeft = node.Side == SideEnum.Left;

    //    // 명세-기준으로 '승진|강등' 판정
    //    bool promote = (!isLeft && dir == ReparentAction.Left) ||
    //                   (isLeft && dir == ReparentAction.Right);

    //    bool demote = (!isLeft && dir == ReparentAction.Right) ||
    //                   (isLeft && dir == ReparentAction.Left);

    //    if (promote)
    //    {
    //        var grand = _sel.GetParent(parent);
    //        if (grand is null)
    //        {
    //            var root = parent;
    //            SetSide(node, node.Side == SideEnum.Left ? SideEnum.Right : SideEnum.Left);
    //            root.Children.Move(root.Children.IndexOf(node), root.Children.Count - 1);
    //            return true;
    //        }
    //        return ReparentTo(node, grand);
    //        //return PromoteAfterParent(node, parent);
    //    }
    //    if (demote)
    //    {
    //        var siblings = parent.Children;
    //        int idx = siblings.IndexOf(node);
    //        if (idx <= 0) return false;          // 앞 형제 없음

    //        // 같은 방향의 앞 형제 찾기
    //        var prev = siblings
    //            .Take(idx)
    //            .LastOrDefault(n => n.Side == node.Side && !_sel.Multi.Contains(n));
    //        if (prev is null)
    //            return false;
    //        return ReparentTo(node, prev);
    //        //return DemoteToPrevSibling(node, parent);
    //    }
    //    return false;
    //}

    public bool Reparent(NodeModel node, NodeModel newParent)
    {
        if (node is null || newParent is null) return false;
        // 현재 부모에서 제거
        var currentParent = _sel.GetParent(node);
        if (currentParent is null)
        {
            // 루트 노드인 경우
            var root = node;
            var destSide = newParent.Side;
            // 자식 중 Side가 destSide와 다른 노드들은 순서를 마지막으로 바꿈.
            for (var i = 0; i < root.Children.Count; i++)
            {
                var child = root.Children[i];
                if (child.Side != destSide)
                {
                    SetSide(child, destSide); // Side 변경
                    root.Children.Move(i, root.Children.Count - 1); // 마지막으로 이동
                    i--; // 인덱스 조정
                }
            }
        }
        else
        {
            currentParent.Children.Remove(node);
            SetSide(node, newParent.Side);
        }
        // 새 부모에 추가
        newParent.Children.Add(node);
        _sel.RegisterParent(node, newParent);
        return true;
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

        //int destIndex = siblings
        //    .Select((n, i) => (n, i))
        //    .Where(t => t.n.Side == destSide)
        //    .Select(t => t.i)
        //    .DefaultIfEmpty(-1)
        //    .Max() + 1;                      // 그룹 마지막 뒤

        //if (destIndex < 0) return false;

        siblings.Remove(node);
        siblings.Add(node);

        
        SetSide(node, destSide);          // Side 변경
        return true;
    }

    // ───── Helper : 강등 (방향이 같은 앞 형제의 마지막 자식) ─────
    private bool DemoteToPrevSibling(NodeModel node, NodeModel parent)
    {
        var siblings = parent.Children;
        int idx = siblings.IndexOf(node);
        if (idx <= 0) return false;          // 앞 형제 없음

        // 같은 방향의 앞 형제 찾기
        var prev = siblings
            .Take(idx)
            .LastOrDefault(n => n.Side == node.Side && !_sel.Multi.Contains(n));
        if (prev is null) return false;       // 같은 방향의 앞 형제 없음

        siblings.RemoveAt(idx);
        prev.Children.Add(node);

        _sel.RegisterParent(node, prev);
        node.Side = prev.Side;
        return true;
    }
}
