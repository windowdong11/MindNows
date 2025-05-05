using MindMap.ViewModels;

namespace MindMap.Services
{
    internal interface ILayoutService
    {
        void RecalculateLayout(MindMapNodeViewModel root);
    }
}
