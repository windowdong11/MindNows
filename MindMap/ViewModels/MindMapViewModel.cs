using MindMap.Common;
using MindMap.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace MindMap.ViewModels
{
    public static class ExtensionMethods
    {
        public static int RemoveAll<T>(
            this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }
    }
    internal class MindMapViewModel : INotifyPropertyChanged
    {
        public event Action? LayoutRequested;

        public void RequestLayout()
        {
            LayoutRequested?.Invoke(); // 내부에서만 호출하므로 안전
        }
        public ObservableCollection<MindMapNodeViewModel> Nodes { get; } = new();
        public MindMapNodeViewModel RootNode;

        private MindMapNodeViewModel? _selectedNode;
        public MindMapNodeViewModel? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    if (_selectedNode != null) _selectedNode.IsSelected = false;
                    _selectedNode = value;
                    if (_selectedNode != null) _selectedNode.IsSelected = true;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<MindMapArrowViewModel> Arrows { get; } = new();


        public MindMapViewModel(MindMapDocument document)
        {
            var nodeMap = new Dictionary<Guid, MindMapNodeViewModel>();

            foreach (var model in document.Nodes)
            {
                var vm = new MindMapNodeViewModel(model, this);
                if (model.Id == document.RootNode.Id)
                {
                    RootNode = vm;
                }
                Nodes.Add(vm);
                nodeMap[model.Id] = vm;
            }
            if (RootNode == null)
            {
                throw new InvalidOperationException("RootNode cannot be null.");
            }

            foreach (var vm in Nodes)
            {
                if (vm.Model.Parent != null && nodeMap.TryGetValue(vm.Model.Parent.Id, out var parentVm))
                {
                    vm.Parent = parentVm;
                    parentVm.Children.Add(vm);
                    Arrows.Add(new MindMapArrowViewModel(parentVm, vm));
                }
            }

            AddChildCommand = new RelayCommand(_ => AddChild(), _ => SelectedNode != null);
            AddSiblingCommand = new RelayCommand(_ => AddSibling(), _ => SelectedNode?.Parent != null);
            DeleteNodeCommand = new RelayCommand(_ => DeleteNode(), _ => SelectedNode != null);
            MoveSelectionLeftCommand = new RelayCommand(_ => MoveSelectionLeft(), _ => SelectedNode != null);
            MoveSelectionRightCommand = new RelayCommand(_ => MoveSelectionRight(), _ => SelectedNode != null);
            MoveSelectionUpCommand = new RelayCommand(_ => MoveSelectionUp(), _ => SelectedNode != null);
            MoveSelectionDownCommand = new RelayCommand(_ => MoveSelectionDown(), _ => SelectedNode != null);
            MoveNodeUpCommand = new RelayCommand(_ => MoveNodeUp(), _ => CanMoveNodeUp());
            MoveNodeDownCommand = new RelayCommand(_ => MoveNodeDown(), _ => CanMoveNodeDown());
            MoveNodeLeftCommand = new RelayCommand(_ => MoveNodeLeft(), _ => CanMoveNodeLeft());
            MoveNodeRightCommand = new RelayCommand(_ => MoveNodeRight(), _ => CanMoveNodeRight());
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /* ---------- 노드 추가/삭제 ---------- */
        public ICommand AddChildCommand { get; }
        public ICommand AddSiblingCommand { get; }
        public ICommand DeleteNodeCommand { get; }

        /* ---------- 노드 선택 ---------- */
        public ICommand MoveSelectionLeftCommand { get; }
        public ICommand MoveSelectionRightCommand { get; }
        public ICommand MoveSelectionUpCommand { get; }
        public ICommand MoveSelectionDownCommand { get; }

        /* ---------- 노드 이동 ---------- */
        public ICommand MoveNodeUpCommand { get; }

        public ICommand MoveNodeDownCommand { get; }
        public ICommand MoveNodeLeftCommand { get; }
        public ICommand MoveNodeRightCommand { get; }


        private void AddChild()
        {
            if (SelectedNode == null) return;

            var model = new MindMapNode
            {
                Text = "New Child",
                Parent = SelectedNode.Model
            };
            SelectedNode.Model.Children.Add(model);

            var vm = new MindMapNodeViewModel(model, this)
            {
                Parent = SelectedNode
            };
            SelectedNode.Children.Add(vm);
            Nodes.Add(vm);
            Arrows.Add(new MindMapArrowViewModel(SelectedNode, vm));
            SelectedNode = vm;
            LayoutRequested?.Invoke();
        }

        private void AddSibling()
        {
            if (SelectedNode?.Parent == null) return;
            var parent = SelectedNode.Parent;

            var model = new MindMapNode
            {
                Text = "New Sibling",
                Parent = parent.Model
            };
            parent.Model.Children.Add(model);

            var vm = new MindMapNodeViewModel(model, this)
            {
                Parent = parent
            };
            parent.Children.Add(vm);
            Nodes.Add(vm);
            Arrows.Add(new MindMapArrowViewModel(parent, vm));
            SelectedNode = vm;

            LayoutRequested?.Invoke();
        }

        private void DeleteNode()
        {
            if (SelectedNode == null || SelectedNode.Parent == null) return;

            void DeleteRecursive(MindMapNodeViewModel vm)
            {
                foreach (var child in vm.Children.ToList())
                    DeleteRecursive(child);

                Nodes.Remove(vm);
                Arrows.RemoveAll(a => a.From == vm || a.To == vm);
            }

            var parent = SelectedNode.Parent;
            parent.Children.Remove(SelectedNode);
            parent.Model.Children.Remove(SelectedNode.Model);
            DeleteRecursive(SelectedNode);
            SelectedNode = parent;
            LayoutRequested?.Invoke();
        }

        private MindMapNodeViewModel? FindOverlappingLeft(MindMapNodeViewModel current)
        {
            return Nodes
                .Where(n => n.Position.X < current.Position.X)
                .Where(n => Math.Abs(n.Position.Y - current.Position.Y) < 50)
                .OrderByDescending(n => n.Position.X)
                .FirstOrDefault();
        }


        private void MoveSelectionLeft()
        {
            var current = SelectedNode;
            if (current == null) return;

            if (current.Parent == null)
            {
                // 루트 노드 → 왼쪽 자식 중 가장 위
                var leftChildren = current.Children.Where(c => c.IsLeftSide).ToList();
                var topMost = leftChildren.OrderBy(c => c.Position.Y).FirstOrDefault();
                if (topMost != null)
                    SelectedNode = topMost;
            }
            else
            {
                if (current.IsLeftSide)
                {
                    if (current.Children.Count > 0)
                    {
                        SelectedNode = current.Children[0];
                    }
                    else
                    {
                        var candidates = FindOverlappingLeft(current);
                        if (candidates != null)
                            SelectedNode = candidates;
                    }
                }
                else
                {
                    SelectedNode = current.Parent;
                }
            }

            //RequestLayout(); // 선택이 바뀌면 자동 레이아웃도 필요하다면
        }

        private MindMapNodeViewModel? FindOverlappingRight(MindMapNodeViewModel current)
        {
            return Nodes
                .Where(n => n.Position.X > current.Position.X)
                .Where(n => Math.Abs(n.Position.Y - current.Position.Y) < 50)
                .OrderBy(n => n.Position.X)
                .FirstOrDefault();
        }

        private void MoveSelectionRight()
        {
            var current = SelectedNode;
            if (current == null) return;

            if (current.Parent == null)
            {
                // 루트 → 오른쪽 자식 중 가장 위
                var rightChildren = current.Children.Where(c => !c.IsLeftSide).ToList();
                var topMost = rightChildren.OrderBy(c => c.Position.Y).FirstOrDefault();
                if (topMost != null)
                    SelectedNode = topMost;
            }
            else
            {
                if (!current.IsLeftSide)
                {
                    if (current.Children.Count > 0)
                    {
                        SelectedNode = current.Children[0];
                    }
                    else
                    {
                        var candidates = FindOverlappingRight(current);
                        if (candidates != null)
                            SelectedNode = candidates;
                    }
                }
                else
                {
                    SelectedNode = current.Parent;
                }
            }

            //RequestLayout(); // 선택이 바뀐 후 재정렬이 필요한 경우
        }

        private MindMapNodeViewModel? FindOverlappingAbove(MindMapNodeViewModel current)
        {
            return Nodes
                .Where(n => n.Position.Y < current.Position.Y)
                .Where(n => Math.Abs(n.Position.X - current.Position.X) < 50)
                .OrderByDescending(n => n.Position.Y)
                .FirstOrDefault();
        }


        private void MoveSelectionUp()
        {
            var current = SelectedNode;
            if (current == null) return;

            if (current.Parent != null)
            {
                var siblings = current.Parent.Children;
                var upper = siblings
                    .Where(s => s.Position.Y < current.Position.Y)
                    .OrderByDescending(s => s.Position.Y)
                    .FirstOrDefault();

                if (upper != null)
                {
                    SelectedNode = upper;
                }
                else
                {
                    var jumpTarget = FindOverlappingAbove(current);
                    if (jumpTarget != null)
                    {
                        SelectedNode = jumpTarget;
                    }
                }
            }
        }

        private MindMapNodeViewModel? FindOverlappingBelow(MindMapNodeViewModel current)
        {
            return Nodes
                .Where(n => n.Position.Y > current.Position.Y)
                .Where(n => Math.Abs(n.Position.X - current.Position.X) < 50)
                .OrderBy(n => n.Position.Y)
                .FirstOrDefault();
        }


        private void MoveSelectionDown()
        {
            var current = SelectedNode;
            if (current == null) return;

            if (current.Parent != null)
            {
                var siblings = current.Parent.Children;
                var lower = siblings
                    .Where(s => s.Position.Y > current.Position.Y)
                    .OrderBy(s => s.Position.Y)
                    .FirstOrDefault();

                if (lower != null)
                {
                    SelectedNode = lower;
                }
                else
                {
                    var jumpTarget = FindOverlappingBelow(current);
                    if (jumpTarget != null)
                    {
                        SelectedNode = jumpTarget;
                    }
                }
            }

            //RequestLayout();
        }

        // ---------- 노드 이동 ----------
        private bool CanMoveNodeUp()
        {
            var node = SelectedNode;
            if (node?.Parent == null) return false;

            var siblings = node.Parent.Children;
            int index = siblings.IndexOf(node);
            return index > 0;
        }
        private void MoveNodeUp()
        {
            var node = SelectedNode;
            if (node?.Parent == null) return;

            var siblings = node.Parent.Children;
            int index = siblings.IndexOf(node);
            if (index > 0)
            {
                // 순서 교환
                var elder = siblings[index - 1];
                siblings[index - 1] = node;
                siblings[index] = elder;

                // 위치 교환
                double tempY = node.Position.Y;
                node.Position = new Point(node.Position.X, elder.Position.Y);
                elder.Position = new Point(elder.Position.X, tempY);

                RequestLayout();
            }
        }
        private bool CanMoveNodeDown()
        {
            var node = SelectedNode;
            if (node?.Parent == null) return false;

            var siblings = node.Parent.Children;
            int index = siblings.IndexOf(node);
            return index >= 0 && index < siblings.Count - 1;
        }

        private void MoveNodeDown()
        {
            var node = SelectedNode;
            if (node?.Parent == null) return;

            var siblings = node.Parent.Children;
            int index = siblings.IndexOf(node);
            if (index >= 0 && index < siblings.Count - 1)
            {
                var younger = siblings[index + 1];
                siblings[index + 1] = node;
                siblings[index] = younger;

                // 위치 교환
                double tempY = node.Position.Y;
                node.Position = new Point(node.Position.X, younger.Position.Y);
                younger.Position = new Point(younger.Position.X, tempY);

                //RequestLayout();
            }
        }

        private bool CanMoveNodeLeft()
        {
            var node = SelectedNode;
            return node?.Parent?.Parent != null;
        }


        private void MoveNodeLeft()
        {
            var node = SelectedNode;
            if (node == null || node.Parent == null) return;

            var parent = node.Parent;
            var grandParent = parent.Parent;
            if (grandParent == null) return;

            // 1. 부모에서 자신 제거
            parent.Children.Remove(node);

            // 2. 부모의 형제 목록에서 자신의 새 위치 계산
            var siblings = grandParent.Children;
            int parentIndex = siblings.IndexOf(parent);

            // 3. 부모 바로 뒤에 삽입 (동생으로)
            int insertIndex = parentIndex + 1;
            if (insertIndex > siblings.Count)
                insertIndex = siblings.Count;

            siblings.Insert(insertIndex, node);
            node.Parent = grandParent;

            RequestLayout();
        }

        private bool CanMoveNodeRight()
        {
            var node = SelectedNode;
            if (node?.Parent == null) return false;

            var siblings = node.Parent.Children;
            int index = siblings.IndexOf(node);
            return index > 0; // 앞에 형제가 있어야 함
        }

        private void MoveNodeRight()
        {
            var node = SelectedNode;
            if (node?.Parent == null) return;

            var oldParent = node.Parent;
            var siblings = oldParent.Children;
            int index = siblings.IndexOf(node);
            if (index <= 0) return;

            var elder = siblings[index - 1]; // 바로 앞의 형
            oldParent.Children.Remove(node);

            elder.Children.Add(node);
            node.Parent = elder;

            // 위치 조정: 형보다 오른쪽/아래
            double newX = elder.Position.X + 150;
            double newY = elder.Position.Y + 100;
            node.Position = new Point(newX, newY);

            RequestLayout();
        }
    }
}
