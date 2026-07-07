using System;
using System.Linq;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the settings-dialog view model: presets, custom clamping, and mode.</summary>
    public class LevelSettingsViewModelTests
    {
        /// <summary>New mode starts on the first preset (320×480) with flags editable and default settings.</summary>
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

        /// <summary>Choosing a resolution preset applies its width and height.</summary>
        [Fact]
        public void SelectingPresetSetsResolution()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedPreset = vm.Presets.Single(p => p is { Width: 640, Height: 960, IsCustom: false });

            LevelSettings s = vm.ToSettings();
            Assert.Equal(640, s.Width);
            Assert.Equal(960, s.Height);
        }

        /// <summary>Custom width and height are clamped to the allowed bounds.</summary>
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

        /// <summary>Edit mode selects the preset matching the level's size and keeps flags editable.</summary>
        [Fact]
        public void EditModePrefillsMatchingPresetAndAllowsFlags()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(
                new LevelSettings(640, 480, 1.0f, 1, TwoParts: true, NightLevel: false));

            Assert.True(vm.FlagsEditable);
            Assert.False(vm.IsCustom);
            Assert.True(vm.TwoParts);
            Assert.Equal(640, vm.ToSettings().Width);
            Assert.Equal(480, vm.ToSettings().Height);
        }

        /// <summary>Edit mode falls back to Custom when the level's size matches no preset.</summary>
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

        /// <summary>The mobile-physics flag flows into the produced settings.</summary>
        [Fact]
        public void ToSettingsIncludesMobilePhysics()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.UseMobilePhysics = true;
            Assert.True(vm.ToSettings().UseMobilePhysics);
        }

        /// <summary>Edit mode prefills the mobile-physics flag from the current level.</summary>
        [Fact]
        public void ForEditPrefillsMobilePhysics()
        {
            LevelSettings current = new(320, 480, 1.0f, 0, false, false, UseMobilePhysics: true);
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(current);
            Assert.True(vm.UseMobilePhysics);
        }

        /// <summary>LoadDecoration prefills the rope skin, background, and remember flag.</summary>
        [Fact]
        public void LoadDecorationPrefillsSelections()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.LoadDecoration(new EditorSettings { RememberDecoration = true, RopeSkin = 3, Background = 4 });
            Assert.Equal(3, vm.SelectedRopeSkin);
            Assert.Equal(4, vm.SelectedBackground);
            Assert.True(vm.RememberDecoration);
        }

        /// <summary>ResolveDecoration turns Random (-1) selections into concrete rope/background ids.</summary>
        [Fact]
        public void ResolveDecorationTurnsRandomIntoConcreteIds()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedRopeSkin = -1;
            vm.SelectedBackground = -1;
            (int skin, int bg) = vm.ResolveDecoration(new Random(1));
            Assert.InRange(skin, 0, 8);
            Assert.InRange(bg, 1, 17);
        }

        /// <summary>When remembering, the raw selections (including Random) are written into settings.</summary>
        [Fact]
        public void WriteDecorationIntoRememberedSavesRawSelections()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedRopeSkin = -1;
            vm.SelectedBackground = 5;
            vm.RememberDecoration = true;
            EditorSettings settings = new() { RopeSkin = 0, Background = 1 };
            vm.WriteDecorationInto(settings);
            Assert.True(settings.RememberDecoration);
            Assert.Equal(-1, settings.RopeSkin);
            Assert.Equal(5, settings.Background);
        }

        /// <summary>When not remembering, saved ids are left untouched and only the remember flag clears.</summary>
        [Fact]
        public void WriteDecorationIntoNotRememberedLeavesIdsUntouched()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedRopeSkin = 7;
            vm.SelectedBackground = 6;
            vm.RememberDecoration = false;
            EditorSettings settings = new() { RememberDecoration = true, RopeSkin = 2, Background = 3 };
            vm.WriteDecorationInto(settings);
            Assert.False(settings.RememberDecoration);
            Assert.Equal(2, settings.RopeSkin);
            Assert.Equal(3, settings.Background);
        }

        /// <summary>The remember-as-default checkbox shows only when creating a level, not when editing.</summary>
        [Fact]
        public void ShowRememberDecorationNewOnly()
        {
            Assert.True(LevelSettingsViewModel.ForNew().ShowRememberDecoration);
            Assert.False(LevelSettingsViewModel.ForEdit(new LevelSettings(320, 480, 1f, 0, false, false)).ShowRememberDecoration);
        }

        /// <summary>Edit mode seeds the decoration pickers from the editor's live rope/background ids.</summary>
        [Fact]
        public void ForEditPrefillsDecoration()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(
                new LevelSettings(320, 480, 1f, 0, false, false), ropeSkin: 5, background: 7);
            Assert.Equal(5, vm.SelectedRopeSkin);
            Assert.Equal(7, vm.SelectedBackground);
        }

        /// <summary>Background options run Blank first, then all 17 box backgrounds, then Random last.</summary>
        [Fact]
        public void BackgroundOptionsCoverBlankAllSeventeenAndRandom()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            int[] ids = [.. vm.BackgroundOptions.Select(o => o.Id)];
            Assert.Equal(0, ids[0]);          // Blank first
            Assert.Equal(-1, ids[^1]);        // Random last
            for (int i = 1; i <= 17; i++)
            {
                Assert.Contains(i, ids);       // every box background
            }
        }

        /// <summary>Setting the selected id marks the matching option and clears the others.</summary>
        [Fact]
        public void SelectingIdMarksMatchingOptionAndClearsOthers()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedRopeSkin = 3;
            Assert.True(vm.RopeSkinOptions.Single(o => o.Id == 3).IsSelected);
            Assert.All(vm.RopeSkinOptions.Where(o => o.Id != 3), o => Assert.False(o.IsSelected));
        }

        /// <summary>Checking an option's IsSelected updates the selected id and clears the others.</summary>
        [Fact]
        public void CheckingAnOptionUpdatesTheSelectedId()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.BackgroundOptions.Single(o => o.Id == 6).IsSelected = true;
            Assert.Equal(6, vm.SelectedBackground);
            Assert.All(vm.BackgroundOptions.Where(o => o.Id != 6), o => Assert.False(o.IsSelected));
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
