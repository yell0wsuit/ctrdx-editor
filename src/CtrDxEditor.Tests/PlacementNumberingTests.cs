using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests 0-based candy/bulb key assignment when placing objects.</summary>
    public class PlacementNumberingTests
    {
        private sealed class EmptyStore : IContentStore
        {
            public Task<bool> ExistsAsync(string relPath)
            {
                return Task.FromResult(false);
            }

            public Task<byte[]> ReadBytesAsync(string relPath)
            {
                return Task.FromResult(System.Array.Empty<byte>());
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
            EditorViewModel vm = new(new SpriteCache(new EmptyStore()));
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            return vm;
        }

        /// <summary>Placed candies receive sequential 0-based candy numbers.</summary>
        [Fact]
        public void FirstCandyGetsZeroSecondGetsOne()
        {
            EditorViewModel vm = Vm();

            LevelObject first = vm.PlaceObject("candy", 100, 100)!;
            LevelObject second = vm.PlaceObject("candy", 200, 200)!;

            Assert.Equal("0", first.GetAttr("candyNumber"));
            Assert.Equal("1", second.GetAttr("candyNumber"));
        }

        /// <summary>The first placed light bulb receives bulb number zero.</summary>
        [Fact]
        public void FirstBulbGetsZero()
        {
            EditorViewModel vm = Vm();

            LevelObject bulb = vm.PlaceObject("lightBulb", 50, 50)!;

            Assert.Equal("0", bulb.GetAttr("bulbNumber"));
        }

        /// <summary>Adding another candy backfills a legacy unnumbered primary candy.</summary>
        [Fact]
        public void UnnumberedPrimaryIsBackfilledWhenSecondCandyAdded()
        {
            EditorViewModel vm = Vm();
            // Simulate a legacy unnumbered primary by placing then clearing its key.
            LevelObject primary = vm.PlaceObject("candy", 100, 100)!;
            primary.Element.SetAttributeValue("candyNumber", null);

            LevelObject second = vm.PlaceObject("candy", 200, 200)!;

            Assert.Equal("0", primary.GetAttr("candyNumber"));
            Assert.Equal("1", second.GetAttr("candyNumber"));
        }
    }
}
