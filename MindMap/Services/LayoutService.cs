using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MindMap.Services
{
    internal class LayoutService : ILayoutService
    {
        public void RecalculateLayout(MindMapNodeViewModel root)
        {
            ComputeBoundingArea(root);
            // 3. 위치 배치
            var startX = root.Position.X;
            var startY = root.Position.Y;
            double offsetY = startY - (root.BoundingArea.Height - root.Size.Height) / 2;
            LayoutTree(root, new Point(startX, offsetY));
        }

        private BoundingArea ComputeBoundingArea(MindMapNodeViewModel root)
        {
            double totalHeight = 0;
            double maxChildWidth = 0;

            foreach (var child in root.Children)
            {
                var childArea = ComputeBoundingArea(child);
                maxChildWidth = Math.Max(maxChildWidth, childArea.Width);
                totalHeight += childArea.Height;
            }

            totalHeight = Math.Max(totalHeight, root.Size.Height);
            root.BoundingArea = new BoundingArea
            {
                Width = root.Size.Width + (root.Children.Count > 0 ? 20 + maxChildWidth : 0),
                Height = totalHeight
            };

            return root.BoundingArea;
        }

        private void LayoutTree(MindMapNodeViewModel node, Point basePosition)
        {
            var rootHeight = node.Size.Height;
            var rootBoundingHeight = node.BoundingArea.Height;
            var rootY = basePosition.Y + (rootBoundingHeight - rootHeight) / 2;
            node.Position = new Point(basePosition.X, rootY);

            var childTop = basePosition.Y;
            var childLeft = basePosition.X + node.Size.Width + 20;

            foreach (var child in node.Children)
            {
                LayoutTree(child, new Point(childLeft, childTop));
                childTop += child.BoundingArea.Height;
            }
        }
    }

    public class BoundingArea
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public BoundingArea() { }
        public BoundingArea(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }
}
