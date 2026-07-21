using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the unsaved-changes signal that gates the new/open/close discard prompts.</summary>
    public class EditorModifiedStateTests
    {
        private static string SpikeLevel(string element, string size)
        {
            return $"""
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings">
                <map gridSize="32" width="640" height="480" />
            </layer>
            <layer name="Objects">
                <{element} x="100" y="100" size="{size}" />
            </layer>
        </map>
        """;
        }

        /// <summary>A level whose spike tag disagrees with its size attribute loads normalized and pending save.</summary>
        [Fact]
        public void LoadingMismatchedSpikeTagNormalizesLiveAndMarksModified()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));

            vm.LoadLevelXml(SpikeLevel("spike2", "3"));

            Assert.Equal("spike3", vm.Document!.AllObjects[0].Type);
            Assert.True(vm.IsModified);
        }

        /// <summary>A level whose spike tag already matches its size attribute loads clean.</summary>
        [Fact]
        public void LoadingConsistentSpikeTagIsNotModified()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));

            vm.LoadLevelXml(SpikeLevel("spike3", "3"));

            Assert.False(vm.IsModified);
        }

        private static string CandyLevel(string x, string y)
        {
            return $"""
        <?xml version='1.0' encoding='utf-8'?>
        <map>
            <layer name="settings">
                <map gridSize="32" width="640" height="480" />
            </layer>
            <layer name="Objects">
                <candy x="{x}" y="{y}" />
            </layer>
        </map>
        """;
        }

        /// <summary>A level authored with decimal coordinates loads truncated and pending save.</summary>
        [Fact]
        public void LoadingDecimalCoordinatesTruncatesLiveAndMarksModified()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));

            vm.LoadLevelXml(CandyLevel("100.9", "-40.5"));

            LevelDocument document = Assert.IsType<LevelDocument>(vm.Document);
            LevelObject candy = document.AllObjects[0];
            Assert.Equal("100", candy.GetAttr("x"));
            Assert.Equal("-40", candy.GetAttr("y"));
            Assert.True(vm.IsModified);
        }

        /// <summary>A level with integer coordinates loads clean.</summary>
        [Fact]
        public void LoadingIntegerCoordinatesIsNotModified()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));

            vm.LoadLevelXml(CandyLevel("100", "-40"));

            Assert.False(vm.IsModified);
        }

        /// <summary>Duplicate ordinary layers load with stable names and remain pending save.</summary>
        [Fact]
        public void LoadingDuplicateLayerNamesNormalizesLiveAndMarksModified()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));

            vm.LoadLevelXml("""
                <map>
                    <layer name="settings"><map width="640" height="480" /></layer>
                    <layer name="Objects" />
                    <layer name="Objects" />
                    <layer name="Objects-2" />
                </map>
                """);

            LevelDocument document = Assert.IsType<LevelDocument>(vm.Document);
            Assert.Equal(["Objects", "Objects-3", "Objects-2"],
                document.Layers.Select(layer => layer.Name));
            Assert.True(vm.IsModified);
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

        /// <summary>
        /// The baseline the Review Changes dialog diffs against is the same snapshot IsModified compares,
        /// so the dialog can never disagree with the title bar's dirty marker.
        /// </summary>
        [Fact]
        public void SavedBaselineMatchesModifiedState()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));

            Assert.Null(vm.SavedBaselineXml);

            vm.LoadLevelXml(SpikeLevel("spike3", "3"));

            Assert.NotNull(vm.SavedBaselineXml);
            Assert.False(vm.IsModified);
            Assert.Equal(vm.SavedBaselineXml, vm.ToXml());
        }

        private static EditorViewModel CreateLoadedViewModel()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.LoadLevelXml(Level);
            return vm;
        }
    }
}
