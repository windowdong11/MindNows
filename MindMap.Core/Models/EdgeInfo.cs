using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Models;

public readonly record struct EdgeInfo(
    PointF Start,   // 실제 world 좌표
    PointF Ctrl1,
    PointF Ctrl2,
    PointF End
);