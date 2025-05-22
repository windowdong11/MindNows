using CommunityToolkit.Mvvm.ComponentModel;
using MindMap.Core.Models;
using MindMap.Core.Services;
using System.Collections.ObjectModel;

namespace MindMap.ViewModels;

public partial class NodeVM : ObservableObject
{
    private NodeModel _model;                 // readonly 제거

    private readonly ISelectionService _sel;

    public NodeVM(NodeModel model, ISelectionService sel)
    {
        _model = model;
        _sel = sel;

        // _model 할당 뒤에 Children 초기화
        Children = new ObservableCollection<NodeVM>(
                        _model.Children.Select(c => new NodeVM(c, sel)));

        // 선택 변경 시각화를 위한 이벤트 구독
        _sel.SelectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsSelected));
            //OnPropertyChanged(nameof(X));   // 위치 변화도 갱신
            //OnPropertyChanged(nameof(Y));
        };
    }

    // ─────────── Properties ───────────

    public string? Text
    {
        get => _model.Text;
        set => SetProperty(_model.Text, value,
                           v => _model = _model with { Text = v });
    }

    public string? ImagePath
    {
        get => _model.ImagePath;
        set => SetProperty(_model.ImagePath, value,
                           v => _model = _model with { ImagePath = v });
    }

public ObservableCollection<NodeVM> Children { get; }
    public bool IsSelected => _sel.Current == _model || _sel.Multi.Contains(_model);
    public NodeModel Model => _model;
}