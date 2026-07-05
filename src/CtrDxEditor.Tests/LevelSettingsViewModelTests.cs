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

        /// <summary>The special dropdown offers only None (0) and Default (1) for a normal level.</summary>
        [Fact]
        public void SpecialOffersNoneAndDefaultOnly()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();

            Assert.Equal([0, 1], vm.SpecialOptions.Select(o => o.Value));
            Assert.Equal(0, vm.ToSettings().Special); // defaults to None
        }

        /// <summary>An imported level's unlisted special value is preserved via an added option so it round-trips.</summary>
        [Fact]
        public void EditModePreservesUnlistedSpecialValue()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(
                new LevelSettings(320, 480, 1.0f, 3, TwoParts: false, NightLevel: false));

            Assert.Contains(vm.SpecialOptions, o => o.Value == 3);
            Assert.Equal(3, vm.SelectedSpecial.Value);
            Assert.Equal(3, vm.ToSettings().Special);
        }
    }
}
