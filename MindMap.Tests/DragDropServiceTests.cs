using FluentAssertions;
using MindMap.Core.Models;
using MindMap.Core.Services;
using MindMap.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DragDropServiceTests
{
    public class FakeHitTest : IHitTestService
    {
        public void BuildIndex(IEnumerable<NodeModel> roots)
        {
            throw new NotImplementedException();
        }

        public NodeModel? HitAttachTarget(NodeModel dragging, PointF worldPos)
        {
            throw new NotImplementedException();
        }

        public NodeModel? HitNode(PointF worldPos)
        {
            throw new NotImplementedException();
        }

        public (NodeModel parent, int insertIndex)? HitSiblingGap(NodeModel dragging, PointF worldPos)
        {
            throw new NotImplementedException();
        }
    }
    private static (NodeModel root, NodeModel a, NodeModel b) SampleTree()
    {
        var root = new NodeModel(Guid.NewGuid(), "R") { Position = new(0, 0) };
        var a = new NodeModel(Guid.NewGuid(), "A") { Position = new(0, 72) };
        var b = new NodeModel(Guid.NewGuid(), "B") { Position = new(0, 144) };
        root.Children.Add(a);
        root.Children.Add(b);
        return (root, a, b);
    }
    [Fact]
    public void Dragging_Gap_Reorders_Siblings()
    {
        var (root, a, b) = SampleTree();
        var sel = new SelectionService();
        sel.RegisterParent(a, root);
        sel.RegisterParent(b, root);
        sel.RegisterParent(root, null); // root has no parent
        sel.Select(a);
        //var hit = new FakeHitTest( /* returns gap between a,b */ );
        var hit = new HitTestService();
        var mut = new NodeMutationService(sel);
        hit.BuildIndex(new[] { root });
        bool rebuilt = false;
        var svc = new DragDropService(sel, hit, mut, () => rebuilt = true);

        svc.BeginDrag(new(10, 154));
        svc.UpdateDrag(new(10, 50));   // above first
        svc.CommitDrag(new(10, 50));

        root.Children.First().Should().Be(b);      // now first
        rebuilt.Should().BeTrue();
    }
}