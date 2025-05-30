using MindMap.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services;
public interface ISelectionService
{
    NodeModel? Current { get; }
    IReadOnlyCollection<NodeModel> Multi { get; }

    public event EventHandler? SelectionChanged;

    // Use Select(null) to clear selection
    void Select(NodeModel? node);
    void ExpandRange(Direction dir);        // Shift+↑/↓
    void Navigate(Direction dir);           // 화살표
    void RegisterParent(NodeModel child, NodeModel? parent);

    public NodeModel? GetParent(NodeModel child);
}