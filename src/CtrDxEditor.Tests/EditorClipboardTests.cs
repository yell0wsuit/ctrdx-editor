using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests editor duplication and clipboard operations.</summary>
    public class EditorClipboardTests
    {
        private static EditorViewModel Load(string layerBody)
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(
                "<map><layer name=\"settings\"><map/></layer>" +
                "<layer name=\"L0\">" + layerBody + "</layer></map>");
            vm.ActiveLayer = vm.Layers[0];
            return vm;
        }

        /// <summary>Verifies duplication offsets clones and selects them.</summary>
        [Fact]
        public void DuplicateSelectionClonesSelectedWithOffsetAndSelectsClones()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            LevelObject original = vm.Document!.AllObjects[0];
            vm.Selection.Replace(original);

            vm.DuplicateSelection(16, 16);

            Assert.Equal(2, vm.Document.AllObjects.Count);
            Assert.Equal(1, vm.Selection.Count);
            LevelObject clone = vm.Selection.Primary!;
            Assert.NotSame(original.Element, clone.Element);
            Assert.Equal("26", clone.GetAttr("x"));
            Assert.Equal("36", clone.GetAttr("y"));
        }

        /// <summary>Verifies copied objects can be pasted at a target position.</summary>
        [Fact]
        public void CopyThenPasteAddsClonesAtTarget()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.Selection.Replace(vm.Document!.AllObjects[0]);

            vm.CopySelection();
            vm.PasteAt(100, 200);

            Assert.Equal(2, vm.Document.AllObjects.Count);
            Assert.Equal(1, vm.Selection.Count);
            Assert.Equal("100", vm.Selection.Primary!.GetAttr("x"));
            Assert.Equal("200", vm.Selection.Primary.GetAttr("y"));
        }

        /// <summary>Verifies paste preserves the copied objects' relative layout.</summary>
        [Fact]
        public void PastePreservesRelativeLayoutAroundTargetCentroid()
        {
            EditorViewModel vm = Load("<bubble x=\"0\" y=\"0\"/><star x=\"20\" y=\"0\"/>");
            vm.Selection.SetRange(vm.Document!.AllObjects, vm.Document.AllObjects[1]);

            vm.CopySelection();
            vm.PasteAt(100, 100);

            LevelObject[] pasted = [.. vm.Selection.Items.OrderBy(o => o.X)];
            Assert.Equal(90, pasted[0].X);
            Assert.Equal(100, pasted[0].Y);
            Assert.Equal(110, pasted[1].X);
            Assert.Equal(100, pasted[1].Y);
        }

        /// <summary>Verifies cut removes originals while retaining clipboard data.</summary>
        [Fact]
        public void CutRemovesOriginalsAndKeepsThemForPaste()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.Selection.Replace(vm.Document!.AllObjects[0]);

            vm.CutSelection();
            Assert.Empty(vm.Document.AllObjects);

            vm.PasteAt(5, 5);
            _ = Assert.Single(vm.Document.AllObjects);
        }

        /// <summary>Verifies deleting a selection removes every selected object.</summary>
        [Fact]
        public void DeleteSelectedRemovesEverySelectedObject()
        {
            EditorViewModel vm = Load("<bubble x=\"1\" y=\"1\"/><star x=\"2\" y=\"2\"/>");
            vm.Selection.SetRange(vm.Document!.AllObjects, vm.Document.AllObjects[0]);

            vm.DeleteSelected();

            Assert.Empty(vm.Document.AllObjects);
            Assert.Equal(0, vm.Selection.Count);
        }

        /// <summary>Closing a level disables document edit commands without discarding copied objects.</summary>
        [Fact]
        public void CloseLevelDisablesEditCommandsWhileRetainingClipboard()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.SetObjectSelection([vm.Document!.AllObjects[0]]);
            vm.CopySelection();

            Assert.True(vm.CanCutSelection);
            Assert.True(vm.CanCopySelection);
            Assert.True(vm.CanPaste);
            Assert.True(vm.CanDeleteSelection);
            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.CloseLevel();

            Assert.False(vm.CanCutSelection);
            Assert.False(vm.CanCopySelection);
            Assert.False(vm.CanPaste);
            Assert.False(vm.CanDeleteSelection);
            Assert.True(vm.HasClipboard);
            Assert.Contains(nameof(EditorViewModel.SelectedObject), changed);
            Assert.Contains(nameof(EditorViewModel.CanCutSelection), changed);
            Assert.Contains(nameof(EditorViewModel.CanCopySelection), changed);
            Assert.Contains(nameof(EditorViewModel.CanPaste), changed);
            Assert.Contains(nameof(EditorViewModel.CanDeleteSelection), changed);
        }
    }
}
