using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MindMap.Core.Layout;


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
        sc.AddSingleton<IFileDialogService, FileDialogService>();
        sc.AddSingleton<IClipboardService, ClipboardService>();
        sc.AddSingleton<IHitTestService, HitTestService>();
        sc.AddSingleton<ILayoutService, LayoutService>();
        sc.AddSingleton<ISelectionService, SelectionService>();
        sc.AddSingleton<INodeMutationService, NodeMutationService>(); // require selection
        sc.AddSingleton<IDragDropService>(provider =>
        {
            var sel = provider.GetRequiredService<ISelectionService>();
            var hit = provider.GetRequiredService<IHitTestService>();
            var mut = provider.GetRequiredService<INodeMutationService>();

            // DocumentVM 생성 시 등록하는 방법도 가능
            Action rebuildVisual = () =>
            {
                var doc = provider.GetRequiredService<DocumentVM>();
                doc.BuildEdgesAndLayout();
                hit.BuildIndex(doc.Roots.Select(n => n.Model));
            };
            var doc = provider.GetRequiredService<DocumentVM>();

            return new DragDropService(sel, hit, mut, rebuildVisual);
        });
        sc.AddSingleton<IZoomPanService, ZoomPanService>();
        // MindMap.Infrastructure\Bootstrap.cs (DI 등록 추가)

        // ViewModels
        sc.AddSingleton<DocumentVM>();

        // 앞으로 단계별로 추가:
        // sc.AddSingleton<IDragDropService, DragDropService>();
        // sc.AddSingleton<IZoomPanService, ZoomPanService>();
        // …

        return sc.BuildServiceProvider();
    }
}
