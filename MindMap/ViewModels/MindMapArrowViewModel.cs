using System.ComponentModel;
using System.Windows;

namespace MindMap.ViewModels
{
    internal class MindMapArrowViewModel : INotifyPropertyChanged
    {
        public MindMapNodeViewModel From { get; }
        public MindMapNodeViewModel To { get; }

        public Point Start => From.Position;
        public Point End => To.Position;

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
