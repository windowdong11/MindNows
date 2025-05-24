using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MindMap.Core.Layout;

public sealed record NodeLayout(SizeF SubtreeSize, Point Pos);