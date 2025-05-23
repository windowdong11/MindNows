﻿using MindMap.Models;
// Services/BranchManager.cs
using MindMap.Views;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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
                StartPoint = GetArrowStartPoint(parent, child.IsLeftSide),
                EndPoint = GetArrowEndPoint(child, child.IsLeftSide)
            };

            _branches[(parent.Id, child.Id)] = branch;
            _canvas.Children.Add(branch);
        }

        public void UpdateBranch(MindMapNode parent, MindMapNode child)
        {
            if (_branches.TryGetValue((parent.Id, child.Id), out var branch))
            {
                branch.StartPoint = GetArrowStartPoint(parent, child.IsLeftSide);
                branch.EndPoint = GetArrowEndPoint(child, child.IsLeftSide);
            }
        }

        public void UpdateAllBranches(MindMapNode node)
        {
            // 부모 → 자식
            foreach (var child in node.LeftChildren)
                UpdateBranch(node, child);
            foreach (var child in node.RightChildren)
                UpdateBranch(node, child);

            // 자식 → 부모
            if (node.Parent != null)
                UpdateBranch(node.Parent, node);
        }

        private Point GetArrowStartPoint(MindMapNode node, bool isLeft)
        {
            if (isLeft)
                return new Point(node.Position.X, node.Position.Y + node.Size.Height / 2); // Center offset
            else
                return new Point(node.Position.X + node.Size.Width, node.Position.Y + node.Size.Height / 2);
        }

        private Point GetArrowEndPoint(MindMapNode node, bool isLeft)
        {
            if (isLeft)
                return new Point(node.Position.X + node.Size.Width, node.Position.Y + node.Size.Height / 2); // Center offset
            else
                return new Point(node.Position.X, node.Position.Y + node.Size.Height / 2);
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
