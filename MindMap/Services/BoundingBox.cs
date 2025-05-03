using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MindMap.Services
{
    public struct BoundingBox
    {
        public double Left;
        public double Top;
        public double Right;
        public double Bottom;

        public double Width => Right - Left;
        public double Height => Bottom - Top;

        public Rect ToRect() => new Rect(Left, Top, Width, Height);

        public static BoundingBox FromNode(Point position, double nodeWidth, double nodeHeight)
        {
            return new BoundingBox
            {
                Left = position.X,
                Top = position.Y,
                Right = position.X + nodeWidth,
                Bottom = position.Y + nodeHeight
            };
        }

        public void Encapsulate(BoundingBox other)
        {
            Left = Math.Min(Left, other.Left);
            Top = Math.Min(Top, other.Top);
            Right = Math.Max(Right, other.Right);
            Bottom = Math.Max(Bottom, other.Bottom);
        }
    }
}
