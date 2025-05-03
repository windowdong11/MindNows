// Views/MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MindMap.Models;
using MindMap.Services;
using MindMap.ViewModels;
using MindMap.Views;

namespace MindMap
{
    public partial class MainWindow : Window
    {
        private MindMapViewModel _viewModel;
        private readonly IMindMapLayoutEngine _layoutEngine = new TopDownTreeLayoutEngine();

        public MainWindow()
        {
            InitializeComponent();

            var root = new MindMapNode
            {
                Text = "Root",
                Position = new Point(300, 200)
            };

            var document = new MindMapDocument(root);
            var child = new MindMapNode { Text = "Child", Position = new Point(500, 150) };
            child.Parent = root;
            document.AddNode(child);
            _viewModel = new MindMapViewModel(document);

            this.DataContext = _viewModel;
            _viewModel.LayoutRequested += RefreshLayout;
            RefreshLayout();
        }

        void RefreshLayout()
        {
            _layoutEngine.ComputeLayout(_viewModel);
        }
    }
}
