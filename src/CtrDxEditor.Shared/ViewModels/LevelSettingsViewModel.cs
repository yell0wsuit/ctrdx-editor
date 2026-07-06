using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

namespace CtrDxEditor.ViewModels
{
    /// <summary>One selectable level resolution; <see cref="IsCustom"/> enables the manual width/height inputs.</summary>
    public sealed record ResolutionPreset(string Label, int Width, int Height, bool IsCustom);

    /// <summary>One selectable special (tutorial-staging) value: user-facing label, XML integer. <see cref="IsCustom"/> reveals the manual input.</summary>
    public sealed record SpecialOption(string Label, int Value, bool IsCustom = false);

    /// <summary>One selectable rope skin; Id -1 is the Random sentinel.</summary>
    public sealed record RopeSkinOption(int Id, string Label);

    /// <summary>One selectable background; Id -1 is Random, 0 is Blank (no background), 1..7 = bgr_01..bgr_07.</summary>
    public sealed record BackgroundOption(int Id, string Label);

    /// <summary>View model for the New / Level Settings dialog.</summary>
    public sealed partial class LevelSettingsViewModel : ViewModelBase
    {
        private const int MinWidth = 320;
        private const int MinHeight = 480;
        private const int MaxDimension = 9999;
        private const int MaxSpecial = 99;

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
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
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
            new("Custom...", 0, IsCustom: true),
        ];

