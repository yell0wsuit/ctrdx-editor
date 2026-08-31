using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>One selectable level resolution; <see cref="IsCustom"/> enables the manual width/height inputs.</summary>
    public sealed record ResolutionPreset(string Label, int Width, int Height, bool IsCustom);

    /// <summary>One selectable special (tutorial-staging) value: user-facing label, XML integer. <see cref="IsCustom"/> reveals the manual input.</summary>
    public sealed record SpecialOption(string Label, int Value, bool IsCustom = false);

    /// <summary>One selectable rope skin; Id -1 is the Random sentinel, 0 is Default, 1..8 are skins.</summary>
    public sealed partial class RopeSkinOption(int id, string label) : ObservableObject
    {
        /// <summary>Rope skin id: -1 = Random, else 0..8 (0 = Default).</summary>
        public int Id { get; } = id;

        /// <summary>Display label shown under the swatch.</summary>
        public string Label { get; } = label;

        /// <summary>Whether this is the Random option; its card shows a dice icon rather than a preview.</summary>
        public bool IsRandom => Id < 0;

        /// <summary>Whether this option is the current pick; drives (and follows) its radio button.</summary>
        [ObservableProperty] public partial bool IsSelected { get; set; }
    }

    /// <summary>One selectable background; Id -1 is Random, 0 is Blank (no background), 1..17 = bgr_01..bgr_17.</summary>
    public sealed partial class BackgroundOption(int id, string label) : ObservableObject
    {
        /// <summary>Background id: -1 = Random, 0 = Blank, else 1..17.</summary>
        public int Id { get; } = id;

        /// <summary>Display label shown under the thumbnail.</summary>
        public string Label { get; } = label;

        /// <summary>Whether this is the Random option; its card shows a dice icon rather than a preview.</summary>
        public bool IsRandom => Id < 0;

        /// <summary>Whether this option is the current pick; drives (and follows) its radio button.</summary>
        [ObservableProperty] public partial bool IsSelected { get; set; }

        /// <summary>Preview of the background art, loaded lazily after the dialog opens; null for Blank/Random.</summary>
        [ObservableProperty] public partial IImage? Thumbnail { get; set; }
    }

    /// <summary>One selectable candy skin; Id -1 is Random, 0 is Candy 1 (default), 1..51 = Candy 2..52.</summary>
    public sealed partial class CandySkinOption(int id, string label) : ObservableObject
    {
        /// <summary>Candy skin index: -1 = Random, else 0..51 (0 = default candy).</summary>
        public int Id { get; } = id;

        /// <summary>Display label shown under the thumbnail.</summary>
        public string Label { get; } = label;

        /// <summary>Whether this is the Random option; its card shows a dice icon rather than a preview.</summary>
        public bool IsRandom => Id < 0;

        /// <summary>Whether this option is the current pick; drives (and follows) its radio button.</summary>
        [ObservableProperty] public partial bool IsSelected { get; set; }

        /// <summary>Preview of the candy sprite, loaded lazily after the dialog opens; null for Random.</summary>
        [ObservableProperty] public partial IImage? Thumbnail { get; set; }
    }

    /// <summary>One selectable Om Nom sitting platform; Id -1 is Random, 0..16 = Platform 1..17.</summary>
    public sealed partial class OmNomSupportOption(int id, string label) : ObservableObject
    {
        /// <summary>Platform index: -1 = Random, else 0..16 (0 = default platform).</summary>
        public int Id { get; } = id;

        /// <summary>Display label shown under the thumbnail.</summary>
        public string Label { get; } = label;

        /// <summary>Whether this is the Random option; its card shows a dice icon rather than a preview.</summary>
        public bool IsRandom => Id < 0;

        /// <summary>Whether this option is the current pick; drives (and follows) its radio button.</summary>
        [ObservableProperty] public partial bool IsSelected { get; set; }

        /// <summary>Preview of Om Nom on the platform, loaded lazily after the dialog opens; null for Random.</summary>
        [ObservableProperty] public partial IImage? Thumbnail { get; set; }
    }

    /// <summary>View model for the New / Level Settings dialog.</summary>
    public sealed partial class LevelSettingsViewModel : ViewModelBase
    {
        private static readonly CompositeFormat WaterDrainHintFormat =
            CompositeFormat.Parse(Localizer.Get("Dialog.LevelSettings.WaterDrainHint"));

        private static readonly CompositeFormat RangeErrorFormat =
            CompositeFormat.Parse(Localizer.Get("Dialog.LevelSettings.OutOfRange"));

        private const int MinWidth = 320;
        private const int MinHeight = 480;
        private const int MaxDimension = 9999;
        private const int MaxSpecial = 99;
        private const int MinRopeSpeed = -100;
        private const int MaxRopeSpeed = 100;
        private const int MaxWater = 10000;
        private const int MinGravity = -10000;
        private const int MaxGravity = 10000;

        /// <summary>Available resolutions; the last entry is the custom sentinel.</summary>
        public ObservableCollection<ResolutionPreset> Presets { get; } =
        [
            new("320 x 480", 320, 480, false),
            new("640 x 480", 640, 480, false),
            new("320 x 960", 320, 960, false),
            new("640 x 960", 640, 960, false),
            new(Localizer.Get("Dialog.LevelSettings.Custom"), 0, 0, true),
        ];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCustom))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial ResolutionPreset SelectedPreset { get; set; }

        /// <summary>
        /// The level's display name; blank levels write no attribute at all. Free text, since the game shows
        /// an unrecognized name verbatim rather than failing the string-table lookup.
        /// </summary>
        [ObservableProperty] public partial string LevelName { get; set; } = string.Empty;

        /// <summary>
        /// Selectable special values. Only 0/1 make sense for a custom level (special stages the game's
        /// built-in tutorial prompts, which the editor can't place); other values are inert here. An
        /// imported level carrying a different value gets it added as an extra option, so it round-trips.
        /// </summary>
        public ObservableCollection<SpecialOption> SpecialOptions { get; } =
        [
            new(Localizer.Get("Dialog.LevelSettings.Special.None"), 0),
            new(Localizer.Get("Dialog.LevelSettings.Special.Default"), 1),
            new(Localizer.Get("Dialog.LevelSettings.Custom"), 0, IsCustom: true),
        ];

        // The numeric fields are stored as the raw text the box holds, because NumericTextBox filters
        // input character by character and so has to bind a string. Each one exposes a decimal? view
        // over that text: null means "empty or not a number", which CanConfirm rejects. The bounds
        // below are only enforced by CanConfirm, not by the box - the box has to accept an out-of-range
        // prefix ("6" on the way to "640") for a value above its minimum to be typeable at all.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CustomWidth))]
        [NotifyPropertyChangedFor(nameof(CustomWidthError))]
        [NotifyPropertyChangedFor(nameof(HasCustomWidthError))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial string CustomWidthText { get; set; } = Format(MinWidth);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CustomHeight))]
        [NotifyPropertyChangedFor(nameof(CustomHeightError))]
        [NotifyPropertyChangedFor(nameof(HasCustomHeightError))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial string CustomHeightText { get; set; } = Format(MinHeight);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RopePhysicsSpeed))]
        [NotifyPropertyChangedFor(nameof(RopePhysicsSpeedError))]
        [NotifyPropertyChangedFor(nameof(HasRopePhysicsSpeedError))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial string RopePhysicsSpeedText { get; set; } = Format(1.0m);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSpecialCustom))]
        [NotifyPropertyChangedFor(nameof(HasCustomSpecialError))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial SpecialOption SelectedSpecial { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CustomSpecial))]
        [NotifyPropertyChangedFor(nameof(CustomSpecialError))]
        [NotifyPropertyChangedFor(nameof(HasCustomSpecialError))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial string CustomSpecialText { get; set; } = Format(0m);

        [ObservableProperty] public partial bool TwoParts { get; set; }
        [ObservableProperty] public partial bool NightLevel { get; set; }
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TimeTravelRocketPhysicsEditable))]
        public partial bool UseMobilePhysics { get; set; }

        /// <summary>
        /// Whether the level asks for Cut the Rope: Time Travel's rocket tuning. Offered as a mode of
        /// <see cref="UseMobilePhysics"/>, so turning that off clears this rather than leaving a setting
        /// checked that the level no longer carries.
        /// </summary>
        [ObservableProperty] public partial bool UseTimeTravelRocketPhysics { get; set; }

        /// <summary>Whether the Time Travel rocket checkbox accepts input; it needs mobile physics on.</summary>
        public bool TimeTravelRocketPhysicsEditable => FlagsEditable && UseMobilePhysics;

        partial void OnUseMobilePhysicsChanged(bool value)
        {
            if (!value)
            {
                UseTimeTravelRocketPhysics = false;
            }
        }

        /// <summary>Height of the water pool in level units; 0 means the level has no water.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Water))]
        [NotifyPropertyChangedFor(nameof(WaterError))]
        [NotifyPropertyChangedFor(nameof(HasWaterError))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        [NotifyPropertyChangedFor(nameof(WaterDrainHint))]
        [NotifyPropertyChangedFor(nameof(HasWaterDrainHint))]
        public partial string WaterText { get; set; } = Format(0m);

        /// <summary>Rate at which the water drains, in level units per second; 0 is a static pool.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WaterSpeed))]
        [NotifyPropertyChangedFor(nameof(WaterSpeedError))]
        [NotifyPropertyChangedFor(nameof(HasWaterSpeedError))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        [NotifyPropertyChangedFor(nameof(WaterDrainHint))]
        [NotifyPropertyChangedFor(nameof(HasWaterDrainHint))]
        public partial string WaterSpeedText { get; set; } = Format(0m);

        /// <summary>Horizontal gravity in game units, positive pointing right; 0 is the game's default.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GravityX))]
        [NotifyPropertyChangedFor(nameof(GravityXError))]
        [NotifyPropertyChangedFor(nameof(HasGravityXError))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial string GravityXText { get; set; } = Format((decimal)LevelGravity.DefaultX);

        /// <summary>Vertical gravity in game units, positive pulling downward; 784 is normal Earth gravity.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(GravityY))]
        [NotifyPropertyChangedFor(nameof(GravityYError))]
        [NotifyPropertyChangedFor(nameof(HasGravityYError))]
        [NotifyPropertyChangedFor(nameof(CanConfirm))]
        public partial string GravityYText { get; set; } = Format((decimal)LevelGravity.DefaultY);

        /// <summary>Custom level width, or null when the box is empty or holds something unparseable.</summary>
        public decimal? CustomWidth { get => Parse(CustomWidthText); set => CustomWidthText = Format(value); }

        /// <summary>Custom level height, or null when the box is empty or holds something unparseable.</summary>
        public decimal? CustomHeight { get => Parse(CustomHeightText); set => CustomHeightText = Format(value); }

        /// <summary>Rope simulation speed multiplier, or null when the box holds no usable number.</summary>
        public decimal? RopePhysicsSpeed { get => Parse(RopePhysicsSpeedText); set => RopePhysicsSpeedText = Format(value); }

        /// <summary>Manually entered special value, or null when the box holds no usable number.</summary>
        public decimal? CustomSpecial { get => Parse(CustomSpecialText); set => CustomSpecialText = Format(value); }

        /// <summary>Water height, or null when the box holds no usable number.</summary>
        public decimal? Water { get => Parse(WaterText); set => WaterText = Format(value); }

        /// <summary>Water drain speed, or null when the box holds no usable number.</summary>
        public decimal? WaterSpeed { get => Parse(WaterSpeedText); set => WaterSpeedText = Format(value); }

        /// <summary>Horizontal gravity, or null when the box holds no usable number.</summary>
        public decimal? GravityX { get => Parse(GravityXText); set => GravityXText = Format(value); }

        /// <summary>Vertical gravity, or null when the box holds no usable number.</summary>
        public decimal? GravityY { get => Parse(GravityYText); set => GravityYText = Format(value); }

        /// <summary>Why the width box is unusable, or empty text when it holds a valid width.</summary>
        public string CustomWidthError => Validate(CustomWidthText, MinWidth, MaxDimension);

        /// <summary>Why the height box is unusable, or empty text when it holds a valid height.</summary>
        public string CustomHeightError => Validate(CustomHeightText, MinHeight, MaxDimension);

        /// <summary>Why the rope speed box is unusable, or empty text when it holds a valid speed.</summary>
        public string RopePhysicsSpeedError => Validate(RopePhysicsSpeedText, MinRopeSpeed, MaxRopeSpeed);

        /// <summary>Why the special box is unusable, or empty text when it holds a valid special value.</summary>
        public string CustomSpecialError => Validate(CustomSpecialText, 0, MaxSpecial);

        /// <summary>Why the water box is unusable, or empty text when it holds a valid height.</summary>
        public string WaterError => Validate(WaterText, 0, MaxWater);

        /// <summary>Why the water speed box is unusable, or empty text when it holds a valid speed.</summary>
        public string WaterSpeedError => Validate(WaterSpeedText, 0, MaxWater);

        /// <summary>Why the horizontal gravity box is unusable, or empty text when it holds a valid value.</summary>
        public string GravityXError => Validate(GravityXText, MinGravity, MaxGravity);

        /// <summary>Why the vertical gravity box is unusable, or empty text when it holds a valid value.</summary>
        public string GravityYError => Validate(GravityYText, MinGravity, MaxGravity);

        /// <summary>Whether the width box has something to complain about (bound to its message's visibility).</summary>
        public bool HasCustomWidthError => CustomWidthError.Length > 0;

        /// <summary>Whether the height box has something to complain about.</summary>
        public bool HasCustomHeightError => CustomHeightError.Length > 0;

        /// <summary>Whether the rope speed box has something to complain about.</summary>
        public bool HasRopePhysicsSpeedError => RopePhysicsSpeedError.Length > 0;

        /// <summary>Whether the special box has something to complain about. Hidden while the box itself is.</summary>
        public bool HasCustomSpecialError => IsSpecialCustom && CustomSpecialError.Length > 0;

        /// <summary>Whether the water box has something to complain about.</summary>
        public bool HasWaterError => WaterError.Length > 0;

        /// <summary>Whether the water speed box has something to complain about.</summary>
        public bool HasWaterSpeedError => WaterSpeedError.Length > 0;

        /// <summary>Whether the horizontal gravity box has something to complain about.</summary>
        public bool HasGravityXError => GravityXError.Length > 0;

        /// <summary>Whether the vertical gravity box has something to complain about.</summary>
        public bool HasGravityYError => GravityYError.Length > 0;

        /// <summary>
        /// How long the pool takes to empty, or empty text when there is no water or no drain. Derived so
        /// authors can see what a raw speed value actually means.
        /// </summary>
        public string WaterDrainHint =>
            Water > 0m && WaterSpeed > 0m
                ? string.Format(
                    CultureInfo.CurrentCulture,
                    WaterDrainHintFormat,
                    Water.Value / WaterSpeed.Value)
                : string.Empty;

        /// <summary>
        /// Whether the drain hint has anything to say. Bound to its visibility so the row claims no layout
        /// space on a level whose water never drains.
        /// </summary>
        public bool HasWaterDrainHint => WaterDrainHint.Length > 0;

        /// <summary>Highest background id (bgr_01..bgr_17); ids map to the game's box backgrounds.</summary>
        private const int BackgroundCount = 17;
        private const int OmNomSupportCount = 17;

        /// <summary>Rope skin choices: default + 8 skins + Random.</summary>
        public IReadOnlyList<RopeSkinOption> RopeSkinOptions { get; } = BuildRopeSkinOptions();

        /// <summary>Background choices: Blank + bgr_01..bgr_17 (the game's box backgrounds) + Random.</summary>
        public IReadOnlyList<BackgroundOption> BackgroundOptions { get; } = BuildBackgroundOptions();

        /// <summary>Candy skin choices: Candy 1..52 (the game's candy skins) + Random.</summary>
        public IReadOnlyList<CandySkinOption> CandySkinOptions { get; } = BuildCandySkinOptions();

        /// <summary>Om Nom sitting platform choices: Platform 1..17 + Random.</summary>
        public IReadOnlyList<OmNomSupportOption> OmNomSupportOptions { get; } = BuildOmNomSupportOptions();

        [ObservableProperty] public partial int SelectedRopeSkin { get; set; }
        [ObservableProperty] public partial int SelectedBackground { get; set; }
        [ObservableProperty] public partial int SelectedCandySkin { get; set; }
        [ObservableProperty] public partial int SelectedOmNomSupport { get; set; }
        [ObservableProperty] public partial bool RememberDecoration { get; set; }

        /// <summary>The "remember as default" checkbox is only meaningful when creating a new level.</summary>
        public bool ShowRememberDecoration => IsNewMode;

        // Guards the two-way mirror between the Selected* ids and each option's IsSelected flag so a
        // change on one side doesn't recurse back through the other.
        private bool _syncingSelection;

        /// <summary>Whether the manual special-value input is active.</summary>
        public bool IsSpecialCustom => SelectedSpecial.IsCustom;

        /// <summary>Whether the dialog is creating a new level rather than editing an existing one.</summary>
        public bool IsNewMode { get; private init; }

        /// <summary>Whether the Half candy / Night level flags may be edited.</summary>
        public bool FlagsEditable { get; private init; } = true;

        /// <summary>Whether the manual width/height inputs are active.</summary>
        public bool IsCustom => SelectedPreset.IsCustom;

        /// <summary>
        /// Whether every currently-required numeric field holds a number within its bounds (gates the
        /// confirm button). The hidden custom inputs are exempt, since a preset supplies their values.
        /// </summary>
        public bool CanConfirm =>
            RopePhysicsSpeedError.Length == 0
            && WaterError.Length == 0
            && WaterSpeedError.Length == 0
            && GravityXError.Length == 0
            && GravityYError.Length == 0
            && (!IsCustom || (CustomWidthError.Length == 0 && CustomHeightError.Length == 0))
            && (!IsSpecialCustom || CustomSpecialError.Length == 0);

        /// <summary>
        /// Reads a box's text as a number. Invariant culture because <c>NumericTextBox</c> only ever lets
        /// "." through as the decimal separator, whatever the current locale would otherwise use.
        /// </summary>
        private static decimal? Parse(string? text)
        {
            return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal value)
                ? value
                : null;
        }

        /// <summary>Renders a value back into box text; null (no value) becomes an empty box.</summary>
        private static string Format(decimal? value)
        {
            return value?.ToString("0.####", CultureInfo.InvariantCulture) ?? string.Empty;
        }

        /// <summary>
        /// The message to show under a box: empty text when it holds a number within
        /// [<paramref name="min"/>, <paramref name="max"/>], and otherwise a prompt to fix it.
        /// </summary>
        private static string Validate(string? text, int min, int max)
        {
            return Parse(text) is not { } value
                ? Localizer.Get("Dialog.LevelSettings.EnterNumber")
                : value < min || value > max
                    ? string.Format(CultureInfo.CurrentCulture, RangeErrorFormat, min, max)
                    : string.Empty;
        }

        private LevelSettingsViewModel(bool isNewMode)
        {
            IsNewMode = isNewMode;
            SelectedPreset = Presets[0];
            SelectedSpecial = SpecialOptions[0];

            // Mirror a radio-button click (option.IsSelected) back into the Selected* id.
            foreach (RopeSkinOption option in RopeSkinOptions)
            {
                option.PropertyChanged += OnRopeOptionChanged;
            }
            foreach (BackgroundOption option in BackgroundOptions)
            {
                option.PropertyChanged += OnBackgroundOptionChanged;
            }
            foreach (CandySkinOption option in CandySkinOptions)
            {
                option.PropertyChanged += OnCandyOptionChanged;
            }
            foreach (OmNomSupportOption option in OmNomSupportOptions)
            {
                option.PropertyChanged += OnOmNomSupportOptionChanged;
            }

            // Reflect the initial ids (Default rope / Blank background / Candy 1 / Platform 1) in the flags.
            SyncRopeOptions(SelectedRopeSkin);
            SyncBackgroundOptions(SelectedBackground);
            SyncCandyOptions(SelectedCandySkin);
            SyncOmNomSupportOptions(SelectedOmNomSupport);
        }

        private void OnRopeOptionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_syncingSelection || e.PropertyName != nameof(RopeSkinOption.IsSelected))
            {
                return;
            }
            if (sender is RopeSkinOption { IsSelected: true } option)
            {
                SelectedRopeSkin = option.Id;
            }
        }

        private void OnBackgroundOptionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_syncingSelection || e.PropertyName != nameof(BackgroundOption.IsSelected))
            {
                return;
            }
            if (sender is BackgroundOption { IsSelected: true } option)
            {
                SelectedBackground = option.Id;
            }
        }

        private void OnCandyOptionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_syncingSelection || e.PropertyName != nameof(CandySkinOption.IsSelected))
            {
                return;
            }
            if (sender is CandySkinOption { IsSelected: true } option)
            {
                SelectedCandySkin = option.Id;
            }
        }

        partial void OnSelectedRopeSkinChanged(int value)
        {
            SyncRopeOptions(value);
        }

        partial void OnSelectedBackgroundChanged(int value)
        {
            SyncBackgroundOptions(value);
        }

        partial void OnSelectedCandySkinChanged(int value)
        {
            SyncCandyOptions(value);
        }

        private void OnOmNomSupportOptionChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_syncingSelection || e.PropertyName != nameof(OmNomSupportOption.IsSelected))
            {
                return;
            }
            if (sender is OmNomSupportOption { IsSelected: true } option)
            {
                SelectedOmNomSupport = option.Id;
            }
        }

        partial void OnSelectedOmNomSupportChanged(int value)
        {
            SyncOmNomSupportOptions(value);
        }

        private void SyncRopeOptions(int value)
        {
            _syncingSelection = true;
            foreach (RopeSkinOption option in RopeSkinOptions)
            {
                option.IsSelected = option.Id == value;
            }
            _syncingSelection = false;
        }

        private void SyncBackgroundOptions(int value)
        {
            _syncingSelection = true;
            foreach (BackgroundOption option in BackgroundOptions)
            {
                option.IsSelected = option.Id == value;
            }
            _syncingSelection = false;
        }

        private void SyncCandyOptions(int value)
        {
            _syncingSelection = true;
            foreach (CandySkinOption option in CandySkinOptions)
            {
                option.IsSelected = option.Id == value;
            }
            _syncingSelection = false;
        }

        private void SyncOmNomSupportOptions(int value)
        {
            _syncingSelection = true;
            foreach (OmNomSupportOption option in OmNomSupportOptions)
            {
                option.IsSelected = option.Id == value;
            }
            _syncingSelection = false;
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

        /// <summary>
        /// A dialog for editing an existing level, prefilled from <paramref name="current"/>. The decoration
        /// pickers seed from the editor's live <paramref name="ropeSkin"/>/<paramref name="background"/>/<paramref name="candySkin"/>.
        /// </summary>
        public static LevelSettingsViewModel ForEdit(
            LevelSettings current, int ropeSkin = 0, int background = 0, int candySkin = 0, int omNomSupport = 0)
        {
            LevelSettingsViewModel vm = new(isNewMode: false)
            {
                LevelName = current.LevelName,
                RopePhysicsSpeed = (decimal)current.RopePhysicsSpeed,
                TwoParts = current.TwoParts,
                NightLevel = current.NightLevel,
                UseMobilePhysics = current.UseMobilePhysics,
                UseTimeTravelRocketPhysics = current.UseTimeTravelRocketPhysics,
                Water = (decimal)current.Water,
                WaterSpeed = (decimal)current.WaterSpeed,
                GravityX = (decimal)current.GravityX,
                GravityY = (decimal)current.GravityY,
                CustomWidth = current.Width,
                CustomHeight = current.Height,
                SelectedRopeSkin = ropeSkin,
                SelectedBackground = background,
                SelectedCandySkin = candySkin,
                SelectedOmNomSupport = omNomSupport,
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
            return new LevelSettings(
                width, height, rope, special, TwoParts, NightLevel, UseMobilePhysics,
                UseMobilePhysics && UseTimeTravelRocketPhysics,
                (float)(Water ?? 0m), (float)(WaterSpeed ?? 0m),
                LevelName?.Trim() ?? string.Empty,
                (float)(GravityX ?? (decimal)LevelGravity.DefaultX),
                (float)(GravityY ?? (decimal)LevelGravity.DefaultY));
        }

        private static RopeSkinOption[] BuildRopeSkinOptions()
        {
            List<RopeSkinOption> list = [new(0, Localizer.Get("Dialog.LevelSettings.RopeSkin.Default"))];
            for (int i = 1; i < RopePalette.SkinCount; i++)
            {
                list.Add(new RopeSkinOption(i, $"{Localizer.Get("Dialog.LevelSettings.RopeSkin.Skin")} {i + 1}"));
            }
            list.Add(new RopeSkinOption(-1, Localizer.Get("Dialog.Common.Random")));
            return [.. list];
        }

        /// <summary>Blank + bgr_01..bgr_17 (the game's box backgrounds) + Random, labelled from localization.</summary>
        private static BackgroundOption[] BuildBackgroundOptions()
        {
            List<BackgroundOption> list = [new(0, Localizer.Get("Dialog.LevelSettings.Background.Blank"))];
            for (int i = 1; i <= BackgroundCount; i++)
            {
                list.Add(new BackgroundOption(i, Localizer.Get($"Dialog.LevelSettings.Background.Bgr{i:D2}")));
            }
            list.Add(new BackgroundOption(-1, Localizer.Get("Dialog.Common.Random")));
            return [.. list];
        }

        /// <summary>Candy 1..52 (the game's candy skins, ids 0..51) + Random, labelled from localization.</summary>
        private static CandySkinOption[] BuildCandySkinOptions()
        {
            List<CandySkinOption> list = [];
            for (int i = 0; i < CandySkins.Count; i++)
            {
                list.Add(new CandySkinOption(i, $"{Localizer.Get("Dialog.LevelSettings.CandySkin.Candy")} {i + 1}"));
            }
            list.Add(new CandySkinOption(-1, Localizer.Get("Dialog.Common.Random")));
            return [.. list];
        }

        /// <summary>Platform 1..17 (the game's char_supports frames, ids 0..16) + Random, labelled from localization.</summary>
        private static OmNomSupportOption[] BuildOmNomSupportOptions()
        {
            List<OmNomSupportOption> list = [];
            for (int i = 0; i < OmNomSupportCount; i++)
            {
                list.Add(new OmNomSupportOption(i, $"{Localizer.Get("Dialog.LevelSettings.OmNomSupport.Platform")} {i + 1}"));
            }
            list.Add(new OmNomSupportOption(-1, Localizer.Get("Dialog.Common.Random")));
            return [.. list];
        }

        /// <summary>Prefills the decoration selections from persisted settings.</summary>
        public void LoadDecoration(EditorSettings settings)
        {
            SelectedRopeSkin = settings.RopeSkin;
            SelectedBackground = settings.Background;
            SelectedCandySkin = settings.CandySkin;
            SelectedOmNomSupport = settings.OmNomSupport;
            RememberDecoration = settings.RememberDecoration;
        }

        /// <summary>Resolves Random (-1) selections into concrete ids for the level being created.</summary>
        public (int RopeSkin, int Background, int CandySkin, int OmNomSupport) ResolveDecoration(Random rng)
        {
            int skin = SelectedRopeSkin >= 0 ? SelectedRopeSkin : rng.Next(0, RopePalette.SkinCount);
            int bg = SelectedBackground switch
            {
                -1 => rng.Next(1, BackgroundCount + 1), // 1..17
                _ => SelectedBackground,                // 0 (Blank) or 1..17 as chosen
            };
            int candy = SelectedCandySkin >= 0 ? SelectedCandySkin : rng.Next(0, CandySkins.Count);
            int support = SelectedOmNomSupport >= 0 ? SelectedOmNomSupport : rng.Next(0, OmNomSupportCount);
            return (skin, bg, candy, support);
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
                settings.CandySkin = SelectedCandySkin;
                settings.OmNomSupport = SelectedOmNomSupport;
            }
        }
    }
}
