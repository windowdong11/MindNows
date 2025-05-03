using MindMap.Common;
using MindMap.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace MindMap.ViewModels
{
    public static class ExtensionMethods
    {
        public static int RemoveAll<T>(
            this ObservableCollection<T> coll, Func<T, bool> condition)
        {
            var itemsToRemove = coll.Where(condition).ToList();

            foreach (var itemToRemove in itemsToRemove)
            {
                coll.Remove(itemToRemove);
            }

            return itemsToRemove.Count;
        }
    }
    internal class MindMapViewModel : INotifyPropertyChanged
    {
        public event Action? LayoutRequested;

        public void RequestLayout()
        {
            LayoutRequested?.Invoke(); // 내부에서만 호출하므로 안전
        }
        public ObservableCollection<MindMapNodeViewModel> Nodes { get; } = new();
        public MindMapNodeViewModel RootNode;

        private MindMapNodeViewModel? _selectedNode;
        public MindMapNodeViewModel? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (_selectedNode != value)
                {
                    if (_selectedNode != null) _selectedNode.IsSelected = false;
                    _selectedNode = value;
                    if (_selectedNode != null) _selectedNode.IsSelected = true;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<MindMapArrowViewModel> Arrows { get; } = new();


        public MindMapViewModel(MindMapDocument document)
        {
            var nodeMap = new Dictionary<Guid, MindMapNodeViewModel>();

            foreach (var model in document.Nodes)
            {
                var vm = new MindMapNodeViewModel(model, this);
                if (model.Id == document.RootNode.Id)
                {
                    RootNode = vm;
                }
                Nodes.Add(vm);
                nodeMap[model.Id] = vm;
            }
            if (RootNode == null)
            {
                throw new InvalidOperationException("RootNode cannot be null.");
            }

            foreach (var vm in Nodes)
            {
                if (vm.Model.Parent != null && nodeMap.TryGetValue(vm.Model.Parent.Id, out var parentVm))
                {
                    vm.Parent = parentVm;
                    parentVm.Children.Add(vm);
                    Arrows.Add(new MindMapArrowViewModel(parentVm, vm));
                }
            }

            AddChildCommand = new RelayCommand(_ => AddChild(), _ => SelectedNode != null);
            AddSiblingCommand = new RelayCommand(_ => AddSibling(), _ => SelectedNode?.Parent != null);
            DeleteNodeCommand = new RelayCommand(_ => DeleteNode(), _ => SelectedNode != null);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ICommand AddChildCommand { get; }
        public ICommand AddSiblingCommand { get; }
        public ICommand DeleteNodeCommand { get; }

        private void AddChild()
        {
            if (SelectedNode == null) return;

            var model = new MindMapNode
            {
                Text = "New Child",
                Parent = SelectedNode.Model
            };
            SelectedNode.Model.Children.Add(model);

            var vm = new MindMapNodeViewModel(model, this)
            {
                Parent = SelectedNode
            };
            SelectedNode.Children.Add(vm);
            Nodes.Add(vm);
            Arrows.Add(new MindMapArrowViewModel(SelectedNode, vm));
            SelectedNode = vm;
            LayoutRequested?.Invoke();
        }

        private void AddSibling()
        {
            if (SelectedNode?.Parent == null) return;
            var parent = SelectedNode.Parent;

            var model = new MindMapNode
            {
                Text = "New Sibling",
                Parent = parent.Model
            };
            parent.Model.Children.Add(model);

            var vm = new MindMapNodeViewModel(model, this)
            {
                Parent = parent
            };
            parent.Children.Add(vm);
            Nodes.Add(vm);
            Arrows.Add(new MindMapArrowViewModel(parent, vm));
            SelectedNode = vm;

            LayoutRequested?.Invoke();
        }

        private void DeleteNode()
        {
            if (SelectedNode == null || SelectedNode.Parent == null) return;

            void DeleteRecursive(MindMapNodeViewModel vm)
            {
                foreach (var child in vm.Children.ToList())
                    DeleteRecursive(child);

                Nodes.Remove(vm);
                Arrows.RemoveAll(a => a.From == vm || a.To == vm);
            }

            var parent = SelectedNode.Parent;
            parent.Children.Remove(SelectedNode);
            parent.Model.Children.Remove(SelectedNode.Model);
            DeleteRecursive(SelectedNode);
            SelectedNode = parent;
            LayoutRequested?.Invoke();
        }
    }
}
