using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MindMap.Core.Models;
using MindMap.Core.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;

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
            OnPropertyChanged(nameof(ImageSectionVisibility));
            //OnPropertyChanged(nameof(X));   // 위치 변화도 갱신
            //OnPropertyChanged(nameof(Y));
        };

        _model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_model.ImagePath))
            {
                OnPropertyChanged(nameof(ImagePath));
                OnPropertyChanged(nameof(ImageSectionVisibility));
            }
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

    public Size ImageSize
    {
        get => _model.ImageSize;
        set
        {
            if (_model.ImageSize == value) return;
            _model.ImageSize = value;
            OnPropertyChanged(nameof(ImageSize));
        }
    }

    public Size OriginalImageSize
    {
        get => _model.OriginalImageSize;
        set
        {
            if (_model.OriginalImageSize == value) return;
            _model.OriginalImageSize = value;
            OnPropertyChanged(nameof(OriginalImageSize));
        }
    }

    public double NodeWidth
    {
        get => _model.NodeWidth;
        set
        {
            if (_model.NodeWidth == value) return;
            _model.NodeWidth = value;
            OnPropertyChanged(nameof(NodeWidth));
        }
    }

    public double NodeHeight
    {
        get => _model.NodeHeight;
        set
        {
            if (_model.NodeHeight == value) return;
            _model.NodeHeight = value;
            OnPropertyChanged(nameof(NodeHeight));
        }
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
    public bool ImageSectionVisibility => IsSelected && !string.IsNullOrEmpty(ImagePath);
    public NodeModel Model => _model;

    public bool IsDragging
    {
        get => _isDragging;
        set => SetProperty(ref _isDragging, value);
    }
    private bool _isDragging;

    public IRelayCommand<double> ResizeImageCommand => new RelayCommand<double>(ResizeImageWidth);

    private void ResizeImageWidth(double newWidth)
    {
        if (_model.OriginalImageSize.Width <= 0 || _model.OriginalImageSize.Height <= 0) return;

        // 새 너비 설정 및 높이 자동 비율 조정
        double ratio = (double)_model.OriginalImageSize.Height / _model.OriginalImageSize.Width;
        double newHeight = newWidth * ratio;
        ImageSize = new Size((int)newWidth, (int)(newHeight));
        if (newWidth > TextWidth)
        {
            NodeWidth = newWidth + 32;
        }
        NodeHeight = 22 + TextHeight + newHeight;
    }

    public IRelayCommand<double> ResizeNodeWidthCommand => new RelayCommand<double>(ResizeNodeWidth);
    private void ResizeNodeWidth(double delta)
    {
        double newWidth = Math.Max(60, NodeWidth + delta); // 최소 60이상
        if (ImageSize.Width + 32 > newWidth)
        {
            ResizeImageWidth(newWidth - 32);
        }
        NodeWidth = newWidth;
    }

    public event EventHandler? ResizeCompleted;

    public void NotifyResizeCompleted()
    {
        ResizeCompleted?.Invoke(this, EventArgs.Empty);
    }

    private double TextWidth { get; set; } = 0; // 텍스트 너비 (자동 계산용)
    private double TextHeight { get; set; } = 0; // 텍스트 높이 (자동 계산용)

    public void UpdateNodeTextSize(double actualW, double actualH)
    {
        const double MinW = 60, MinH = 24;
        const double Eps = 0.5;        // 허용 오차
        const double verticalOffset = 22;
        const double horizontalOffset = 32;

        bool changed = false;
        TextWidth = actualW;
        TextHeight = actualH;

        // ── 너비 : NodeWidth보다 더 커질 때 업데이트
        double wantedW = Math.Max(MinW, actualW) + horizontalOffset;
        if (Math.Abs(wantedW - NodeWidth) > Eps && wantedW > NodeWidth)
        {
            NodeWidth = wantedW;
            if (_model.ImageSize.Width > wantedW)
            {
                ResizeImageWidth(wantedW - horizontalOffset);
            }
            changed = true;
        }

        // ── 높이
        double wantedH = Math.Max(MinH, actualH) + verticalOffset + _model.ImageSize.Height;
        if (Math.Abs(wantedH - NodeHeight) > Eps)
        {
            NodeHeight = wantedH;
            changed = true;
        }

        if (changed)
            ResizeCompleted?.Invoke(this, EventArgs.Empty);
    }

    public bool IsRoot => _sel.GetParent(_model) is null;
}