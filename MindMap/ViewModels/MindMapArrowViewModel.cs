using System.ComponentModel;
using System.Windows;

namespace MindMap.ViewModels
{
    internal class MindMapArrowViewModel : INotifyPropertyChanged
    {
        public MindMapNodeViewModel From { get; }
        public MindMapNodeViewModel To { get; }

        public Point Start => new Point(From.Position.X + From.Width - 8, From.Position.Y + From.Height / 2.0);
        public Point End => new Point(To.Position.X + 8, To.Position.Y + To.Height / 2.0);

        public MindMapArrowViewModel(MindMapNodeViewModel from, MindMapNodeViewModel to)
        {
            From = from;
            To = to;

            // 연결된 노드가 이동하면 Start, End 변경 알림
            From.PropertyChanged += OnNodePropertyChanged;
            To.PropertyChanged += OnNodePropertyChanged;
        }

        private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MindMapNodeViewModel.Position))
            {
                OnPropertyChanged(nameof(Start));
                OnPropertyChanged(nameof(End));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
