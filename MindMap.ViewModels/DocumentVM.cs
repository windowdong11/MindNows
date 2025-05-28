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

    // 의존 서비스(나중 단계에서 주입)
    public DocumentVM(ISelectionService selSvc, ILayoutService laySvc, INodeMutationService mutSvc, IHitTestService hitSvc /* DI 주입 */ )
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
        Roots = new ObservableCollection<NodeVM> { new(root, selSvc), new(secondRoot, selSvc) };
        BuildParentMap(root, null); // 주행성 보조

        // 플랫 컬렉션 초기화
        AllNodes = new ObservableCollection<NodeVM>();

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

            var grandParent = _sel.GetParent(parent);
            var nodeSide = selection[0].Side;

            bool ConnectToElderSibling()
            {
                var ordered = selection.OrderBy(n => parent.Children.IndexOf(n));
                var idx = parent.Children.IndexOf(ordered.First());
                var elderSibling = idx > 0 ? parent.Children[idx - 1] : null;
                if (elderSibling is null) return false; // 형제가 없는 경우 이동 불가
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
                var insertIdx = grandParent.Children.IndexOf(parent) + 1;
                var lastIdx = grandParent.Children.Count;
                var delta = insertIdx - lastIdx;
                var changed = false;
                foreach (var n in ordered)
                {
                    changed |= _mut.Reparent(n, grandParent);
                    _mut.MoveWithinSiblings(n, delta);
                }
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
    private readonly IHitTestService _hit;
}