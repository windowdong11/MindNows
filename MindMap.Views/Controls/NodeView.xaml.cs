using System.Windows;
using System.Windows.Controls;

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
    }
}
