using MindMap.Models;
using System;

namespace MindMap.Models
{
    public class MindMapArrow
    {
        public Guid Id { get; } = Guid.NewGuid();
        public MindMapNode From { get; set; }
        public MindMapNode To { get; set; }

        public string? Label { get; set; }
        public bool IsBidirectional { get; set; } = false;

        public MindMapArrow(MindMapNode from, MindMapNode to)
        {
            From = from;
            To = to;
        }
    }
}