using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests undo and redo behavior in the editor view model.</summary>
    public class EditorUndoRedoTests
    {
        private sealed class EmptyStore : IContentStore
        {
            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(false);
            }

            public Task<byte[]> ReadBytesAsync(string relPath)
            {
                return Task.FromResult(Array.Empty<byte>());
            }

            public Task<string> ReadTextAsync(string relPath)
            {
                return Task.FromResult("");
            }

            public Task<bool> IsPopulatedAsync()
            {
                return Task.FromResult(false);
            }
        }

        private const string Level = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings">
                <map gridSize="32" width="640" height="480" />
            </layer>
            <layer name="Objects">
                <candy x="100" y="100" />
            </layer>
        </map>
        """;

        private const string MultiLayerLevel = """
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings">
                <map gridSize="32" width="640" height="480" />
            </layer>
            <layer name="first">
                <candy x="100" y="100" />
            </layer>
            <layer name="second">
                <star x="200" y="200" timeout="-1" />
            </layer>
        </map>
        """;

        /// <summary>Verifies that undoing and redoing placement removes and restores the object.</summary>
        [Fact]
        public void UndoAndRedoRestorePlacedObject()
        {
            EditorViewModel vm = CreateLoadedViewModel();

            _ = vm.PlaceObject("star", 50, 60);

            Assert.True(vm.CanUndo);
            Assert.False(vm.CanRedo);
            Assert.Equal(2, vm.Document!.AllObjects.Count);

            vm.Undo();

            _ = Assert.Single(vm.Document.AllObjects);
            Assert.False(vm.CanUndo);
            Assert.True(vm.CanRedo);

            vm.Redo();

            Assert.Equal(2, vm.Document.AllObjects.Count);
            Assert.Equal("star", vm.Document.AllObjects[1].Type);
            Assert.True(vm.CanUndo);
            Assert.False(vm.CanRedo);
        }

        /// <summary>Verifies that making a new edit after undo replaces the redo chain.</summary>
        [Fact]
        public void NewEditAfterUndoClearsRedo()
        {
            EditorViewModel vm = CreateLoadedViewModel();
            _ = vm.PlaceObject("star", 50, 60);
            vm.Undo();

            _ = vm.PlaceObject("target", 300, 200);

            Assert.True(vm.CanUndo);
            Assert.False(vm.CanRedo);
            Assert.Equal(["candy", "target"], vm.Document!.AllObjects.Select(o => o.Type));
        }

        /// <summary>Verifies that undoing deletion restores the object and selection.</summary>
        [Fact]
        public void UndoRestoresDeletedSelection()
        {
            EditorViewModel vm = CreateLoadedViewModel();
            LevelObject candy = vm.Document!.AllObjects[0];
            vm.SelectedObject = candy;

            vm.DeleteSelected();

            Assert.Empty(vm.Document.AllObjects);

            vm.Undo();

            LevelObject restored = Assert.Single(vm.Document.AllObjects);
            Assert.Equal("candy", restored.Type);
            Assert.Same(restored.Element, vm.SelectedObject!.Element);
        }

        /// <summary>Verifies that property-panel edits are undoable.</summary>
        [Fact]
        public void UndoRestoresPropertyFieldEdit()
        {
            EditorViewModel vm = CreateLoadedViewModel();
            vm.SelectedObject = vm.Document!.AllObjects[0];
            AttributeFieldViewModel xField = vm.Fields.Single(f => f.Name == "x");

            xField.Value = "250";

            Assert.Equal(250, vm.Document.AllObjects[0].X);

            vm.Undo();

            Assert.Equal(100, vm.Document.AllObjects[0].X);
            Assert.Equal("100", vm.Fields.Single(f => f.Name == "x").Value);
        }

        /// <summary>Verifies that direct document edits can be grouped into one undo entry.</summary>
        [Fact]
        public void UndoTransactionCoalescesDirectMutations()
        {
            EditorViewModel vm = CreateLoadedViewModel();
            LevelObject candy = vm.Document!.AllObjects[0];
            vm.SelectedObject = candy;

            vm.BeginUndoTransaction();
            candy.X = 120;
            candy.Y = 140;
            vm.CompleteUndoTransaction();

            vm.Undo();

            Assert.Equal(100, vm.Document.AllObjects[0].X);
            Assert.Equal(100, vm.Document.AllObjects[0].Y);
        }

        /// <summary>Verifies the undo stack keeps only the most recent snapshots.</summary>
        [Fact]
        public void UndoHistoryIsCapped()
        {
            const int historyLimit = 100;
            EditorViewModel vm = CreateLoadedViewModel();
            vm.SelectedObject = vm.Document!.AllObjects[0];
            AttributeFieldViewModel xField = vm.Fields.Single(f => f.Name == "x");

            for (int i = 0; i < historyLimit + 5; i++)
            {
                xField.Value = (200 + i).ToString(CultureInfo.InvariantCulture);
            }

            int undoCount = 0;
            while (vm.CanUndo)
            {
                vm.Undo();
                undoCount++;
            }

            Assert.Equal(historyLimit, undoCount);
        }

        /// <summary>Preserves editor-only tutorial auto-width state across undo and redo.</summary>
        [Fact]
        public void UndoAndRedoRestoreTutorialAutoWidthState()
        {
            EditorViewModel vm = CreateLoadedViewModel();
            LevelObject text = vm.PlaceObject("tutorialText", 20, 30)!;
            AttributeFieldViewModel autoWidth = vm.Fields.Single(f => f.Name == "autoWidth");

            autoWidth.Value = "false";
            Assert.False(TutorialObject.IsAutoWidth(vm.Document!.AllObjects[1]));

            vm.Undo();
            Assert.True(TutorialObject.IsAutoWidth(vm.Document.AllObjects[1]));

            vm.Redo();
            Assert.False(TutorialObject.IsAutoWidth(vm.Document.AllObjects[1]));
        }

        /// <summary>Groups a canvas tutorial-width drag into one undoable edit.</summary>
        [Fact]
        public void UndoAndRedoRestoreTutorialCanvasWidthResize()
        {
            EditorViewModel vm = CreateLoadedViewModel();
            LevelObject text = vm.PlaceObject("tutorialText", 20, 30)!;
            string initialWidth = text.GetAttr("width")!;

            vm.BeginUndoTransaction();
            TutorialTextResize.ApplyDrag(text, 220);
            vm.CompleteUndoTransaction();

            Assert.Equal("200", vm.Document!.AllObjects[1].GetAttr("width"));
            Assert.False(TutorialObject.IsAutoWidth(vm.Document.AllObjects[1]));

            vm.Undo();
            Assert.Equal(initialWidth, vm.Document.AllObjects[1].GetAttr("width"));
            Assert.True(TutorialObject.IsAutoWidth(vm.Document.AllObjects[1]));

            vm.Redo();
            Assert.Equal("200", vm.Document.AllObjects[1].GetAttr("width"));
            Assert.False(TutorialObject.IsAutoWidth(vm.Document.AllObjects[1]));
        }

        /// <summary>Restores selection to the same structural object in a later layer after undo.</summary>
        [Fact]
        public void UndoRestoresSelectionInSecondLayer()
        {
            EditorViewModel vm = CreateLoadedViewModel(MultiLayerLevel);
            LevelObject star = vm.Document!.Layers[1].Objects[0];
            vm.SelectedObject = star;
            AttributeFieldViewModel xField = vm.Fields.Single(f => f.Name == "x");

            xField.Value = "250";
            vm.Undo();

            LevelObject restored = vm.Document.Layers[1].Objects[0];
            Assert.Equal(200, restored.X);
            Assert.Same(restored.Element, vm.SelectedObject!.Element);
        }

        private static EditorViewModel CreateLoadedViewModel()
        {
            return CreateLoadedViewModel(Level);
        }

        private static EditorViewModel CreateLoadedViewModel(string xml)
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.LoadLevelXml(xml);
            return vm;
        }
    }
}
