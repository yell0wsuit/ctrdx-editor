using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the settings-dialog view model: presets, custom clamping, and mode.</summary>
    public class LevelSettingsViewModelTests
    {
        [Fact]
        public void NewModeDefaultsToFirstPresetAndEditableFlags()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();

            Assert.True(vm.FlagsEditable);
            Assert.False(vm.IsCustom);
            LevelSettings s = vm.ToSettings();
            Assert.Equal(320, s.Width);
            Assert.Equal(480, s.Height);
            Assert.Equal(1.0f, s.RopePhysicsSpeed);
            Assert.False(s.TwoParts);
        }

        [Fact]
        public void SelectingPresetSetsResolution()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedPreset = vm.Presets.Single(p => p is { Width: 640, Height: 960, IsCustom: false });

            LevelSettings s = vm.ToSettings();
            Assert.Equal(640, s.Width);
            Assert.Equal(960, s.Height);
        }

        [Fact]
        public void CustomResolutionIsClampedToBounds()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedPreset = vm.Presets.Single(p => p.IsCustom);
            Assert.True(vm.IsCustom);

            vm.CustomWidth = 10;
            vm.CustomHeight = 99999;
            LevelSettings s = vm.ToSettings();
            Assert.Equal(320, s.Width);
            Assert.Equal(9999, s.Height);

            vm.CustomHeight = 1;
            Assert.Equal(480, vm.ToSettings().Height);
        }

        [Fact]
        public void EditModePrefillsMatchingPresetAndLocksFlags()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(
                new LevelSettings(640, 480, 1.0f, 1, TwoParts: true, NightLevel: false));

            Assert.False(vm.FlagsEditable);
            Assert.False(vm.IsCustom);
            Assert.True(vm.TwoParts);
            Assert.Equal(640, vm.ToSettings().Width);
            Assert.Equal(480, vm.ToSettings().Height);
        }

        [Fact]
        public void EditModeWithNonPresetResolutionSelectsCustom()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(
                new LevelSettings(500, 700, 1.0f, 0, TwoParts: false, NightLevel: false));

            Assert.True(vm.IsCustom);
            Assert.Equal(500, vm.ToSettings().Width);
            Assert.Equal(700, vm.ToSettings().Height);
        }

        /// <summary>The special dropdown offers None (0), Default (1), and a Custom sentinel, defaulting to None.</summary>
        [Fact]
        public void SpecialOffersNoneDefaultAndCustom()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();

            Assert.Equal([0, 1], vm.SpecialOptions.Where(o => !o.IsCustom).Select(o => o.Value));
            Assert.Contains(vm.SpecialOptions, o => o.IsCustom);
            Assert.False(vm.IsSpecialCustom);
            Assert.Equal(0, vm.ToSettings().Special); // defaults to None
        }

        /// <summary>Selecting Custom writes the manually entered special value (clamped to 0..99).</summary>
        [Fact]
        public void CustomSpecialValueIsUsedAndClamped()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedSpecial = vm.SpecialOptions.Single(o => o.IsCustom);

            Assert.True(vm.IsSpecialCustom);
            vm.CustomSpecial = 7;
            Assert.Equal(7, vm.ToSettings().Special);

            vm.CustomSpecial = 500; // above the 99 cap
            Assert.Equal(99, vm.ToSettings().Special);
        }

        /// <summary>Confirm is blocked while a required numeric field is empty, and allowed once filled.</summary>
        [Fact]
        public void CanConfirmRequiresVisibleNumericFields()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            Assert.True(vm.CanConfirm); // defaults are all filled

            vm.RopePhysicsSpeed = null; // clearing the always-visible field blocks confirm
            Assert.False(vm.CanConfirm);
            vm.RopePhysicsSpeed = 1.0m;
            Assert.True(vm.CanConfirm);

            vm.SelectedPreset = vm.Presets.Single(p => p.IsCustom);
            vm.CustomWidth = null; // an empty custom width blocks confirm
            Assert.False(vm.CanConfirm);
            vm.CustomWidth = 640m;
            Assert.True(vm.CanConfirm);
        }

        /// <summary>A hidden custom field being null does not block confirm (only visible required fields count).</summary>
        [Fact]
        public void CanConfirmIgnoresHiddenCustomFields()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.CustomSpecial = null; // special is on None, so the custom input is hidden

            Assert.True(vm.CanConfirm);
        }

        /// <summary>An imported level's unlisted special value routes through Custom so it round-trips.</summary>
        [Fact]
        public void EditModeWithUnlistedSpecialSelectsCustom()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(
                new LevelSettings(320, 480, 1.0f, 3, TwoParts: false, NightLevel: false));

            Assert.True(vm.IsSpecialCustom);
            Assert.Equal(3m, vm.CustomSpecial);
            Assert.Equal(3, vm.ToSettings().Special);
        }
    }
}
