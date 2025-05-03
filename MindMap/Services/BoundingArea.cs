namespace MindMap.Services
{
    internal class BoundingArea
    {
        public double Width { get; set; }
        public double Height { get; set; }

        public BoundingArea(double width, double height)
        {
            Width = width;
            Height = height;
        }
    }
}
