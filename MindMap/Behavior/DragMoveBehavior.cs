using Microsoft.Xaml.Behaviors;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MindMap.Behavior
{
    internal class DragMoveBehavior : Behavior<FrameworkElement>
    {
        #region --- TargetPanel (필수, 변경 불가) ----------------------------
        public Panel TargetPanel
        {
            get => (Panel)GetValue(TargetPanelProperty);
            set => SetValue(TargetPanelProperty, value);
        }

        public static readonly DependencyProperty TargetPanelProperty =
            DependencyProperty.Register(
                nameof(TargetPanel),
                typeof(Panel),
                typeof(DragMoveBehavior),
                new PropertyMetadata(null, OnTargetPanelChanged));

        private static void OnTargetPanelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                // 두 번째 변경 시도 → 되돌리고 경고
                d.SetValue(TargetPanelProperty, e.OldValue);
                Debug.WriteLine("[DragMoveBehavior] TargetPanel은 초기화 후 변경할 수 없습니다.");
            }
        }
        #endregion -----------------------------------------------------------

        private Point _startMousePos;
        private Vector _startOffset;
        private bool _isDragging;
        private Panel _panel;

        public static readonly DependencyProperty OnDragCompletedCommandProperty =
            DependencyProperty.Register(nameof(OnDragCompletedCommand), typeof(ICommand), typeof(DragMoveBehavior), new PropertyMetadata(null));

        public ICommand? OnDragCompletedCommand
        {
            get => (ICommand?)GetValue(OnDragCompletedCommandProperty);
            set => SetValue(OnDragCompletedCommandProperty, value);
        }

        //[MemberNotNull(nameof(_panel))]
        protected override void OnAttached()
        {
            if (TargetPanel == null)
                throw new InvalidOperationException(
                    "DragMoveBehavior: TargetPanel 속성은 XAML에서 반드시 지정해야 합니다.");
            _panel = TargetPanel;
            AssociatedObject.MouseLeftButtonDown += OnMouseDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseLeftButtonUp += OnMouseUp;
            AssociatedObject.MouseLeave += OnMouseUp;
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonDown -= OnMouseDown;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.MouseLeftButtonUp -= OnMouseUp;
            AssociatedObject.MouseLeave -= OnMouseUp;
            base.OnDetaching();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.DataContext is IPosition vm)
            {
                _startMousePos = e.GetPosition(_panel);
                _startOffset = new Point(vm.X, vm.Y) - _startMousePos;
                _isDragging = true;
                AssociatedObject.CaptureMouse();
                e.Handled = true;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;


            if (AssociatedObject.DataContext is IPosition vm)
            {
                var pos = e.GetPosition(_panel);
                vm.X = pos.X + _startOffset.X;
                vm.Y = pos.Y + _startOffset.Y;
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            AssociatedObject.ReleaseMouseCapture();
            if (OnDragCompletedCommand?.CanExecute(null) == true)
                OnDragCompletedCommand.Execute(null);
        }
    }

    /// 간단한 좌표 인터페이스 (ViewModel 에 재사용)
    public interface IPosition
    {
        double X { get; set; }
        double Y { get; set; }

        event PropertyChangedEventHandler PropertyChanged;
    }
}
