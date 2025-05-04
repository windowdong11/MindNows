using MindMap.Behavior;
using MindMap.Common;
using MindMap.Models;
using MindMap.Services;
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
    internal class MindMapNodeViewModel : INotifyPropertyChanged, IPosition
    {
        public MindMapNode Model { get; }

        public ICommand SelectImageCommand { get; }

        private void SelectImage()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                ImagePath = dialog.FileName;
            }
        }

        public double Width
        {
            get => Model.Size.Width;
            set
            {
                if (Model.Size.Width != value)
                {
                    Model.Size = new Size(value, Model.Size.Height);
                    OnPropertyChanged();
                    RootViewModel.RequestLayout();
                }
            }
        }

        public double Height
        {
            get => Model.Size.Height;
            set
            {
                if (Model.Size.Height != value)
                {
                    Model.Size = new Size(Model.Size.Width, value);
                    OnPropertyChanged();
                }
            }
        }

        public BoundingArea _boundingArea;

        public BoundingArea BoundingArea
        {
            get => _boundingArea;
            set
            {
                if (_boundingArea != value)
                {
                    _boundingArea = value;
                    OnPropertyChanged();
                }
            }
        }



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
        public double X
        {
            get => Position.X; set
            {
                if (Position.X != value)
                {
                    Position = new Point(value, Position.Y);
                    OnPropertyChanged();
                }
            }
        }
        public double Y
        {
            get => Position.Y;
            set
            {
                if (Position.Y != value)
                {
                    Position = new Point(Position.X, value);
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
            SelectImageCommand = new RelayCommand(_ => SelectImage());
        }
    }
}
