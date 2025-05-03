using MindMap.Models;
// Services/BranchManager.cs
using MindMap.Views;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;

namespace MindMap.Services
{
    internal class BranchManager
    {
        private Canvas _canvas;
        private Dictionary<(Guid parentId, Guid childId), BranchControl> _branches = new();

        public BranchManager(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void RegisterBranch(MindMapNode parent, MindMapNode child)
        {
            var branch = new BranchControl
            {
                StartPoint = GetNodeCenter(parent),
                EndPoint = GetNodeCenter(child)
            };

            _branches[(parent.Id, child.Id)] = branch;
            _canvas.Children.Add(branch);
        }

        public void UpdateBranch(MindMapNode parent, MindMapNode child)
        {
            if (_branches.TryGetValue((parent.Id, child.Id), out var branch))
            {
                branch.StartPoint = GetNodeCenter(parent);
                branch.EndPoint = GetNodeCenter(child);
            }
        }

        public void UpdateAllBranches(MindMapNode node)
        {
            // 부모 → 자식
            foreach (var child in node.Children)
                UpdateBranch(node, child);

            // 자식 → 부모
            if (node.Parent != null)
                UpdateBranch(node.Parent, node);
        }

        private Point GetNodeCenter(MindMapNode node)
        {
            return new Point(node.Position.X + node.Size.Width / 2, node.Position.Y + node.Size.Height / 2); // Center offset
        }


        public void RemoveBranch(MindMapNode parent, MindMapNode child)
        {
            if (_branches.TryGetValue((parent.Id, child.Id), out var branch))
            {
                _canvas.Children.Remove(branch);
                _branches.Remove((parent.Id, child.Id));
            }
        }

    }
}
