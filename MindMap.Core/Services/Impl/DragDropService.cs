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
        _state = new DragState(DragPhase.Idle, null,
                               default, default, default, null, null);
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
        //var sel = new[] { root };
        var minX = root.Position.X;
        var minY = root.Position.Y;
        _state = new DragState(
            DragPhase.Dragging, root,
            pos, new PointF(minX - pos.X, minY - pos.Y), pos,
            null, null);
        Raise();
    }

    public void UpdateDrag(PointF pos)
    {
        if (_state.Phase != DragPhase.Dragging) return;

        // Hover 상태 갱신
        var selRoot = _state.Selection;
        if (selRoot is null)
            throw new InvalidOperationException("Selection is null during drag."); // null일 수 없음. BeginDrag에서 설정됨
        var gap = _hit.HitSiblingGap(selRoot, pos);
        var attach = _hit.HitAttachTarget(selRoot, pos);

        _state = _state with { HoverGap = gap, HoverAttachTarget = attach, CurrentWorldPos = pos };
        Raise();
    }

    public void CommitDrag(PointF pos)
    {
        if (_state.Phase != DragPhase.Dragging) return;
        var selRoot = _state.Selection;
        if (selRoot is null)
            throw new InvalidOperationException("Selection is null during drag."); // null일 수 없음. BeginDrag에서 설정됨
        var sibgap = _hit.HitSiblingGap(selRoot, pos);
        var attach = _hit.HitAttachTarget(selRoot, pos);

        _state = _state with { HoverGap = sibgap, HoverAttachTarget = attach };

        bool changed = false;

        if (_hit.HitNode(pos) == null && _sel.GetParent(selRoot) == null)
        {
            // 빈 공간에 드랍: 루트 노드 이동
            // -- 다음 단계에서 실제 구현 --
            var actualPos = new Point((int)(pos.X + _state.CursorOffset.X), (int)(pos.Y + _state.CursorOffset.Y)); // 클릭 위치 ↔ 루트 선택 중심
            selRoot.Position = actualPos;
            changed = true;
        }
        else if (_state.HoverAttachTarget is { } tgt)
        {
            //var dir = tgt.Side == SideEnum.Right
            //    ? ReparentAction.Right : ReparentAction.Left;
            //foreach (var n in selRoots)
            if (selRoot != tgt)
                changed |= _mut.Reparent(selRoot, tgt);
        }
        else if (_state.HoverGap is { } gap)
        {
            // 형제 순서 이동
            var nodeIndex = gap.parent.Children.IndexOf(selRoot);
            var delta = gap.index > nodeIndex
                ? gap.index - nodeIndex - 1
                : gap.index - nodeIndex;
            if (delta != 0)
                changed |= _mut.MoveWithinSiblings(selRoot, delta);
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