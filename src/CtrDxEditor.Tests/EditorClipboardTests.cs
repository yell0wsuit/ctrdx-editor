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

        /// <summary>Clearing the internal clipboard does not hide XML already observed outside the editor.</summary>
        /// <remarks>
        /// The cache cannot prove whether the OS clipboard still holds our disowned text or was replaced
        /// with someone else's valid XML. Keeping Paste reachable is safer; a later activation or paste
        /// probe settles the approximation.
        /// </remarks>
        [Fact]
        public async Task ClearClipboardKeepsObservedExternalXmlReachable()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.ReadClipboardText = () => Task.FromResult<string?>("<star x=\"1\" y=\"2\" />");
            await vm.RefreshSystemClipboardStateAsync();
            vm.Selection.Replace(vm.Document!.AllObjects[0]);
            vm.CopySelection();
            Assert.True(vm.HasClipboard);
            Assert.True(vm.CanPaste);

            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);
            vm.ClearClipboard();

            Assert.False(vm.HasClipboard);
            Assert.True(vm.CanPaste);
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

        /// <summary>XML pasted from elsewhere becomes objects.</summary>
        [Fact]
        public async Task PasteUsesSystemClipboardXmlWhenItParses()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.ReadClipboardText = () => Task.FromResult<string?>("<star x=\"1\" y=\"2\" />");

            PasteOutcome outcome = await vm.PasteFromClipboardAsync(100, 200);

            Assert.Equal(PasteOutcome.Pasted, outcome);
            Assert.Equal(2, vm.Document!.AllObjects.Count);
            Assert.Equal("star", vm.Selection.Primary!.Type);
            Assert.Equal("100", vm.Selection.Primary.GetAttr("x"));
        }

        /// <summary>Unrelated clipboard text leaves the internal buffer in charge.</summary>
        [Fact]
        public async Task PasteFallsBackToTheInternalBufferForUnrelatedText()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.Selection.Replace(vm.Document!.AllObjects[0]);
            vm.CopySelection();
            vm.ReadClipboardText = () => Task.FromResult<string?>("a shopping list");

            PasteOutcome outcome = await vm.PasteFromClipboardAsync(100, 200);

            Assert.Equal(PasteOutcome.Pasted, outcome);
            Assert.Equal(2, vm.Document.AllObjects.Count);
            Assert.Equal("bubble", vm.Selection.Primary!.Type);
        }

        /// <summary>
        /// A rejected paste reports itself instead of silently pasting the internal buffer.
        /// </summary>
        /// <remarks>
        /// Falling back here is the bug the design exists to avoid: from the user's side, Paste would have
        /// produced the wrong objects with no hint their XML was refused.
        /// </remarks>
        [Fact]
        public async Task PasteReportsRejectedXmlAndPastesNothing()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.Selection.Replace(vm.Document!.AllObjects[0]);
            vm.CopySelection();
            vm.ReadClipboardText = () => Task.FromResult<string?>("<star x=\"1\" y=\"2\"");

            PasteOutcome outcome = await vm.PasteFromClipboardAsync(100, 200);

            Assert.Equal(PasteOutcome.InvalidXml, outcome);
            _ = Assert.Single(vm.Document!.AllObjects);
        }

        /// <summary>With both stores empty there is nothing to report and nothing to do.</summary>
        [Fact]
        public async Task PasteWithNothingAnywhereReportsNothingToPaste()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.ReadClipboardText = () => Task.FromResult<string?>(null);

            PasteOutcome outcome = await vm.PasteFromClipboardAsync(100, 200);

            Assert.Equal(PasteOutcome.NothingToPaste, outcome);
            _ = Assert.Single(vm.Document!.AllObjects);
        }

        /// <summary>Paste stands down when neither clipboard has anything for it.</summary>
        /// <remarks>
        /// The system half is a cached observation, so on the desktop heads it starts false and Paste greys
        /// out until a refresh finds something. The browser starts it true instead and never narrows it,
        /// because reading there would prompt - these tests run on a desktop head, so they see the observed
        /// behaviour.
        /// </remarks>
        [Fact]
        public void PasteIsUnavailableWhenNeitherClipboardHasObjects()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");

            Assert.False(vm.HasClipboard);
            Assert.False(vm.CanPaste);
        }

        /// <summary>A refresh that finds objects on the system clipboard offers Paste with an empty buffer.</summary>
        [Fact]
        public async Task RefreshOffersPasteForSystemClipboardObjects()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.ReadClipboardText = () => Task.FromResult<string?>("<star x=\"1\" y=\"2\" />");
            Assert.False(vm.CanPaste);

            await vm.RefreshSystemClipboardStateAsync();

            Assert.False(vm.HasClipboard);
            Assert.True(vm.CanPaste);
        }

        /// <summary>Object-like XML stays actionable when malformed so Paste can explain the rejection.</summary>
        [Fact]
        public async Task RefreshOffersPasteForRejectedObjectXml()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.ReadClipboardText = () => Task.FromResult<string?>("<star x=\"1\" y=\"2\"");

            await vm.RefreshSystemClipboardStateAsync();

            Assert.True(vm.CanPaste);
        }

        /// <summary>A refresh that finds unrelated text takes Paste away again.</summary>
        [Fact]
        public async Task RefreshWithdrawsPasteWhenTheClipboardMovesOn()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.ReadClipboardText = () => Task.FromResult<string?>("<star x=\"1\" y=\"2\" />");
            await vm.RefreshSystemClipboardStateAsync();
            Assert.True(vm.CanPaste);

            vm.ReadClipboardText = () => Task.FromResult<string?>("a shopping list");
            await vm.RefreshSystemClipboardStateAsync();

            Assert.False(vm.CanPaste);
        }

        /// <summary>An older clipboard read cannot overwrite the result of a newer observation.</summary>
        [Fact]
        public async Task RefreshIgnoresAnOlderReadThatCompletesLast()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            TaskCompletionSource<string?> older = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<string?> newer = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int reads = 0;
            vm.ReadClipboardText = () => ++reads == 1 ? older.Task : newer.Task;

            Task olderRefresh = vm.RefreshSystemClipboardStateAsync();
            Task newerRefresh = vm.RefreshSystemClipboardStateAsync();
            newer.SetResult("<star x=\"1\" y=\"2\" />");
            await newerRefresh;
            Assert.True(vm.CanPaste);

            older.SetResult("a shopping list");
            await olderRefresh;

            Assert.True(vm.CanPaste);
        }

        /// <summary>
        /// Clear Clipboard stops Paste, even though our text is still on the system clipboard.
        /// </summary>
        /// <remarks>
        /// The OS clipboard is never modified - clearing someone's system clipboard from a level editor is
        /// out of scope - so the text we wrote is suppressed by value instead.
        /// </remarks>
        [Fact]
        public async Task ClearClipboardSuppressesTheTextWeWrote()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            string? written = null;
            vm.WriteClipboardText = text => { written = text; return Task.CompletedTask; };
            vm.Selection.Replace(vm.Document!.AllObjects[0]);
            await vm.CopySelectionAsync();
            vm.ReadClipboardText = () => Task.FromResult(written);

            vm.ClearClipboard();
            Assert.True(vm.CanPaste);
            PasteOutcome outcome = await vm.PasteFromClipboardAsync(100, 200);

            Assert.Equal(PasteOutcome.NothingToPaste, outcome);
            Assert.False(vm.CanPaste);
            _ = Assert.Single(vm.Document.AllObjects);
        }

        /// <summary>Copying elsewhere after a clear revives the external path.</summary>
        [Fact]
        public async Task ClearClipboardOnlySuppressesOurOwnText()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.WriteClipboardText = _ => Task.CompletedTask;
            vm.Selection.Replace(vm.Document!.AllObjects[0]);
            await vm.CopySelectionAsync();
            vm.ClearClipboard();
            vm.ReadClipboardText = () => Task.FromResult<string?>("<star x=\"1\" y=\"2\" />");

            PasteOutcome outcome = await vm.PasteFromClipboardAsync(100, 200);

            Assert.Equal(PasteOutcome.Pasted, outcome);
            Assert.Equal("star", vm.Selection.Primary!.Type);
        }

        /// <summary>A clipboard the platform will not hand over is not an error.</summary>
        [Fact]
        public async Task PasteSurvivesAClipboardThatThrows()
        {
            EditorViewModel vm = Load("<bubble x=\"10\" y=\"20\"/>");
            vm.Selection.Replace(vm.Document!.AllObjects[0]);
            vm.CopySelection();
            vm.ReadClipboardText = () => throw new InvalidOperationException("denied");

            PasteOutcome outcome = await vm.PasteFromClipboardAsync(100, 200);

            Assert.Equal(PasteOutcome.Pasted, outcome);
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
