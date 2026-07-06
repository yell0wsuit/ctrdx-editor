using System;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests that the palette shows candy/light objects appropriate to the level settings.</summary>
    public class EditorPaletteGatingTests
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

        private static EditorViewModel Vm()
        {
            return new(new SpriteCache(new EmptyStore()));
        }

        private static bool PaletteHas(EditorViewModel vm, string element)
        {
            return vm.Palette.Any(p => p.Element == element);
        }

        private static PaletteItemViewModel PaletteItem(EditorViewModel vm, string element)
        {
            return vm.Palette.Single(p => p.Element == element);
        }

        /// <summary>A single-candy level offers whole candy and the bulb, but not the split-candy halves.</summary>
        [Fact]
        public void SingleCandyLevelShowsCandyAndBulbNotHalves()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));

            Assert.True(PaletteHas(vm, "candy"));
            Assert.False(PaletteHas(vm, "candyL"));
            Assert.False(PaletteHas(vm, "candyR"));
            Assert.True(PaletteHas(vm, "lightBulb"));
        }

        /// <summary>A two-part night level offers the split-candy halves and the bulb, but not whole candy.</summary>
        [Fact]
        public void TwoPartNightLevelShowsHalvesAndBulbNotCandy()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(320, 480, 1.0f, 0, TwoParts: true, NightLevel: true));

            Assert.False(PaletteHas(vm, "candy"));
            Assert.True(PaletteHas(vm, "candyL"));
            Assert.True(PaletteHas(vm, "candyR"));
            Assert.True(PaletteHas(vm, "lightBulb"));
        }

        /// <summary>Updating level settings rewrites the document's resolution and special value.</summary>
        [Fact]
        public void UpdateLevelSettingsWritesResolution()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(320, 480, 1.0f, 0, TwoParts: false, NightLevel: false));

            vm.UpdateLevelSettings(new LevelSettings(640, 960, 1.0f, 2, TwoParts: false, NightLevel: false));

            Assert.Equal(640, vm.Document!.Width);
            Assert.Equal(960, vm.Document!.Height);
            Assert.Equal(2, vm.Document!.Special);
        }

        /// <summary>Turning on two-part mode auto-creates the halves, so their palette items gray out.</summary>
        [Fact]
        public void SwitchingBackToHalfCandyGraysOutAutoCreatedHalves()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 101, 170);

            vm.UpdateLevelSettings(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));

            Assert.False(PaletteItem(vm, "candyL").Enabled);
            Assert.False(PaletteItem(vm, "candyR").Enabled);
        }

        /// <summary>Auto-creating the halves raises one mutation and adds candyL/candyR to the document.</summary>
        [Fact]
        public void SwitchingBackToHalfCandyNotifiesCanvasRefresh()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 101, 170);
            int mutations = 0;
            vm.ObjectMutated += () => mutations++;

            vm.UpdateLevelSettings(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));

            Assert.Equal(1, mutations);
            Assert.Contains(vm.Document!.Objects, o => o.Type == "candyL");
            Assert.Contains(vm.Document.Objects, o => o.Type == "candyR");
        }

        /// <summary>The half-candy palette allows exactly one of each half; placing one disables it.</summary>
        [Fact]
        public void HalfCandyPaletteAllowsOneOfEachHalf()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));

            _ = vm.PlaceObject("candyL", 320, 240);

            Assert.False(PaletteItem(vm, "candyL").Enabled);
            Assert.True(PaletteItem(vm, "candyR").Enabled);

            _ = vm.PlaceObject("candyR", 400, 300);

            Assert.False(PaletteItem(vm, "candyL").Enabled);
            Assert.False(PaletteItem(vm, "candyR").Enabled);
        }
    }
}
