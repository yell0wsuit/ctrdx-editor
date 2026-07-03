using System;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;

namespace CtrDxEditor.ViewModels
{
    public sealed class AttributeFieldViewModel(LevelObject target, string name, string[]? enumValues, Action onChanged) : ViewModelBase
    {
        private readonly LevelObject _target = target;
        private readonly Action _onChanged = onChanged;

        /// <summary>The raw attribute id, used as the XML get/set key (never localized).</summary>
        public string Name { get; } = name;

        /// <summary>The localized label shown in the Properties panel.</summary>
        public string Label { get; } = Localizer.AttributeName(name);

        public string[]? EnumValues { get; } = enumValues;

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
    }
}
