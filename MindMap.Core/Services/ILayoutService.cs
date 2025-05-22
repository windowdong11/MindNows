using MindMap.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Core.Services;

public interface ILayoutService
{
    void Arrange(NodeModel root);
}
