using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services.Impl;

public sealed class ZoomPanService : IZoomPanService
{
    private float _zoom = 1.0f;
    private PointF _pan = new(0, 0);

    public float Zoom
    {
        get => _zoom;
        set
        {
            var clamped = Math.Clamp(value, 0.25f, 3.0f); // 최소/최대 배율 제한
            if (_zoom != clamped)
            {
                _zoom = clamped;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    public PointF Pan
    {
        get => _pan;
        set
        {
            if (_pan != value)
            {
                _pan = value;
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? Changed;

    /// <summary>
    /// 지정 위치를 중심으로 delta배율만큼 확대/축소.
    /// delta: +0.1이면 10% 확대, -0.1이면 10% 축소
    /// </summary>
    public void ZoomAt(PointF mouseWorld, float delta)
    {
        float prevZoom = _zoom;
        float newZoom = Math.Clamp(_zoom * (1 + delta), 0.25f, 3.0f);

        // 확대/축소 시, 마우스 좌표가 같은 위치에 있도록 pan 조정
        // (mouseWorld - pan) * (newZoom/prevZoom) = (mouseWorld' - newPan)
        // => newPan = mouseWorld - (mouseWorld - pan) * (newZoom/prevZoom)
        var prevPan = _pan;
        var newPan = new PointF(
            mouseWorld.X - (mouseWorld.X - prevPan.X) * (newZoom / prevZoom),
            mouseWorld.Y - (mouseWorld.Y - prevPan.Y) * (newZoom / prevZoom)
        );

        _zoom = newZoom;
        _pan = newPan;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        _zoom = 1.0f;
        _pan = new(0, 0);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}