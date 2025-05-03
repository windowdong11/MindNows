using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Services
{
    internal interface IMindMapLayoutEngine
    {
        void ComputeLayout(MindMapViewModel viewModel);
    }
}
