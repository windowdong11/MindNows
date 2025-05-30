using CommunityToolkit.Mvvm.ComponentModel;
using MindMap.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.ViewModels;


public record ArrowCurvePoints(
    PointF Start, PointF Control, PointF End
);


public partial class ArrowVM : ObservableObject
{
    private readonly NodeVM _from;
    private readonly NodeVM _to;
    private float _curvature = 30; // 기본값

    public NodeVM From => _from;
    public NodeVM To => _to;

    public ArrowVM(NodeVM from, NodeVM to)
    {
        _from = from;
        _to = to;

        // 두 노드의 위치 변화 이벤트 구독
        _from.Model.PropertyChanged += Node_PropertyChanged;
        _to.Model.PropertyChanged += Node_PropertyChanged;
    }

    public float Curvature
    {
        get => _curvature;
        set => SetProperty(ref _curvature, value);
    }

    // Geometry 대신 플랫폼 중립 데이터만 노출
    public ArrowCurvePoints Curve
    {
        get
        {
            // 예: 노드 중심 좌표 + 곡률 반영
            var start = new PointF(_from.Model.Position.X + 60, _from.Model.Position.Y + 24);
            var end = new PointF(_to.Model.Position.X + 60, _to.Model.Position.Y + 24);
            var mid = new PointF((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            float dx = end.X - start.X, dy = end.Y - start.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            float nx = -dy / len, ny = dx / len;
            var control = new PointF(mid.X + nx * Curvature, mid.Y + ny * Curvature);
            return new ArrowCurvePoints(start, control, end);
        }
    }

    private void Node_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NodeModel.Position))
            OnPropertyChanged(nameof(Curve));
    }
}