using MindMap.Core.Models;
using MindMap.Core.Services.DragDrop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services.Impl;

public sealed class DragDropService : IDragDropService
{
    private readonly ISelectionService _sel;
    private readonly IHitTestService _hit;
    private readonly INodeMutationService _mut;
    private readonly Action _rebuildVisual;  // VM → World 갱신 delegate

    public DragDropService(ISelectionService sel,
                           IHitTestService hit,
                           INodeMutationService mut,
                           Action rebuildVisual)
    {
        _sel = sel; _hit = hit; _mut = mut; _rebuildVisual = rebuildVisual;
        _state = new DragState(DragPhase.Idle, Array.Empty<NodeModel>(),
                               default, default, null, null);
    }

    private DragState _state;
    public DragState State => _state;
    public event EventHandler? StateChanged;

    public void BeginDrag(PointF pos)
    {
        if (_state.Phase != DragPhase.Idle) return;
        var root = _hit.HitNode(pos);
        if (root is null) return;

        //var sel = _sel.Multi.Any() ? _sel.Multi : new[] { root };
        // 루트 중심 구해 Offset 계산
        //var minX = sel.Min(n => n.Position.X);
        //var minY = sel.Min(n => n.Position.Y);
        _sel.Select(root);  // 현재 선택 노드로 설정
        var sel = new[] { root };
        var minX = root.Position.X;
        var minY = root.Position.Y;
        _state = new DragState(
            DragPhase.Dragging, sel,
            pos, new PointF(pos.X - minX, pos.Y - minY),
            null, null);
        Raise();
    }

    public void UpdateDrag(PointF pos)
    {
        if (_state.Phase != DragPhase.Dragging) return;

        // Hover 상태 갱신
        var selRoot = _state.Selection.First();
        var gap = _hit.HitSiblingGap(selRoot, pos);
        var attach = _hit.HitAttachTarget(selRoot, pos);

        _state = _state with { HoverGap = gap, HoverAttachTarget = attach };
        Raise();
    }

    public void CommitDrag(PointF pos)
    {
        if (_state.Phase != DragPhase.Dragging) return;

        var selRoots = _state.Selection;
        bool changed = false;

        if (_state.HoverGap is { } gap)
        {
            // 형제 순서 이동
            foreach (var n in selRoots.Reverse())      // 아래→위 충돌방지
                changed |= _mut.MoveWithinSiblings(n, gap.index - // insertIndex인데 수정했음
                       gap.parent.Children.IndexOf(n));
        }
        else if (_state.HoverAttachTarget is { } tgt)
        {
            var dir = tgt.Side == SideEnum.Right
                ? ReparentAction.Right : ReparentAction.Left;
            foreach (var n in selRoots)
                changed |= _mut.Reparent(n, dir);
        }
        // else 영역 밖 → 위치 변화 없음

        _state = _state with { Phase = DragPhase.Idle };
        Raise();

        if (changed) _rebuildVisual();   // VM Rebuild + Layout + Edge.Refresh
    }

    public void CancelDrag()
    {
        if (_state.Phase == DragPhase.Dragging)
        {
            _state = _state with { Phase = DragPhase.Idle };
            Raise();
        }
    }

    private void Raise() => StateChanged?.Invoke(this, EventArgs.Empty);
}