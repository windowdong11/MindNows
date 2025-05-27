using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

//using MindMap.Core.Drag;
using MindMap.Core.Services;
using MindMap.Core.Services.Impl;
using MindMap.ViewModels;

//using MindMap.ViewModels.DragDrop;
using ISelectionService = MindMap.Core.Services.ISelectionService;

namespace MindMap.Infrastructure;

public static class Bootstrap
{
    public static IServiceProvider BuildContainer()
    {
        var sc = new ServiceCollection();

        // Singleton 서비스 등록
        sc.AddSingleton<ILayoutService, LayoutService>();
        sc.AddSingleton<ISelectionService, SelectionService>();
        sc.AddSingleton<INodeMutationService, NodeMutationService>();

        // ViewModels
        sc.AddSingleton<DocumentVM>();

        // 앞으로 단계별로 추가:
        // sc.AddSingleton<IDragDropService, DragDropService>();
        // sc.AddSingleton<IZoomPanService, ZoomPanService>();
        // …

        return sc.BuildServiceProvider();
    }
}
