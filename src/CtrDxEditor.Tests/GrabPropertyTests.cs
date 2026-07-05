using System;
using System.Linq;
using System.Threading.Tasks;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests grab property visibility and defaults for single-candy vs half-candy levels.</summary>
    public class GrabPropertyTests
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

        [Fact]
        public void FullCandyGrabPropertiesHidePart()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));

            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.DoesNotContain(vm.Fields, f => f.Name == "part");
            Assert.Null(grab.GetAttr("part"));
        }

        [Fact]
        public void HalfCandyGrabDefaultsPartToLeftAndShowsPart()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));

            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            AttributeFieldViewModel part = vm.Fields.Single(f => f.Name == "part");
            Assert.Equal("L", grab.GetAttr("part"));
            Assert.Equal("L", part.Value);
            Assert.NotNull(part.EnumValues);
            Assert.Equal(["L", "R"], part.EnumValues);
            Assert.NotNull(part.EnumOptions);
            Assert.Equal(["left", "right"], part.EnumOptions.Select(o => o.Label));

            part.SelectedOption = part.EnumOptions.Single(o => o.Label == "right");

            Assert.Equal("R", grab.GetAttr("part"));
            Assert.Equal("R", part.Value);
        }

        /// <summary>Selected single-candy objects expose the editable candyNumber field.</summary>
        [Fact]
        public void SelectedCandyShowsCandyNumberField()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));

            LevelObject candy = vm.PlaceObject("candy", 100, 120)!;
            vm.SelectedObject = candy;

            AttributeFieldViewModel field = vm.Fields.Single(f => f.Name == "candyNumber");
            Assert.Equal("0", field.Value);
        }
    }
}
