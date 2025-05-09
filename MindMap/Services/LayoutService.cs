using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;

namespace MindMap.Services
{
    internal class LayoutService : ILayoutService
    {
        private Dictionary<Guid, BoundingArea> BoundingAreaMap { get; } = new();
        public void RecalculateLayout(MindMapNodeViewModel root)
        {
            //ComputeBoundingArea(root);
            //ComputeTreeBoundingArea(root);
            // 3. 위치 배치
            //var startX = root.Position.X;
            //var startY = root.Position.Y;
            //double offsetY = startY - (BoundingAreaMap[root.Id].Height - root.Size.Height) / 2;
            //LayoutTree(root, new Point(startX, offsetY));
            LayoutTree(root, ComputeTreeBoundingArea(root));
        }

        private TreeBoundingArea ComputeTreeBoundingArea(MindMapNodeViewModel root)
        {

            var leftChildren = root.LeftChildren;
            var rightChildren = root.RightChildren;

            double maxLeftChildWidth = 0;
            double leftHeight = 0;
            foreach (var child in leftChildren)
            {
                var childArea = ComputeBoundingArea(child);
                maxLeftChildWidth = Math.Max(maxLeftChildWidth, childArea.Width);
                leftHeight += childArea.Height;
            }

            double maxRightChildWidth = 0;
            double rightHeight = 0;
            foreach (var child in rightChildren)
            {
                var childArea = ComputeBoundingArea(child);
                maxRightChildWidth = Math.Max(maxRightChildWidth, childArea.Width);
                rightHeight += childArea.Height;
            }

            rightHeight = Math.Max(rightHeight, root.Size.Height);
            var rightBoundingAreaWidth = root.Size.Width + (root.RightChildren.Count > 0 ? 20 + maxRightChildWidth : 0);
            var rightBoundingAreaHeight = rightHeight;
            var leftBoundingAreaWidth = root.Size.Width + (root.LeftChildren.Count > 0 ? 20 + maxLeftChildWidth : 0);
            var leftBoundingAreaHeight = leftHeight;
            return new TreeBoundingArea(leftBoundingAreaWidth, leftBoundingAreaHeight, rightBoundingAreaWidth, rightBoundingAreaHeight);
        }

        private BoundingArea ComputeBoundingArea(MindMapNodeViewModel root)
        {
            double totalHeight = 0;
            double maxChildWidth = 0;

            var children = root.IsLeftSide ? root.LeftChildren : root.RightChildren;

            foreach (var child in children)
            {
                var childArea = ComputeBoundingArea(child);
                maxChildWidth = Math.Max(maxChildWidth, childArea.Width);
                totalHeight += childArea.Height;
            }

            totalHeight = Math.Max(totalHeight, root.Size.Height);
            var boundingAreaWidth = root.Size.Width + (children.Count > 0 ? 20 + maxChildWidth : 0);
            var boundingAreaHeight = totalHeight;
            if (BoundingAreaMap.ContainsKey(root.Id))
            {
                BoundingAreaMap[root.Id] = new BoundingArea(boundingAreaWidth, boundingAreaHeight);
            }
            else
            {
                BoundingAreaMap.Add(root.Id, new BoundingArea(boundingAreaWidth, boundingAreaHeight));
            }
            //root.BoundingArea = new BoundingArea
            //{
            //    Width = root.Size.Width + (root.Children.Count > 0 ? 20 + maxChildWidth : 0),
            //    Height = totalHeight
            //};

            return BoundingAreaMap[root.Id];
        }

        private void LayoutTree(MindMapNodeViewModel root, TreeBoundingArea boundingArea)
        {
            //var boundingArea = area;
            var rootX = root.Position.X;
            var rootY = root.Position.Y;
            //root.Position = new Point(rootX, rootY);
            // 루트 노드 기준 좌우 분기
            var leftChildren = root.LeftChildren;
            var rightChildren = root.RightChildren;
            double spacing = 20;
            double leftTopY = rootY - (boundingArea.LeftBoundingArea.Height - root.Size.Height) / 2;
            double leftX = rootX - spacing;
            foreach (var child in leftChildren)
            {
                LayoutSubTree(child, new Point(leftX, leftTopY), isLeft: true);
                leftTopY += BoundingAreaMap[child.Id].Height;
            }
            double rightTopY = rootY - (boundingArea.RightBoundingArea.Height - root.Size.Height) / 2;
            double rightX = rootX + spacing + root.Size.Width;
            foreach (var child in rightChildren)
            {
                LayoutSubTree(child, new Point(rightX, rightTopY), isLeft: false);
                rightTopY += BoundingAreaMap[child.Id].Height;
            }
        }

        private void LayoutSubTree(MindMapNodeViewModel node, Point basePosition, bool isLeft)
        {
            var rootHeight = node.Size.Height;
            var rootBoundingHeight = BoundingAreaMap[node.Id].Height;
            var rootY = basePosition.Y + (rootBoundingHeight - rootHeight) / 2;
            var rootX = basePosition.X;
            if (node.IsLeftSide)
            {
                rootX -= node.Size.Width;
            }
            node.Position = new Point(rootX, rootY);

            //var childTop = basePosition.Y;
            //var childLeft = basePosition.X + node.Size.Width + 20;

            //foreach (var child in node.Children)
            //{
            //    LayoutTree(child, new Point(childLeft, childTop));
            //    childTop += child.BoundingArea.Height;
            //}
            // 루트 노드 기준 좌우 분기
            var children = isLeft ? node.LeftChildren : node.RightChildren;

            double spacing = 20;
            double X;
            if (isLeft)
            {
                X = basePosition.X - spacing;
            }
            else
            {
                X = basePosition.X + node.Size.Width + spacing;
            }

            double topY = basePosition.Y;
            foreach (var child in children)
            {
                if (isLeft)
                    X = basePosition.X - spacing - child.Width;
                LayoutSubTree(child, new Point(X, topY), isLeft);
                topY += BoundingAreaMap[child.Id].Height;
            }

            //double rightTopY = basePosition.Y;
            //foreach (var child in rightChildren)
            //{
            //    LayoutSubTree(child, new Point(rightX, rightTopY));
            //    rightTopY += BoundingAreaMap[child.Id].Height;
            //}
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
    public class TreeBoundingArea
    {
        public BoundingArea LeftBoundingArea { get; set; }
        public BoundingArea RightBoundingArea { get; set; }
        public TreeBoundingArea(double lw, double lh, double rw, double rh)
        {
            LeftBoundingArea = new BoundingArea(lw, lh);
            RightBoundingArea = new BoundingArea(rw, rh);
        }
    }
}
