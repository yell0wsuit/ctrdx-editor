using System;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.ViewModels
{
    /// <summary>One selectable level resolution; <see cref="IsCustom"/> enables the manual width/height inputs.</summary>
    public sealed record ResolutionPreset(string Label, int Width, int Height, bool IsCustom);

    /// <summary>One selectable value for the special (tutorial-staging) setting: user-facing label, XML integer.</summary>
    public sealed record SpecialOption(string Label, int Value);

    /// <summary>View model for the New / Level Settings dialog.</summary>
    public sealed partial class LevelSettingsViewModel : ViewModelBase
    {
        private const int MinWidth = 320;
        private const int MinHeight = 480;
        private const int MaxDimension = 9999;

        /// <summary>Available resolutions; the last entry is the custom sentinel.</summary>
        public ObservableCollection<ResolutionPreset> Presets { get; } =
        [
            new("320 x 480", 320, 480, false),
            new("640 x 480", 640, 480, false),
            new("320 x 960", 320, 960, false),
            new("640 x 960", 640, 960, false),
            new("Custom...", 0, 0, true),
        ];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustom))]
        public partial ResolutionPreset SelectedPreset { get; set; }

        /// <summary>
        /// Selectable special values. Only 0/1 make sense for a custom level (special stages the game's
        /// built-in tutorial prompts, which the editor can't place); other values are inert here. An
        /// imported level carrying a different value gets it added as an extra option, so it round-trips.
        /// </summary>
        public ObservableCollection<SpecialOption> SpecialOptions { get; } =
        [
            new("None", 0),
            new("Default", 1),
        ];

        [ObservableProperty] public partial int CustomWidth { get; set; } = MinWidth;
        [ObservableProperty] public partial int CustomHeight { get; set; } = MinHeight;
        [ObservableProperty] public partial double RopePhysicsSpeed { get; set; } = 1.0;
        [ObservableProperty] public partial SpecialOption SelectedSpecial { get; set; }
        [ObservableProperty] public partial bool TwoParts { get; set; }
        [ObservableProperty] public partial bool NightLevel { get; set; }

        /// <summary>Whether the locked flags (Half candy / Night level) may be edited (New mode only).</summary>
        public bool FlagsEditable { get; private init; }

        /// <summary>Whether the manual width/height inputs are active.</summary>
        public bool IsCustom => SelectedPreset.IsCustom;

        private LevelSettingsViewModel(bool flagsEditable)
        {
            FlagsEditable = flagsEditable;
            SelectedPreset = Presets[0];
            SelectedSpecial = SpecialOptions[0];
        }

        // Selects the option matching value, adding a preserve-option when an imported level uses an
        // unlisted special value so it survives a settings edit unchanged.
        private void SelectSpecial(int value)
        {
            SpecialOption? match = SpecialOptions.FirstOrDefault(o => o.Value == value);
            if (match is null)
            {
                match = new SpecialOption($"Special {value}", value);
                SpecialOptions.Add(match);
            }
            SelectedSpecial = match;
        }

        /// <summary>A dialog for creating a new level (all fields editable).</summary>
        public static LevelSettingsViewModel ForNew()
        {
            return new LevelSettingsViewModel(flagsEditable: true);
        }

        /// <summary>A dialog for editing an existing level (locked flags disabled), prefilled from <paramref name="current"/>.</summary>
        public static LevelSettingsViewModel ForEdit(LevelSettings current)
        {
            LevelSettingsViewModel vm = new(flagsEditable: false)
            {
                RopePhysicsSpeed = current.RopePhysicsSpeed,
                TwoParts = current.TwoParts,
                NightLevel = current.NightLevel,
                CustomWidth = current.Width,
                CustomHeight = current.Height,
            };
            vm.SelectSpecial(current.Special);
            ResolutionPreset? match = vm.Presets.FirstOrDefault(
                p => !p.IsCustom && p.Width == current.Width && p.Height == current.Height);
            vm.SelectedPreset = match ?? vm.Presets.Single(p => p.IsCustom);
            return vm;
        }

        /// <summary>Builds the settings record from the current selections, clamping custom sizes.</summary>
        public LevelSettings ToSettings()
        {
            int width = IsCustom ? Math.Clamp(CustomWidth, MinWidth, MaxDimension) : SelectedPreset.Width;
            int height = IsCustom ? Math.Clamp(CustomHeight, MinHeight, MaxDimension) : SelectedPreset.Height;
            return new LevelSettings(width, height, (float)RopePhysicsSpeed, SelectedSpecial.Value, TwoParts, NightLevel);
        }
    }
}
