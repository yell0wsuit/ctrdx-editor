using System;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using CtrDxEditor.Core.Document;

namespace CtrDxEditor.ViewModels
{
    /// <summary>One selectable level resolution; <see cref="IsCustom"/> enables the manual width/height inputs.</summary>
    public sealed record ResolutionPreset(string Label, int Width, int Height, bool IsCustom);

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

        [ObservableProperty] public partial int CustomWidth { get; set; } = MinWidth;
        [ObservableProperty] public partial int CustomHeight { get; set; } = MinHeight;
        [ObservableProperty] public partial double RopePhysicsSpeed { get; set; } = 1.0;
        [ObservableProperty] public partial int Special { get; set; }
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
                Special = current.Special,
                TwoParts = current.TwoParts,
                NightLevel = current.NightLevel,
                CustomWidth = current.Width,
                CustomHeight = current.Height,
            };
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
            return new LevelSettings(width, height, (float)RopePhysicsSpeed, Special, TwoParts, NightLevel);
        }
    }
}
