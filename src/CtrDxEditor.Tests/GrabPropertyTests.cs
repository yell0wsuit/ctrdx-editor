using System;
using System.Globalization;
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

        /// <summary>Grab attribute should be a checkbox in the UI.</summary>
        [Fact]
        public void GrabBoolAttributeSurfacesAsCheckbox()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));

            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            AttributeFieldViewModel wheel = vm.Fields.Single(f => f.Name == "wheel");
            Assert.True(wheel.IsBool);
            Assert.False(wheel.BoolValue);

            wheel.BoolValue = true;
            Assert.Equal("true", grab.GetAttr("wheel"));
        }

        /// <summary>Auto-catch toggles radius disclosure and hides authored length.</summary>
        [Fact]
        public void AutoCatchToggleRevealsRadiusAndHidesLength()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.Contains(vm.Fields, f => f.Name == "length");
            Assert.DoesNotContain(vm.Fields, f => f.Name == "radius");

            AttributeFieldViewModel autoCatch = vm.Fields.Single(f => f.Name == "autoCatch");
            autoCatch.BoolValue = true;

            Assert.True(int.Parse(grab.GetAttr("radius")!, CultureInfo.InvariantCulture) > 0);
            Assert.DoesNotContain(vm.Fields, f => f.Name == "length");
            Assert.Contains(vm.Fields, f => f.Name == "radius");
        }

        /// <summary>Movable rail toggles rail sub-field disclosure.</summary>
        [Fact]
        public void MovableToggleRevealsRailFields()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.DoesNotContain(vm.Fields, f => f.Name == "moveOffset");

            vm.Fields.Single(f => f.Name == "movable").BoolValue = true;

            Assert.Contains(vm.Fields, f => f.Name == "moveVertical");
            Assert.Contains(vm.Fields, f => f.Name == "moveLength");
            Assert.Contains(vm.Fields, f => f.Name == "moveOffset");
        }

        /// <summary>Detached is a sub-option of suction cup.</summary>
        [Fact]
        public void DetachedShownOnlyWhenSuctionCupOn()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.DoesNotContain(vm.Fields, f => f.Name == "kicked");

            vm.Fields.Single(f => f.Name == "kickable").BoolValue = true;

            Assert.Contains(vm.Fields, f => f.Name == "kicked");
        }

        /// <summary>Gun mode disables hook variants and rope geometry controls.</summary>
        [Fact]
        public void GunDisablesHookVariantsAndRopeGeometry()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            vm.Fields.Single(f => f.Name == "gun").BoolValue = true;

            Assert.False(vm.Fields.Single(f => f.Name == "wheel").IsEnabled);
            Assert.False(vm.Fields.Single(f => f.Name == "spider").IsEnabled);
            Assert.False(vm.Fields.Single(f => f.Name == "kickable").IsEnabled);
            Assert.False(vm.Fields.Single(f => f.Name == "length").IsEnabled);
            Assert.False(vm.Fields.Single(f => f.Name == "autoCatch").IsEnabled);
            Assert.False(vm.Fields.Single(f => f.Name == "movable").IsEnabled);
        }

        /// <summary>An active hook variant disables gun without clearing the chosen variant.</summary>
        [Fact]
        public void ActiveHookVariantDisablesGun()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            vm.Fields.Single(f => f.Name == "spider").BoolValue = true;

            Assert.False(vm.Fields.Single(f => f.Name == "gun").IsEnabled);
        }

        /// <summary>Movable rail disables suction cup, matching game load behavior.</summary>
        [Fact]
        public void MovableRailDisablesSuctionCup()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            vm.Fields.Single(f => f.Name == "movable").BoolValue = true;

            Assert.False(vm.Fields.Single(f => f.Name == "kickable").IsEnabled);
        }

        /// <summary>Gun is not offered for split-candy levels.</summary>
        [Fact]
        public void GunHiddenInTwoPartsLevel()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: true, NightLevel: false));
            _ = vm.PlaceObject("candyL", 200, 200);
            _ = vm.PlaceObject("candyR", 300, 200);
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.DoesNotContain(vm.Fields, f => f.Name == "gun");
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

        /// <summary>Length and radius are magnitudes: their numeric box refuses negatives; x/y still allow them.</summary>
        [Fact]
        public void LengthAndRadiusForbidNegativesButCoordsAllow()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.Equal(0, vm.Fields.Single(f => f.Name == "length").NumericMinimum);
            Assert.Equal(-9999, vm.Fields.Single(f => f.Name == "x").NumericMinimum);

            vm.Fields.Single(f => f.Name == "autoCatch").BoolValue = true;
            Assert.Equal(0, vm.Fields.Single(f => f.Name == "radius").NumericMinimum);
        }

        /// <summary>Auto-catch greys out Attach-to: an auto-catch grab binds candy at runtime, not by number.</summary>
        [Fact]
        public void AutoCatchGraysOutAttachTo()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
            _ = vm.PlaceObject("candy", 300, 200);
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.True(vm.Fields.Single(f => f.Name == "attachTo").IsEnabled);

            vm.Fields.Single(f => f.Name == "autoCatch").BoolValue = true;

            Assert.False(vm.Fields.Single(f => f.Name == "attachTo").IsEnabled);
        }

        /// <summary>Gun greys out the Attach-to control rather than removing it, avoiding a layout shift.</summary>
        [Fact]
        public void GunGraysOutAttachToInsteadOfHiding()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, 0, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
            _ = vm.PlaceObject("candy", 300, 200);
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.True(vm.Fields.Single(f => f.Name == "attachTo").IsEnabled);

            vm.Fields.Single(f => f.Name == "gun").BoolValue = true;

            Assert.Contains(vm.Fields, f => f.Name == "attachTo");
            Assert.False(vm.Fields.Single(f => f.Name == "attachTo").IsEnabled);
        }
    }
}
