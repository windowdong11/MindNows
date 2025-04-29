using MindMap.Services;
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
        }

        private void NodeControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _clickPosition = e.GetPosition(this);
            this.CaptureMouse();

            // 노드 선택
            if (this.DataContext is Models.MindMapNode node)
            {
                SelectionService.Select(node);
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

                // TODO: 가지 업데이트 호출 필요
            }
        }

        private void NodeControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            this.ReleaseMouseCapture();
        }
    }
}