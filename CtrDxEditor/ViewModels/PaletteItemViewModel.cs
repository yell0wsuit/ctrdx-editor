using Avalonia.Media;

namespace CtrDxEditor.ViewModels
{
    public sealed partial class PaletteItemViewModel(string element, string displayName, bool enabled, IImage? icon)
        : ViewModelBase
    {
        public string Element { get; } = element;
        public string DisplayName { get; } = displayName;
        public IImage? Icon { get; } = icon;
        public bool Enabled
        {
            get;
            set => SetProperty(ref field, value);
        } = enabled;
    }
}
