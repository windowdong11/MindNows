using MindMap.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services.DragDrop;

public enum DragPhase { Idle, Dragging }

public sealed record DragState(
    DragPhase Phase,
    IReadOnlyCollection<NodeModel> Selection,   // 트리 루트들 (VM 아님)
    PointF DragStartWorld,                     // 마우스가 눌린 월드 좌표
    PointF CursorOffset,                      // 클릭 위치 ↔ 루트 선택 중심
    NodeModel? HoverAttachTarget,              // 현재 Attach 후보
    (NodeModel parent, int index)? HoverGap);  // 형제 Gap 후보