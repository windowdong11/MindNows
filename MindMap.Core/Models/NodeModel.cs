using System.Collections.ObjectModel;
using System.Drawing;

namespace MindMap.Core.Models;

public enum SideEnum { Left, Right }

public sealed record NodeModel(
    Guid Id,
    string? Text = null,
    string? ImagePath = null,      // 경로나 Data URI
    Size ImageSize = default,
    SideEnum Side = SideEnum.Right,
    Size Desired = default,
    Point Position = default
)
{
    public ObservableCollection<NodeModel> Children { get; } = new();
}
