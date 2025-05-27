using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Point = System.Windows.Point;

namespace MindMap.Views;

public sealed class DragDropController
{
    private readonly IDragDropService _svc;
    private readonly ScrollViewer _scroll;   // 좌표 변환
    private readonly Canvas _world;

    public DragDropController(IDragDropService svc,
                              ScrollViewer scroll, Canvas world)
    {
        _svc = svc; _scroll = scroll; _world = world;

        // Input hookups
        _world.MouseLeftButtonDown += OnDown;
        _world.MouseMove += OnMove;
        _world.MouseLeftButtonUp += OnUp;
        _world.MouseRightButtonDown += (_, _) => _svc.CancelDrag();
        _world.KeyDown += (_, e) => { if (e.Key == Key.Escape) _svc.CancelDrag(); };
    }

    PointF ToWorld(Point pScreen)
    {
        var point = _scroll.TranslatePoint(pScreen, _world);
        return new PointF((float)point.X, (float)point.Y);
    }

    void OnDown(object? s, MouseButtonEventArgs e)
    { _svc.BeginDrag(ToWorld(e.GetPosition(_world))); }

    void OnMove(object? s, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            _svc.UpdateDrag(ToWorld(e.GetPosition(_world)));
    }

    void OnUp(object? s, MouseButtonEventArgs e)
    { _svc.CommitDrag(ToWorld(e.GetPosition(_world))); }
}
