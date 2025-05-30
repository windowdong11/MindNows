using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MindMap.Core.Layout;

public sealed class ClipboardService : IClipboardService
{
    public bool ContainsImage()
        => Clipboard.ContainsImage();

    public Bitmap? GetImage()
    {
        var bmpSource = Clipboard.GetImage();
        if (bmpSource == null)
            return null;

        // WPF BitmapSource → System.Drawing.Bitmap 변환
        using var ms = new MemoryStream();
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmpSource));
        encoder.Save(ms);
        ms.Position = 0;
        return new Bitmap(ms);
    }
}
