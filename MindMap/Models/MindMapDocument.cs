using MindMap.Models;
using System.Collections.Generic;

namespace MindMap.Models
{
    public class MindMapDocument
    {
        public MindMapNode RootNode { get; set; }
        public List<MindMapArrow> Arrows { get; } = [];

        public MindMapDocument(MindMapNode root)
        {
            RootNode = root;
        }
    }
}
