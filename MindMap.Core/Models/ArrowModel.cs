using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Models;

public class ArrowModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required NodeModel From { get; set; }
    public required NodeModel To { get; set; }
    public float Curvature { get; set; } = 0; // 기본은 직선, 양수/음수로 커브 방향
}