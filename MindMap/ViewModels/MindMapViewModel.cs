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
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ICommand AddChildCommand { get; }
        public ICommand AddSiblingCommand { get; }
        public ICommand DeleteNodeCommand { get; }
        public ICommand MoveSelectionLeftCommand { get; }
        public ICommand MoveSelectionRightCommand { get; }
        public ICommand MoveSelectionUpCommand { get; }
        public ICommand MoveSelectionDownCommand { get; }

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
    }
}
