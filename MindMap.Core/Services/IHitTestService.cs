using MindMap.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services;

public interface IHitTestService
{
    void BuildIndex(IEnumerable<NodeModel> roots);      // 레이아웃 후 호출

    NodeModel? HitNode(PointF worldPos);

    /// <summary>
    /// 드래그 대상과 같은 부모의 Gap 에 들어간 경우
    /// </summary>
    /// <returns>(parent, insertIndex) 또는 null</returns>
    (NodeModel parent, int insertIndex)? HitSiblingGap(
        NodeModel dragging, PointF worldPos);

    /// <summary>
    /// Attach 대상 노드 (드래그 노드 자체·하위 제외)
    /// </summary>
    NodeModel? HitAttachTarget(NodeModel dragging, PointF worldPos);
}