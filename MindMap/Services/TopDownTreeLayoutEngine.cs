using MindMap.Models;
using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MindMap.Services
{
    internal class TopDownTreeLayoutEngine : IMindMapLayoutEngine
    {
        private readonly Dictionary<Guid, Size> _nodeSizes = new();
        private readonly Dictionary<Guid, BoundingArea> _boundingAreas = new();

        public void ComputeLayout(MindMapViewModel viewModel)
        {
            // 1. 각 노드의 실제 크기 기록 (Size는 ViewModel이 갖고 있음)
            foreach (var node in viewModel.Nodes)
            {
                _nodeSizes[node.Id] = node.Size;
            }

            // 2. BoundingArea 계산
            ComputeBoundingArea(viewModel.RootNode);

            // 3. 위치 배치
            var root = viewModel.RootNode;
            var startX = root.Position.X;
            var startY = root.Position.Y;

            double offsetY = startY - (_boundingAreas[root.Id].Height - _nodeSizes[root.Id].Height) / 2;
            ApplyLayout(root, new Point(startX, offsetY));
        }

        private BoundingArea ComputeBoundingArea(MindMapNodeViewModel node)
        {
            double totalHeight = 0;
            double maxChildWidth = 0;

            foreach (var child in node.Children)
            {
                var area = ComputeBoundingArea(child);
                totalHeight += area.Height;
                maxChildWidth = Math.Max(maxChildWidth, area.Width);
            }

            var size = _nodeSizes[node.Id];
            totalHeight = Math.Max(totalHeight, size.Height);
            _boundingAreas[node.Id] = new BoundingArea(size.Width + (maxChildWidth > 0 ? 20 + maxChildWidth : 0), totalHeight);
            return _boundingAreas[node.Id];
        }

        private void ApplyLayout(MindMapNodeViewModel node, Point position)
        {
            var rootY = position.Y + (_boundingAreas[node.Id].Height - _nodeSizes[node.Id].Height) / 2;
            node.Position = new Point(position.X, rootY);
            double currentY = position.Y;
            double childX = position.X + _nodeSizes[node.Id].Width + 20;

            foreach (var child in node.Children)
            {
                var area = _boundingAreas[child.Id];
                double childY = currentY;
                ApplyLayout(child, new Point(childX, childY));
                currentY += area.Height;
            }
        }
    }
}
