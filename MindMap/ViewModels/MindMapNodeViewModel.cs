using MindMap.Common;
using MindMap.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MindMap.ViewModels
{
    internal class MindMapNodeViewModel : INotifyPropertyChanged
    {
        public MindMapNode Model { get; }

        private bool _isFocused;
        public bool IsFocused
        {
            get => _isFocused;
            set
            {
                if (_isFocused != value)
                {
                    _isFocused = value;
                    OnPropertyChanged(nameof(IsFocused));
                }
            }
        }
        public Guid Id => Model.Id;

        public string Text
        {
            get => Model.Text;
            set
            {
                if (Model.Text != value)
                {
                    Model.Text = value;
                    OnPropertyChanged();
                }
            }
        }

        public string? ImagePath
        {
            get => Model.ImagePath;
            set
            {
                if (Model.ImagePath != value)
                {
                    Model.ImagePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public Point Position
        {
            get => Model.Position;
            set
            {
                if (Model.Position != value)
                {
                    Model.Position = value;
                    OnPropertyChanged();
                }
            }
        }

        public Size Size
        {
            get => Model.Size;
            set
            {
                if (Model.Size != value)
                {
                    Model.Size = value;
                    OnPropertyChanged();
                    RootViewModel.RequestLayout();
                }
            }
        }

        public bool IsLeftSide
        {
            get => Model.IsLeftSide;
            set
            {
                if (Model.IsLeftSide != value)
                {
                    Model.IsLeftSide = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => Model.IsSelected;
            set
            {
                if (Model.IsSelected != value)
                {
                    Model.IsSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public MindMapNodeViewModel? Parent { get; set; }
        public List<MindMapNodeViewModel> Children { get; } = new();

        //public MindMapNodeViewModel(MindMapNode model)
        //{
        //    Model = model;
        //}

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ICommand SelectCommand { get; }

        public MindMapViewModel RootViewModel { get; }

        public MindMapNodeViewModel(MindMapNode model, MindMapViewModel root)
        {
            Model = model;
            Debug.WriteLine($"{model.Text} {model.Position}");
            RootViewModel = root;

            SelectCommand = new RelayCommand(_ => RootViewModel.SelectedNode = this);
        }
    }
}
