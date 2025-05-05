// Views/NodeControl.xaml.cs
using MindMap.Services;
using MindMap.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MindMap.Views
{
    public partial class NodeControl : UserControl
    {
        public NodeControl()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += NodeControl_MouseLeftButtonDown;
            //this.MouseMove += NodeControl_MouseMove;
            //this.MouseLeftButtonUp += NodeControl_MouseLeftButtonUp;


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
            //_isDragging = true;
            //_clickPosition = e.GetPosition(this);
            //this.CaptureMouse();

            // MVVM 방식으로 명령 호출
            if (this.DataContext is MindMapNodeViewModel vm)
            {
                if (vm.SelectCommand.CanExecute(null))
                    vm.SelectCommand.Execute(null);
            }
        }

        private void NodeResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is MindMapNodeViewModel vm)
            {
                double newWidth = vm.Width + e.HorizontalChange;

                const double MinWidth = 50;
                const double MaxWidth = 600;
                Debug.WriteLine($"Width: {newWidth}");

                vm.Width = Math.Max(MinWidth, Math.Min(MaxWidth, newWidth));
            }
        }

        private void LeftImageThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is MindMapNodeViewModel vm)
            {
                double newWidth = vm.ImageWidth - e.HorizontalChange;
                vm.ImageWidth = Math.Max(20, newWidth);
            }
        }

        private void RightImageThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is MindMapNodeViewModel vm)
            {
                double newWidth = vm.ImageWidth + e.HorizontalChange;
                vm.ImageWidth = Math.Max(20, newWidth);
            }
        }
    }
}