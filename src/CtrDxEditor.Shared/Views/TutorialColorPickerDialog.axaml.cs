using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using AvaloniaDialogs.Views;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;

namespace CtrDxEditor.Views
{
    /// <summary>Color picker for tutorial ink. Returns canonical RGB hex, empty for the DX default, or no result on cancel.</summary>
    public partial class TutorialColorPickerDialog : BaseDialog<string>
    {
        private readonly ColorView _picker;

        /// <summary>Creates the default picker instance used by the XAML loader and designer.</summary>
        public TutorialColorPickerDialog()
            : this(null, true)
        {
        }

        /// <summary>Creates a picker initialized from the authored color, falling back to DX's white ink.</summary>
        public TutorialColorPickerDialog(string? initialValue, bool canApplyCustomColor)
        {
            CanApplyCustomColor = canApplyCustomColor;
            AvaloniaXamlLoader.Load(this);
            _picker = this.FindControl<ColorView>("Picker")!;
            _picker.Color = InitialColor(initialValue);
            DataContext = this;
        }

        /// <summary>Whether Apply is available for this icon.</summary>
        public bool CanApplyCustomColor { get; }

        /// <summary>Short explanation matching the selected icon's DX capabilities.</summary>
        public string Guidance => Localizer.Get(CanApplyCustomColor
            ? "Dialog.ColorPicker.Guidance"
            : "Dialog.ColorPicker.FullColorGuidance");

        private static Color InitialColor(string? value)
        {
            return TutorialColor.TryParse(value, out TutorialColor color)
                ? Color.FromRgb(color.Red, color.Green, color.Blue)
                : Colors.White;
        }

        private void UseDefault_Click(object? sender, RoutedEventArgs e)
        {
            Close(string.Empty);
        }

        private void Cancel_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Apply_Click(object? sender, RoutedEventArgs e)
        {
            if (CanApplyCustomColor)
            {
                Color color = _picker.Color;
                Close(TutorialColor.FormatHex(color.R, color.G, color.B));
            }
        }
    }
}
