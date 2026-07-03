using Avalonia.Media;

namespace CtrDxEditor.ViewModels
{
    /// <summary>Palette entry for one placeable object type.</summary>
    public sealed partial class PaletteItemViewModel(string element, string displayName, bool enabled, IImage? icon)
        : ViewModelBase
    {
        /// <summary>The raw object element name to place.</summary>
        public string Element { get; } = element;

        /// <summary>The localized display name shown in the palette.</summary>
        public string DisplayName { get; } = displayName;

        /// <summary>Small sprite preview shown in the palette.</summary>
        public IImage? Icon { get; } = icon;

        /// <summary>Whether the palette item can currently be placed.</summary>
        public bool Enabled
        {
            get;
            set => SetProperty(ref field, value);
        } = enabled;
    }
}
