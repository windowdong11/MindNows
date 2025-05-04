// Views/MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        //private readonly IMindMapLayoutEngine _layoutEngine = new TopDownTreeLayoutEngine();
        private readonly ILayoutService _layoutEngine = new LayoutService();
        private readonly MindMapDocument document;

        public MainWindow()
        {
            InitializeComponent();

            var root = new MindMapNode
            {
                Text = "Root",
                Position = new Point(300, 200)
            };

            document = new MindMapDocument(root);
            _viewModel = new MindMapViewModel(document, _layoutEngine);

            this.DataContext = _viewModel;
            Loaded += (_, _) =>
            {
                var focused = Keyboard.FocusedElement;
                Debug.WriteLine($"초기 포커스: {focused}");
            };
            _viewModel.LayoutRequested += RefreshLayout;
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            RefreshLayout();
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var focused = Keyboard.FocusedElement;
            string info = focused != null
                ? $"{focused.GetType().Name} ({focused})"
                : "null (포커스 없음)";
            Console.WriteLine($"[{e.Key}] 키 입력 - 현재 포커스 대상: {info}");
            var scope = FocusManager.GetFocusScope(GetWindow(this));
            var focusedScope = FocusManager.GetFocusedElement(scope);
            Console.WriteLine($"포커스 스코프: {scope}");
            Console.WriteLine($"포커스된 스코프: {focusedScope}");
        }

        void RefreshLayout()
        {
            _layoutEngine.RecalculateLayout(_viewModel.RootNode);
        }
    }
}
