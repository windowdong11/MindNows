using MindMap.Common;
using MindMap.Models;
using MindMap.Repositiory;
using MindMap.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly ILayoutService _layoutService;

        public void RequestLayout()
        {
            //if (RootNode != null)
            //    _layoutService.RecalculateLayout(RootNode);
            //LayoutRequested()?.Invoke(); // 내부에서만 호출하므로 안전
            foreach (var root in RootNodes)
            {
                _layoutService.RecalculateLayout(root);
            }
        }
        public ObservableCollection<MindMapNodeViewModel> Nodes { get; } = new();
        //public MindMapNodeViewModel RootNode;
        public List<MindMapNodeViewModel> RootNodes { get; set; } = new();

        private MindMapNodeViewModel? _selectedNode;
        public MindMapNodeViewModel? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    if (_selectedNode != null)
                    {
                        _selectedNode.IsSelected = false;
                        _selectedNode.IsImageSelected = false;
                    }
                    _selectedNode = value;
                    if (_selectedNode != null) _selectedNode.IsSelected = true;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<MindMapArrowViewModel> Arrows { get; } = new();

        public void RegisterArrow(MindMapNodeViewModel from, MindMapNodeViewModel to)
        {
            var arrow = new MindMapArrowViewModel(from, to);
            Arrows.Add(arrow);
        }

        public void RemoveArrow(MindMapNodeViewModel from, MindMapNodeViewModel to)
        {
            var arrow = Arrows.FirstOrDefault(a => a.From == from && a.To == to);
            if (arrow != null)
                Arrows.Remove(arrow);
        }


        public MindMapViewModel(MindMapDocument document, ILayoutService layoutService)
        {
            _layoutService = layoutService;
            var nodeMap = new Dictionary<Guid, MindMapNodeViewModel>();

            foreach (var model in document.RootNodes)
            {
                var vm = new MindMapNodeViewModel(model, this);
                //if (model.Id == document.RootNode.Id)
                //{
                //    RootNodes.Add(vm);
                //}
                RootNodes.Add(vm);
                Nodes.Add(vm);
                nodeMap[model.Id] = vm;
            }
            //if (RootNode == null)
            //{
            //    throw new InvalidOperationException("RootNode cannot be null.");
            //}

            foreach (var vm in Nodes)
            {
                if (vm.Parent != null && nodeMap.TryGetValue(vm.Parent.Id, out var parentVm))
                {
                    vm.Parent = parentVm;
                    parentVm.AddChild(vm);
                    Arrows.Add(new MindMapArrowViewModel(parentVm, vm));
                }
            }

            AddChildCommand = new RelayCommand(_ => AddChild(), _ => !IsEditMode && SelectedNode != null);
            AddSiblingCommand = new RelayCommand(_ => AddSibling(), _ => !IsEditMode && SelectedNode?.Parent != null);
            DeleteNodeCommand = new RelayCommand(_ => DeleteNode(), _ => !IsEditMode && SelectedNode != null);
            MoveSelectionLeftCommand = new RelayCommand(_ => MoveSelectionLeft(), _ => !IsEditMode);
            MoveSelectionRightCommand = new RelayCommand(_ => MoveSelectionRight(), _ => !IsEditMode);
            MoveSelectionUpCommand = new RelayCommand(_ => MoveSelectionUp(), _ => !IsEditMode);
            MoveSelectionDownCommand = new RelayCommand(_ => MoveSelectionDown(), _ => !IsEditMode);
            MoveNodeUpCommand = new RelayCommand(_ => MoveNodeUp(), _ => !IsEditMode && CanMoveNodeUp());
            MoveNodeDownCommand = new RelayCommand(_ => MoveNodeDown(), _ => !IsEditMode && CanMoveNodeDown());
            MoveNodeLeftCommand = new RelayCommand(_ => MoveNodeLeft(), _ => !IsEditMode && CanMoveNodeLeft());
            MoveNodeRightCommand = new RelayCommand(_ => MoveNodeRight(), _ => !IsEditMode && CanMoveNodeRight());
            EnterEditModeCommand = new RelayCommand(_ => EnterEditMode(), _ => !IsEditMode && SelectedNode != null);
            ExitEditModeCommand = new RelayCommand(_ => ExitEditMode(), _ => IsEditMode);
            RecalculateLayoutCommand = new RelayCommand(_ => RequestLayout());
            SaveCommand = new RelayCommand(_ =>
            {
                var service = new MindMapPersistenceService();
                service.Save("map.json", this);
            }, _ => !IsEditMode); // 편집 모드일 때는 저장 불가
            LoadCommand = new RelayCommand(_ =>
            {
                var service = new MindMapPersistenceService();
                var (loadedDocument, loadedViewState) = service.Load("map.json");
                document = loadedDocument;
                Nodes.Clear();
                RootNodes.Clear();
                Arrows.Clear();
                var result = loadedDocument.RootNodes.Select(rootModel => BuildViewModelTree(rootModel, this)).ToList();
                foreach (var (vm, nodeList) in result)
                {
                    RootNodes.Add(vm);
                    Nodes.Add(vm);
                    foreach (var node in nodeList)
                    {
                        Nodes.Add(node);
                    }
                }
                MindMapPersistenceService.ApplyViewState(this, loadedViewState);
                RequestLayout();
            }, _ => !IsEditMode); // 편집 모드일 때는 불러오기 불가
            AddRootCommand = new RelayCommand(_ =>
            {
                var rootModel = new MindMapNode
                {
                    Text = "New Root",
                    Position = new Point(300, 200)
                };
                var rootVM = new MindMapNodeViewModel(rootModel, this);
                RootNodes.Add(rootVM);
                Nodes.Add(rootVM);
                SelectedNode = rootVM;
            }, _ => !IsEditMode); // 루트 노드는 최대 2개까지
        }

        private (MindMapNodeViewModel, List<MindMapNodeViewModel>) BuildViewModelTree(MindMapNode model, MindMapViewModel rootViewModel)
        {
            var vm = new MindMapNodeViewModel(model, rootViewModel);
            var nodeList = new List<MindMapNodeViewModel> { vm };
            foreach (var childModel in model.LeftChildren)
            {
                var (childVm, nodes) = BuildViewModelTree(childModel, rootViewModel);
                childVm.Parent = vm;
                vm.LeftChildren.Add(childVm);
                nodeList.AddRange(nodes);
                RegisterArrow(vm, childVm);
            }
            foreach (var childModel in model.RightChildren)
            {
                var (childVm, nodes) = BuildViewModelTree(childModel, rootViewModel);
                childVm.Parent = vm;
                vm.RightChildren.Add(childVm);
                nodeList.AddRange(nodes);
                RegisterArrow(vm, childVm);
            }
            return (vm, nodeList);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ICommand EnterEditModeCommand { get; }
        public bool IsEditMode { get; set; }

        //private bool _isFocused;
        //public bool IsFocused
        //{
        //    get => _isFocused;
        //    set
        //    {
        //        if (_isFocused != value)
        //        {
        //            _isFocused = value;
        //            OnPropertyChanged(nameof(IsFocused));
        //        }
        //    }
        //}

        public ICommand RecalculateLayoutCommand { get; }


        public ICommand ExitEditModeCommand { get; }
        private void ExitEditMode()
        {
            IsEditMode = false;
            if (SelectedNode != null)
                SelectedNode.IsFocused = false; // 포커스 해제
            //OnPropertyChanged(nameof(IsEditMode));
        }

        private void EnterEditMode()
        {
            if (SelectedNode != null)
            {
                SelectedNode.IsFocused = true;
            }

            IsEditMode = true;
            //OnPropertyChanged(nameof(IsEditMode));
        }


        /* ---------- 노드 추가/삭제 ---------- */
        public ICommand AddChildCommand { get; }
        public ICommand AddSiblingCommand { get; }
        public ICommand DeleteNodeCommand { get; }

        public ICommand AddRootCommand { get; }

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

        /* ---------- 저장/불러오기 ---------- */

        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }

        private void AddChild()
        {
            if (SelectedNode == null) return;

            bool isLeftSide = SelectedNode.IsLeftSide;
            if (RootNodes.Contains(SelectedNode))
            {
                var RootNode = SelectedNode;
                isLeftSide = RootNode.LeftChildren.Count > 0;
            }

            var childModel = new MindMapNode
            {
                Text = "New Child",
                Parent = SelectedNode.Model,
                IsLeftSide = isLeftSide
            };

            var childVM = new MindMapNodeViewModel(childModel, this)
            {
                Parent = SelectedNode
            };
            SelectedNode.AddChild(childVM);
            Nodes.Add(childVM);
            RegisterArrow(SelectedNode, childVM);
            SelectedNode = childVM;
            RequestLayout();
        }

        private void AddSibling()
        {
            if (SelectedNode?.Parent == null) return;
            var parent = SelectedNode.Parent;

            var childModel = new MindMapNode
            {
                Text = "New Sibling",
                Parent = parent.Model,
                IsLeftSide = SelectedNode.IsLeftSide
            };

            var childVM = new MindMapNodeViewModel(childModel, this)
            {
                Parent = parent,
            };
            //var siblings = SelectedNode.IsLeftSide ? parent.LeftChildren
            //    : parent.RightChildren;
            var siblingsIdx = SelectedNode.Siblings.IndexOf(SelectedNode);
            parent.AddChildAt(childVM, siblingsIdx + 1);
            Nodes.Add(childVM);
            RegisterArrow(parent, childVM);
            SelectedNode = childVM;

            RequestLayout();
        }

        private void DeleteNode()
        {
            if (SelectedNode == null || SelectedNode.Parent == null) return;

            void DeleteRecursive(MindMapNodeViewModel vm)
            {
                foreach (var child in vm.LeftChildren)
                    DeleteRecursive(child);
                foreach (var child in vm.RightChildren)
                    DeleteRecursive(child);

                Nodes.Remove(vm);
                RemoveArrow(vm.Parent, vm);
            }

            DeleteRecursive(SelectedNode);
            var parent = SelectedNode.Parent;
            parent.RemoveChild(SelectedNode);
            SelectedNode = parent;
            RequestLayout();
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
            if (current == null)
            {
                // 선택된 노드가 없으면 RootNode를 선택
                // TODO : RootNode가 여러개일 때는 화면 가운데에서 가장 가까운 노드 선택
                if (RootNodes.Count > 0)
                    SelectedNode = RootNodes[0];
                return;
            }

            if (current.Parent == null)
            {
                // 루트 노드 → 왼쪽 자식 중 가장 위
                var leftChildren = current.LeftChildren;
                var topMost = leftChildren.FirstOrDefault();
                if (topMost != null)
                    SelectedNode = topMost;
            }
            else
            {
                if (current.IsLeftSide)
                {
                    if (current.LeftChildren.Count > 0)
                    {
                        SelectedNode = current.LeftChildren[0];
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
                    // 오른쪽 그룹인 경우, 부모 선택
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
            if (current == null)
            {
                // 선택된 노드가 없으면 RootNode를 선택
                // TODO : RootNode가 여러개일 때는 화면 가운데에서 가장 가까운 노드 선택
                if (RootNodes.Count > 0)
                    SelectedNode = RootNodes[0];
                return;
            }

            if (current.Parent == null)
            {
                // 루트 → 오른쪽 자식 중 가장 위
                var rightChildren = current.RightChildren;
                var topMost = rightChildren.OrderBy(c => c.Position.Y).FirstOrDefault();
                if (topMost != null)
                    SelectedNode = topMost;
            }
            else
            {
                if (!current.IsLeftSide)
                {
                    if (current.LeftChildren.Count > 0)
                    {
                        SelectedNode = current.LeftChildren[0];
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
                    // 왼쪽 그룹인 경우, 부모 선택
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
            if (current == null)
            {
                // 선택된 노드가 없으면 RootNode를 선택
                // TODO : RootNode가 여러개일 때는 화면 가운데에서 가장 가까운 노드 선택
                if (RootNodes.Count > 0)
                    SelectedNode = RootNodes[0];
                return;
            }

            if (RootNodes.Contains(current))
            {
                var jumpTarget = FindOverlappingAbove(current);
                if (jumpTarget != null)
                {
                    SelectedNode = jumpTarget;
                }
            }
            else
            {
                var siblings = current.Siblings;
                //if (current.Parent.Id == RootNode.Id)
                //{
                //    if (current.IsLeftSide)
                //    {
                //        siblings = RootNode.LeftChildren.ToList();
                //    }
                //    else
                //    {
                //        siblings = RootNode.RightChildren.ToList();
                //    }
                //}
                var currentIndex = siblings.IndexOf(current);

                if (currentIndex > 0)
                {
                    SelectedNode = siblings[currentIndex - 1];
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
            if (current == null)
            {
                // 선택된 노드가 없으면 RootNode를 선택
                // TODO : RootNode가 여러개일 때는 화면 가운데에서 가장 가까운 노드 선택
                if (RootNodes.Count > 0)
                    SelectedNode = RootNodes[0];
                return;
            }

            if (RootNodes.Contains(current))
            {
                var siblings = current.Siblings;
                //var siblings = current.Parent.Children;
                //if (current.Parent.Id == RootNode.Id)
                //{
                //    if (current.IsLeftSide)
                //    {
                //        siblings = RootNode.LeftChildren.ToList();
                //    }
                //    else
                //    {
                //        siblings = RootNode.RightChildren.ToList();
                //    }
                //}
                var currentIndex = siblings.IndexOf(current);

                if (currentIndex < siblings.Count - 1)
                {
                    SelectedNode = siblings[currentIndex + 1];
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
            else
            {
                var siblings = current.Siblings;
                //if (current.Parent.Id == RootNode.Id)
                //{
                //    if (current.IsLeftSide)
                //    {
                //        siblings = RootNode.LeftChildren.ToList();
                //    }
                //    else
                //    {
                //        siblings = RootNode.RightChildren.ToList();
                //    }
                //}
                var currentIndex = siblings.IndexOf(current);

                if (currentIndex < siblings.Count - 1)
                {
                    SelectedNode = siblings[currentIndex + 1];
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
        }

        // ---------- 노드 이동 ----------
        private bool CanMoveNodeUp()
        {
            var node = SelectedNode;
            if (node == null || RootNodes.Contains(node)) return false;

            var siblings = node.Siblings;
            int index = siblings.IndexOf(node);
            return index > 0;
        }
        private void MoveNodeUp()
        {
            var node = SelectedNode;
            // 선택된 노드가 없거나, 루트 노드인 경우 무시
            if (node == null || RootNodes.Contains(node)) return;

            var siblings = node.Siblings;
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
            if (node == null || RootNodes.Contains(node)) return false;

            var siblings = node.Siblings;
            int index = siblings.IndexOf(node);
            return index >= 0 && index < siblings.Count - 1;
        }

        private void MoveNodeDown()
        {
            var node = SelectedNode;
            if (node == null || RootNodes.Contains(node)) return;

            var siblings = node.Siblings;
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

                RequestLayout();
            }
        }

        private bool CanMoveNodeLeft()
        {
            var node = SelectedNode;
            if (node == null || RootNodes.Contains(node)) return false;
            if (node.IsLeftSide)
            {
                return CanMoveNodeToBrotherChild();
            }
            else
                return true;
        }

        // 이 함수는 node의 조부모 노드가 있다고 가정함. 조부모 노드가 없으면 exception 발생할 수 있음
        private void MoveToParentSibling(MindMapNodeViewModel node)
        {
            var parent = node.Parent;
            if (parent == null) throw new InvalidOperationException("Parent is null.");
            if (parent.Parent == null) throw new InvalidOperationException("Grandparent is null.");
            var grandParent = parent.Parent;
            RemoveArrow(parent, node);
            RegisterArrow(grandParent, node);
            node.MoveToParentYongerSibling();
        }


        private void MoveNodeLeft()
        {
            var node = SelectedNode;
            if (node == null || node.Parent == null) return;

            if (node.IsLeftSide)
            {
                MoveNodeToBrotherChild(node);
            }
            else
            {
                var parent = node.Parent;
                var grandParent = parent.Parent;
                if (RootNodes.Contains(parent))
                {
                    // 왼쪽 노드로 이동
                    parent.RightChildren.Remove(node);
                    parent.Model.RightChildren.Remove(node.Model);
                    parent.LeftChildren.Add(node);
                    parent.Model.LeftChildren.Add(node.Model);
                    static void recursiveUpdate(MindMapNodeViewModel node)
                    {
                        node.IsLeftSide = true;
                        node.Model.IsLeftSide = true;
                        (node.LeftChildren, node.RightChildren) = (node.RightChildren, node.LeftChildren);
                        (node.Model.LeftChildren, node.Model.RightChildren) = (node.Model.RightChildren, node.Model.LeftChildren);
                        foreach (var item in node.LeftChildren)
                        {
                            recursiveUpdate(item);
                        }
                    }
                    recursiveUpdate(node);
                }
                else
                {
                    // 1. 부모에서 자신 제거
                    MoveToParentSibling(node);
                }
            }


            RequestLayout();
        }

        private bool CanMoveNodeToBrotherChild()
        {
            var node = SelectedNode;
            if (node == null || RootNodes.Contains(node)) return false;
            var siblings = node.Siblings;
            int index = siblings.IndexOf(node);
            return index > 0; // 앞에 형제가 있어야 함
        }

        private void MoveNodeToBrotherChild(MindMapNodeViewModel node)
        {
            // 부모에서 자신 제거
            var siblings = node.Siblings;
            int index = siblings.IndexOf(node);
            if (index == -1) throw new InvalidOperationException("Node not found in siblings.");
            if (index == 0) return;
            var elder = siblings[index - 1]; // 바로 앞의 형
            var oldParent = node.Parent;
            RemoveArrow(oldParent, node);
            RegisterArrow(elder, node);
            node.MoveToElderSiblingLastChild();
        }

        private bool CanMoveNodeRight()
        {
            var node = SelectedNode;
            if (node == null || RootNodes.Contains(node)) return false;

            if (node.IsLeftSide)
            {
                return true;
            }
            return CanMoveNodeToBrotherChild();
        }

        private void MoveNodeRight()
        {
            var node = SelectedNode;
            if (node == null || RootNodes.Contains(node)) return;

            if (node.IsLeftSide)
            {
                if (RootNodes.Contains(node.Parent))
                {
                    var parent = node.Parent;
                    // 왼쪽에서 오른쪽으로 이동
                    parent.LeftChildren.Remove(node);
                    parent.Model.LeftChildren.Remove(node.Model);
                    parent.RightChildren.Add(node);
                    parent.Model.RightChildren.Add(node.Model);
                    static void recursiveUpdate(MindMapNodeViewModel node)
                    {
                        node.IsLeftSide = false;
                        node.Model.IsLeftSide = false;
                        (node.LeftChildren, node.RightChildren) = (node.RightChildren, node.LeftChildren);
                        (node.Model.LeftChildren, node.Model.RightChildren) = (node.Model.RightChildren, node.Model.LeftChildren);
                        foreach (var item in node.RightChildren)
                        {
                            recursiveUpdate(item);
                        }
                    }
                    recursiveUpdate(node);
                }
                else
                {
                    MoveToParentSibling(node);
                }
            }
            else
            {
                MoveNodeToBrotherChild(node);
            }

            RequestLayout();
        }

        private double _offsetX;
        public double OffsetX
        {
            get => _offsetX;
            set { _offsetX = value; OnPropertyChanged(); }
        }

        private double _offsetY;
        public double OffsetY
        {
            get => _offsetY;
            set { _offsetY = value; OnPropertyChanged(); }
        }

        private double _scale = 1.0;
        public double Scale
        {
            get => _scale;
            set
            {
                _scale = Math.Max(0.1, Math.Min(3.0, value)); // 최소/최대 스케일 제한
                OnPropertyChanged();
            }
        }
    }
}
