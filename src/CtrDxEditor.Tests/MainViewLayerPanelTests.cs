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

        /// <summary>Object drag-over highlights the full destination layer and clears every terminal path.</summary>
        [Fact]
        public void ObjectDragHighlightsDestinationLayerRow()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string codeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Commands.cs"));

            Assert.Contains("Selector=\"Border.layer-row.drop-target\"", view, StringComparison.Ordinal);
            Assert.Contains("DragDrop.DragLeave=\"LayerRow_DragLeave\"", view, StringComparison.Ordinal);
            Assert.Contains("SetLayerDropTarget(sender as Border);", codeBehind, StringComparison.Ordinal);
            Assert.Contains("private void ClearLayerDropTarget()", codeBehind, StringComparison.Ordinal);
            Assert.Contains("_layerDropTarget.Classes.Remove(\"drop-target\")", codeBehind, StringComparison.Ordinal);
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
            Assert.Contains("Property=\"IsExpanded\"", view, StringComparison.Ordinal);
            Assert.Contains("((vm:LayerViewModel)DataContext).IsExpanded", view, StringComparison.Ordinal);
            Assert.Contains("Mode=TwoWay", view, StringComparison.Ordinal);
            Assert.Contains("e.Key == Key.F2", codeBehind, StringComparison.Ordinal);
            Assert.Contains("LayerRename_Click", codeBehind, StringComparison.Ordinal);
            Assert.Contains("IsRenaming", layerViewModel, StringComparison.Ordinal);
        }

        /// <summary>Active-layer and selected-object rows share one borderless, bold selection treatment.</summary>
        [Fact]
        public void SelectionBlueIsUniformAndDoesNotBleedIntoObjectChildren()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            Assert.Contains("Selector=\"TreeViewItem:selected\"", view, StringComparison.Ordinal);
            Assert.Contains("Selector=\"TextBlock.object-name.selected\"", view, StringComparison.Ordinal);
            Assert.Contains("Classes.object-name=\"True\"", view, StringComparison.Ordinal);
            Assert.Contains("<Classes.selected>", view, StringComparison.Ordinal);
            Assert.Contains("$parent[TreeView].SelectedItem", view, StringComparison.Ordinal);
            Assert.DoesNotContain("TreeViewItem:selected Border.object-row", view, StringComparison.Ordinal);
            Assert.DoesNotContain("Selector=\"TreeViewItem:selected\">\n              <Setter Property=\"Background\" Value=\"{DynamicResource SystemControlHighlightListAccentLowBrush}\" />\n              <Setter Property=\"FontWeight\"", view, StringComparison.Ordinal);
            Assert.DoesNotContain("Value=\"3,0,0,0\"", view, StringComparison.Ordinal);
            Assert.True(
                view.Split("SystemControlHighlightListAccentLowBrush", StringSplitOptions.None).Length >= 3,
                "Expected the active layer and selected item to use the same low-accent brush.");
        }

        /// <summary>The pencil toggles rename without stealing focus before it can commit.</summary>
        [Fact]
        public void PencilClickTogglesLayerRename()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string codeBehind = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Commands.cs"));

            Assert.Contains("Kind=\"Pencil\"", view, StringComparison.Ordinal);
            Assert.Contains("Focusable=\"False\"", view, StringComparison.Ordinal);
            Assert.Contains("if (row.IsRenaming)", codeBehind, StringComparison.Ordinal);
            Assert.Contains("CommitLayerRename(row, editor?.Text ?? row.Name);", codeBehind, StringComparison.Ordinal);
        }

        /// <summary>Startup bindings hide the locale picker and disable layer actions until a document exists.</summary>
        [Fact]
        public void StartupLayerControlsUseSafeDocumentFallbacks()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            Assert.Contains("IsVisible=\"{Binding HasLocalizedText, FallbackValue=False}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding HasDocument, FallbackValue=False}\"", view, StringComparison.Ordinal);
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
