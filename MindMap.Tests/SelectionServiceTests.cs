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
    }
}
