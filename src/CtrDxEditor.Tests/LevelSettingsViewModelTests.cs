using System;
using System.Collections.Generic;
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
                new LevelSettings(640, 480, 1.0f, TwoParts: true, NightLevel: false));

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
                new LevelSettings(500, 700, 1.0f, TwoParts: false, NightLevel: false));

            Assert.True(vm.IsCustom);
            Assert.Equal(500, vm.ToSettings().Width);
            Assert.Equal(700, vm.ToSettings().Height);
        }

        /// <summary>A new level starts unnamed, and a name typed in reaches the settings trimmed.</summary>
        [Fact]
        public void LevelNameIsBlankByDefaultAndTrimmedOnConfirm()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            Assert.Equal(string.Empty, vm.ToSettings().LevelName);

            vm.LevelName = "  Spider Season  ";
            Assert.Equal("Spider Season", vm.ToSettings().LevelName);
        }

        /// <summary>Edit mode prefills the existing level name so it survives an unrelated settings change.</summary>
        [Fact]
        public void EditModePrefillsLevelName()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(
                new LevelSettings(640, 480, 1.0f, false, false, LevelName: "Bath Time"));

            Assert.Equal("Bath Time", vm.LevelName);
            Assert.Equal("Bath Time", vm.ToSettings().LevelName);
        }

        /// <summary>A new level starts at the game's gravity defaults and carries edited values through.</summary>
        [Fact]
        public void GravityDefaultsToEarthAndRoundTrips()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();

            LevelSettings defaults = vm.ToSettings();
            Assert.Equal(LevelGravity.DefaultX, defaults.GravityX);
            Assert.Equal(LevelGravity.DefaultY, defaults.GravityY);

            vm.GravityX = -60m;
            vm.GravityY = 0m;
            LevelSettings edited = vm.ToSettings();
            Assert.Equal(-60f, edited.GravityX);
            Assert.Equal(0f, edited.GravityY);

            Assert.Equal(0m, LevelSettingsViewModel.ForEdit(edited).GravityY);
        }

        /// <summary>Out-of-range or unparseable gravity blocks confirmation instead of silently clamping.</summary>
        [Fact]
        public void GravityOutOfRangeBlocksConfirm()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            Assert.True(vm.CanConfirm);

            vm.GravityYText = "99999";
            Assert.True(vm.HasGravityYError);
            Assert.False(vm.CanConfirm);

            vm.GravityYText = "784";
            Assert.False(vm.HasGravityYError);
            Assert.True(vm.CanConfirm);

            vm.GravityXText = "";
            Assert.True(vm.HasGravityXError);
            Assert.False(vm.CanConfirm);
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

        /// <summary>
        /// Confirm is blocked while a box holds a number outside its bounds. The box itself has to accept
        /// out-of-range text (its minimum is relaxed so longer numbers can be typed a digit at a time), so
        /// this is the only thing standing between a stray value and a level built from it.
        /// </summary>
        [Fact]
        public void CanConfirmRejectsOutOfRangeValues()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedPreset = vm.Presets.Single(p => p.IsCustom);

            vm.CustomWidthText = "100"; // below the 320 minimum
            Assert.False(vm.CanConfirm);
            Assert.True(vm.HasCustomWidthError);

            vm.CustomWidthText = "640";
            Assert.True(vm.CanConfirm);
            Assert.False(vm.HasCustomWidthError);
        }

        /// <summary>Non-numeric text reads back as no value at all, and says so rather than blowing up.</summary>
        [Fact]
        public void UnparseableTextReadsAsNull()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.RopePhysicsSpeedText = "abc";

            Assert.Null(vm.RopePhysicsSpeed);
            Assert.False(vm.CanConfirm);
            Assert.True(vm.HasRopePhysicsSpeedError);
        }

        /// <summary>Decimal rope speeds survive the text round-trip, in invariant form.</summary>
        [Fact]
        public void RopePhysicsSpeedKeepsDecimals()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.RopePhysicsSpeedText = "1.5";

            Assert.Equal(1.5m, vm.RopePhysicsSpeed);
            Assert.True(vm.CanConfirm);
            Assert.Equal(1.5f, vm.ToSettings().RopePhysicsSpeed);

            vm.RopePhysicsSpeed = 2.25m;
            Assert.Equal("2.25", vm.RopePhysicsSpeedText);
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
            LevelSettings current = new(320, 480, 1.0f, false, false, UseMobilePhysics: true);
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(current);
            Assert.True(vm.UseMobilePhysics);
        }

        /// <summary>LoadDecoration prefills the rope skin, background, candy skin, and remember flag.</summary>
        [Fact]
        public void LoadDecorationPrefillsSelections()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.LoadDecoration(new EditorSettings { RememberDecoration = true, RopeSkin = 3, Background = 4, CandySkin = 6, OmNomSupport = 10 });
            Assert.Equal(3, vm.SelectedRopeSkin);
            Assert.Equal(4, vm.SelectedBackground);
            Assert.Equal(6, vm.SelectedCandySkin);
            Assert.Equal(10, vm.SelectedOmNomSupport);
            Assert.True(vm.RememberDecoration);
        }

        /// <summary>ResolveDecoration turns Random (-1) selections into concrete rope/background/candy/platform ids.</summary>
        [Fact]
        public void ResolveDecorationTurnsRandomIntoConcreteIds()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedRopeSkin = -1;
            vm.SelectedBackground = -1;
            vm.SelectedCandySkin = -1;
            vm.SelectedOmNomSupport = -1;
            (int skin, int bg, int candy, int support) = vm.ResolveDecoration(new Random(1));
            Assert.InRange(skin, 0, 8);
            Assert.InRange(bg, 1, 17);
            Assert.InRange(candy, 0, 51);
            Assert.InRange(support, 0, 16);
        }

        /// <summary>When remembering, the raw selections (including Random) are written into settings.</summary>
        [Fact]
        public void WriteDecorationIntoRememberedSavesRawSelections()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedRopeSkin = -1;
            vm.SelectedBackground = 5;
            vm.SelectedCandySkin = -1;
            vm.SelectedOmNomSupport = -1;
            vm.RememberDecoration = true;
            EditorSettings settings = new() { RopeSkin = 0, Background = 1, CandySkin = 2, OmNomSupport = 3 };
            vm.WriteDecorationInto(settings);
            Assert.True(settings.RememberDecoration);
            Assert.Equal(-1, settings.RopeSkin);
            Assert.Equal(5, settings.Background);
            Assert.Equal(-1, settings.CandySkin);
            Assert.Equal(-1, settings.OmNomSupport);
        }

        /// <summary>When not remembering, saved ids are left untouched and only the remember flag clears.</summary>
        [Fact]
        public void WriteDecorationIntoNotRememberedLeavesIdsUntouched()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.SelectedRopeSkin = 7;
            vm.SelectedBackground = 6;
            vm.SelectedCandySkin = 8;
            vm.SelectedOmNomSupport = 9;
            vm.RememberDecoration = false;
            EditorSettings settings = new() { RememberDecoration = true, RopeSkin = 2, Background = 3, CandySkin = 4, OmNomSupport = 5 };
            vm.WriteDecorationInto(settings);
            Assert.False(settings.RememberDecoration);
            Assert.Equal(2, settings.RopeSkin);
            Assert.Equal(3, settings.Background);
            Assert.Equal(4, settings.CandySkin);
            Assert.Equal(5, settings.OmNomSupport);
        }

        /// <summary>The remember-as-default checkbox shows only when creating a level, not when editing.</summary>
        [Fact]
        public void ShowRememberDecorationNewOnly()
        {
            Assert.True(LevelSettingsViewModel.ForNew().ShowRememberDecoration);
            Assert.False(LevelSettingsViewModel.ForEdit(new LevelSettings(320, 480, 1f, false, false)).ShowRememberDecoration);
        }

        /// <summary>Edit mode seeds the decoration pickers from the editor's live rope/background/candy/platform ids.</summary>
        [Fact]
        public void ForEditPrefillsDecoration()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForEdit(
                new LevelSettings(320, 480, 1f, false, false), ropeSkin: 5, background: 7, candySkin: 9, omNomSupport: 11);
            Assert.Equal(5, vm.SelectedRopeSkin);
            Assert.Equal(7, vm.SelectedBackground);
            Assert.Equal(9, vm.SelectedCandySkin);
            Assert.Equal(11, vm.SelectedOmNomSupport);
        }

        /// <summary>Candy options run Candy 1 (id 0) first, all 52 skins, then Random last.</summary>
        [Fact]
        public void CandySkinOptionsCoverAllFiftyTwoAndRandom()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            int[] ids = [.. vm.CandySkinOptions.Select(o => o.Id)];
            Assert.Equal(0, ids[0]);           // Candy 1 first
            Assert.Equal(-1, ids[^1]);         // Random last
            for (int i = 0; i < 52; i++)
            {
                Assert.Contains(i, ids);        // every candy skin 0..51
            }
        }

        /// <summary>Platform options run Platform 1 (id 0) first, all 17 platforms, then Random last.</summary>
        [Fact]
        public void OmNomSupportOptionsCoverAllSeventeenAndRandom()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            int[] ids = [.. vm.OmNomSupportOptions.Select(o => o.Id)];
            Assert.Equal(0, ids[0]);           // Platform 1 first
            Assert.Equal(-1, ids[^1]);         // Random last
            for (int i = 0; i < 17; i++)
            {
                Assert.Contains(i, ids);        // every platform 0..16
            }
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

        /// <summary>The drain hint is hidden unless the level has both a pool and a speed to drain it.</summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(240, 0)]
        [InlineData(0, 12)]
        public void DrainHintHiddenWithoutBothWaterAndSpeed(int water, int speed)
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.Water = water;
            vm.WaterSpeed = speed;

            Assert.False(vm.HasWaterDrainHint);
            Assert.Empty(vm.WaterDrainHint);
        }

        /// <summary>A pool with a drain speed shows the hint.</summary>
        [Fact]
        public void DrainHintShownWhenWaterDrains()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.Water = 240m;
            vm.WaterSpeed = 12m;

            Assert.True(vm.HasWaterDrainHint);
            Assert.NotEmpty(vm.WaterDrainHint);
        }

        /// <summary>A blanked-out field hides the hint rather than leaving a stale duration on screen.</summary>
        [Fact]
        public void DrainHintHiddenWhenAFieldIsCleared()
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            vm.Water = 240m;
            vm.WaterSpeed = 12m;

            vm.WaterSpeed = null;

            Assert.False(vm.HasWaterDrainHint);
        }

        /// <summary>Both water fields notify the hint's visibility, so the row appears and collapses live.</summary>
        [Theory]
        [InlineData(nameof(LevelSettingsViewModel.Water))]
        [InlineData(nameof(LevelSettingsViewModel.WaterSpeed))]
        public void WaterFieldsNotifyDrainHintVisibility(string property)
        {
            LevelSettingsViewModel vm = LevelSettingsViewModel.ForNew();
            List<string?> changed = [];
            vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

            if (property == nameof(LevelSettingsViewModel.Water))
            {
                vm.Water = 240m;
            }
            else
            {
                vm.WaterSpeed = 12m;
            }

            Assert.Contains(nameof(LevelSettingsViewModel.HasWaterDrainHint), changed);
        }
    }
}
