using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MindMap.Core.Models;
using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace MindMap.ViewModels;

public enum SiblingMove
{
    Up,
    Down,
}

public partial class DocumentVM : ObservableObject
{
    public ObservableCollection<NodeVM> Roots { get; }
    public ObservableCollection<NodeVM> AllNodes { get; }
    public ObservableCollection<EdgeVM> Edges { get; } = new();

    public NodeVM? ArrowSourceNode { get; set; }
    public ObservableCollection<ArrowVM> Arrows { get; } = new();

    private ArrowVM? _selectedArrow;
    public ArrowVM? SelectedArrow
    {
        get => _selectedArrow;
        set => SetProperty(ref _selectedArrow, value);
    }

    // editing node property, and IsEditing
    private NodeVM? _editingNode;
    public bool IsEditing => EditingNode is not null;
    public NodeVM? EditingNode
    {
        get => _editingNode;
        set
        {
            if (SetProperty(ref _editingNode, value))
            {
                // 편집 모드가 설정되면 선택 해제
                if (value is not null) _sel.Select(_sel.Current);
            }
        }
    }

    // 의존 서비스(나중 단계에서 주입)
    public DocumentVM(ISelectionService selSvc, ILayoutService laySvc, INodeMutationService mutSvc, IHitTestService hitSvc, IClipboardService clipboardSvc)
    {
        // ───── 샘플 트리 ─────
        var root = new NodeModel(Guid.NewGuid(), "Root");
        var childA = new NodeModel(Guid.NewGuid(), "Child A");
        var childC = new NodeModel(Guid.NewGuid(), "Child C");
        var childE = new NodeModel(Guid.NewGuid(), "Child E");
        childA.Children.Add(childC);
        childA.Children.Add(childE);
        root.Children.Add(childA);
        root.Children.Add(new NodeModel(Guid.NewGuid(), "Child B"));
        root.Children.Add(new NodeModel(Guid.NewGuid(), "Child D"));
        var secondRoot = new NodeModel(Guid.NewGuid(), "sec Root")
        {
            Position = new System.Drawing.Point(300, 200)
        };


        // 초기화
        _sel = selSvc;
        _lay = laySvc;
        _mut = mutSvc;
        _hit = hitSvc;
        _clipboard = clipboardSvc;
        Roots = new ObservableCollection<NodeVM> { new(root, selSvc), new(secondRoot, selSvc) };
        BuildParentMap(root, null); // 주행성 보조

        // 플랫 컬렉션 초기화
        AllNodes = new ObservableCollection<NodeVM>();

        // 루트 선택 기본값
        _sel.Select(Roots.First().Model);
        //_lay.Arrange(root);
        BuildEdgesAndLayout();

        _sel.SelectionChanged += (_, _) =>
        {
            if (EditingNode != null && _sel.Current != EditingNode.Model)
                EditingNode = null;
            if (ArrowSourceNode != null && _sel.Current != ArrowSourceNode.Model)
            {
                if (!Arrows.Any(a => a.From == ArrowSourceNode && a.To.Model == _sel.Current))
                {
                    var arrowDestNode = AllNodes.FirstOrDefault(n => n.Model == _sel.Current);
                    if (arrowDestNode is null) throw new Exception("Arrow destination node not found in AllNodes.");
                    AddArrow(ArrowSourceNode, arrowDestNode);
                }

                ArrowSourceNode = null; // 모드 해제
            }
        };

        // Key 명령 래퍼
        NavigateCommand = new RelayCommand<Direction?>(dir =>
        {
            if (dir is not null) _sel.Navigate(dir.Value);
        }, _ => !IsEditing);
        ExpandSelectionCommand = new RelayCommand<Direction?>(d =>
        {
            if (d is Direction.Up or Direction.Down)
                _sel.ExpandRange(d.Value);
        }, _ => !IsEditing);
        MoveSiblingCommand = new RelayCommand<Direction>(direction =>
        {
            // 다중 선택 시, 방향에 따라 정렬 후 순차 이동
            var selected = _sel.Multi.OfType<NodeModel>().ToList();
            if (!selected.Any()) return;

            var parent = _sel.GetParent(selected.First());
            if (parent is null)
            {
                // 부모가 없는 경우(루트 노드 등) 이동 불가
                return;
            }
            var ordered = direction switch
            {
                Direction.Up => selected.OrderBy(n => parent.Children.IndexOf(n)),          // 위로 이동: 작은 인덱스부터
                Direction.Down => selected.OrderByDescending(n => parent.Children.IndexOf(n)), // 아래 이동: 큰 인덱스부터
                _ => throw new ArgumentException("Invalid direction for sibling move.", nameof(direction))
            };

            int delta = direction switch
            {
                Direction.Up => -1,
                Direction.Down => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction for sibling move.")
            };
            bool changed = false;
            foreach (var n in ordered)
                changed |= _mut.MoveWithinSiblings(n, delta);

            if (changed)
            {
                BuildEdgesAndLayout();  // LayoutService.Arrange + Edge.Refresh
            }
        }, _ => !IsEditing);
        ReparentCommand = new RelayCommand<ReparentAction?>(moveOpt =>
        {
            if (moveOpt is not { } move) return;
            var selection = _sel.Multi.OfType<NodeModel>().ToList();
            if (selection.Count == 0) return;

            var parent = _sel.GetParent(selection.First());
            if (parent is null)
            {
                // 부모가 없는 경우(루트 노드 등) 이동 불가
                return;
            }

            var grandParent = _sel.GetParent(parent);
            var nodeSide = selection[0].Side;

            bool ConnectToElderSibling()
            {
                var ordered = selection.OrderBy(n => parent.Children.IndexOf(n));
                var idx = parent.Children.IndexOf(ordered.First());
                var elderSibling = parent.Children
                    .Take(idx)
                    .LastOrDefault(sibling => sibling.Side == ordered.First().Side);
                if (elderSibling is null) return false; // 같은 방향의 형제가 없는 경우 이동 불가
                var changed = false;
                foreach (var n in ordered)
                {
                    // 형제 노드로 이동
                    changed = _mut.Reparent(n, elderSibling);
                }
                return changed;
            }
            bool ConnectToGrandParent()
            {
                var ordered = selection.OrderBy(n => parent.Children.IndexOf(n));
                // 부모의 자식으로 이동
                var changed = false;
                var parentIdx = grandParent.Children.IndexOf(parent);
                var delta = -grandParent.Children
                    .Skip(parentIdx + 1)
                    .Count(sibling => sibling.Side == nodeSide);
                foreach (var n in ordered)
                {
                    changed |= _mut.Reparent(n, grandParent);
                    _mut.MoveWithinSiblings(n, delta);
                }
                // parentIdx부터 마지막까지 같은 방향에 있는 형제 노드의 수
                return changed;
            }
            bool MoveRootChildrenToSide(SideEnum side)
            {
                var ordered = selection.OrderBy(n => parent.Children.IndexOf(n));
                var changed = false;
                foreach (var n in ordered)
                {
                    changed |= _mut.Reparent(n, parent); // 부모의 자식으로 이동
                    changed |= _mut.SetSide(n, side);
                }
                return changed;
            }

            bool changed = false;
            if (move == ReparentAction.Left)
            {
                if (nodeSide == SideEnum.Right)
                {
                    if (grandParent is null)
                        changed = MoveRootChildrenToSide(SideEnum.Left); // 루트 노드의 자식으로 이동
                    else 
                        changed = ConnectToGrandParent();
                }
                else if (nodeSide == SideEnum.Left)
                {
                    changed = ConnectToElderSibling();
                }
                else
                {
                    throw new InvalidOperationException("Invalid node side for left reparenting.");
                }
            }
            else
            {
                if (nodeSide == SideEnum.Right)
                    changed = ConnectToElderSibling();
                else if (nodeSide == SideEnum.Left)
                {
                   if (grandParent is null)
                        changed = MoveRootChildrenToSide(SideEnum.Right); // 루트 노드의 자식으로 이동
                    else
                        changed = ConnectToGrandParent();
                }
                else
                {
                    throw new InvalidOperationException("Invalid node side for right reparenting.");
                }
            }

            if (changed) BuildEdgesAndLayout();
        }, _ => !IsEditing);
        // Edit Mode Commands
        EnterEditModeCommand = new RelayCommand(() =>
        {
            if (_sel.Current is NodeModel current)
            {
                EditingNode = AllNodes.FirstOrDefault(n => n.Model == current);
            }
        }, () => !IsEditing);
        ExitEditModeCommand = new RelayCommand(() =>
        {
            EditingNode = null; // 편집 모드 종료
        }, () => IsEditing);
        // Add Nodes
        AddChildCommand = new RelayCommand(() =>
        {
            var selNode = _sel.Current;
            if (selNode == null) return;

            // 새 모델 생성
            var newNode = new NodeModel(Guid.NewGuid(), "새 노드")
            {
                Side = selNode.Side
            };

            // 트리 연결
            selNode.Children.Add(newNode);
            _sel.RegisterParent(newNode, selNode);

            // 뷰 갱신
            BuildEdgesAndLayout();

            // 새 노드 선택
            _sel.Select(newNode);
        }, () => !IsEditing && _sel.Current != null);
        AddSiblingCommand = new RelayCommand(() =>
        {
            var selNode = _sel.Current;
            if (selNode == null) return;
            // 새 모델 생성
            var newNode = new NodeModel(Guid.NewGuid(), "새 형제 노드")
            {
                Side = selNode.Side
            };
            // 부모 노드 찾기
            var parent = _sel.GetParent(selNode);
            if (parent == null) return; // 루트 노드 등 부모가 없는 경우
            // 트리 연결
            parent.Children.Add(newNode);
            _sel.RegisterParent(newNode, parent);
            // 뷰 갱신
            BuildEdgesAndLayout();
            // 새 노드 선택
            _sel.Select(newNode);
        }, () => !IsEditing && _sel.Current != null);
    }
    void BuildParentMap(NodeModel node, NodeModel? parent)
    {
        _sel.RegisterParent(node, parent);
        foreach (var child in node.Children)
            BuildParentMap(child, node);
    }

