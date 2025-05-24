using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Geometry;

/// <summary>부모-자식 연결선용 큐빅 베지어 제어점 정보 (UI 무관)</summary>
public readonly record struct BezierEdge(
    float X0, float Y0,          // start
    float C1X, float C1Y,        // control-1
    float C2X, float C2Y,        // control-2
    float X3, float Y3           // end
);
