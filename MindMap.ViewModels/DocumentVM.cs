using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MindMap.Core.Models;
using MindMap.Core.Services;
using MindMap.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;

namespace MindMap.ViewModels;

public enum SiblingMove
{
    Up,
    Down,
}

public partial class DocumentVM : ObservableObject
{
    public ObservableCollection<NodeVM> Roots { get; }
    public ObservableCollection<NodeVM> AllNodes { get; }
    public ObservableCollection<EdgeVM> Edges { get; } = new();

    public NodeVM? ArrowSourceNode { get; set; }
    public ObservableCollection<ArrowVM> Arrows { get; } = new();

    private ArrowVM? _selectedArrow;
    public ArrowVM? SelectedArrow
    {
        get => _selectedArrow;
        set => SetProperty(ref _selectedArrow, value);
    }

    // editing node property, and IsEditing
    private NodeVM? _editingNode;
    public bool IsEditing => EditingNode is not null;
    public NodeVM? EditingNode
    {
        get => _editingNode;
        set
        {
            if (SetProperty(ref _editingNode, value))
            {
                // 편집 모드가 설정되면 선택 해제
                if (value is not null) _sel.Select(_sel.Current);
            }
        }
    }

    // 의존 서비스(나중 단계에서 주입)
    public DocumentVM(ISelectionService selSvc, ILayoutService laySvc, INodeMutationService mutSvc, IHitTestService hitSvc, IClipboardService clipboardSvc, IFileDialogService fileSvc, IZoomPanService zoomPanService)
    {
        // ───── 샘플 트리 ─────
        var root = new NodeModel(Guid.NewGuid(), "루트");


        // 초기화
        _sel = selSvc;
        _lay = laySvc;
        _mut = mutSvc;
        _hit = hitSvc;
        _fileDialog = fileSvc;
        _clipboard = clipboardSvc;
        _zoomPanSvc = zoomPanService;
        Roots = new ObservableCollection<NodeVM> { new(root, selSvc) };
        BuildParentMap(root, null); // 주행성 보조

        // 플랫 컬렉션 초기화
        AllNodes = new ObservableCollection<NodeVM>();

        // 루트 선택 기본값
        _sel.Select(Roots.First().Model);
        //_lay.Arrange(root);
        BuildEdgesAndLayout();

        _sel.SelectionChanged += (_, _) =>
        {
            if (EditingNode != null && _sel.Current != EditingNode.Model)
                EditingNode = null;
            if (ArrowSourceNode != null && _sel.Current != ArrowSourceNode.Model)
            {
                if (!Arrows.Any(a => a.From == ArrowSourceNode && a.To.Model == _sel.Current))
                {
                    var arrowDestNode = AllNodes.FirstOrDefault(n => n.Model == _sel.Current);
                    if (arrowDestNode is null) throw new Exception("Arrow destination node not found in AllNodes.");
                    AddArrow(ArrowSourceNode, arrowDestNode);
                }

                ArrowSourceNode = null; // 모드 해제
            }
        };

        // Key 명령 래퍼
        NavigateCommand = new RelayCommand<Direction?>(dir =>
        {
            if (dir is not null) _sel.Navigate(dir.Value);
        }, _ => !IsEditing);
        ExpandSelectionCommand = new RelayCommand<Direction?>(d =>
        {
            if (d is Direction.Up or Direction.Down)
                _sel.ExpandRange(d.Value);
        }, _ => !IsEditing);
        MoveSiblingCommand = new RelayCommand<Direction>(direction =>
        {
            // 다중 선택 시, 방향에 따라 정렬 후 순차 이동
            var selected = _sel.Multi.OfType<NodeModel>().ToList();
            if (!selected.Any()) return;

            var parent = _sel.GetParent(selected.First());
            if (parent is null)
            {
                // 부모가 없는 경우(루트 노드 등) 이동 불가
                return;
            }
            var ordered = direction switch
            {
                Direction.Up => selected.OrderBy(n => parent.Children.IndexOf(n)),          // 위로 이동: 작은 인덱스부터
                Direction.Down => selected.OrderByDescending(n => parent.Children.IndexOf(n)), // 아래 이동: 큰 인덱스부터
                _ => throw new ArgumentException("Invalid direction for sibling move.", nameof(direction))
            };

            int delta = direction switch
            {
                Direction.Up => -1,
                Direction.Down => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), "Invalid direction for sibling move.")
            };
            bool changed = false;
            foreach (var n in ordered)
                changed |= _mut.MoveWithinSiblings(n, delta);

            if (changed)
            {
                BuildEdgesAndLayout();  // LayoutService.Arrange + Edge.Refresh
            }
        }, _ => !IsEditing);
        ReparentCommand = new RelayCommand<ReparentAction?>(moveOpt =>
        {
            if (moveOpt is not { } move) return;
            var selection = _sel.Multi.OfType<NodeModel>().ToList();
            if (selection.Count == 0) return;

            var parent = _sel.GetParent(selection.First());
            if (parent is null)
            {
                // 부모가 없는 경우(루트 노드 등) 이동 불가
                return;
            }

            var grandParent = _sel.GetParent(parent);
            var nodeSide = selection[0].Side;

            bool ConnectToElderSibling()
            {
                var ordered = selection.OrderBy(n => parent.Children.IndexOf(n));
                var idx = parent.Children.IndexOf(ordered.First());
                var elderSibling = parent.Children
                    .Take(idx)
                    .LastOrDefault(sibling => sibling.Side == ordered.First().Side);
                if (elderSibling is null) return false; // 같은 방향의 형제가 없는 경우 이동 불가
                var changed = false;
                foreach (var n in ordered)
                {
                    // 형제 노드로 이동
                    changed = _mut.Reparent(n, elderSibling);
                }
                return changed;
            }
            bool ConnectToGrandParent()
            {
                var ordered = selection.OrderBy(n => parent.Children.IndexOf(n));
                // 부모의 자식으로 이동
                var changed = false;
                var parentIdx = grandParent.Children.IndexOf(parent);
                var delta = -grandParent.Children
                    .Skip(parentIdx + 1)
                    .Count(sibling => sibling.Side == nodeSide);
                foreach (var n in ordered)
                {
                    changed |= _mut.Reparent(n, grandParent);
                    _mut.MoveWithinSiblings(n, delta);
                }
                // parentIdx부터 마지막까지 같은 방향에 있는 형제 노드의 수
                return changed;
            }
            bool MoveRootChildrenToSide(SideEnum side)
            {
                var ordered = selection.OrderBy(n => parent.Children.IndexOf(n));
                var changed = false;
                foreach (var n in ordered)
                {
                    //changed |= _mut.Reparent(n, parent); // 부모의 자식으로 이동
                    changed |= _mut.SetSide(n, side);
                }
                return changed;
            }

            bool changed = false;
            if (move == ReparentAction.Left)
            {
                if (nodeSide == SideEnum.Right)
                {
                    if (grandParent is null)
                        changed = MoveRootChildrenToSide(SideEnum.Left); // 루트 노드의 자식으로 이동
                    else 
                        changed = ConnectToGrandParent();
                }
                else if (nodeSide == SideEnum.Left)
                {
                    changed = ConnectToElderSibling();
                }
                else
                {
                    throw new InvalidOperationException("Invalid node side for left reparenting.");
                }
            }
            else
            {
                if (nodeSide == SideEnum.Right)
                    changed = ConnectToElderSibling();
                else if (nodeSide == SideEnum.Left)
                {
                   if (grandParent is null)
                        changed = MoveRootChildrenToSide(SideEnum.Right); // 루트 노드의 자식으로 이동
                    else
                        changed = ConnectToGrandParent();
                }
                else
                {
                    throw new InvalidOperationException("Invalid node side for right reparenting.");
                }
            }
            if (changed)
            {
                var lastSelectionId = _sel.Current.Id;
                var lastSelectionGroups = _sel.Multi.Select(n => n.Id).ToList();
                BuildEdgesAndLayout();
                var matchingGroups = lastSelectionGroups.Select(id => AllNodes.FirstOrDefault(n => n.Model.Id == id));
                foreach (var node in matchingGroups)
                {
                    if (node is null) continue;
                    _sel.AddSelection(node.Model);
                }
                var matchingNode = AllNodes.FirstOrDefault(n => n.Model.Id == lastSelectionId);
                _sel.AddSelection(matchingNode.Model);
            }
        }, _ => !IsEditing);
        // Edit Mode Commands
        EnterEditModeCommand = new RelayCommand(() =>
        {
            if (_sel.Current is NodeModel current)
            {
                EditingNode = AllNodes.FirstOrDefault(n => n.Model == current);
            }
        }, () => !IsEditing);
        ExitEditModeCommand = new RelayCommand(() =>
        {
            EditingNode = null; // 편집 모드 종료
        }, () => IsEditing);
        // Add Nodes
        AddChildCommand = new RelayCommand(() =>
        {
            var selNode = _sel.Current;
            if (selNode == null) return;

            // 새 모델 생성
            var newNode = new NodeModel(Guid.NewGuid(), "새 노드")
            {
                Side = selNode.Side
            };

            // 트리 연결
            selNode.Children.Add(newNode);
            _sel.RegisterParent(newNode, selNode);

            // 뷰 갱신
            BuildEdgesAndLayout();

            // 새 노드 선택
            _sel.Select(newNode);
        }, () => !IsEditing && _sel.Current != null);
        AddSiblingCommand = new RelayCommand(() =>
        {
            var selNode = _sel.Current;
            if (selNode == null) return;
            // 새 모델 생성
            var newNode = new NodeModel(Guid.NewGuid(), "새 형제 노드")
            {
                Side = selNode.Side
            };
            // 부모 노드 찾기
            var parent = _sel.GetParent(selNode);
            if (parent == null) return; // 루트 노드 등 부모가 없는 경우
            // 트리 연결
            parent.Children.Add(newNode);
            _sel.RegisterParent(newNode, parent);
            // 뷰 갱신
            BuildEdgesAndLayout();
            // 새 노드 선택
            _sel.Select(newNode);
        }, () => !IsEditing && _sel.Current != null);
        _mut.OnReparent += (node, newParent) =>
        {
            if (_sel.GetParent(node) != newParent)
            {
                var rootVM = AllNodes.FirstOrDefault(n => n.Model == node);
                if (rootVM == null) return; // 노드가 VM에 없으면 리턴
                Roots.Remove(rootVM);
            }
        };
    }

    private void NodeVM_ResizeCompleted(object? sender, EventArgs e)
    {
        BuildEdgesAndLayout(); // 드래그 완료 후 레이아웃 갱신
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


    public void BuildEdgesAndLayout()
    {
        Debug.WriteLine($"[BuildEdgesAndLayout] Called");
        //Edges.Clear();
        //AllNodes.Clear();

        
        List<NodeVM> newNodes = new();
        List<NodeVM> matchedNodes = new();
        List<EdgeVM> newEdges = new();
        List<EdgeVM> matchedEdges = new();
        Dictionary<NodeVM, NodeVM> chidToParentMap = new();
        foreach (var root in Roots)
        {
            void FlattenAndEdges(NodeVM vm)
            {
                //AllNodes.Add(vm);
                var matchedNode = AllNodes.FirstOrDefault(n => n == vm);
                if (matchedNode is null)
                    newNodes.Add(vm);
                else
                    matchedNodes.Add(vm);
                foreach (var c in vm.Children)
                {
                    var match = Edges.FirstOrDefault(e => e.Parent == vm && e.Child == c);
                    if (match is null)
                        newEdges.Add(new EdgeVM(vm, c));
                    else
                    {
                        matchedEdges.Add(match);
                    }
                    FlattenAndEdges(c);
                    chidToParentMap.Add(c, vm);
                }
            }
            FlattenAndEdges(root);
        }
        var notMatchedNodes = AllNodes.Except(matchedNodes).ToList();
        var notMatchedEdges = Edges.Except(matchedEdges).ToList();
        // 삭제된 노드와 엣지 제거
        foreach (var e in notMatchedEdges)
        {
            Edges.Remove(e);
        }
        foreach (var n in notMatchedNodes)
        {
            AllNodes.Remove(n);
            _sel.Remove(n.Model);
            // 선택된 노드가 삭제된 경우 선택 해제
        }
        // 새로 추가된 노드와 엣지 추가
        foreach (var e in newEdges) Edges.Add(e);
        foreach (var n in newNodes)
        {
            AllNodes.Add(n);
            var parentModel = chidToParentMap.ContainsKey(n) ? chidToParentMap[n].Model : null;
            _sel.RegisterParent(n.Model, parentModel);

        }

        foreach (var root in Roots)
        {

            // 1) 레이아웃
            _lay.Arrange(root.Model);
            // 2) Edge Path 계산 갱신
            foreach (var e in Edges) e.Refresh();
        }
        _hit.BuildIndex(Roots.Select(r => r.Model));
        foreach (var node in AllNodes)
        {
            node.ResizeCompleted -= NodeVM_ResizeCompleted; // 중복 방지
            node.ResizeCompleted += NodeVM_ResizeCompleted; // Resize 이벤트 핸들러 등록
        }
    }

    public void AddArrow(NodeVM from, NodeVM to)
    {
        //var model = new ArrowModel { From = from.Model, To = to.Model };
        var arrowVM = new ArrowVM(from, to);
        Arrows.Add(arrowVM);
        SelectedArrow = arrowVM;
    }
    public void RemoveArrow(ArrowVM arrow)
    {
        Arrows.Remove(arrow);
        if (SelectedArrow == arrow)
            SelectedArrow = null;
    }


    public IRelayCommand<Direction?> NavigateCommand { get; }
    public IRelayCommand<Direction?> ExpandSelectionCommand { get; }
    public IRelayCommand<Direction> MoveSiblingCommand { get; }
    public IRelayCommand<ReparentAction?> ReparentCommand { get; }
    // Edit Mode Commands
    public IRelayCommand EnterEditModeCommand { get; }
    public IRelayCommand ExitEditModeCommand { get; }
    // Add Node Commands
    public IRelayCommand AddChildCommand { get; }
    public IRelayCommand AddSiblingCommand { get; }
    public IRelayCommand EnterArrowModeCommand => new RelayCommand(() =>
    {
        if (_sel.Current is null) return; // 선택된 노드가 없으면 리턴
        // 화살표 모드 진입
        ArrowSourceNode = AllNodes.FirstOrDefault(n => n.Model == _sel.Current);
    }, () => !IsEditing && _sel.Current is not null);

    public IRelayCommand AddRootNodeAtCommand => new RelayCommand<System.Drawing.Point>((Point pos) =>
        {
            var newNode = new NodeModel(Guid.NewGuid(), "중심 노드")
            {
                Position = pos
            };
            var newVM = new NodeVM(newNode, _sel);
            Roots.Add(newVM);
            BuildEdgesAndLayout();
            _sel.Select(newNode); // 새로 추가된 노드 선택
        }, _ => !IsEditing);

    public IRelayCommand PasteImageCommand => new RelayCommand(OnPasteImage, () => !IsEditing && _sel.Current is not null);

    private void OnPasteImage()
    {
        var node = _sel.Current;
        if (node == null) return;

        if (!_clipboard.ContainsImage())
        {
            // 필요시 사용자에게 경고 메시지 노출 (View 쪽에서 Command CanExecute로 처리 가능)
            return;
        }

        // 1. 이미지 추출
        var img = _clipboard.GetImage();
        if (img == null) return;

        // 2. 임시 파일 저장 (현재 폴더에 images 생성)
        string tempDir = System.IO.Path.Combine(Environment.CurrentDirectory, "images");
        System.IO.Directory.CreateDirectory(tempDir);

        string fileName = $"img_{Guid.NewGuid()}.png";
        string fullPath = System.IO.Path.Combine(tempDir, fileName);

        img.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);

        // 3. NodeModel에 이미지 경로 저장
        //node.ImagePath = fullPath;

        // 4. 필요시: VM에 Notify (NodeVM이 Model.ImagePath를 중계)
        var nodeVM = AllNodes.FirstOrDefault(n => n.Model == node);
        if (nodeVM != null)
        {
            nodeVM.OriginalImageSize = new Size(img.Width, img.Height);
            var width = Math.Min(nodeVM.NodeWidth - 32, img.Width);
            var height = width * (double)img.Height / img.Width;
            nodeVM.ImageSize = new Size((int)width, (int)height);
            nodeVM.NodeHeight = nodeVM.NodeHeight + height;
            nodeVM.ImagePath = fullPath; // NotifyPropertyChanged 호출
        }
    }

    public IRelayCommand DetachToRootCommand => new RelayCommand(DetachSelectedNodeToRoot, CanDetachToRoot);

    private bool CanDetachToRoot()
    {
        var sel = _sel.Current;
        if (sel == null) return false;
        return _sel.GetParent(sel) != null; // 부모가 있을 때만 가능
    }

    private void DetachSelectedNodeToRoot()
    {
        var sel = _sel.Current;
        if (sel == null) return;

        var parent = _sel.GetParent(sel);
        if (parent == null) return;

        // 1. 부모의 Children에서 제거
        parent.Children.Remove(sel);

        // 2. Roots 컬렉션 및 트리 구조 갱신
        var rootVM = AllNodes.FirstOrDefault(n => n.Model == sel);
        if (rootVM is null)
        {
            throw new InvalidOperationException("Selected node not found in AllNodes.");
        }
        Roots.Add(rootVM);

        // 3. 부모맵/선택 갱신
        _sel.RegisterParent(sel, null);
        //_sel.Select(sel);

        // 4. 전체 레이아웃, 에지, 플랫 리스트 갱신
        BuildEdgesAndLayout();
    }

    public IRelayCommand DeleteNodeCommand => new RelayCommand(() =>
    {
        var current = _sel.Current;
        if (current == null) return;

        var parent = _sel.GetParent(current);

        // 1) 부모에서 제거
        if (parent != null)
        {
            if (parent.Children.Count > 1)
            {
                var idx = parent.Children.IndexOf(current);
                var nearSiblingIdx = idx == 0 ? idx + 1 : idx - 1;
                var sibling = parent.Children[nearSiblingIdx];
                _sel.Select(sibling);
            }
            else
                _sel.Select(parent);
            parent.Children.Remove(current);
            _sel.Remove(current); // 선택된 노드 제거
        }
        else
        {
            // 루트 노드인 경우 Roots에서 직접 제거
            var rootVM = Roots.FirstOrDefault(vm => vm.Model == current);
            if (rootVM != null)
                Roots.Remove(rootVM);
            _sel.Select(null);
            _sel.Remove(current); // 선택된 노드 제거
        }

        BuildEdgesAndLayout();
    }, () => !IsEditing);

    // 현재 파일 경로 상태를 기억
    private string? _currentFilePath;
    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set => SetProperty(ref _currentFilePath, value);
    }

    // ... (루트, 노드 등 기존 멤버)

    // 커맨드
    public IRelayCommand SaveCommand => new RelayCommand(Save);
    public IRelayCommand SaveAsCommand => new RelayCommand(SaveAs);
    public IRelayCommand OpenCommand => new RelayCommand(Open);

    // 실제 저장 로직 (경로 지정)

    public class MindMapSaveData
    {
        public List<NodeModel> Nodes { get; set; } = new();
        public float ZoomLevel { get; set; }
        public float PanX { get; set; }
        public float PanY { get; set; }
        // 기타 필요한 데이터...
    }
    private void SaveTo(string filePath)
    {
        var data = new MindMapSaveData
        {
            Nodes = Roots.Select(n => n.Model).ToList(),
            ZoomLevel = _zoomPanSvc.Zoom,
            PanX = _zoomPanSvc.Pan.X,
            PanY = _zoomPanSvc.Pan.Y
        };
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(data, options);
        File.WriteAllText(filePath, json);
        CurrentFilePath = filePath;
    }

    // Ctrl+S: 기존 파일은 그대로, 새 문서는 SaveAs와 동일
    private void Save()
    {
        if (!string.IsNullOrEmpty(CurrentFilePath) && File.Exists(CurrentFilePath))
        {
            SaveTo(CurrentFilePath);
        }
        else
        {
            SaveAs();
        }
    }

    // Ctrl+Shift+S: 항상 새 파일 경로 요청
    private void SaveAs()
    {
        var path = _fileDialog.ShowSaveFileDialog("JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*");
        if (!string.IsNullOrEmpty(path))
        {
            SaveTo(path);
        }
    }

    // Ctrl+O: 불러오기
    private void Open()
    {
        var path = _fileDialog.ShowOpenFileDialog("JSON 파일 (*.json)|*.json|모든 파일 (*.*)|*.*");
        if (!string.IsNullOrEmpty(path))
        {
            CurrentFilePath = path;
            string json = File.ReadAllText(path);
            // 1. 역직렬화 (배열 = 여러 루트 지원)
            var data = JsonSerializer.Deserialize<MindMapSaveData>(json);
            if (data is null)
                return;

            _zoomPanSvc.Pan = new PointF(data.PanX, data.PanY);
            _zoomPanSvc.Zoom = data.ZoomLevel;

            // 2. 기존 데이터 교체 (AllNodes, Roots 등 갱신)
            Roots.Clear();
            AllNodes.Clear();
            Edges.Clear();

            var hasParent = new bool[data.Nodes.Count];
            for (int i = 0; i < data.Nodes.Count; i++)
            {
                var node = data.Nodes[i];
                hasParent[i] = false; // 초기화
                //var vm = new NodeVM(node, _sel);
                //vm.ResizeCompleted += NodeVM_ResizeCompleted; // Resize 이벤트 핸들러 등록
                //AllNodes.Add(vm);
            }
            // check has parent
            for (int i = 0; i < data.Nodes.Count; i++)
            {
                var node = data.Nodes[i];
                for (int j = 0; j < node.Children.Count; j++)
                {
                    var childId = node.Children[j].Id;
                    int childIndex = data.Nodes.FindIndex(n => n.Id == childId);
                    if (childIndex >= 0)
                    {
                        hasParent[childIndex] = true;
                    }
                }
            }
            // insert roots
            for (int i = 0; i < data.Nodes.Count; i++)
            {
                var node = data.Nodes[i];
                if (!hasParent[i]) // 부모가 없는 노드
                {
                    var vm = new NodeVM(node, _sel);
                    Roots.Add(vm);
                }
            }
            BuildEdgesAndLayout();
        }
    }

    private readonly ISelectionService _sel;
    private readonly ILayoutService _lay;
    private readonly INodeMutationService _mut;
    private readonly IHitTestService _hit;
    private readonly IFileDialogService _fileDialog;
    private readonly IClipboardService _clipboard;
    private readonly IZoomPanService _zoomPanSvc;
}