using FluentAssertions;
using MindMap.Core.Services;
using MindMap.Core.Services.Impl;
using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MindMap.Core.Layout;

public class DocumentVmTests
{
    [Fact]
    public void ShouldContainOneRoot()
    {
        var vm = new DocumentVM(null!, null!, null!, null!, null!, null!, null!);
        vm.Roots.Should().HaveCount(1);
    }
}
