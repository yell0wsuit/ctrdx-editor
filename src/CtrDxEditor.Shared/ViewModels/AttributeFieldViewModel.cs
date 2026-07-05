using System;
using System.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>One selectable enum option: raw value plus UI label.</summary>
    public sealed record AttributeOptionViewModel(string Value, string Label);

    /// <summary>Editable field view model for a single attribute or a delegate-backed choice.</summary>
    public sealed class AttributeFieldViewModel : ViewModelBase
    {
        private readonly Func<string?> _get;
        private readonly Action<string?> _set;
        private readonly Action _onChanged;
        private bool _isEnabled = true;

        /// <summary>Attribute-backed field (text, number, bool, or fixed enum).</summary>
        public AttributeFieldViewModel(LevelObject target, string name, AttrType type, string[]? enumValues, Action onChanged)
        {
            Name = name;
            Label = Localizer.AttributeName(name);
            IsBool = type == AttrType.Bool;
            EnumValues = enumValues;
            EnumOptions = enumValues?.Select(v => new AttributeOptionViewModel(v, LabelForOption(name, v))).ToArray();
            _get = () => target.GetAttr(name);
            _set = v => target.SetAttr(name, v ?? string.Empty);
            _onChanged = onChanged;
        }

        /// <summary>Delegate-backed choice field with dynamic options (e.g. grab "Attach to").</summary>
        public AttributeFieldViewModel(string name, AttributeOptionViewModel[] options, Func<string?> get, Action<string?> set, Action onChanged)
        {
            Name = name;
            Label = Localizer.AttributeName(name);
            IsBool = false;
            EnumOptions = options;
            _get = get;
            _set = set;
            _onChanged = onChanged;
        }

        /// <summary>Delegate-backed simple field (bool/text/number) with no fixed option list.</summary>
        public AttributeFieldViewModel(string name, AttrType type, Func<string?> get, Action<string?> set, Action onChanged)
        {
            Name = name;
            Label = Localizer.AttributeName(name);
            IsBool = type == AttrType.Bool;
            _get = get;
            _set = set;
            _onChanged = onChanged;
        }

        /// <summary>The raw attribute id / field key (never localized).</summary>
        public string Name { get; }

        /// <summary>The localized label shown in the Properties panel.</summary>
        public string Label { get; }

        /// <summary>Allowed values for fixed enum attributes, or null.</summary>
        public string[]? EnumValues { get; }

        /// <summary>Allowed options with UI labels, or null for free-form attributes.</summary>
        public AttributeOptionViewModel[]? EnumOptions { get; }

        /// <summary>Whether this field renders as a checkbox.</summary>
        public bool IsBool { get; }

        /// <summary>Whether this field renders as a free-form text box.</summary>
        public bool IsText => EnumOptions is null && !IsBool;

        /// <summary>Whether the field's control is interactive; false greys it out.</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

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
            get => _get();
            set
            {
                _set(value);
                _onChanged();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedOption));
                OnPropertyChanged(nameof(BoolValue));
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
        }

        private static string LabelForOption(string attribute, string value)
        {
            return attribute == "part"
                ? value switch
                {
                    "L" => "left",
                    "R" => "right",
                    _ => value,
                }
                : value;
        }
    }
}
