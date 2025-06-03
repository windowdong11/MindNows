using MindMap.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services;


public interface INodeMutationService
{
    /// <summary>같은 부모의 Children 안에서 상대 위치 이동</summary>
    /// <param name="delta">-1 = 위로, +1 = 아래로</param>
    /// <returns>true = 변화 발생</returns>
    bool MoveWithinSiblings(NodeModel node, int delta);
    bool Reparent(NodeModel node, NodeModel newParent);
    void MoveRootChildSide(NodeModel node);

    bool SetSide(NodeModel n, SideEnum side);

    public Action<NodeModel, NodeModel>? OnReparent { get; set; }
}
