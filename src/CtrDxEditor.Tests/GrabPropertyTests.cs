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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: true, NightLevel: false));
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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
            _ = vm.PlaceObject("candy", 300, 200);

            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            AttributeFieldViewModel attach = vm.Fields.Single(f => f.Name == "attachTo");
            attach.SelectedOption = attach.EnumOptions!.Single(o => o.Label == "Candy 1");

            Assert.Equal("1", grab.GetAttr("candyNumber"));
        }

        /// <summary>Candy and bulb binding keys stay internal instead of becoming property fields.</summary>
        [Theory]
        [InlineData("candy", "candyNumber")]
        [InlineData("lightBulb", "bulbNumber")]
        public void CandyAndBulbIdsAreNotEditablePropertyFields(string element, string attribute)
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: true));

            LevelObject obj = vm.PlaceObject(element, 100, 120)!;
            vm.SelectedObject = obj;

            Assert.NotNull(obj.GetAttr(attribute));
            Assert.DoesNotContain(vm.Fields, f => f.Name == attribute);
        }

        /// <summary>Grab attribute should be a checkbox in the UI.</summary>
        [Fact]
        public void GrabBoolAttributeSurfacesAsCheckbox()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));

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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
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

        /// <summary>Star timed disclosure keeps untimed stars compact, and toggling on authors a duration.</summary>
        [Fact]
        public void StarTimedToggleRevealsDuration()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            LevelObject star = vm.PlaceObject("star", 100, 120)!;
            vm.SelectedObject = star;

            AttributeFieldViewModel timed = vm.Fields.Single(f => f.Name == "timed");
            Assert.True(timed.IsBool);
            Assert.False(timed.BoolValue);
            Assert.DoesNotContain(vm.Fields, f => f.Name == "timeout");
            Assert.Equal("-1", star.GetAttr("timeout"));

            timed.BoolValue = true;

            Assert.Equal("5", star.GetAttr("timeout"));
            AttributeFieldViewModel duration = vm.Fields.Single(f => f.Name == "timeout");
            Assert.Equal(1, duration.NumericMinimum);
            Assert.True(duration.AllowsDecimal);
        }

        /// <summary>A loaded timeout of zero reads as untimed and is not rewritten by field construction.</summary>
        [Fact]
        public void StarTimeoutZeroReadsAsUntimed()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            LevelObject star = vm.PlaceObject("star", 100, 120)!;
            star.SetAttr("timeout", "0");
            vm.SelectedObject = star;

            Assert.False(vm.Fields.Single(f => f.Name == "timed").BoolValue);
            Assert.DoesNotContain(vm.Fields, f => f.Name == "timeout");
            Assert.Equal("0", star.GetAttr("timeout"));
        }

        /// <summary>Movable rail toggles rail sub-field disclosure.</summary>
        [Fact]
        public void MovableToggleRevealsRailFields()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            vm.Fields.Single(f => f.Name == "spider").BoolValue = true;

            Assert.False(vm.Fields.Single(f => f.Name == "gun").IsEnabled);
        }

        /// <summary>Movable rail disables hook variants that cannot coexist with rail art.</summary>
        [Fact]
        public void MovableRailDisablesRailBlockingVariants()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            vm.Fields.Single(f => f.Name == "movable").BoolValue = true;

            Assert.False(vm.Fields.Single(f => f.Name == "wheel").IsEnabled);
            Assert.False(vm.Fields.Single(f => f.Name == "gun").IsEnabled);
            Assert.False(vm.Fields.Single(f => f.Name == "kickable").IsEnabled);
        }

        /// <summary>Wheel and gun grabs clear movable rail geometry so their sprites render in the canvas.</summary>
        [Theory]
        [InlineData("wheel")]
        [InlineData("gun")]
        public void RailBlockingVariantsClearMoveLength(string fieldName)
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            vm.Fields.Single(f => f.Name == "movable").BoolValue = true;
            Assert.Equal("100", grab.GetAttr("moveLength"));

            vm.Fields.Single(f => f.Name == fieldName).BoolValue = true;

            Assert.Equal("-1", grab.GetAttr("moveLength"));
            Assert.False(vm.Fields.Single(f => f.Name == "movable").IsEnabled);
            Assert.DoesNotContain(vm.Fields, f => f.Name == "moveOffset");
        }

        /// <summary>Suction cup grabs disable the movable toggle so move rails cannot replace suction art.</summary>
        [Fact]
        public void SuctionCupDisablesMovableRail()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            vm.Fields.Single(f => f.Name == "kickable").BoolValue = true;

            Assert.False(vm.Fields.Single(f => f.Name == "movable").IsEnabled);
        }

        /// <summary>Gun can still be authored in split-candy levels; only aim animation is gated.</summary>
        [Fact]
        public void GunEnabledInTwoPartsLevel()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: true, NightLevel: false));
            _ = vm.PlaceObject("candyL", 200, 200);
            _ = vm.PlaceObject("candyR", 300, 200);
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.True(vm.Fields.Single(f => f.Name == "gun").IsEnabled);
        }

        /// <summary>Gun targeting follows DX's single-primary-candy path, so one full candy enables it.</summary>
        [Fact]
        public void SingleFullCandyEnablesGunTargeting()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.True(vm.Fields.Single(f => f.Name == "gun").IsEnabled);
        }

        /// <summary>Gun can be authored before placing candy; aim movement activates once one full candy exists.</summary>
        [Fact]
        public void EmptyFullCandyLevelEnablesGunAuthoring()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.True(vm.Fields.Single(f => f.Name == "gun").IsEnabled);
        }

        /// <summary>Gun can still be authored in multi-candy levels; only aim animation is gated.</summary>
        [Fact]
        public void MultiCandyEnablesGunAuthoring()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
            _ = vm.PlaceObject("candy", 300, 200);
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            vm.SelectedObject = grab;

            Assert.True(vm.Fields.Single(f => f.Name == "gun").IsEnabled);
        }

        /// <summary>Selected single-candy objects keep their internal id without exposing an input.</summary>
        [Fact]
        public void SelectedCandyHidesCandyNumberField()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));

            LevelObject candy = vm.PlaceObject("candy", 100, 120)!;
            vm.SelectedObject = candy;

            Assert.Equal("0", candy.GetAttr("candyNumber"));
            Assert.DoesNotContain(vm.Fields, f => f.Name == "candyNumber");
        }

        /// <summary>Length and radius are magnitudes: their numeric box refuses negatives; x/y still allow them.</summary>
        [Fact]
        public void LengthAndRadiusForbidNegativesButCoordsAllow()
        {
            EditorViewModel vm = Vm();
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
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
            vm.NewLevel(new LevelSettings(640, 480, 1.0f, TwoParts: false, NightLevel: false));
            _ = vm.PlaceObject("candy", 200, 200);
            _ = vm.PlaceObject("lightBulb", 300, 200);
            LevelObject grab = vm.PlaceObject("grab", 100, 120)!;
            grab.SetAttr("bindBulb", "true");
            vm.SelectedObject = grab;

            Assert.True(vm.Fields.Single(f => f.Name == "attachTo").IsEnabled);

            vm.Fields.Single(f => f.Name == "gun").BoolValue = true;

            Assert.Contains(vm.Fields, f => f.Name == "attachTo");
            Assert.False(vm.Fields.Single(f => f.Name == "attachTo").IsEnabled);
        }
    }
}
