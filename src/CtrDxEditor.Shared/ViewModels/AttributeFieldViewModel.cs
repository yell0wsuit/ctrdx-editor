using System;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    /// <summary>One selectable enum option: raw XML value plus UI label.</summary>
    public sealed record AttributeOptionViewModel(string Value, string Label);

    /// <summary>Editable field view model for a single XML attribute on a selected object.</summary>
    public sealed class AttributeFieldViewModel(LevelObject target, string name, string[]? enumValues, Action onChanged) : ViewModelBase
    {
        private readonly LevelObject _target = target;
        private readonly Action _onChanged = onChanged;

        /// <summary>The raw attribute id, used as the XML get/set key (never localized).</summary>
        public string Name { get; } = name;

        /// <summary>The localized label shown in the Properties panel.</summary>
        public string Label { get; } = Localizer.AttributeName(name);

        /// <summary>Allowed values for enum attributes, or null for free-form attributes.</summary>
        public string[]? EnumValues { get; } = enumValues;

        /// <summary>Allowed enum options with UI labels, or null for free-form attributes.</summary>
        public AttributeOptionViewModel[]? EnumOptions { get; } =
            enumValues?.Select(v => new AttributeOptionViewModel(v, LabelForOption(name, v))).ToArray();

        /// <summary>The selected enum option, mapped to the raw XML attribute value.</summary>
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

        /// <summary>The current raw XML attribute value.</summary>
        public string? Value
        {
            get => _target.GetAttr(Name);
            set
            {
                _target.SetAttr(Name, value ?? string.Empty);
                _onChanged();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedOption));
            }
        }

        /// <summary>Re-reads <see cref="Value"/> from the target, for when the object is mutated elsewhere (e.g. a canvas drag).</summary>
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
