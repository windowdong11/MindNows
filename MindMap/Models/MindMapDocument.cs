using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MindMap.Models
{
    public class MindMapDocument
    {
        internal ObservableCollection<MindMapNode> Nodes { get; }
        = new ObservableCollection<MindMapNode>();

        /* 노드 동적 추가 예시 */
        internal MindMapNode AddNode(string text)
        {
            var newNode = new MindMapNode
            {
                Text = text,
            };
            Nodes.Add(newNode);
            return newNode;
        }
        public MindMapNode RootNode { get; set; }
        public List<MindMapArrow> Arrows { get; } = [];

        public MindMapDocument(MindMapNode root)
        {
            RootNode = root;
            Nodes.Add(new MindMapNode
            {
                Text = root.Text,
            });
        }
    }
}