    public void RefreshLayout()
    {
        foreach (var r in Roots)
            _lay.Arrange(r.Model);
    }


    public void BuildEdgesAndLayout()
    {
        Edges.Clear();
        AllNodes.Clear();
        foreach (var root in Roots)
        {
            FlattenAndEdges(root);

            // 1) 레이아웃
            _lay.Arrange(root.Model);
            // 2) Edge Path 계산 갱신
            foreach (var e in Edges) e.Refresh();
        }
        _hit.BuildIndex(Roots.Select(r => r.Model));
        foreach (var arrow in Arrows)
        {
            //arrow.
        }
    }

    void FlattenAndEdges(NodeVM vm)
    {
        AllNodes.Add(vm);
        foreach (var c in vm.Children)
        {
            Edges.Add(new EdgeVM(vm, c));
            FlattenAndEdges(c);
        }
    }

    public void AddArrow(NodeVM from, NodeVM to)
    {
        var model = new ArrowModel { From = from.Model, To = to.Model };
        var arrowVM = new ArrowVM(from, to);
        Arrows.Add(arrowVM);
        SelectedArrow = arrowVM;
    }
    public void RemoveArrow(ArrowVM arrow)
    {
        Arrows.Remove(arrow);
        if (SelectedArrow == arrow)
            SelectedArrow = null;
    }


