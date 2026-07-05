using System;
using System.Linq;

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

        /// <summary>Attribute-backed field (text, number, or fixed enum).</summary>
        public AttributeFieldViewModel(LevelObject target, string name, string[]? enumValues, Action onChanged)
        {
            Name = name;
            Label = Localizer.AttributeName(name);
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
            EnumOptions = options;
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
            }
        }

        /// <summary>Re-reads <see cref="Value"/> from the source, for external mutations.</summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(SelectedOption));
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
