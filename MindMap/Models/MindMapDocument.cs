// Models/MindMapDocument.cs
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MindMap.Models
{
    public class MindMapDocument
    {
        public List<MindMapNode> RootNodes { get; } = new List<MindMapNode>();
        public List<MindMapArrow> Arrows { get; } = [];
        //internal List<MindMapNode> Nodes { get; } = new List<MindMapNode>();

        /* 노드 동적 추가 예시 */
        //internal MindMapNode AddNode(MindMapNode newNode)
        //{
        //    //Nodes.Add(newNode);
        //    RootNodes.Add(newNode);
        //    return newNode;
        //}
        //public MindMapNode RootNode { get; set; }

        public MindMapDocument(MindMapNode root)
        {
            RootNodes.Add(root);
            //RootNode = root;
            //Nodes.Add(root);
        }
    }
}
