using MindMap.Core.Geometry;
using MindMap.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services;

public interface ILayoutService
{
    //SizeF MeasureAndArrange(NodeModel root);
    //IReadOnlyList<BezierEdge> Edges { get; }    // 계산 결과 노출
    void Arrange(NodeModel root);
}
