using System.Linq;

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
            public System.Threading.Tasks.Task<bool> ExistsAsync(string relPath)
            {
                return System.Threading.Tasks.Task.FromResult(false);
            }

            public System.Threading.Tasks.Task<byte[]> ReadBytesAsync(string relPath)
            {
                return System.Threading.Tasks.Task.FromResult(System.Array.Empty<byte>());
            }

            public System.Threading.Tasks.Task<string> ReadTextAsync(string relPath)
            {
                return System.Threading.Tasks.Task.FromResult("");
            }

            public System.Threading.Tasks.Task<bool> IsPopulatedAsync()
            {
                return System.Threading.Tasks.Task.FromResult(false);
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
    }
}
