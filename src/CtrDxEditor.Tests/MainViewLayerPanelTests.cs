using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests layer-panel wiring that is difficult to exercise through headless pointer input.</summary>
    public class MainViewLayerPanelTests
    {
        /// <summary>Every terminal pointer path clears pending layer-drag gesture state.</summary>
        [Fact]
        public void LayerDragClearsPendingStateWhenPointerGestureEnds()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string codeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Commands.cs"));
            string viewCodeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));

            Assert.Contains("PointerReleased=\"LayerRow_PointerReleased\"", view, StringComparison.Ordinal);
            Assert.Contains("PointerCaptureLost=\"LayerRow_PointerCaptureLost\"", view, StringComparison.Ordinal);
            Assert.Contains("ClearPendingLayerDrag();", codeBehind, StringComparison.Ordinal);
            Assert.Contains(
                "PointerReleasedEvent, LayerRow_PointerReleased, RoutingStrategies.Bubble, handledEventsToo: true",
                viewCodeBehind,
                StringComparison.Ordinal);
        }

        /// <summary>Object rows start drags, clear terminal gesture state, and layers accept their payload.</summary>
        [Fact]
        public void ObjectRowsDragOntoLayerRows()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string codeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Commands.cs"));
            string viewCodeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));

            Assert.Contains("PointerPressed=\"ObjectRow_PointerPressed\"", view, StringComparison.Ordinal);
            Assert.Contains("PointerMoved=\"ObjectRow_PointerMoved\"", view, StringComparison.Ordinal);
            Assert.Contains("PointerReleased=\"ObjectRow_PointerReleased\"", view, StringComparison.Ordinal);
            Assert.Contains("PointerCaptureLost=\"ObjectRow_PointerCaptureLost\"", view, StringComparison.Ordinal);
            Assert.Contains("DataTransferItem.Create(ObjectDragFormat, obj)", codeBehind, StringComparison.Ordinal);
            Assert.Contains("ClearPendingObjectDrag();", codeBehind, StringComparison.Ordinal);
            Assert.Contains("e.DataTransfer.Contains(ObjectDragFormat)", codeBehind, StringComparison.Ordinal);
            Assert.Contains("e.DataTransfer.TryGetValue(ObjectDragFormat) is LevelObject obj", codeBehind, StringComparison.Ordinal);
            Assert.Contains("vm.MoveObjectToLayer(obj, target.Layer)", codeBehind, StringComparison.Ordinal);
            Assert.Contains(
                "PointerReleasedEvent, ObjectRow_PointerReleased, RoutingStrategies.Bubble, handledEventsToo: true",
                viewCodeBehind,
                StringComparison.Ordinal);
        }

        /// <summary>The redesigned layer row exposes organization state without staying permanently editable.</summary>
        [Fact]
        public void LayerRowsUseExplicitRenameModeAndOrganizationalStyling()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string codeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Commands.cs"));
            string layerViewModel = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "ViewModels", "LayerViewModel.cs"));

            Assert.Contains("Classes.layer-name-display=\"True\"", view, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding !IsRenaming}\"", view, StringComparison.Ordinal);
            Assert.Contains("Classes.layer-name-editor=\"True\"", view, StringComparison.Ordinal);
            Assert.Contains("IsVisible=\"{Binding IsRenaming}\"", view, StringComparison.Ordinal);
            Assert.Contains("Kind=\"Pencil\"", view, StringComparison.Ordinal);
            Assert.Contains("DoubleTapped=\"LayerName_DoubleTapped\"", view, StringComparison.Ordinal);
            Assert.Contains("KeyDown=\"LayerTree_KeyDown\"", view, StringComparison.Ordinal);
            Assert.Contains("Text=\"{Binding Objects.Count}\"", view, StringComparison.Ordinal);
            Assert.Contains("Classes.active=\"{Binding IsActive}\"", view, StringComparison.Ordinal);
            Assert.Contains("Property=\"BorderThickness\" Value=\"3,0,0,0\"", view, StringComparison.Ordinal);
            Assert.Contains("Property=\"IsExpanded\"", view, StringComparison.Ordinal);
            Assert.Contains("((vm:LayerViewModel)DataContext).IsExpanded", view, StringComparison.Ordinal);
            Assert.Contains("Mode=TwoWay", view, StringComparison.Ordinal);
            Assert.Contains("e.Key == Key.F2", codeBehind, StringComparison.Ordinal);
            Assert.Contains("LayerRename_Click", codeBehind, StringComparison.Ordinal);
            Assert.Contains("IsRenaming", layerViewModel, StringComparison.Ordinal);
        }

        /// <summary>Selection changes queue a null-safe scroll after the tree has laid out expanded children.</summary>
        [Fact]
        public void TreeSelectionChangesBringRealizedItemIntoView()
        {
            string viewCodeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));

            Assert.Contains("nameof(EditorViewModel.SelectedTreeItem)", viewCodeBehind, StringComparison.Ordinal);
            Assert.Contains("Dispatcher.UIThread.Post(BringSelectedTreeItemIntoView", viewCodeBehind, StringComparison.Ordinal);
            Assert.Contains("container.BringIntoView();", viewCodeBehind, StringComparison.Ordinal);
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine([path, .. parts]);
        }
    }
}
