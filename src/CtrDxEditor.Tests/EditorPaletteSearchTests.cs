using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests object-palette filtering and game identity exposed by the editor view model.</summary>
    public class EditorPaletteSearchTests
    {
        private static EditorViewModel Vm()
        {
            EditorViewModel vm = new(new SpriteCache(new EmptyContentStore()));
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            return vm;
        }

        /// <summary>An empty search leaves every source palette item visible.</summary>
        [Fact]
        public void EmptySearchShowsEveryPaletteItem()
        {
            EditorViewModel vm = Vm();
            Assert.Equal(vm.Palette.Count, vm.PaletteView.Count);
        }

        /// <summary>Search matches display-name substrings without regard to case.</summary>
        [Fact]
        public void SearchFiltersByDisplayNameCaseInsensitively()
        {
            EditorViewModel vm = Vm();
            string sample = vm.Palette.First().DisplayName;
            string needle = sample[..2].ToUpperInvariant();

            vm.PaletteSearchText = needle;

            Assert.NotEmpty(vm.PaletteView);
            Assert.All(vm.PaletteView, i =>
                Assert.Contains(needle, i.DisplayName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>A search with no display-name match produces an empty view.</summary>
        [Fact]
        public void NonMatchingSearchYieldsEmptyView()
        {
            EditorViewModel vm = Vm();
            vm.PaletteSearchText = "zzz-no-such-object";
            Assert.Empty(vm.PaletteView);
        }

        /// <summary>Refreshing the source palette reapplies the current search filter.</summary>
        [Fact]
        public void RefreshPaletteKeepsActiveFilterApplied()
        {
            EditorViewModel vm = Vm();
            string needle = vm.Palette.First().DisplayName;
            vm.PaletteSearchText = needle;

            vm.RefreshPalette();

            Assert.All(vm.PaletteView, i =>
                Assert.Contains(needle, i.DisplayName, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The palette reports the game whose object catalog it displays.</summary>
        [Fact]
        public void CurrentGameNameIsCutTheRope()
        {
            Assert.Equal("Cut the Rope", Vm().CurrentGameName);
        }
    }
}
