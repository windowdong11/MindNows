// Views/NodeControl.xaml.cs
using MindMap.Services;
using MindMap.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MindMap.Views
{
    public partial class NodeControl : UserControl
    {
        private bool _isDragging = false;
        private Point _clickPosition;

        public NodeControl()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += NodeControl_MouseLeftButtonDown;
            this.MouseMove += NodeControl_MouseMove;
            this.MouseLeftButtonUp += NodeControl_MouseLeftButtonUp;


            this.Loaded += OnLoaded;
            this.SizeChanged += OnSizeChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateSize();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateSize();
        }

        private void UpdateSize()
        {
            if (DataContext is MindMapNodeViewModel vm)
            {
                var newSize = new Size(this.ActualWidth, this.ActualHeight);
                if (vm.Size != newSize)
                    vm.Size = newSize;
            }
        }


        private void NodeControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _clickPosition = e.GetPosition(this);
            this.CaptureMouse();

            // MVVM 방식으로 명령 호출
            if (this.DataContext is MindMapNodeViewModel vm)
            {
                if (vm.SelectCommand.CanExecute(null))
                    vm.SelectCommand.Execute(null);
            }
        }

        private void NodeControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && this.Parent is Canvas canvas)
            {
                Point currentPosition = e.GetPosition(canvas);
                double newX = currentPosition.X - _clickPosition.X;
                double newY = currentPosition.Y - _clickPosition.Y;

                Canvas.SetLeft(this, newX);
                Canvas.SetTop(this, newY);

                // Move한 위치를 Node 데이터에도 반영
                if (this.DataContext is Models.MindMapNode node)
                {
                    node.Position = new Point(newX, newY);
                }
            }
        }

        private void NodeControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            this.ReleaseMouseCapture();
        }
    }
}