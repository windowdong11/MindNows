﻿// Views/MainWindow.xaml.cs
using MindMap.Models;
using MindMap.Repositiory;
using MindMap.Services;
using MindMap.ViewModels;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MindMap
{
    public partial class MainWindow : Window
    {
        private MindMapViewModel _viewModel;
        //private readonly IMindMapLayoutEngine _layoutEngine = new TopDownTreeLayoutEngine();
        private readonly ILayoutService _layoutEngine = new LayoutService();
        private readonly MindMapDocument document;
        //private MindMapPersistenceService _persistenceService = new MindMapPersistenceService();

        public MainWindow()
        {
            InitializeComponent();

            //var (loadedDocument, loadedViewState) = _persistenceService.Load("map.json");
            //document = loadedDocument;
            //_persistenceService.ApplyViewState(_viewModel, loadedViewState);

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
            //_viewModel.LayoutRequested += RefreshLayout;
            //PreviewKeyDown += MainWindow_PreviewKeyDown;
            //RefreshLayout();
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

        //void RefreshLayout()
        //{
        //    _layoutEngine.RecalculateLayout(_viewModel.RootNode);
        //}
    }
}
