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
        var sel = new SelectionService();
        var vm = new DocumentVM(sel, new LayoutService(), new NodeMutationService(sel));
        vm.Roots.Should().HaveCount(1);
    }
}
