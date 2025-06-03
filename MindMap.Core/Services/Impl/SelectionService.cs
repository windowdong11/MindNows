using MindMap.Core.Models;
using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services.Impl;

public sealed class SelectionService : ISelectionService
{
    private readonly HashSet<NodeModel> _multi = new();
    //private readonly Dictionary<NodeModel, NodeModel?> _parent = new();
    public NodeModel? Current { get; private set; }
    public IReadOnlyCollection<NodeModel> Multi => _multi;
    public event EventHandler? SelectionChanged;

    public void Select(NodeModel? node)
    {
        if (Current == node && _multi.Count == 1) return;

        _multi.Clear();
        Current = node;
        if (node != null) _multi.Add(node);

        Raise();
    }

    public void AddSelection(NodeModel node)
    {
        if (Current == node) return;
        if (Current is null)
        {
            Select(node);
            return;
        }
        if (!_multi.Contains(node))
        {
            _multi.Add(node);
        }
        Current = node;
        Raise();
    }

    public void ExpandRange(Direction dir)
    {
        if (Current is null) return;
        var parent = _parentMap.GetValueOrDefault(Current);
        if (parent is null) return;

        var list = parent.Children;
        var idx = list.IndexOf(Current);
        var next = dir == Direction.Up ? idx - 1
                 : dir == Direction.Down ? idx + 1 : -1;
        if (next < 0 || next >= list.Count) return;

        var target = list[next];

        // 이미 포함돼 있으면 범위 축소, 아니면 확장
        if (_multi.Contains(target))
            _multi.Remove(Current);   // 방향대로 한 칸 축소
        else
            _multi.Add(target);       // 확장

        Current = target;
        Raise();
    }
    public void Navigate(Direction dir)
    {
        if (Current is null) return;

        // 1) 간단 규칙: 형제 간 ↑/↓, 부모/첫-자식 ←/→
        var parent = _parentMap.GetValueOrDefault(Current);
        if (dir is Direction.Up or Direction.Down)
        {
            if (parent is null) return;                       // 루트는 형/동생 없음
            // 같은 방향의 형제에서 이동
            var list = parent.Children.Where(c => c.Side == Current.Side).ToList();
            var idx = list.IndexOf(Current);
            var next = dir == Direction.Up ? idx - 1
                     : dir == Direction.Down ? idx + 1 : -1;
            if (next >= 0 && next < list.Count)
                Select(list[next]);
            else
                Select(Current);       // 범위 초과 시 선택 해제
        }
        else // Left/Right
        {
            // 왼쪽에 위치한 노드를 선택한 경우, 방향 반전
            if (Current.Side == SideEnum.Left)
            {
                dir = dir switch
                {
                    Direction.Left => Direction.Right,
                    Direction.Right => Direction.Left,
                    _ => dir
                };
            }
            if (dir == Direction.Left)
            {
                if (parent is null)
                {
                    // 루트는 왼쪽 첫 자식 선택
                    if (Current.Children.Any(c => c.Side == SideEnum.Left))
                        Select(Current.Children.First(c => c.Side == SideEnum.Left));
                }
                else
                    Select(parent);                               // 부모
            }
            else if (dir == Direction.Right)
            {
                if (parent is null)
                {
                    // 루트는 오른쪽 첫 자식 선택
                    if (Current.Children.Any(c => c.Side == SideEnum.Right))
                        Select(Current.Children.First(c => c.Side == SideEnum.Right));
                }
                else if (Current.Children.Any())
                    Select(Current.Children.First());             // 첫 자식
            }
        }
    }

    /// <summary>트리 추가/삭제 시 부모 맵 갱신용</summary>
    public void RegisterParent(NodeModel child, NodeModel? parent)
        => _parentMap[child] = parent;

    private readonly Dictionary<NodeModel, NodeModel?> _parentMap = new();
    public NodeModel? GetParent(NodeModel child)
    => _parentMap.TryGetValue(child, out var p) ? p : null;

    private void Raise() => SelectionChanged?.Invoke(this, EventArgs.Empty);

    public void Remove(NodeModel node)
    {
        if (node == null) return;
        if (Current == node)
        {
            Current = null;  // 선택 해제
            _multi.Clear();  // 멀티 선택 해제
        }
        else
        {
            _multi.Remove(node);
        }
        _parentMap.Remove(node);  // 부모 맵에서 제거
        Raise();
    }
}
