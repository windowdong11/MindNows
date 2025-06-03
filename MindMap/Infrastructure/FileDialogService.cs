using Microsoft.Win32;
using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Infrastructure;

public class FileDialogService : IFileDialogService
{
    public string? ShowOpenFileDialog(string filter)
    {
        var dlg = new OpenFileDialog { Filter = filter };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
    public string? ShowSaveFileDialog(string filter, string? defaultName = null)
    {
        var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName ?? "" };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }
}
