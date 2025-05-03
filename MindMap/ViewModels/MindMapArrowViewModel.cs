using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MindMap.ViewModels
{
    internal class MindMapArrowViewModel : INotifyPropertyChanged
    {
        public MindMapNodeViewModel From { get; }
        public MindMapNodeViewModel To { get; }

        public Point StartPoint => new(
            From.Position.X + From.Size.Width / 2,
            From.Position.Y + From.Size.Height / 2);

        public Point EndPoint => new(
            To.Position.X + To.Size.Width / 2,
            To.Position.Y + To.Size.Height / 2);

        public bool IsBidirectional { get; set; }

        public MindMapArrowViewModel(MindMapNodeViewModel from, MindMapNodeViewModel to)
        {
            From = from;
            To = to;

            // From 또는 To 위치가 바뀌면 알림 발생
            From.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(From.Position) || e.PropertyName == nameof(From.Size))
                    OnPropertyChanged(nameof(StartPoint));
            };
            To.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(To.Position) || e.PropertyName == nameof(To.Size))
                    OnPropertyChanged(nameof(EndPoint));
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
