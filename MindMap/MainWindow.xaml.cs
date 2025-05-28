using Microsoft.Extensions.DependencyInjection;
using MindMap.Core.Services;

//using MindMap.Core.Drag;
using MindMap.ViewModels;
using MindMap.Views.DragPreview;

using MindMap.Views;

//using MindMap.Views.Adorners;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MindMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // DI로부터 DocumentVM 해와서 DataContext 지정
            if (Application.Current is App app)
                DataContext = app.Services.GetRequiredService<DocumentVM>();
            Loaded += Window_Loaded;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                var layer = AdornerLayer.GetAdornerLayer(World);
                var svc = app.Services.GetRequiredService<IDragDropService>();

                layer.Add(new GhostAdorner(World, svc));
                layer.Add(new PlaceholderAdorner(World, svc));
                layer.Add(new TooltipAdorner(World, svc));

                // Drag controller 초기화
                _ = new DragDropController(svc, WorldScrollViewer, World);
                var _hitService = app.Services.GetRequiredService<IHitTestService>();
                var _vm = app.Services.GetRequiredService<DocumentVM>();
                _vm.BuildEdgesAndLayout();
                _hitService.BuildIndex(_vm.Roots.Select(n => n.Model));
            }
        }
    }

}