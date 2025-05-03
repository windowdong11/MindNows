using MindMap.Models;

namespace MindMap.Services
{
    internal class SelectionService
    {
        public static MindMapNode? SelectedNode { get; private set; }

        public static void Select(MindMapNode node)
        {
            if (SelectedNode != null)
            {
                SelectedNode.IsSelected = false; // 이전 선택 해제
            }
            SelectedNode = node;
            SelectedNode.IsSelected = true; // 현재 선택
        }

        public static void Clear()
        {
            if (SelectedNode == null)
            {
                return;
            }
            SelectedNode.IsSelected = false;
            SelectedNode = null;
        }
    }
}
