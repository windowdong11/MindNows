using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using MindMap.ViewModels;
using System.Diagnostics;
using System.Windows.Media;

namespace MindMap.Behavior
{
    internal class CanvasDragBehavior : Behavior<FrameworkElement>
    {
        private Point _lastPosition;
        private bool _isDragging;

        protected override void OnAttached()
        {
            AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DragLayer != null && !(AssociatedObject == e.OriginalSource || DragLayer == e.OriginalSource))
                return;
            _isDragging = true;
            _lastPosition = e.GetPosition(Application.Current.MainWindow);
            AssociatedObject.CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && AssociatedObject.DataContext is MindMapViewModel vm)
            {
                var currentPosition = e.GetPosition(Application.Current.MainWindow);
                var delta = currentPosition - _lastPosition;

                vm.OffsetX += delta.X;
                vm.OffsetY += delta.Y;

                _lastPosition = currentPosition;
                Debug.WriteLine(delta);
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                AssociatedObject.ReleaseMouseCapture();
            }
        }

        public static readonly DependencyProperty DragLayerProperty =
        DependencyProperty.Register(nameof(DragLayer), typeof(FrameworkElement), typeof(CanvasDragBehavior));

        public FrameworkElement DragLayer
        {
            get => (FrameworkElement)GetValue(DragLayerProperty);
            set => SetValue(DragLayerProperty, value);
        }

        private bool IsClickOnLayer(object source)
        {
            DependencyObject depObj = source as DependencyObject;
            while (depObj != null)
            {
                if (depObj == DragLayer)
                    return true; // 레이어 바깥이 아님
                depObj = VisualTreeHelper.GetParent(depObj);
            }
            return false; // 레이어 바깥 클릭
        }
    }
}
