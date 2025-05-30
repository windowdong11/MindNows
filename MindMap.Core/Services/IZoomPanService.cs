using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Drawing;

namespace MindMap.Core.Services;

public interface IZoomPanService
{
    float Zoom { get; set; }        // 1.0=100%
    PointF Pan { get; set; }        // Canvas 이동(오프셋)
    event EventHandler? Changed;

    void ZoomAt(PointF worldPos, float delta); // worldPos 기준으로 delta만큼 줌
    void Reset();
}