    public IRelayCommand<Direction?> NavigateCommand { get; }
    public IRelayCommand<Direction?> ExpandSelectionCommand { get; }
    public IRelayCommand<Direction> MoveSiblingCommand { get; }
    public IRelayCommand<ReparentAction?> ReparentCommand { get; }
    // Edit Mode Commands
    public IRelayCommand EnterEditModeCommand { get; }
    public IRelayCommand ExitEditModeCommand { get; }
    // Add Node Commands
    public IRelayCommand AddChildCommand { get; }
    public IRelayCommand AddSiblingCommand { get; }
    public IRelayCommand EnterArrowModeCommand => new RelayCommand(() =>
    {
        if (_sel.Current is null) return; // 선택된 노드가 없으면 리턴
        // 화살표 모드 진입
        ArrowSourceNode = AllNodes.FirstOrDefault(n => n.Model == _sel.Current);
    }, () => !IsEditing && _sel.Current is not null);

    public IRelayCommand PasteImageCommand => new RelayCommand(OnPasteImage, () => !IsEditing && _sel.Current is not null);

    private void OnPasteImage()
    {
        var node = _sel.Current;
        if (node == null) return;

        if (!_clipboard.ContainsImage())
        {
            // 필요시 사용자에게 경고 메시지 노출 (View 쪽에서 Command CanExecute로 처리 가능)
            return;
        }

        // 1. 이미지 추출
        var img = _clipboard.GetImage();
        if (img == null) return;

        // 2. 임시 파일 저장 (예: AppData/Temp/mindmap_img_GUID.png)
        string tempDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MindMapTemp");
        System.IO.Directory.CreateDirectory(tempDir);

        string fileName = $"img_{Guid.NewGuid()}.png";
        string fullPath = System.IO.Path.Combine(tempDir, fileName);

        img.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);

        // 3. NodeModel에 이미지 경로 저장
        //node.ImagePath = fullPath;

        // 4. 필요시: VM에 Notify (NodeVM이 Model.ImagePath를 중계)
        var nodeVM = AllNodes.FirstOrDefault(n => n.Model == node);
        if (nodeVM != null)
        {
            nodeVM.ImagePath = fullPath; // NotifyPropertyChanged 호출
        }
    }

    private readonly ISelectionService _sel;
    private readonly ILayoutService _lay;
    private readonly INodeMutationService _mut;
    private readonly IHitTestService _hit;
    private readonly IClipboardService _clipboard;
}