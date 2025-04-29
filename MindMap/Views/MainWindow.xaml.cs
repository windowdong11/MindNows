using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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

            if (SelectionService.SelectedNode == null)
                return;

            bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            Console.WriteLine($"Key: {e.Key}, Ctrl: {isCtrlPressed}");

            if (isCtrlPressed)
            {
                if (e.Key == Key.Up)
                {
                    MoveNodeUp();
                    e.Handled = true;
                }
                else if (e.Key == Key.Down)
                {
                    MoveNodeDown();
                    e.Handled = true;
                }
                else if (e.Key == Key.Left)
                {
                    MoveNodeLeft();
                    e.Handled = true;
                }
                else if (e.Key == Key.Right)
                {
                    MoveNodeRight();
                    e.Handled = true;
                }
            }
            else
            {
                // 기본 방향키 이동
                HandleNormalArrowKeys(e);
            }
        }

        private void HandleNormalArrowKeys(KeyEventArgs e)
        {
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

        // 노드 이동
        private void UpdateNodeControlPosition(MindMapNode node)
        {
            var control = FindNodeControl(node);
            if (control != null)
            {
                Canvas.SetLeft(control, node.Position.X);
                Canvas.SetTop(control, node.Position.Y);
            }

            _branchManager.UpdateAllBranches(node); // 연결선도 같이 업데이트
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
        }


    }

}
