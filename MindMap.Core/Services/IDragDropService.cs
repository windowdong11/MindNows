using MindMap.Core.Services.DragDrop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services;

public interface IDragDropService
{
    DragState State { get; }

    void BeginDrag(PointF worldPos);                  // MouseDown
    void UpdateDrag(PointF worldPos);                 // MouseMove
    void CommitDrag(PointF worldPos);                 // MouseUp
    void CancelDrag();                                // Esc / RightClick

    event EventHandler? StateChanged;                 // 뷰 Preview Layer 갱신용
}
