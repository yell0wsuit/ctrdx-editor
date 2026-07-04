using System;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
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

        /// <summary>The current raw XML attribute value.</summary>
        public string? Value
        {
            get => _target.GetAttr(Name);
            set
            {
                _target.SetAttr(Name, value ?? string.Empty);
                _onChanged();
                OnPropertyChanged();
            }
        }

        /// <summary>Re-reads <see cref="Value"/> from the target, for when the object is mutated elsewhere (e.g. a canvas drag).</summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(Value));
        }
    }
}
