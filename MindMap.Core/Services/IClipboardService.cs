using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services;

public interface IClipboardService
{
    /// <summary>
    /// 클립보드에 이미지가 있으면 true
    /// </summary>
    bool ContainsImage();

    /// <summary>
    /// 클립보드에서 이미지를 꺼내 비트맵으로 반환
    /// </summary>
    Bitmap? GetImage();
}
