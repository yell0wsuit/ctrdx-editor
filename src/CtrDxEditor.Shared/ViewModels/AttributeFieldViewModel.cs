using System;
using System.Globalization;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>One selectable enum option: raw value plus UI label.</summary>
    public sealed record AttributeOptionViewModel(string Value, string Label);

    /// <summary>Editable field view model for a single attribute or a delegate-backed choice.</summary>
    public sealed partial class AttributeFieldViewModel : ViewModelBase
    {
        private readonly Func<string?> _get;
        private readonly Action<string?> _set;
        private readonly Action _onChanging;
        private readonly Action _onChanged;
        private readonly Func<bool>? _isEnabledFn;

        /// <summary>Attribute-backed field (text, number, bool, or fixed enum).</summary>
        public AttributeFieldViewModel(
            LevelObject target,
            string name,
            AttrType type,
            string[]? enumValues,
            Action onChanged,
            Action? onChanging = null,
            string? labelName = null)
        {
            Name = name;
            string localizationName = labelName ?? name;
            Label = Localizer.AttributeName(localizationName);
            IsBool = type == AttrType.Bool;
            IsNumeric = type is AttrType.Whole or AttrType.Number;
            AllowsDecimal = type == AttrType.Number;
            IsColor = type == AttrType.Color;
            EnumValues = enumValues;
            EnumOptions = enumValues?.Select(v => new AttributeOptionViewModel(v, Localizer.AttributeOption(localizationName, v))).ToArray();
            HelpText = ConventionHelp(localizationName);
            _get = () => target.GetAttr(name);
            _set = v => target.SetAttr(name, v ?? string.Empty);
            _onChanging = onChanging ?? (() => { });
            _onChanged = onChanged;
        }

        /// <summary>Delegate-backed choice field with dynamic options (e.g. grab "Attach to").</summary>
        public AttributeFieldViewModel(
            string name,
            AttributeOptionViewModel[] options,
            Func<string?> get,
            Action<string?> set,
            Action onChanged,
            Action? onChanging = null)
        {
            Name = name;
            Label = Localizer.AttributeName(name);
            IsBool = false;
            EnumOptions = options;
            HelpText = ConventionHelp(name);
            _get = get;
            _set = set;
            _onChanging = onChanging ?? (() => { });
            _onChanged = onChanged;
        }

        /// <summary>
        /// Delegate-backed simple field (bool/text/number) with no fixed option list. The optional
        /// <paramref name="isEnabled"/> predicate greys the field out when it returns false (re-evaluated on
        /// <see cref="Refresh"/>).
        /// </summary>
        public AttributeFieldViewModel(
            string name,
            AttrType type,
            Func<string?> get,
            Action<string?> set,
            Action onChanged,
            Action? onChanging = null,
            Func<bool>? isEnabled = null,
            string? labelName = null)
        {
            Name = name;
            Label = Localizer.AttributeName(labelName ?? name);
            IsBool = type == AttrType.Bool;
            IsNumeric = type is AttrType.Whole or AttrType.Number;
            AllowsDecimal = type == AttrType.Number;
            IsColor = type == AttrType.Color;
            HelpText = ConventionHelp(labelName ?? name);
            _get = get;
            _set = set;
            _onChanging = onChanging ?? (() => { });
            _onChanged = onChanged;
            _isEnabledFn = isEnabled;
            if (isEnabled is not null)
            {
                IsEnabled = isEnabled();
            }
        }

        /// <summary>
        /// Resolves the conventional <c>Attr.&lt;name&gt;.Help</c> string for a field, or null when the
        /// localization file has no entry for it. Constructors seed <see cref="HelpText"/> from this, so a
        /// field gains its help button by adding the string alone; an explicit object-initializer assignment
        /// still wins, because initializers run after the constructor.
        /// </summary>
        /// <param name="localizationName">The name used to build the field's localization keys.</param>
        /// <returns>The help text, or null when no entry exists.</returns>
        private static string? ConventionHelp(string localizationName)
        {
            string key = $"Attr.{localizationName}.Help";
            string value = Localizer.Get(key);
            return value == key ? null : value;
        }

        /// <summary>The raw attribute id / field key (never localized).</summary>
        public string Name { get; }

        /// <summary>The localized label shown in the Properties panel.</summary>
        public string Label { get; }

        /// <summary>Optional help text; when set, the panel shows a help icon with this as its tooltip.</summary>
        public string? HelpText { get; init; }

        /// <summary>
        /// The title of the collapsible section this field belongs to, or null to render it bare. Null on
        /// every field unless a builder opts in, so ungrouped panels are unchanged.
        /// </summary>
        public string? GroupHeader { get; init; }

        /// <summary>
        /// A stable identity for this field's section, letting two sections share a header text without
        /// merging. -1 means the anonymous ungrouped section.
        /// </summary>
        public int GroupIndex { get; init; } = -1;

        /// <summary>
        /// Whether the section this field belongs to should start collapsed because every attribute in
        /// it currently sits at its default. Read only from the field that opens a new section (see
        /// <c>EditorViewModel.GroupFields</c>); false on every field unless a builder opts in, so
        /// existing panels stay expanded as before.
        /// </summary>
        public bool GroupStartsCollapsed { get; init; }

        /// <summary>Whether this field has help text to surface via a help icon.</summary>
        public bool HasHelp => !string.IsNullOrEmpty(HelpText);

        /// <summary>Allowed values for fixed enum attributes, or null.</summary>
        public string[]? EnumValues { get; }

        /// <summary>Allowed options with UI labels, or null for free-form attributes.</summary>
        public AttributeOptionViewModel[]? EnumOptions { get; }

        /// <summary>Whether this field renders as a checkbox.</summary>
        public bool IsBool { get; }

        /// <summary>Whether this field renders as a numeric box.</summary>
        public bool IsNumeric { get; }

        /// <summary>Whether this numeric field accepts decimal values.</summary>
        public bool AllowsDecimal { get; }

        /// <summary>Whether this field edits a color, shown as a swatch beside a hex box.</summary>
        public bool IsColor { get; }

        /// <summary>
        /// Whether a color field may apply a custom tint. Full-color tutorial artwork opts out because
        /// DX ignores authored tints for those icons; the picker still opens so an imported tint can be cleared.
        /// </summary>
        public bool CanApplyCustomColor { get; init; } = true;

        /// <summary>
        /// Smallest value a numeric field accepts. Lengths and radii are magnitudes and cannot go
        /// negative in game; coordinates and sentinel fields (e.g. timeout = -1) can, so they keep the
        /// default lower bound.
        /// </summary>
        public int NumericMinimum => NumericMinimumOverride ?? (Name switch
        {
            "timeout" or "time" => 1,
            "spinSpeed" or "orbitRadius" or "orbitSpeed" or "polylineSpeed" => 1,
            "length" or "radius" or "moveLength" or "moveOffset" or "litRadius" or "group" or "segmentsCount" => 0,
            _ => -9999,
        });

        /// <summary>Optional per-field lower bound for semantic numeric fields sharing an attribute label.</summary>
        public int? NumericMinimumOverride { get; init; }

        /// <summary>
        /// Whether this numeric field renders as an up/down spinner (like the level-settings numbers) instead
        /// of a plain box. Opt-in per field; used for the hand's segment count so segments can be added and
        /// removed by stepping.
        /// </summary>
        public bool IsStepper { get; init; }

        /// <summary>Whether this field renders as the plain numeric box (numeric, but not a stepper).</summary>
        public bool IsPlainNumeric => IsNumeric && !IsStepper;

        /// <summary>The current value as a number for a <see cref="IsStepper"/> field, or null when unset.</summary>
        public decimal? NumericValue
        {
            get => decimal.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal d) ? d : null;
            set
            {
                if (value is { } v)
                {
                    Value = AllowsDecimal
                        ? v.ToString(CultureInfo.InvariantCulture)
                        : decimal.Truncate(v).ToString("0", CultureInfo.InvariantCulture);
                }
            }
        }

        /// <summary>Whether this field renders as a free-form text box.</summary>
        public bool IsText => EnumOptions is null && !IsBool && !IsNumeric && !IsColor;

        /// <summary>Whether the field's control is interactive; false greys it out.</summary>
        [ObservableProperty]
        public partial bool IsEnabled { get; set; } = true;

        /// <summary>The selected option, mapped to the underlying value.</summary>
        public AttributeOptionViewModel? SelectedOption
        {
            get => EnumOptions?.FirstOrDefault(o => o.Value == Value);
            set
            {
                if (value is not null)
                {
                    Value = value.Value;
                }
            }
        }

        /// <summary>The current underlying value.</summary>
        public string? Value
        {
            get => DisplayValue(_get());
            set
            {
                if (_get() == value)
                {
                    return;
                }
                _onChanging();
                _set(value);
                _onChanged();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedOption));
                OnPropertyChanged(nameof(BoolValue));
                OnPropertyChanged(nameof(NumericValue));
            }
        }

        /// <summary>The bool value for checkbox fields, mapped to the "true"/"false" string.</summary>
        public bool BoolValue
        {
            get => Value == "true";
            set => Value = value ? "true" : "false";
        }

        /// <summary>Re-reads <see cref="Value"/> from the source, for external mutations.</summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(SelectedOption));
            OnPropertyChanged(nameof(BoolValue));
            OnPropertyChanged(nameof(NumericValue));
            if (_isEnabledFn is not null)
            {
                IsEnabled = _isEnabledFn();
            }
        }

        private string? DisplayValue(string? value)
        {
            return AllowsDecimal && value?.EndsWith(".0", StringComparison.Ordinal) == true
                ? value[..^2]
                : value;
        }
    }
}
