using MindMap.Models;
using MindMap.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Windows;

namespace MindMap.Repositiory
{
    internal class MindMapPersistenceService : IMindMapPersistenceService
    {
        public void Save(string filePath, MindMapViewModel viewModel)
        {
            var serializableFile = new SerializableMindMapFile
            {
                Document = ToSerializable(viewModel),
                ViewState = ToSerializableViewState(viewModel)
            };

            var json = JsonSerializer.Serialize(serializableFile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        public (MindMapDocument Document, SerializableMindMapViewState ViewState) Load(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var serializableFile = JsonSerializer.Deserialize<SerializableMindMapFile>(json);

            if (serializableFile == null)
                throw new InvalidOperationException("Failed to deserialize the mind map file.");

            var document = FromSerializable(serializableFile.Document);
            return (document, serializableFile.ViewState);
        }
        public static SerializableMindMapDocument ToSerializable(MindMapViewModel vm)
        {
            return new SerializableMindMapDocument
            {
                RootNodes = vm.RootNodes.Select(root => ToSerializableNode(root.Model)).ToList(),
                //Arrows = vm.Arrows.Select(arrow => new SerializableMindMapArrow
                //{
                //    FromId = arrow.From.Id,
                //    ToId = arrow.To.Id,
                //    Label = arrow.Label,
                //    IsBidirectional = arrow.IsBidirectional
                //}).ToList()
            };
        }

        private static SerializableMindMapNode ToSerializableNode(MindMapNode node)
        {
            return new SerializableMindMapNode
            {
                Id = node.Id,
                Text = node.Text,
                ImagePath = node.ImagePath,
                X = node.Position.X,
                Y = node.Position.Y,
                Width = node.Size.Width,
                Height = node.Size.Height,
                IsLeftSide = node.IsLeftSide,
                LeftChildren = node.LeftChildren.Select(ToSerializableNode).ToList(),
                RightChildren = node.RightChildren.Select(ToSerializableNode).ToList()
            };
        }

        static SerializableMindMapViewState ToSerializableViewState(MindMapViewModel viewModel)
        {
            return new SerializableMindMapViewState
            {
                OffsetX = viewModel.OffsetX,
                OffsetY = viewModel.OffsetY,
                Scale = viewModel.Scale
            };
        }

        public static void ApplyViewState(MindMapViewModel viewModel, SerializableMindMapViewState state)
        {
            viewModel.OffsetX = state.OffsetX;
            viewModel.OffsetY = state.OffsetY;
            viewModel.Scale = state.Scale;
        }

        static MindMapDocument FromSerializable(SerializableMindMapDocument serializable)
        {
            var nodeMap = new Dictionary<Guid, MindMapNode>();

            var rootNodes = serializable.RootNodes.Select(n => FromSerializableNode(n, nodeMap)).ToList();
            var document = new MindMapDocument(rootNodes.First());  // 첫 루트를 초기화용으로 사용
            document.RootNodes.Clear();
            document.RootNodes.AddRange(rootNodes);

            //foreach (var arrow in serializable.Arrows)
            //{
            //    if (nodeMap.TryGetValue(arrow.FromId, out var from) && nodeMap.TryGetValue(arrow.ToId, out var to))
            //    {
            //        document.Arrows.Add(new MindMapArrow(from, to)
            //        {
            //            Label = arrow.Label,
            //            IsBidirectional = arrow.IsBidirectional
            //        });
            //    }
            //}

            return document;
        }

        private static MindMapNode FromSerializableNode(SerializableMindMapNode serializable, Dictionary<Guid, MindMapNode> nodeMap)
        {
            var node = new MindMapNode
            {
                Text = serializable.Text,
                ImagePath = serializable.ImagePath,
                Position = new Point(serializable.X, serializable.Y),
                Size = new Size(serializable.Width, serializable.Height),
                IsLeftSide = serializable.IsLeftSide
            };

            nodeMap[serializable.Id] = node;

            foreach (var child in serializable.LeftChildren)
            {
                var childNode = FromSerializableNode(child, nodeMap);
                childNode.Parent = node;
                node.LeftChildren.Add(childNode);
            }
            foreach (var child in serializable.RightChildren)
            {
                var childNode = FromSerializableNode(child, nodeMap);
                childNode.Parent = node;
                node.RightChildren.Add(childNode);
            }

            return node;
        }
    }


}
