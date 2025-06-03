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
using System.Drawing;
using Point = System.Windows.Point;
using System.Diagnostics;

namespace MindMap
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly private IZoomPanService _zoomSvc;
        readonly private IHitTestService _hitSvc;
        private bool _isPanning = false;
        private Point _screenStartMouse;       // 스크린 좌표 기준 마우스 시작점
        private PointF _panStartValue;         // 패닝 시작 시 Pan 값

        public MainWindow()
        {
            InitializeComponent();

            // DI로부터 DocumentVM 해와서 DataContext 지정
            if (Application.Current is App app)
            {
                DataContext = app.Services.GetRequiredService<DocumentVM>();
                Loaded += Window_Loaded;
                _zoomSvc = app.Services.GetRequiredService<IZoomPanService>();
                _hitSvc = app.Services.GetRequiredService<IHitTestService>();
                if (_zoomSvc == null || _hitSvc == null)
                    throw new InvalidOperationException("ZoomPanService is not available.");
            }
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

                //Drag controller 초기화
               _ = new DragDropController(svc, World);
                var _hitService = app.Services.GetRequiredService<IHitTestService>();
                var _vm = app.Services.GetRequiredService<DocumentVM>();
                _vm.BuildEdgesAndLayout();
                _hitService.BuildIndex(_vm.Roots.Select(n => n.Model));

                _zoomSvc.Changed += (_, _) => UpdateTransform();
                UpdateTransform();
                // 마우스 휠 이벤트 연결
                World.MouseWheel += OnWorldMouseWheel;
                World.MouseLeftButtonDown += OnWorldMouseLeftButtonDown;
                World.MouseMove += OnWorldMouseMove;
                World.MouseLeftButtonUp += OnWorldMouseLeftButtonUp;
                this.KeyDown += Window_KeyDown;
            }
        }

        private void UpdateTransform()
        {
            if (_zoomSvc is null) return;
            ZoomTransform.ScaleX = ZoomTransform.ScaleY = _zoomSvc.Zoom;
            PanTransform.X = _zoomSvc.Pan.X;
            PanTransform.Y = _zoomSvc.Pan.Y;
        }

        private void OnWorldMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_zoomSvc is null) return;

            // WPF에서 마우스 위치를 World(Canvas) 기준으로 변환
            var p = e.GetPosition(null);
            var mouseWorld = new System.Drawing.PointF((float)p.X, (float)p.Y);

            // 보통 120당 1단계(10%)로 조정
            float delta = e.Delta > 0 ? +0.1f : -0.1f;
            _zoomSvc.ZoomAt(mouseWorld, delta);

            e.Handled = true;
        }

        private void OnWorldMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(null);
            if (!IsOnBackground(TranslatePoint(mousePos, World))) return;
            _isPanning = true;
            _panStartValue = _zoomSvc?.Pan ?? new PointF(0, 0);
            _screenStartMouse = mousePos;
            World.CaptureMouse();
        }

        private void OnWorldMouseMove(object sender, MouseEventArgs e)
        {
            if (_isPanning && e.LeftButton == MouseButtonState.Pressed)
            {
                var winPt = e.GetPosition(null);
                var dx = winPt.X - _screenStartMouse.X;
                var dy = winPt.Y - _screenStartMouse.Y;
                var zoom = _zoomSvc?.Zoom ?? 1.0f;
                var newPan = new PointF(
                    _panStartValue.X + (float)(dx),
                    _panStartValue.Y + (float)(dy)
                );
                if (_zoomSvc != null)
                    _zoomSvc.Pan = newPan;
            }
        }

        private void OnWorldMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                World.ReleaseMouseCapture();
                if (_screenStartMouse == e.GetPosition(null))
                {
                    // 마우스가 움직이지 않았다면 배경 클릭으로 간주
                    (DataContext as DocumentVM)?.ExitEditModeCommand.Execute(null);
                }
            }
        }

        // 노드/에지가 아닌 영역이면 true 반환
        private bool IsOnBackground(Point p)
        {
            // HitTestService 활용해서 노드/에지 위가 아니면 true
            var pt = new PointF((float)p.X, (float)p.Y);
            return _hitSvc.HitNode(pt) == null;
        }
        public System.Drawing.Point ViewToWorld(Point viewPoint)
        {
            // XAML에서 이름을 부여한 Transform을 직접 참조
            double zoom = ZoomTransform.ScaleX;
            double panX = PanTransform.X;
            double panY = PanTransform.Y;

            // 변환: (화면좌표 - 팬) / 줌 = 월드좌표
            double worldX = (viewPoint.X - panX) / zoom;
            double worldY = (viewPoint.Y - panY) / zoom;
            return new System.Drawing.Point((int)worldX, (int)worldY);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.Space)
            //{
            //    (DataContext as DocumentVM)?.EnterEditModeCommand.Execute(null);
            //    e.Handled = true;
            //}
            //else if (e.Key == Key.Escape)
            //{
            //    (DataContext as DocumentVM)?.ExitEditModeCommand.Execute(null);
            //    e.Handled = true;
            //}
            if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                // 1. 현재 윈도우의 클라이언트 영역 중앙(화면 좌표) 구하기
                double winWidth = ActualWidth;
                double winHeight = ActualHeight;

                // Border, Margin 등이 있으면 World(Canvas)의 실제 화면 내 위치 계산 필요
                // VisualTreeHelper를 활용한 변환
                var relativeToWorld = World.TransformToAncestor(this)
                    .Transform(new Point(0, 0));
                double cx = relativeToWorld.X + World.ActualWidth / 2;
                double cy = relativeToWorld.Y + World.ActualHeight / 2;

                // 실제 화면 중앙좌표를 구한다면:
                // var centerScreen = new Point(winWidth / 2, winHeight / 2);

                // 여기서는 "화면 중앙" 기준. 스크롤이나 윈도우 사이즈에 따라 다르게 조정 가능
                var center = new Point(winWidth / 2, winHeight / 2);

                // 2. 화면 중앙을 월드좌표로 변환
                var worldPos = ViewToWorld(center);

                // 3. ViewModel에 명령 전달
                if (DataContext is DocumentVM vm)
                {
                    if (vm.AddRootNodeAtCommand.CanExecute(worldPos))
                    {
                        vm.AddRootNodeAtCommand.Execute(worldPos);
                        e.Handled = true;
                    }
                }
            }
        }
    }

}