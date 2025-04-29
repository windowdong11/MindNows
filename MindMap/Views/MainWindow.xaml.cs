using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using MindMap.Models;
using MindMap.Services;
using MindMap.Views;

namespace MindMap
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        //public BranchControl _branch;
        private BranchManager _branchManager;
        private MindMapDocument _document;

        //private SelectionService _selectionService;
        public MainWindow()
        {
            InitializeComponent();
            _branchManager = new BranchManager(MindMapCanvas);
            //_selectionService = new SelectionService();
            this.KeyDown += MainWindow_KeyDown;
            this.Focusable = true;
            this.Focus();
            InitializeTestNodes();
        }

        private void InitializeTestNodes()
        {
            var rootNode = new MindMapNode
            {
                Text = "Root Node",
                Position = new Point(100, 100),
                //Size = new Size(100, 50)
            };
            _document = new MindMapDocument(rootNode);

            var rootNodeControl = new NodeControl
            {
                DataContext = rootNode,
                //Width = rootNode.Size.Width,
                //Height = rootNode.Size.Height,
            };

            Canvas.SetLeft(rootNodeControl, rootNode.Position.X);
            Canvas.SetTop(rootNodeControl, rootNode.Position.Y);
            MindMapCanvas.Children.Add(rootNodeControl);

            // 자식 노드 생성
            var childNode = new MindMapNode
            {
                Text = "Child Node",
                Position = new Point(600, 400),
                Parent = rootNode,
                //Size = new Size(100, 50)
            };

            rootNode.Children.Add(childNode);

            var childNodeControl = new NodeControl
            {
                DataContext = childNode,
                //Width = childNode.Size.Width,
                //Height = childNode.Size.Height
            };

            Canvas.SetLeft(childNodeControl, childNode.Position.X);
            Canvas.SetTop(childNodeControl, childNode.Position.Y);
            MindMapCanvas.Children.Add(childNodeControl);

            // 자식 노드와 부모 노드 사이에 선을 그리기 위해 BranchControl 사용
            //_branch = new BranchControl
            //{
            //    StartPoint = new Point(rootNode.Position.X, rootNode.Position.Y),
            //    EndPoint = new Point(childNode.Position.X, childNode.Position.Y)
            //};
            //MindMapCanvas.Children.Add(_branch);

            //// NodeControl을 MouseMove에 등록해서 Branch 업데이트
            //rootNodeControl.MouseMove += (s, e) => UpdateBranch(rootNode, childNode);
            //childNodeControl.MouseMove += (s, e) => UpdateBranch(rootNode, childNode);
            _branchManager.RegisterBranch(rootNode, childNode);
            rootNodeControl.MouseMove += (s, e) => _branchManager.UpdateAllBranches(rootNode);
            childNodeControl.MouseMove += (s, e) => _branchManager.UpdateAllBranches(childNode);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Tab)
            {
                AddChildNode();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                AddSiblingNode();
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                DeleteSelectedNode();
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                MoveSelectionLeft();
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                MoveSelectionRight();
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                MoveSelectionUp();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                MoveSelectionDown();
                e.Handled = true;
            }
        }

        private void AddNodeToCanvas(MindMapNode node)
        {
            var nodeControl = new NodeControl
            {
                DataContext = node
            };

            Canvas.SetLeft(nodeControl, node.Position.X);
            Canvas.SetTop(nodeControl, node.Position.Y);
            MindMapCanvas.Children.Add(nodeControl);

            nodeControl.MouseMove += (s, e) => _branchManager.UpdateAllBranches(node);
            //nodeControl.MouseLeftButtonDown += (s, e) => SelectionService.SelectNode(node);
        }

        private void MindMapCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Focus(); // 키 입력을 MainWindow에서 받을 수 있게
        }

        private void AddChildNode()
        {
            var parentNode = SelectionService.SelectedNode;
            if (parentNode == null) return;

            var childNode = new MindMapNode
            {
                Text = "New Child",
                Position = new Point(parentNode.Position.X + 150, parentNode.Position.Y + 100),
                Parent = parentNode
            };

            parentNode.Children.Add(childNode);

            var childNodeControl = new NodeControl
            {
                DataContext = childNode
            };

            Canvas.SetLeft(childNodeControl, childNode.Position.X);
            Canvas.SetTop(childNodeControl, childNode.Position.Y);
            MindMapCanvas.Children.Add(childNodeControl);

            // Branch 연결
            _branchManager.RegisterBranch(parentNode, childNode);

            // 이동시 선 자동 갱신
            childNodeControl.MouseMove += (s, e) => _branchManager.UpdateAllBranches(childNode);
        }

        private void AddSiblingNode()
        {
            var currentNode = SelectionService.SelectedNode;
            if (currentNode?.Parent == null) return; // 부모가 없는 경우(최상위)는 형제 추가 불가

            var parentNode = currentNode.Parent;

            var siblingNode = new MindMapNode
            {
                Text = "New Sibling",
                Position = new Point(currentNode.Position.X, currentNode.Position.Y + 150),
                Parent = parentNode
            };

            parentNode.Children.Add(siblingNode);

            var siblingNodeControl = new NodeControl
            {
                DataContext = siblingNode
            };

            Canvas.SetLeft(siblingNodeControl, siblingNode.Position.X);
            Canvas.SetTop(siblingNodeControl, siblingNode.Position.Y);
            MindMapCanvas.Children.Add(siblingNodeControl);

            _branchManager.RegisterBranch(parentNode, siblingNode);

            siblingNodeControl.MouseMove += (s, e) => _branchManager.UpdateAllBranches(siblingNode);
        }

        private void DeleteSelectedNode()
        {
            var targetNode = SelectionService.SelectedNode;
            if (targetNode == null) return;

            // 1. 화면에서 NodeControl, BranchControl 삭제
            DeleteNodeAndChildren(targetNode);

            // 2. 부모의 Children 리스트에서도 제거
            targetNode.Parent?.Children.Remove(targetNode);

            // 3. 선택 상태 초기화
            SelectionService.Clear();
        }

        private void DeleteNodeAndChildren(MindMapNode node)
        {
            // 자식 노드 먼저 삭제
            foreach (var child in node.Children)
            {
                DeleteNodeAndChildren(child);
            }

            // BranchControl 제거
            if (node.Parent != null)
            {
                _branchManager.RemoveBranch(node.Parent, node);
            }

            // NodeControl 제거
            var nodeControl = FindNodeControl(node);
            if (nodeControl != null)
            {
                MindMapCanvas.Children.Remove(nodeControl);
            }
        }

        private NodeControl? FindNodeControl(MindMapNode node)
        {
            foreach (var child in MindMapCanvas.Children)
            {
                if (child is NodeControl control && control.DataContext == node)
                {
                    return control;
                }
            }
            return null;
        }

        //방향키 이동
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
        private List<MindMapNode> GetAllNodes()
        {
            if (_document == null) return new List<MindMapNode>();
            var nodes = new List<MindMapNode>();
            TraverseNode(_document.RootNode, nodes);
            return nodes;
        }

        private void TraverseNode(MindMapNode node, List<MindMapNode> collected)
        {
            collected.Add(node);
            foreach (var child in node.Children)
            {
                TraverseNode(child, collected);
            }
        }

    }

}
