using MindMap.Core.Models;
using MindMap.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

public class HitServiceTests
{
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
    public void HitNode_Returns_Correct_Node()
    {
        var (r, a, b) = SampleTree();
        var svc = new HitTestService();
        svc.BuildIndex(new[] { r });

        svc.HitNode(new(10, 80)).Should().Be(a);
        svc.HitNode(new(10, 10)).Should().Be(r);
    }

    [Fact]
    public void HitSiblingGap_Computes_InsertIndex()
    {
        var (r, a, b) = SampleTree();
        var svc = new HitTestService();
        svc.BuildIndex(new[] { r });

        var gap = svc.HitSiblingGap(b, new PointF(10, 50)); // a 위쪽
        gap.Should().NotBeNull();
        gap!.Value.insertIndex.Should().Be(0);              // 맨 앞
    }

    [Fact]
    public void HitAttachTarget_Skips_Own_Subtree()
    {
        var (r, a, b) = SampleTree();
        var svc = new HitTestService();
        svc.BuildIndex(new[] { r });

        svc.HitAttachTarget(a, new PointF(10, 10)).Should().BeNull(); // root (ancestor) 금지
        svc.HitAttachTarget(a, new PointF(10, 150)).Should().Be(b);   // 다른 형제 OK
    }

    [Fact]
    public void HitSiblingGap_AboveFirstNode_ReturnsInsertIndex0()
    {
        var (root, a, b) = SampleTree();
        a.Position = new Point(0, 100);
        b.Position = new Point(0, 200);

        var svc = new HitTestService();
        svc.BuildIndex(new[] { root });

        var result = svc.HitSiblingGap(b, new PointF(10, 50));  // a 위쪽
        result.Should().NotBeNull();
        result!.Value.insertIndex.Should().Be(0);
    }

    [Fact]
    public void HitSiblingGap_BelowLastNode_ReturnsLastIndex()
    {
        var (root, a, b) = SampleTree();
        a.Position = new Point(0, 100);
        b.Position = new Point(0, 200);

        var svc = new HitTestService();
        svc.BuildIndex(new[] { root });

        var result = svc.HitSiblingGap(a, new PointF(10, 260)); // b 아래쪽
        result.Should().NotBeNull();
        result!.Value.insertIndex.Should().Be(2);               // a, b → insert at 2
    }
}
