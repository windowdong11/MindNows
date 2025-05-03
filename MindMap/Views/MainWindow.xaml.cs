// MainWindow.xaml.cs (정리된 버전)
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MindMap.Models;
using MindMap.Services;
using MindMap.Views;

namespace MindMap
{
    public partial class MainWindow : Window
    {

        public event PropertyChangedEventHandler PropertyChanged;
        private BranchManager _branchManager;
        private MindMapDocument _document;

        private readonly Dictionary<Guid, NodeControl> _nodeControls = new();
        private readonly Dictionary<Guid, BoundingBox> _boundingBoxes = new();
        private readonly Dictionary<Guid, Rectangle> _boundingBoxRects = new();
        private readonly Dictionary<Guid, BoundingArea> _boundingAreas = new();
        private readonly Dictionary<Guid, Rectangle> _boundingAreaRects = new();

        public MainWindow()
        {
            InitializeComponent();
            _branchManager = new BranchManager(MindMapCanvas);
            KeyDown += MainWindow_KeyDown;
            Focusable = true;
            Focus();

            InitializeTestNodes();
            RefreshLayout();
        }

        private void InitializeTestNodes()
        {
            var rootNode = AddNode(null);
            AddNode(rootNode);
        }

        private MindMapNode AddNode(MindMapNode? parent)
        {
            var node = new MindMapNode { Text = "New node" };
            if (parent == null)
            {
                node.Position = new Point(200, 100);
                _document = new MindMapDocument(node);
            }
            else
            {
                node.Parent = parent;
                parent.Children.Add(node);
                _branchManager.RegisterBranch(parent, node);
            }

            var control = new NodeControl { DataContext = node };
            MindMapCanvas.Children.Add(control);

            _nodeControls[node.Id] = control;
            _boundingAreas[node.Id] = new BoundingArea(control.ActualWidth, control.ActualHeight);
            control.MouseMove += (s, e) => _branchManager.UpdateAllBranches(node);

            return node;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (SelectionService.SelectedNode == null && (Keyboard.Modifiers & ModifierKeys.Control) == 0)
            {
                SelectionService.Select(_document.RootNode);
                return;
            }

            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

            if (ctrl)
            {
                switch (e.Key)
                {
                    case Key.Up: MoveNodeUp(); break;
                    case Key.Down: MoveNodeDown(); break;
                    case Key.Left: MoveNodeLeft(); break;
                    case Key.Right: MoveNodeRight(); break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Tab: AddChildNode(); break;
                    case Key.Enter: AddSiblingNode(); break;
                    case Key.Back: DeleteSelectedNode(); break;
                    case Key.Left: MoveSelectionLeft(); break;
                    case Key.Right: MoveSelectionRight(); break;
                    case Key.Up: MoveSelectionUp(); break;
                    case Key.Down: MoveSelectionDown(); break;
                }
            }

            e.Handled = true;
        }

        private void AddChildNode()
        {
            if (SelectionService.SelectedNode != null)
            {
                AddNode(SelectionService.SelectedNode);
                RefreshLayout();
            }
        }

        private void AddSiblingNode()
        {
            var node = SelectionService.SelectedNode;
            if (node?.Parent != null)
            {
                AddNode(node.Parent);
                RefreshLayout();
            }
        }

        private void DeleteSelectedNode()
        {
            var node = SelectionService.SelectedNode;
            if (node == null) return;

            DeleteNodeRecursive(node);
            node.Parent?.Children.Remove(node);
            SelectionService.Clear();
            RefreshLayout();
        }

        private void DeleteNodeRecursive(MindMapNode node)
        {
            foreach (var child in node.Children.ToList())
                DeleteNodeRecursive(child);

            if (node.Parent != null)
                _branchManager.RemoveBranch(node.Parent, node);

            if (_nodeControls.TryGetValue(node.Id, out var control))
            {
                MindMapCanvas.Children.Remove(control);
                _nodeControls.Remove(node.Id);
            }
        }

        private NodeControl? FindNodeControl(MindMapNode node) =>
            _nodeControls.TryGetValue(node.Id, out var control) ? control : null;

        private List<MindMapNode> GetAllNodes()
        {
            var result = new List<MindMapNode>();
            void Traverse(MindMapNode n)
            {
                result.Add(n);
                foreach (var c in n.Children) Traverse(c);
            }
            Traverse(_document.RootNode);
            return result;
        }

        private void UpdateNodeControlPosition(MindMapNode node)
        {
            if (_nodeControls.TryGetValue(node.Id, out var control))
            {
                Canvas.SetLeft(control, node.Position.X);
                Canvas.SetTop(control, node.Position.Y);
            }
            _branchManager.UpdateAllBranches(node);
        }

