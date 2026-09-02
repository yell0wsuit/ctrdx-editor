using System;
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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            return vm;
        }

        /// <summary>An empty search leaves every source palette item visible.</summary>
        [Fact]
        public void EmptySearchShowsEveryPaletteItem()
        {
            EditorViewModel vm = Vm();
            Assert.Equal(vm.Palette.Count, vm.PaletteView.Count);
        }

        /// <summary>A whitespace-only search leaves every source palette item visible.</summary>
        [Fact]
        public void WhitespaceSearchShowsEveryPaletteItem()
        {
            EditorViewModel vm = Vm();

            vm.PaletteSearchText = " \t ";

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
            Assert.Contains(vm.PaletteView, i =>
                i.DisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase));
            Assert.All(vm.PaletteView, i =>
                Assert.True(
                    i.DisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase)
                    || i.Element.Contains(needle, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>Search also matches the raw XML element name, not just the display name.</summary>
        [Fact]
        public void SearchMatchesXmlElementName()
        {
            EditorViewModel vm = Vm();

            // "bouncer1" is the element name; its display name has no digit, so a match here
            // can only come from the element-name comparison.
            vm.PaletteSearchText = "bouncer1";

            Assert.Contains(vm.PaletteView, i => i.Element == "bouncer1");
            Assert.All(vm.PaletteView, i =>
                Assert.Contains("bouncer1", i.Element, StringComparison.OrdinalIgnoreCase));
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

            Assert.NotEmpty(vm.PaletteView);
            Assert.All(vm.PaletteView, i =>
                Assert.Contains(needle, i.DisplayName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Closing a level clears both the source palette and its filtered view.</summary>
        [Fact]
        public void CloseLevelClearsPaletteView()
        {
            EditorViewModel vm = Vm();
            Assert.NotEmpty(vm.PaletteView);

            vm.CloseLevel();

            Assert.Empty(vm.Palette);
            Assert.Empty(vm.PaletteView);
        }
    }
}
