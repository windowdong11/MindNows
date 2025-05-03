//Views/BranchControl.xaml.cs
using System.Windows;
using System.Windows.Controls;

namespace MindMap.Views
{
    public partial class BranchControl : UserControl
    {
        public BranchControl()
        {
            InitializeComponent();
            DataContext = this;
        }

        public Point StartPoint
        {
            get { return (Point)GetValue(StartPointProperty); }
            set { SetValue(StartPointProperty, value); }
        }

        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register(nameof(StartPoint), typeof(Point), typeof(BranchControl), new PropertyMetadata(default(Point)));

        public Point EndPoint
        {
            get { return (Point)GetValue(EndPointProperty); }
            set { SetValue(EndPointProperty, value); }
        }

        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register(nameof(EndPoint), typeof(Point), typeof(BranchControl), new PropertyMetadata(default(Point)));
    }
}
