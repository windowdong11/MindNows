using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MindMap.Core.Models;
using MindMap.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.ViewModels;

public partial class DocumentVM : ObservableObject
{
    public ObservableCollection<NodeVM> Roots { get; }
    public ObservableCollection<NodeVM> AllNodes { get; }

    // 의존 서비스(나중 단계에서 주입)
    public DocumentVM(ISelectionService selSvc, ILayoutService laySvc /* DI 주입 */ )
    {
        // ───── 샘플 트리 ─────
        var root = new NodeModel(Guid.NewGuid(), "Root");
        var childA = new NodeModel(Guid.NewGuid(), "Child A");
        var childC = new NodeModel(Guid.NewGuid(), "Child C");
        var childE = new NodeModel(Guid.NewGuid(), "Child E");
        childA.Children.Add(childC);
        childA.Children.Add(childE);
        root.Children.Add(childA);
        root.Children.Add(new NodeModel(Guid.NewGuid(), "Child B"));
        root.Children.Add(new NodeModel(Guid.NewGuid(), "Child D"));


        // 초기화
        _sel = selSvc;
        Roots = new ObservableCollection<NodeVM> { new(root, selSvc) };
        BuildParentMap(root, null); // 주행성 보조

        // 플랫 컬렉션 초기화
        AllNodes = new ObservableCollection<NodeVM>();
        Flatten(Roots.First());

        void Flatten(NodeVM vm)
        {
            AllNodes.Add(vm);
            foreach (var c in vm.Children) Flatten(c);
        }

        // 루트 선택 기본값
        _sel.Select(Roots.First().Model);
        _lay = laySvc;
        _lay.Arrange(root);

        // Key 명령 래퍼
        NavigateCommand = new RelayCommand<Direction?>(dir =>
        {
            if (dir is not null) _sel.Navigate(dir.Value);
        });
        ExpandSelectionCommand = new RelayCommand<Direction?>(d =>
        {
            if (d is Direction.Up or Direction.Down)
                _sel.ExpandRange(d.Value);
        });
    }
    void BuildParentMap(NodeModel node, NodeModel? parent)
    {
        _sel.RegisterParent(node, parent);
        foreach (var child in node.Children)
            BuildParentMap(child, node);
    }

    public void RefreshLayout()
    {
        foreach (var r in Roots)
            _lay.Arrange(r.Model);
    }

    public IRelayCommand<Direction?> NavigateCommand { get; }
    public IRelayCommand<Direction?> ExpandSelectionCommand { get; }
    private readonly ISelectionService _sel;
    private readonly ILayoutService _lay;
}