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

        /// <summary>Single-candy grabs with only one candy have no raw part or attachTo field.</summary>
        [Fact]
        public void FullCandyGrabHasNoPartOrAttachToWhenSingleCandy()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 300, 300);

            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.DoesNotContain(vm.Fields, f => f.Name == "part");
            Assert.DoesNotContain(vm.Fields, f => f.Name == "attachTo");
        }

        /// <summary>Two-part grabs expose an attachTo choice for left and right candy halves.</summary>
        [Fact]
        public void HalfCandyGrabShowsAttachToLeftRight()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));
            _ = vm.PlaceObject("candyL", 200, 200);
            _ = vm.PlaceObject("candyR", 300, 200);

            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.DoesNotContain(vm.Fields, f => f.Name == "part");
            AttributeFieldViewModel attach = vm.Fields.Single(f => f.Name == "attachTo");
            Assert.Equal(["Candy (left)", "Candy (right)"], attach.EnumOptions!.Select(o => o.Label));
            // Default part "L" (applied on placement) selects the left option.
            Assert.Equal("Candy (left)", attach.SelectedOption!.Label);

            attach.SelectedOption = attach.EnumOptions!.Single(o => o.Label == "Candy (right)");

            Assert.Equal("R", grab.GetAttr("part"));
        }

        /// <summary>Multi-candy grabs expose attachTo choices that write candyNumber.</summary>
        [Fact]
        public void MultiCandyGrabAttachToBindsByNumber()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
            _ = vm.PlaceObject("candy", 300, 200);

            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            AttributeFieldViewModel attach = vm.Fields.Single(f => f.Name == "attachTo");
            attach.SelectedOption = attach.EnumOptions!.Single(o => o.Label == "Candy 1");

            Assert.Equal("1", grab.GetAttr("candyNumber"));
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
