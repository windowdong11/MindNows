using CommunityToolkit.Mvvm.ComponentModel;
using MindMap.Core.Models;
using MindMap.Core.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

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

        _model.Children.CollectionChanged += OnModelChildrenChanged;
    }

    // ─────────── Properties ───────────

    public string? Text
    {
        get => _model.Text;
        set { _model.Text = value; OnPropertyChanged(nameof(Text)); }
    }

    public string? ImagePath
    {
        get => _model.ImagePath;
        set { _model.ImagePath = value; OnPropertyChanged(nameof(ImagePath)); }
    }

    private void OnModelChildrenChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            foreach (NodeModel added in e.NewItems.Cast<NodeModel>())
                Children.Add(new NodeVM(added, _sel));
        }

        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            foreach (NodeModel removed in e.OldItems.Cast<NodeModel>())
            {
                var childVm = Children.FirstOrDefault(vm => vm.Model == removed);
                if (childVm != null)
                    Children.Remove(childVm);
            }
        }
    }
    public ObservableCollection<NodeVM> Children { get; }  // 로직·이동·선 연결용
    public bool IsSelected => _sel.Current == _model || _sel.Multi.Contains(_model);
    public NodeModel Model => _model;
}