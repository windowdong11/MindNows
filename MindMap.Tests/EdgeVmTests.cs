using MindMap.Core.Models;
using MindMap.Core.Services.Impl;
using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace MindMap.Tests;

public class EdgeVmTests
{
    [Fact]
    public void EdgeVM_Info_Start_End_Are_Correct()
    {
        var p = new NodeModel(Guid.NewGuid(), "P") { Position = new(0, 0) };
        var c = new NodeModel(Guid.NewGuid(), "C") { Position = new(200, 0), Side = SideEnum.Right };
        var sel = new SelectionService();
        var pvm = new NodeVM(p, sel);
        var cvm = new NodeVM(c, sel);
        var edge = new EdgeVM(pvm, cvm);

        edge.Info.Start.X.Should().Be(120);    // NodeWidth (120) 오른쪽 끝
        edge.Info.End.X.Should().Be(200);      // Child 왼쪽
    }
}
