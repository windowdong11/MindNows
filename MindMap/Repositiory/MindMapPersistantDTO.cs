using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Repositiory
{
    public class SerializableMindMapDocument
    {
        public List<SerializableMindMapNode> RootNodes { get; set; } = new();
        //public List<SerializableMindMapArrow> Arrows { get; set; } = new();
    }

    public class SerializableMindMapFile
    {
        public SerializableMindMapDocument Document { get; set; }
        public SerializableMindMapViewState ViewState { get; set; }
    }


    public class SerializableMindMapViewState
    {
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double Scale { get; set; }
    }


    public class SerializableMindMapNode
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public string? ImagePath { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsLeftSide { get; set; }
        public List<SerializableMindMapNode> LeftChildren { get; set; } = new();
        public List<SerializableMindMapNode> RightChildren { get; set; } = new();
    }

    public class SerializableMindMapArrow
    {
        public Guid FromId { get; set; }
        public Guid ToId { get; set; }
        public string? Label { get; set; }
        public bool IsBidirectional { get; set; }
    }
}