        private void RefreshLayout()
        {
            ComputeBoundingArea(_document.RootNode);
            LayoutTree(_document.RootNode);
            foreach (var node in GetAllNodes())
                UpdateNodeControlPosition(node);
            ComputeBoundingBox(_document.RootNode, n =>
            {
                var c = FindNodeControl(n);
                return new Size(c?.ActualWidth ?? 150, c?.ActualHeight ?? 60);
            });
            DrawBoundingBox(_document.RootNode);
        }

        public BoundingBox ComputeBoundingBox(MindMapNode node, Func<MindMapNode, Size> getSize)
        {
            var size = getSize(node);
            var box = BoundingBox.FromNode(node.Position, size.Width, size.Height);
            foreach (var child in node.Children)
                box.Encapsulate(ComputeBoundingBox(child, getSize));
            _boundingBoxes[node.Id] = box;
            return box;
        }

        public void DrawBoundingBox(MindMapNode node)
        {
            if (!_boundingBoxes.TryGetValue(node.Id, out var box)) return;

            var isNew = !_boundingBoxRects.TryGetValue(node.Id, out var rect);
            rect ??= new Rectangle { Stroke = Brushes.Red, StrokeThickness = 2 };
            rect.Width = box.Width;
            rect.Height = box.Height;
            Canvas.SetLeft(rect, box.Left);
            Canvas.SetTop(rect, box.Top);

            if (isNew)
            {
                MindMapCanvas.Children.Add(rect);
                _boundingBoxRects[node.Id] = rect;
            }

            foreach (var child in node.Children)
                DrawBoundingBox(child);
        }

        // 이하: MoveSelection / MoveNode / LayoutTree / ComputeBoundingArea 등은 구조 동일 → 필요 시 별도 정리 가능
        public void LayoutTree(MindMapNode root)
        {
            var rootY = root.Position.Y;
            var rootX = root.Position.X;
            var boundingHeight = _boundingAreas[root.Id].Height;
            var rootHeight = _nodeControls[root.Id].ActualHeight;
            var basePosition = new Point(rootX, rootY - (boundingHeight - rootHeight) / 2);
            LayoutTree(root, basePosition);
        }

        public void LayoutTree(MindMapNode root, Point basePosition)
        {
            var rootHeight = _nodeControls[root.Id].ActualHeight;
            var rootBoundingAreaHeight = _boundingAreas[root.Id].Height;
            var childTop = basePosition.Y;
            var childLeft = basePosition.X + _nodeControls[root.Id].ActualWidth + 20;
            if (!_boundingAreaRects.ContainsKey(root.Id))
            {
                _boundingAreaRects.Add(root.Id, new Rectangle
                {
                    Stroke = Brushes.Blue,
                    StrokeThickness = 5,
                });
                MindMapCanvas.Children.Add(_boundingAreaRects[root.Id]);
            }
            _boundingAreaRects[root.Id].Width = _boundingAreas[root.Id].Width;
            _boundingAreaRects[root.Id].Height = _boundingAreas[root.Id].Height;
            Canvas.SetLeft(_boundingAreaRects[root.Id], basePosition.X);
            Canvas.SetTop(_boundingAreaRects[root.Id], basePosition.Y);

            var rootY = basePosition.Y + (rootBoundingAreaHeight - rootHeight) / 2;
            root.Position = new Point(basePosition.X, rootY);
            foreach (var child in root.Children)
            {
                var curChildBoundingAreaHeight = _boundingAreas[child.Id].Height;
                LayoutTree(child, new Point(childLeft, childTop));
                childTop += curChildBoundingAreaHeight;
            }
        }
        /*
        1. 모든 트리의 바운딩 박스를 구함
        2. 바운딩 박스를 바탕으로 자식의 총 높이를 구함 (아래 규칙을 따름)
        - 1번째 요소 : (바운딩 박스 + 자신 높이) / 2
        - 2~n-1번째 요소 : 바운딩 박스
        - n번째 요소 : (바운딩 박스 + 자신 높이) / 2
        3. 부모의 Y좌표 - 총 높이 / 2 부터 자식을 배치
        4. 다음 자식의 Y좌표는 현재 자식의 Y좌표 + 현재 자식의 바운딩 박스 높이 / 2 + 다음 자식의 바운딩 박스 높이 / 2
        */
        internal BoundingArea ComputeBoundingArea(MindMapNode root)
        {
            // 자식 노드의 총 높이 계산
            double totalHeight = 0;
            double maxChildWidth = 0;
            for (int i = 0; i < root.Children.Count; i++)
            {
                //var box = _boundingBoxs[root.Children[i].Id];
                var childBoundingArea = ComputeBoundingArea(root.Children[i]);
                maxChildWidth = Math.Max(maxChildWidth, childBoundingArea.Width);
                totalHeight += childBoundingArea.Height;
            }
            var rootControl = _nodeControls[root.Id];
            totalHeight = Math.Max(totalHeight, rootControl.ActualHeight);
            _boundingAreas[root.Id].Height = totalHeight;
            _boundingAreas[root.Id].Width = rootControl.ActualWidth + maxChildWidth > 0 ? 10 + maxChildWidth : 0;
            return _boundingAreas[root.Id];
        }

