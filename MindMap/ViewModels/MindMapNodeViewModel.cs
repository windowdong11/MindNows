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
        static double defaultWidth = 60;
        public MindMapNode Model { get; }

        public double ImagePadding => 20;

        private double MinImageWidth => 10;
        private double MinNodeWidth => 20;

        public ICommand ResizeNodeCommand => new RelayCommand(param =>
        {
            if (param is Vector delta)
            {
                Width = Math.Max(MinNodeWidth, Width + delta.X);

                // 이미지보다 작아지면 이미지 축소
                if (ImageWidth + ImagePadding > Width)
                {
                    ImageWidth = Width - ImagePadding;
                }
            }
        });


        public ICommand ResizeImageRightCommand => new RelayCommand(param =>
        {
            if (param is Vector delta)
            {
                ImageWidth = Math.Max(MinImageWidth, ImageWidth + delta.X);
                SyncNodeWidthWithImage();
            }
        });

        public ICommand ResizeImageLeftCommand => new RelayCommand(param =>
        {
            if (param is Vector delta)
            {
                ImageWidth = Math.Max(MinImageWidth, ImageWidth - delta.X);
                SyncNodeWidthWithImage();
            }
        });

        private void SyncNodeWidthWithImage()
        {
            const double Padding = 20;
            double requiredNodeWidth = ImageWidth + Padding;

            if (requiredNodeWidth > Width)
                Width = requiredNodeWidth;
        }

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

        private double _imageWidth;
        public double ImageWidth
        {
            get => _imageWidth;
            set
            {
                if (_imageWidth != value)
                {
                    _imageWidth = value;
                    OnPropertyChanged();
                    //OnPropertyChanged(nameof(ImageHeight));
                }
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
                    if (_isFocused == false && IsImageSelected == true)
                        IsImageSelected = false;
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
                    if (value == false && IsImageSelected)
                        IsImageSelected = false;
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

        public ICommand ToggleSelectImageAreaCommand { get; }
        public ICommand RemoveImageCommand { get; }
        public ICommand PasteImageCommand { get; }


        private bool _isImageSelected;
        public bool IsImageSelected
        {
            get => _isImageSelected;
            set
            {
                if (_isImageSelected != value)
                {
                    _isImageSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        private void PasteImageFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    var imageFile = files.Cast<string>().FirstOrDefault(f =>
                        f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase));

                    if (imageFile != null)
                    {
                        ImagePath = imageFile;
                        IsImageSelected = true;
                        return;
                    }
                }

                if (Clipboard.ContainsImage())
                {
                    var image = Clipboard.GetImage();
                    if (image != null)
                    {
                        // 파일 경로로 저장
                        var tempPath = System.IO.Path.Combine(
                            System.IO.Path.GetTempPath(),
                            $"pasted_{Guid.NewGuid()}.png");

                        using (var fileStream = new System.IO.FileStream(tempPath, System.IO.FileMode.Create))
                        {
                            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
                            encoder.Save(fileStream);
                        }

                        ImagePath = tempPath;
                        IsImageSelected = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("붙여넣기 실패: " + ex.Message);
            }
        }

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
            ToggleSelectImageAreaCommand = new RelayCommand(_ =>
            {
                IsImageSelected = !IsImageSelected;
            });
            RemoveImageCommand = new RelayCommand(_ =>
            {
                ImagePath = null;
                IsImageSelected = false;
                //if (IsImageSelected && !string.IsNullOrEmpty(ImagePath))
                //{
                //}
            }, _ => IsImageSelected && !string.IsNullOrEmpty(ImagePath));
            PasteImageCommand = new RelayCommand(_ => PasteImageFromClipboard(), _=> IsSelected);
        }
    }
}
