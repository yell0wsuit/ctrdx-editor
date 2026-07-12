using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the unsaved-changes signal that gates the new/open/close discard prompts.</summary>
    public class EditorModifiedStateTests
    {
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

        /// <summary>A freshly loaded level has no unsaved changes.</summary>
        [Fact]
        public void FreshlyLoadedLevelIsNotModified()
        {
            EditorViewModel vm = CreateLoadedViewModel();

            Assert.False(vm.IsModified);
        }

        /// <summary>Placing an object marks the level as modified.</summary>
        [Fact]
        public void EditMarksModified()
        {
            EditorViewModel vm = CreateLoadedViewModel();

            _ = vm.PlaceObject("star", 50, 60);

            Assert.True(vm.IsModified);
        }

        /// <summary>Marking the document saved clears the modified state until the next edit.</summary>
        [Fact]
        public void MarkSavedClearsModified()
        {
            EditorViewModel vm = CreateLoadedViewModel();
            _ = vm.PlaceObject("star", 50, 60);

            vm.MarkSaved();

            Assert.False(vm.IsModified);

            _ = vm.PlaceObject("target", 300, 200);

            Assert.True(vm.IsModified);
        }

        /// <summary>Undoing every edit back to the saved state clears the modified flag (no false positive).</summary>
        [Fact]
        public void UndoBackToSavedStateIsNotModified()
        {
            EditorViewModel vm = CreateLoadedViewModel();
            _ = vm.PlaceObject("star", 50, 60);
            Assert.True(vm.IsModified);

            vm.Undo();

            Assert.False(vm.IsModified);
        }

        /// <summary>With no level open there are no unsaved changes to warn about.</summary>
        [Fact]
        public void ClosedLevelIsNotModified()
        {
            EditorViewModel vm = CreateLoadedViewModel();
            _ = vm.PlaceObject("star", 50, 60);

            vm.CloseLevel();

            Assert.False(vm.IsModified);
        }

        private static EditorViewModel CreateLoadedViewModel()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(Level);
            return vm;
        }
    }
}
