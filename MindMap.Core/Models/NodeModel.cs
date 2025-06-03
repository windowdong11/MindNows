using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;

namespace MindMap.Core.Models;

public enum SideEnum { Left, Right }

//public sealed record NodeModel(
//    Guid Id,
//    string? Text = null,
//    string? ImagePath = null,      // 경로나 Data URI
//    Size ImageSize = default,
//    SideEnum Side = SideEnum.Right,
//    Size Desired = default,
//    Point Position = default
//)
//{
//    public ObservableCollection<NodeModel> Children { get; } = new();
//}

// mutable record
public class NodeModel : INotifyPropertyChanged
{
    // ───── 생성자 ─────
    public NodeModel(Guid id, string? text = null)
    {
        Id = id;
        Text = text;
    }

    // ───── 불변 키 정보 ─────
    public Guid Id { get; }

    // ───── 편집 가능한 정보 ─────
    private string? _text;
    public string? Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    private string? _imagePath;
    public string? ImagePath
    {
        get => _imagePath;
        set => SetField(ref _imagePath, value);
    }

    private Size _imageSize;
    public Size ImageSize
    {
        get => _imageSize;
        set => SetField(ref _imageSize, value);
    }

    public Size OriginalImageSize { get; set; }

    private Size _desired;
    public Size Desired
    {
        get => _desired;
        set => SetField(ref _desired, value);
    }

    private Point _position;
    public Point Position
    {
        get => _position;
        set => SetField(ref _position, value);
    }

    private SideEnum _side = SideEnum.Right;
    public SideEnum Side
    {
        get => _side;
        set => SetField(ref _side, value);
    }

    private double _nodeWidth = 120; // 기본값 설정
    public double NodeWidth
    {
        get => _nodeWidth;
        set => SetField(ref _nodeWidth, value);
    }

    private double _nodeHeight = 48; // 기본값 설정
    public double NodeHeight
    {
        get => _nodeHeight;
        set => SetField(ref _nodeHeight, value);
    }

    // Never set Children in user's code.
    public ObservableCollection<NodeModel> Children { get; set; } = new();

    // ───── INotifyPropertyChanged 헬퍼 ─────
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetField<T>(ref T storage, T value, [CallerMemberName] string? p = null)
    {
        if (Equals(storage, value)) return false;
        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        return true;
    }
}