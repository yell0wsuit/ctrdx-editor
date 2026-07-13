using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the palette's per-game section headers and divider flags.</summary>
    public class PaletteGroupingTests
    {
        private static EditorViewModel NewEditor() => new(new SpriteCache(new EmptyContentStore()));

        [Fact]
        public void FirstItemOfEachGroupGetsAHeaderAndLaterGroupsGetADivider()
        {
            EditorViewModel vm = NewEditor();
            vm.RefreshPalette();

            PaletteItemViewModel[] view = [.. vm.PaletteView];
            Assert.NotEmpty(view);

            // The very first item opens the first group with a header and no divider.
            Assert.True(view[0].ShowGroupHeader);
            Assert.False(view[0].ShowDivider);

            // The rocket is the first (only) item of the Experiments group: header + divider.
            PaletteItemViewModel rocket = view.Single(i => i.Element == "rocket");
            Assert.True(rocket.ShowGroupHeader);
            Assert.True(rocket.ShowDivider);
            Assert.Equal("Cut the Rope: Experiments", rocket.GroupName);

            // The item just before the rocket is the last base-group item: no header, no divider.
            int rocketIndex = System.Array.IndexOf(view, rocket);
            Assert.True(rocketIndex > 0);
            Assert.False(view[rocketIndex - 1].ShowGroupHeader);
            Assert.False(view[rocketIndex - 1].ShowDivider);
        }

        [Fact]
        public void SearchRecomputesHeadersForVisibleGroupsOnly()
        {
            EditorViewModel vm = NewEditor();
            vm.RefreshPalette();
            vm.PaletteSearchText = "rocket";

            PaletteItemViewModel[] view = [.. vm.PaletteView];
            PaletteItemViewModel rocket = Assert.Single(view);
            Assert.True(rocket.ShowGroupHeader);
            // Only one visible group, so the first visible item shows no divider.
            Assert.False(rocket.ShowDivider);
        }
    }
}
