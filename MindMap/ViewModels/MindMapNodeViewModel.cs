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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace MindMap.ViewModels
{
    internal class MindMapNodeViewModel : INotifyPropertyChanged, IPosition
    {
        public MindMapNode Model { get; }

        //private MindMapNodeViewModel? _parent;

        public MindMapNodeViewModel? Parent {
            get
            {
                if (Model.Parent == null)
                    return null;
                return RootViewModel.Nodes
                    .First(n => n.Id == Model.Parent.Id);
            }
            set
            {
                if (value == null)
                    Model.Parent = null;
                else
                    Model.Parent = value.Model;
            }
        }

        public List<MindMapNodeViewModel> LeftChildren = new();
        public List<MindMapNodeViewModel> RightChildren = new();
        public bool IsRoot => Model.IsRoot; // 최상위 노드 여부

        //public List<MindMapNodeViewModel> Children { get; set; }

        public List<MindMapNodeViewModel>? Siblings
        {
            get
            {
                if (IsRoot)
                {
                    return null;
                }
                if (IsLeftSide)
                {
                    return Parent.LeftChildren;
                }
                else
                {
                    return Parent.RightChildren;
                }
            }
        }

        public void AddChild(MindMapNodeViewModel child)
        {
            if (IsRoot)
            {
                if(LeftChildren.Count > 0)
                {
                    LeftChildren.Add(child);
                    Model.LeftChildren.Add(child.Model);
                }
                else
                {
                    RightChildren.Add(child);
                    Model.RightChildren.Add(child.Model);
                }
            }
            else if (child.IsLeftSide)
            {
                LeftChildren.Add(child);
                Model.LeftChildren.Add(child.Model);
            }
            else
            {
                RightChildren.Add(child);
                Model.RightChildren.Add(child.Model);
            }
        }

        public void AddChildAt(MindMapNodeViewModel child, int idx)
        {
            if (IsRoot)
            {
                if (child.IsLeftSide)
                {
                    LeftChildren.Insert(idx, child);
                    Model.LeftChildren.Insert(idx, child.Model);
                }
                else
                {
                    RightChildren.Insert(idx, child);
                    Model.RightChildren.Insert(idx, child.Model);
                }
            }
            else if (child.IsLeftSide)
            {
                LeftChildren.Insert(idx, child);
                Model.LeftChildren.Insert(idx, child.Model);
            }
            else
            {
                RightChildren.Insert(idx, child);
                Model.RightChildren.Insert(idx, child.Model);
            }
        }

        public void RemoveChild(MindMapNodeViewModel child)
        {
            if (child.IsLeftSide)
            {
                // 하위 트리 제거
                var grandChildren = child.LeftChildren.ToList();
                foreach (var grandChild in grandChildren)
                {
                    child.RemoveChild(grandChild);
                }
                child.LeftChildren.Clear();
                // 자식 제거
                Model.RemoveChild(child.Model);
                LeftChildren.Remove(child);
            }
            else
            {
                // 하위 트리 제거
                var grandChildren = child.RightChildren.ToList();
                foreach (var grandChild in grandChildren)
                {
                    child.RemoveChild(grandChild);
                }
                child.RightChildren.Clear();
                // 자식 제거
                Model.RemoveChild(child.Model);
                RightChildren.Remove(child);
            }
        }

        public void DetachChild(MindMapNodeViewModel child)
        {
            if (child.IsLeftSide)
            {
                LeftChildren.Remove(child);
                Model.DetachChild(child.Model);
            }
            else
            {
                RightChildren.Remove(child);
                Model.DetachChild(child.Model);
            }
        }

        public void MoveToParentYongerSibling()
        {
            if (Parent == null) throw new InvalidOperationException("Parent is null.");
            if (Parent.Parent == null) throw new InvalidOperationException("Grandparent is null.");
            var grandParent = Parent.Parent;
            var siblings = Parent.IsLeftSide
                ? Parent.Parent.LeftChildren
                : Parent.Parent.RightChildren;
            var index = siblings.IndexOf(Parent);
            if (index == -1)
                throw new InvalidOperationException("Parent is not found in siblings.");
            Parent.DetachChild(this);
            siblings.Insert(index + 1, this);
            grandParent.Model.AddChildAt(Model, index + 1);
        }

        /* 형의 마지막 자식 노드로 이동 */
        public bool MoveToElderSiblingLastChild()
        {
            if (Parent == null) throw new InvalidOperationException("Root cannot move to elder sibling's child.");

            var siblings = IsLeftSide ? Parent.LeftChildren : Parent.RightChildren;
            var index = siblings.IndexOf(this);
            if (index == -1)
                throw new InvalidOperationException("Node is not found in siblings.");
            if (index == 0)
                return false;
            var elderSibling = siblings[index - 1];
            if (IsLeftSide)
                elderSibling.LeftChildren.Add(this);
            else
                elderSibling.RightChildren.Add(this);
            Parent.DetachChild(this);
            elderSibling.Model.AddChild(Model);
            return true;
        }

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

        private bool HasImage = false;
        public string? ImagePath
        {
            get => Model.ImagePath;
            set
            {
                if (Model.ImagePath != value)
                {
                    HasImage = !string.IsNullOrEmpty(value);
                    Model.ImagePath = value;
                    if (HasImage)
                    {
                        LoadImageMetadata(value); // 이미지 크기 불러오기
                    }
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

        private void LoadImageMetadata(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));

                // 강제로 초기화
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

                bitmap.Freeze(); // Freezable 객체로 메모리 안전하게

                var width = bitmap.PixelWidth;
                //var height = bitmap.PixelHeight;

                //AspectRatio = (double)width / height;

                ImageWidth = Math.Min(width, Width); // 최대 300까지 제한
                                                     // ImageHeight는 ImageWidth와 AspectRatio 기반으로 자동 계산되게 해둔 상태라면 OK
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"이미지 로드 실패: {ex.Message}");
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
            PasteImageCommand = new RelayCommand(_ => PasteImageFromClipboard(), _ => IsSelected);
        }
    }
}
