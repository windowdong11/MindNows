using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MindMap.Core.Models;
using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    // 의존 서비스(나중 단계에서 주입)
    public DocumentVM(ISelectionService selSvc, ILayoutService laySvc, INodeMutationService mutSvc /* DI 주입 */ )
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


        // 초기화
        _sel = selSvc;
        _lay = laySvc;
        _mut = mutSvc;
        Roots = new ObservableCollection<NodeVM> { new(root, selSvc) };
        BuildParentMap(root, null); // 주행성 보조

        // 플랫 컬렉션 초기화
        AllNodes = new ObservableCollection<NodeVM>();
        Flatten(Roots.First());

        void Flatten(NodeVM vm)
        {
            AllNodes.Add(vm);
            foreach (var c in vm.Children) Flatten(c);
        }

        // 루트 선택 기본값
        _sel.Select(Roots.First().Model);
        //_lay.Arrange(root);
        BuildEdgesAndLayout();

        // Key 명령 래퍼
        NavigateCommand = new RelayCommand<Direction?>(dir =>
        {
            if (dir is not null) _sel.Navigate(dir.Value);
        });
        ExpandSelectionCommand = new RelayCommand<Direction?>(d =>
        {
            if (d is Direction.Up or Direction.Down)
                _sel.ExpandRange(d.Value);
        });
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
        });
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
            // Ctrl+← (Left) → 루프를 '아래부터',  Ctrl+→ (Right) → '위부터'
            var ordered = move == ReparentAction.Left
                        ? selection.OrderByDescending(n => parent.Children.IndexOf(n))
                        : selection.OrderBy(n => parent.Children.IndexOf(n));

            bool changed = false;
            foreach (var n in ordered)
                changed |= _mut.Reparent(n, move);

            if (changed) BuildEdgesAndLayout();
        });
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


    void BuildEdgesAndLayout()
    {
        Edges.Clear();
        FlattenAndEdges(Roots.First());

        // 1) 레이아웃
        _lay.Arrange(Roots.First().Model);
        // 2) Edge Path 계산 갱신
        foreach (var e in Edges) e.Refresh();
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

    public IRelayCommand<Direction?> NavigateCommand { get; }
    public IRelayCommand<Direction?> ExpandSelectionCommand { get; }
    public IRelayCommand<Direction> MoveSiblingCommand { get; }
    public IRelayCommand<ReparentAction?> ReparentCommand { get; }
    private readonly ISelectionService _sel;
    private readonly ILayoutService _lay;
    private readonly INodeMutationService _mut;
}