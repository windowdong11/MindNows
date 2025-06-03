using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services;

public interface IFileDialogService
{
    string? ShowOpenFileDialog(string filter);
    string? ShowSaveFileDialog(string filter, string? defaultName = null);
}
