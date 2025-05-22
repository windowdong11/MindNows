using FluentAssertions;
using MindMap.Core.Services.Impl;
using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DocumentVmTests
{
    [Fact]
    public void ShouldContainOneRoot()
    {
        var vm = new DocumentVM(new SelectionService());
        vm.Roots.Should().HaveCount(1);
        vm.Roots[0].Children.Should().HaveCount(2);
    }
}
