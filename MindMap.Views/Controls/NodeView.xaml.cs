using MindMap.Core.Models;
using MindMap.ViewModels;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MindMap.Views.Controls
{
    /// <summary>
    /// NodeView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class NodeView : UserControl
    {
        public NodeView()
        {
            InitializeComponent();
        }

        private void EditBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.IsVisible)
            {
                // Dispatcher로 다음 렌더 주기에 Focus 요청
                tb.Dispatcher.BeginInvoke(new Action(() =>
                {
                    tb.Focus();
                    //tb.SelectAll(); // 선택 전체 (선택사항)
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void Thumb_DragDelta_Left(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is NodeVM vm)
            {
                var delta = -e.HorizontalChange; // 왼쪽으로 늘이면 음수
                var newWidth = Math.Max(20, vm.ImageSize.Width + delta);
                vm.ResizeImageCommand.Execute(newWidth);
            }
        }


        private void Thumb_DragDelta_Right(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is NodeVM vm)
            {
                var delta = e.HorizontalChange; // 오른쪽으로 늘이면 양수
                var newWidth = Math.Max(20, vm.ImageSize.Width + delta);
                vm.ResizeImageCommand.Execute(newWidth);
            }
        }
        private void NodeSizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is NodeVM vm)
            {
                // Side에 따라 delta 방향 반전
                //double delta = vm.Model.Side == SideEnum.Right ? e.HorizontalChange : -e.HorizontalChange;
                double delta = e.HorizontalChange;
                Debug.WriteLine($"[Thumb_DragDelta : before] vm.NodeWidth: {vm.NodeWidth}, Model.NodeWidth: {vm.Model.NodeWidth}, delta: {delta}");
                vm.ResizeNodeWidthCommand.Execute(delta);
                Debug.WriteLine($"[Thumb_DragDelta : after] vm.NodeWidth: {vm.NodeWidth}, Model.NodeWidth: {vm.Model.NodeWidth}, delta: {delta}");
            }
        }

        private void NodeSizeThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (DataContext is NodeVM vm)
            {
                // Resize 완료되었다고 DocumentVM으로 알림
                Debug.WriteLine("[DragCompleted : before]");
                vm.NotifyResizeCompleted();
                Debug.WriteLine("[DragCompleted : after]");
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && DataContext is NodeVM vm)
            {
                //Debug.WriteLine($"Size : {tb.ActualWidth} {tb.ActualHeight}");
                // border까지 여백 포함한 크기 업데이트
                var formatted = new FormattedText(
                    tb.Text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
                    tb.FontSize,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(tb).PixelsPerDip
                );
                double width = formatted.Width;
                double height = formatted.Height;
                var newSize = new Size(width, height);
                Debug.WriteLine($"Size: {newSize}");
                vm.UpdateNodeTextSize(newSize.Width, newSize.Height);
                e.Handled = true;
            }
        }
    }
}
