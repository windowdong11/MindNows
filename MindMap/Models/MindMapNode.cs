// Models/MindMapNode.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MindMap.Models
{
    public class MindMapNode : INotifyPropertyChanged
    {
        public Guid Id { get; } = Guid.NewGuid();   // 내부 식별자
        public string Text { get; set; }
        public string? ImagePath { get; set; }      // 이미지 파일 경로 또는 null
        public Point Position { get; set; }         // 화면 좌표
        public Size Size { get; set; }              // 노드 크기 (이미지 비율 유지 고려)

        public MindMapNode? Parent { get; set; }
        //public List<MindMapNode> Children { get; } = new();
        public List<MindMapNode> LeftChildren { get; } = new();  // 왼쪽 자식 노드
        public List<MindMapNode> RightChildren { get; } = new(); // 오른쪽 자식 노드

        public bool IsLeftSide { get; set; }        // 최상위 자식 분기 여부

        public bool IsRoot => Parent == null;      // 루트 노드 여부

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /* 자식 노드 생성 / 삭제 */
        public void  AddChild(MindMapNode child)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            child.Parent = this;
            if (IsRoot)
            {
                if (LeftChildren.Count > 0)
                    LeftChildren.Add(child);
                else
                    RightChildren.Add(child);
            }
            else
            {
                if (IsLeftSide)
                    LeftChildren.Add(child);
                else
                    RightChildren.Add(child);
            }
        }

        public void RemoveChild(MindMapNode child)
        {
            // 자식 트리 삭제
            List<MindMapNode> grandChildren;
            if (IsLeftSide)
                grandChildren = child.LeftChildren.ToList();
            else
                grandChildren = child.RightChildren.ToList();
            foreach (var grandChild in grandChildren)
                child.RemoveChild(grandChild);

            // 자식 노드 삭제
            if (IsLeftSide)
                LeftChildren.Remove(child);
            else
                RightChildren.Remove(child);
        }

        public void DetachChild(MindMapNode child)
        {
            child.Parent = null;
            if (IsLeftSide)
                LeftChildren.Remove(child);
            else
                RightChildren.Remove(child);
        }

        /* 부모의 동생 노드로 이동 */
        public void MoveToParentYongerSibling()
        {
            if (Parent == null) throw new InvalidOperationException("Parent is null.");
            if (Parent.Parent == null) throw new InvalidOperationException("Grandparent is null.");

            var siblings = Parent.IsLeftSide
                ? Parent.Parent.LeftChildren
                : Parent.Parent.RightChildren;
            var index = siblings.IndexOf(Parent);
            if (index == -1)
                throw new InvalidOperationException("Parent is not found in siblings.");
            siblings.Insert(index + 1, this);
            Parent.DetachChild(this);
            Parent = Parent.Parent;
        }

        /* 형의 마지막 자식 노드로 이동 */
        public bool MoveToElderSiblingLastChild()
        {
            if (Parent == null) throw new InvalidOperationException("Root cannot move to elder sibling's child.");
            
            var siblings = IsLeftSide ? Parent.LeftChildren : Parent.RightChildren;
            var index = siblings.IndexOf(this);
            if (index == -1)
                throw new InvalidOperationException("Node is not found in siblings.");
            if (index == 0)
                return false;
            var elderSibling = siblings[index - 1];
            if (IsLeftSide)
                elderSibling.LeftChildren.Add(this);
            else
                elderSibling.RightChildren.Add(this);
            Parent.DetachChild(this);
            return true;
        }

        /* 현재 노드가 node의 조상인지 확인 */
        public bool IsAncestorOf(MindMapNode node)
        {
            if (IsRoot) return node.Parent.Id == Id;
            return node.Parent.Id == Id || node.Parent.IsAncestorOf(this);
        }

        /* 현재 노드가 node의 자손인지 확인 */
        public bool IsDescendantOf(MindMapNode node)
        {
            return node.IsAncestorOf(this);
        }

        /* node의 자식으로 이동할 수 있는지 확인 */
        public bool CanMoveToChildOf(MindMapNode node)
        {
            return !IsAncestorOf(node);
        }

        /* node를 idx번째 자식 노드로 추가 */
        public void AddChildAt(MindMapNode node, int idx)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (idx < 0 || idx > LeftChildren.Count + RightChildren.Count)
                throw new ArgumentOutOfRangeException(nameof(idx));
            node.Parent = this;
            if (idx < LeftChildren.Count)
                LeftChildren.Insert(idx, node);
            else
                RightChildren.Insert(idx - LeftChildren.Count, node);
        }
    }
}