        private void MoveNodeUp()
        {
            var current = SelectionService.SelectedNode;
            if (current?.Parent == null) return;

            var siblings = current.Parent.Children;
            int index = siblings.IndexOf(current);
            if (index > 0)
            {
                var elder = siblings[index - 1];
                siblings[index - 1] = current;
                siblings[index] = elder;

                // Y좌표 교환
                double tempY = current.Position.Y;
                current.Position = new Point(current.Position.X, elder.Position.Y);
                elder.Position = new Point(elder.Position.X, tempY);

                UpdateNodeControlPosition(current);
                UpdateNodeControlPosition(elder);

                SelectionService.Select(current);
            }
            RefreshLayout();
        }

        private void MoveNodeDown()
        {
            var current = SelectionService.SelectedNode;
            if (current?.Parent == null) return;

            var siblings = current.Parent.Children;
            int index = siblings.IndexOf(current);
            if (index >= 0 && index < siblings.Count - 1)
            {
                var younger = siblings[index + 1];
                siblings[index + 1] = current;
                siblings[index] = younger;

                // Y좌표 교환
                double tempY = current.Position.Y;
                current.Position = new Point(current.Position.X, younger.Position.Y);
                younger.Position = new Point(younger.Position.X, tempY);

                UpdateNodeControlPosition(current);
                UpdateNodeControlPosition(younger);

                SelectionService.Select(current);
            }
            RefreshLayout();
        }

        private void MoveNodeLeft()
        {
            var current = SelectionService.SelectedNode;
            if (current == null || current.Parent == null) return;

            if (!current.IsLeftSide)
            {
                var parent = current.Parent;
                var grandParent = parent.Parent;

                if (grandParent != null)
                {
                    var parentSiblings = grandParent.Children;
                    int parentIndex = parentSiblings.IndexOf(parent);

                    if (parentIndex >= 0)
                    {
                        // 기존 부모에서 current 제거
                        parent.Children.Remove(current);

                        // 부모 다음에 current 삽입
                        int insertIndex = parentIndex + 1;
                        if (insertIndex > parentSiblings.Count)
                            insertIndex = parentSiblings.Count; // 혹시 모를 오버플로 방지

                        grandParent.Children.Insert(insertIndex, current);
                        current.Parent = grandParent;

                        // 기존 Branch 제거, 새로운 Branch 등록
                        _branchManager.RemoveBranch(parent, current);
                        _branchManager.RegisterBranch(grandParent, current);

                        // 위치 갱신
                        double newX = parent.Position.X + 250; // 부모보다 오른쪽
                        double newY = parent.Position.Y + 100; // 약간 아래
                        current.Position = new Point(newX, newY);
                        UpdateNodeControlPosition(current);
                    }
                }
            }
            else
            {
                // 왼쪽 그룹 로직은 기존과 동일
                var siblings = current.Parent.Children;
                int index = siblings.IndexOf(current);
                if (index > 0)
                {
                    var elder = siblings[index - 1];
                    siblings.RemoveAt(index);
                    elder.Children.Add(current);
                    current.Parent = elder;

                    _branchManager.RemoveBranch(current.Parent, current);
                    _branchManager.RegisterBranch(elder, current);

                    current.Position = new Point(elder.Position.X - 150, elder.Position.Y + 100);
                    UpdateNodeControlPosition(current);
                }
            }
            RefreshLayout();
        }




