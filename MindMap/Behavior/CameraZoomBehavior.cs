using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using MindMap.ViewModels;

namespace MindMap.Behavior
{
    internal class CanvasZoomBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(MindMapViewModel), typeof(CanvasZoomBehavior));

        public MindMapViewModel ViewModel
        {
            get => (MindMapViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel == null) return;

            var position = e.GetPosition(AssociatedObject);

            double scaleFactor = e.Delta > 0 ? 1.1 : 0.9;
            double oldScale = ViewModel.Scale;
            double newScale = Math.Max(0.1, Math.Min(3.0, oldScale * scaleFactor));

            // 기준점 보정 계산
            double scaleChange = newScale / oldScale;

            ViewModel.OffsetX = position.X - scaleChange * (position.X - ViewModel.OffsetX);
            ViewModel.OffsetY = position.Y - scaleChange * (position.Y - ViewModel.OffsetY);
            ViewModel.Scale = newScale;

            e.Handled = true;
        }
    }
}
