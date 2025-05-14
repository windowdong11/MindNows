using MindMap.Models;
using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MindMap.Repositiory
{
    interface IMindMapPersistenceService
    {
        void Save(string filePath, MindMapViewModel viewModel);
        (MindMapDocument Document, SerializableMindMapViewState ViewState) Load(string filePath);
    }
}