        private void MoveNodeRight()
        {
            var current = SelectionService.SelectedNode;
            if (current == null || current.Parent == null) return;

            if (current.IsLeftSide)
            {
                // 왼쪽 그룹 노드
                var parent = current.Parent;
                var grandParent = parent.Parent;

                if (grandParent != null)
                {
                    var parentSiblings = grandParent.Children;
                    int parentIndex = parentSiblings.IndexOf(parent);

                    if (parentIndex >= 0)
                    {
                        // 기존 부모에서 current 제거
                        parent.Children.Remove(current);

                        // 부모 다음에 current 삽입
                        int insertIndex = parentIndex + 1;
                        if (insertIndex > parentSiblings.Count)
                            insertIndex = parentSiblings.Count;

                        grandParent.Children.Insert(insertIndex, current);
                        current.Parent = grandParent;

                        // 가지 재설정
                        _branchManager.RemoveBranch(parent, current);
                        _branchManager.RegisterBranch(grandParent, current);

                        // 위치 조정
                        double newX = parent.Position.X + 250; // 부모보다 오른쪽
                        double newY = parent.Position.Y + 100;
                        current.Position = new Point(newX, newY);
                        UpdateNodeControlPosition(current);
                    }
                }
            }
            else
            {
                // 오른쪽 그룹 노드
                var siblings = current.Parent.Children;
                int index = siblings.IndexOf(current);
                if (index > 0)
                {
                    var elder = siblings[index - 1];
                    var oldParent = current.Parent; // 이동 전 부모를 저장

                    siblings.RemoveAt(index);
                    elder.Children.Add(current);
                    current.Parent = elder;

                    _branchManager.RemoveBranch(oldParent, current); // 이동 전 부모와의 Branch를 삭제
                    _branchManager.RegisterBranch(elder, current);   // 이동 후 새 부모와 Branch를 등록

                    current.Position = new Point(elder.Position.X + 150, elder.Position.Y + 100);
                    UpdateNodeControlPosition(current);
                }
            }
            RefreshLayout();
        }
        private void MoveSelectionLeft()
        {
            var current = SelectionService.SelectedNode;
            if (current == null) return;

            if (current.Parent == null)
            {
                // 최상위 루트 → 왼쪽 자식 노드들 중 가장 위
                var leftChildren = current.Children.Where(c => c.IsLeftSide).ToList();
                var topMost = leftChildren.OrderBy(c => c.Position.Y).FirstOrDefault();
                if (topMost != null)
                    SelectionService.Select(topMost);
            }
            else
            {
                // 부모가 있는 경우
                if (current.IsLeftSide)
                {
                    if (current.Children.Count > 0)
                    {
                        SelectionService.Select(current.Children[0]);
                    }
                    else
                    {
                        var candidates = FindOverlappingNodesLeft(current);
                        if (candidates != null)
                            SelectionService.Select(candidates);
                    }
                }
                else
                {
                    // 오른쪽 노드 → 부모로 이동
                    SelectionService.Select(current.Parent);
                }
            }

            RefreshLayout();
        }

        private void MoveSelectionRight()
        {
            var current = SelectionService.SelectedNode;
            if (current == null) return;

            if (current.Parent == null)
            {
                // 최상위 루트 → 오른쪽 자식 노드들 중 가장 위
                var rightChildren = current.Children.Where(c => !c.IsLeftSide).ToList();
                var topMost = rightChildren.OrderBy(c => c.Position.Y).FirstOrDefault();
                if (topMost != null)
                    SelectionService.Select(topMost);
            }
            else
            {
                if (!current.IsLeftSide)
                {
                    if (current.Children.Count > 0)
                    {
                        SelectionService.Select(current.Children[0]);
                    }
                    else
                    {
                        var candidates = FindOverlappingNodesRight(current);
                        if (candidates != null)
                            SelectionService.Select(candidates);
                    }
                }
                else
                {
                    SelectionService.Select(current.Parent);
                }
            }

            RefreshLayout();
        }

        private void MoveSelectionUp()
        {
            var current = SelectionService.SelectedNode;
            if (current?.Parent == null) return;

            var siblings = current.Parent.Children;
            var upper = siblings.Where(s => s.Position.Y < current.Position.Y)
                                 .OrderByDescending(s => s.Position.Y)
                                 .FirstOrDefault();
            if (upper != null)
                SelectionService.Select(upper);

            RefreshLayout();
        }

        private void MoveSelectionDown()
        {
            var current = SelectionService.SelectedNode;
            if (current?.Parent == null) return;

            var siblings = current.Parent.Children;
            var lower = siblings.Where(s => s.Position.Y > current.Position.Y)
                                 .OrderBy(s => s.Position.Y)
                                 .FirstOrDefault();
            if (lower != null)
                SelectionService.Select(lower);

            RefreshLayout();
        }
        private MindMapNode? FindOverlappingNodesLeft(MindMapNode current)
        {
            var candidates = GetAllNodes()
                             .Where(n => n.Position.X < current.Position.X)
                             .Where(n => Math.Abs(n.Position.Y - current.Position.Y) < 50) // Y축 오차 허용
                             .OrderByDescending(n => n.Position.X)
                             .FirstOrDefault();
            return candidates;
        }

        private MindMapNode? FindOverlappingNodesRight(MindMapNode current)
        {
            var candidates = GetAllNodes()
                             .Where(n => n.Position.X > current.Position.X)
                             .Where(n => Math.Abs(n.Position.Y - current.Position.Y) < 50)
                             .OrderBy(n => n.Position.X)
                             .FirstOrDefault();
            return candidates;
        }
    }
}
