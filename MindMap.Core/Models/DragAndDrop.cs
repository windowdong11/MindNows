using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Models;

public record struct DragContext(NodeModel Dragging, PointF Cursor);

// 드롭 의도
public enum DropIntentKind { None, ReorderBetween, AdoptAsLastChild, PromoteToRoot }

public record struct DropIntent(
    DropIntentKind Kind,
    NodeModel? Target,         // 형제 or 부모 대상
    int? Index);          // Reorder 시 삽입 인덱스