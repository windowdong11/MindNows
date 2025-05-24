using MindMap.Core.Models;
using MindMap.Core.Services.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;
using MindMap.Core.Services;

public class SelectionServiceTests
{
    [Fact]
    public void Select_Sets_Current()
    {
        var svc = new SelectionService();
        var node = new NodeModel(Guid.NewGuid());
        svc.Select(node);

        svc.Current.Should().Be(node);
        svc.Multi.Should().ContainSingle().Which.Should().Be(node);
    }

    [Fact]
    public void Navigate_Down_Selects_Next_Sibling()
    {
        var root = new NodeModel(Guid.NewGuid(), "R");
        var a = new NodeModel(Guid.NewGuid(), "A");
        var b = new NodeModel(Guid.NewGuid(), "B");
        root.Children.Add(a); root.Children.Add(b);

        var sel = new SelectionService();
        sel.RegisterParent(a, root);
        sel.RegisterParent(b, root);

        sel.Select(a);
        sel.Navigate(Direction.Down);

        sel.Current.Should().Be(b);
    }

    public class MultiSelectTests
    {
        [Fact]
        public void ShiftDown_Extends_Selection()
        {
            // Arrange
            var root = new NodeModel(Guid.NewGuid(), "R");
            var a = new NodeModel(Guid.NewGuid(), "A");
            var b = new NodeModel(Guid.NewGuid(), "B");
            root.Children.Add(a); root.Children.Add(b);

            var sel = new SelectionService();
            sel.RegisterParent(a, root);
            sel.RegisterParent(b, root);

            sel.Select(a);

            // Act
            sel.ExpandRange(Direction.Down);

            // Assert
            sel.Multi.Should().BeEquivalentTo(new[] { a, b });
            sel.Current.Should().Be(b);
        }

        [Fact]
        public void CtrlLeft_RightChild_Promotes_AfterParent()
        {
            var root = new NodeModel(Guid.NewGuid(), "Root");
            var p = new NodeModel(Guid.NewGuid(), "P");
            var a = new NodeModel(Guid.NewGuid(), "A") { Side = SideEnum.Right };
            var s = new NodeModel(Guid.NewGuid(), "S");             // Parent sibling

            root.Children.Add(p);
            root.Children.Add(s);
            p.Children.Add(a);

            var sel = new SelectionService();
            sel.RegisterParent(p, root); sel.RegisterParent(s, root);
            sel.RegisterParent(a, p);

            var mut = new NodeMutationService(sel);
            mut.Reparent(a, ReparentAction.Left).Should().BeTrue();

            root.Children[1].Should().Be(a);                        // after parent
            sel.GetParent(a).Should().Be(root);
        }

        [Fact]
        public void CtrlRight_RightChild_Demotes_ToPrevSibling()
        {
            var root = new NodeModel(Guid.NewGuid(), "Root");
            var p = new NodeModel(Guid.NewGuid(), "P");
            var a = new NodeModel(Guid.NewGuid(), "A") { Side = SideEnum.Right };
            var prev = new NodeModel(Guid.NewGuid(), "Prev");
            //p.Children.AddRange(new[] { prev, a });
            p.Children.Add(prev);
            p.Children.Add(a);

            var sel = new SelectionService();
            sel.RegisterParent(p, root);
            sel.RegisterParent(prev, p); sel.RegisterParent(a, p);

            var mut = new NodeMutationService(sel);
            mut.Reparent(a, ReparentAction.Right).Should().BeTrue();

            prev.Children.Last().Should().Be(a);
            sel.GetParent(a).Should().Be(prev);
        }
    }
}
