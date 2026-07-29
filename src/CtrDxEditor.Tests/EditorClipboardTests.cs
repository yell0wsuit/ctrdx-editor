using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        /// <summary>Clearing the buffer takes Paste with it, because the buffer is all Paste ever reads.</summary>
        [Fact]
        public void ClearClipboardWithdrawsPaste()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.Selection.Replace(vm.Document!.AllObjects[0]);
            vm.CopySelection();
            Assert.True(vm.CanPaste);

            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            vm.ClearClipboard();

            Assert.False(vm.HasClipboard);
            Assert.False(vm.CanPaste);
            Assert.Contains(nameof(EditorViewModel.HasClipboard), changed);
            Assert.Contains(nameof(EditorViewModel.CanPaste), changed);

            // The buffer is genuinely gone, not just reported empty.
            vm.PasteAt(100, 200);
            _ = Assert.Single(vm.Document.AllObjects);
        }

        /// <summary>Clearing an already-empty clipboard raises nothing.</summary>
        [Fact]
        public void ClearClipboardOnAnEmptyBufferIsSilent()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            vm.ClearClipboard();

            Assert.False(vm.HasClipboard);
            Assert.Empty(changed);
        }

        /// <summary>Copy fills both stores, so the two can never disagree about which is fresher.</summary>
        [Fact]
        public async Task CopyWritesTheSelectionToTheSystemClipboard()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            string? written = null;
            vm.WriteClipboardText = text => { written = text; return Task.CompletedTask; };
            vm.Selection.Replace(vm.Document!.AllObjects[0]);

            await vm.CopySelectionAsync();

            Assert.True(vm.HasClipboard);
            Assert.Contains("<bubble", written);
            Assert.Contains("x=\"10\"", written);
        }

        /// <summary>Paste stands down until something is copied inside the editor.</summary>
        /// <remarks>
        /// The system clipboard is write-only: objects go out so they can be pasted elsewhere, and nothing
        /// ever comes back in. Paste therefore answers to the internal buffer alone, on every platform,
        /// which is also what keeps the browser from having to guess at a clipboard it may not read.
        /// </remarks>
        [Fact]
        public void PasteIsUnavailableUntilSomethingIsCopiedInTheEditor()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");

            Assert.False(vm.HasClipboard);
            Assert.False(vm.CanPaste);

            vm.Selection.Replace(vm.Document!.AllObjects[0]);
            vm.CopySelection();

            Assert.True(vm.CanPaste);
        }

        /// <summary>A clipboard the platform will not accept still leaves the copy usable in the editor.</summary>
        [Fact]
        public async Task CopySurvivesAClipboardWriteThatThrows()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.Selection.Replace(vm.Document!.AllObjects[0]);
            vm.WriteClipboardText = _ => throw new InvalidOperationException("denied");

            await vm.CopySelectionAsync();

            Assert.True(vm.HasClipboard);
            Assert.True(vm.CanPaste);

            vm.PasteAt(100, 200);

            Assert.Equal(2, vm.Document!.AllObjects.Count);
            Assert.Equal("bubble", vm.Selection.Primary!.Type);
        }

        /// <summary>Cut deletes the selection it copied even when publishing to the platform clipboard is delayed.</summary>
        [Fact]
        public async Task CutDeletesTheCopiedSelectionWhenSelectionChangesDuringClipboardWrite()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/><star x=\"30\" y=\"40\"/>");
            TaskCompletionSource published = new();
            vm.WriteClipboardText = _ => published.Task;
            LevelObject bubble = vm.Document!.AllObjects[0];
            LevelObject star = vm.Document.AllObjects[1];
            vm.Selection.Replace(bubble);

            Task cut = vm.CutSelectionAsync();
            vm.Selection.Replace(star);
            published.SetResult();
            await cut;

            Assert.DoesNotContain(bubble, vm.Document.AllObjects);
            Assert.Contains(star, vm.Document.AllObjects);
        }
    }
}