        // NumericUpDown.Value is decimal?, so these bind as decimal? (an exact match avoids the
        // raw "could not convert (null)" cast errors, and an empty box is a valid null that
        // CanConfirm rejects so the level can't be created with a blank number).
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial decimal? CustomWidth { get; set; } = MinWidth;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial decimal? CustomHeight { get; set; } = MinHeight;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial decimal? RopePhysicsSpeed { get; set; } = 1.0m;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSpecialCustom))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial SpecialOption SelectedSpecial { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial decimal? CustomSpecial { get; set; } = 0m;
        [ObservableProperty] public partial bool TwoParts { get; set; }
        [ObservableProperty] public partial bool NightLevel { get; set; }
        [ObservableProperty] public partial bool UseMobilePhysics { get; set; }

        /// <summary>Rope skin choices: default + 8 skins + Random.</summary>
        public IReadOnlyList<RopeSkinOption> RopeSkinOptions { get; } = BuildRopeSkinOptions();

        /// <summary>Background choices: Blank + bgr_01..bgr_07 + Random.</summary>
        public IReadOnlyList<BackgroundOption> BackgroundOptions { get; } =
        [
            new(0, "Blank"),
            new(1, "Cardboard Box"),
            new(2, "Fabric Box"),
            new(3, "Foil Box"),
            new(4, "Magic Box"),
            new(5, "Valentine's Box"),
            new(6, "Toy Box"),
            new(7, "Gift Box"),
            new(-1, "Random"),
        ];

        [ObservableProperty] public partial int SelectedRopeSkin { get; set; }
        [ObservableProperty] public partial int SelectedBackground { get; set; }
        [ObservableProperty] public partial bool RememberDecoration { get; set; }

        /// <summary>Decoration options are only offered when creating a new level.</summary>
        public bool ShowDecoration => IsNewMode;

        /// <summary>Whether the manual special-value input is active.</summary>
        public bool IsSpecialCustom => SelectedSpecial.IsCustom;

        /// <summary>Whether the dialog is creating a new level rather than editing an existing one.</summary>
        public bool IsNewMode { get; private init; }

        /// <summary>Whether the Half candy / Night level flags may be edited.</summary>
        public bool FlagsEditable { get; private init; } = true;

        /// <summary>Whether the manual width/height inputs are active.</summary>
        public bool IsCustom => SelectedPreset.IsCustom;

        /// <summary>Whether every currently-required numeric field has a value (gates the confirm button).</summary>
        public bool CanConfirm =>
            RopePhysicsSpeed is not null
            && (!IsCustom || (CustomWidth is not null && CustomHeight is not null))
            && (!IsSpecialCustom || CustomSpecial is not null);

        private LevelSettingsViewModel(bool isNewMode)
        {
            IsNewMode = isNewMode;
            SelectedPreset = Presets[0];
            SelectedSpecial = SpecialOptions[0];
        }

        // Selects the listed option matching value, or routes an unlisted value (e.g. an imported
        // tutorial pack's special=3) through Custom so a settings edit preserves it unchanged.
        private void SelectSpecial(int value)
        {
            SpecialOption? match = SpecialOptions.FirstOrDefault(o => !o.IsCustom && o.Value == value);
            if (match is not null)
            {
                SelectedSpecial = match;
            }
            else
            {
                CustomSpecial = value;
                SelectedSpecial = SpecialOptions.Single(o => o.IsCustom);
            }
        }

        // Coalesces a nullable numeric field (empty box) to a fallback, then clamps into range.
        private static int ClampOrDefault(decimal? value, int fallback, int min, int max)
        {
            return (int)Math.Clamp(value ?? fallback, min, max);
        }

        /// <summary>A dialog for creating a new level (all fields editable).</summary>
        public static LevelSettingsViewModel ForNew()
        {
            return new LevelSettingsViewModel(isNewMode: true);
        }

        /// <summary>A dialog for editing an existing level, prefilled from <paramref name="current"/>.</summary>
        public static LevelSettingsViewModel ForEdit(LevelSettings current)
        {
            LevelSettingsViewModel vm = new(isNewMode: false)
            {
                RopePhysicsSpeed = (decimal)current.RopePhysicsSpeed,
                TwoParts = current.TwoParts,
                NightLevel = current.NightLevel,
                UseMobilePhysics = current.UseMobilePhysics,
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
            int width = IsCustom ? ClampOrDefault(CustomWidth, MinWidth, MinWidth, MaxDimension) : SelectedPreset.Width;
            int height = IsCustom ? ClampOrDefault(CustomHeight, MinHeight, MinHeight, MaxDimension) : SelectedPreset.Height;
            int special = IsSpecialCustom ? ClampOrDefault(CustomSpecial, 0, 0, MaxSpecial) : SelectedSpecial.Value;
            float rope = (float)(RopePhysicsSpeed ?? 1.0m);
            return new LevelSettings(width, height, rope, special, TwoParts, NightLevel, UseMobilePhysics);
        }

        private static RopeSkinOption[] BuildRopeSkinOptions()
        {
            List<RopeSkinOption> list = [new(0, "Default")];
            for (int i = 1; i < RopePalette.SkinCount; i++)
            {
                list.Add(new RopeSkinOption(i, $"Rope {i + 1}"));
            }
            list.Add(new RopeSkinOption(-1, "Random"));
            return [.. list];
        }

        /// <summary>Prefills the decoration selections from persisted settings.</summary>
        public void LoadDecoration(EditorSettings settings)
        {
            SelectedRopeSkin = settings.RopeSkin;
            SelectedBackground = settings.Background;
            RememberDecoration = settings.RememberDecoration;
        }

        /// <summary>Resolves Random (-1) selections into concrete ids for the level being created.</summary>
        public (int RopeSkin, int Background) ResolveDecoration(Random rng)
        {
            int skin = SelectedRopeSkin >= 0 ? SelectedRopeSkin : rng.Next(0, RopePalette.SkinCount);
            int bg = SelectedBackground switch
            {
                -1 => rng.Next(1, 8),    // 1..7
                _ => SelectedBackground, // 0 (Blank) or 1..7 as chosen
            };
            return (skin, bg);
        }

        /// <summary>
        /// Persists decoration defaults: when Remember is on, stores the raw (possibly Random) selections;
        /// when off, only clears the remember flag and leaves any previously remembered ids untouched.
        /// </summary>
        public void WriteDecorationInto(EditorSettings settings)
        {
            settings.RememberDecoration = RememberDecoration;
            if (RememberDecoration)
            {
                settings.RopeSkin = SelectedRopeSkin;
                settings.Background = SelectedBackground;
            }
        }
    }
}
