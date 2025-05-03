// Models/MindMapNode.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MindMap.Models
{
    public class MindMapNode : INotifyPropertyChanged
    {
        public Guid Id { get; } = Guid.NewGuid();   // 내부 식별자
        public string Text { get; set; }
        public string? ImagePath { get; set; }      // 이미지 파일 경로 또는 null
        public Point Position { get; set; }         // 화면 좌표
        public Size Size { get; set; }              // 노드 크기 (이미지 비율 유지 고려)

        public MindMapNode? Parent { get; set; }
        public List<MindMapNode> Children { get; } = new();

        public bool IsLeftSide { get; set; }        // 최상위 자식 분기 여부

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
