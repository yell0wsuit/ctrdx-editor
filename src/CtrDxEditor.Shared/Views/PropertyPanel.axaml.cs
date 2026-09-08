using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>Properties panel view for editing selected object attributes.</summary>
    public partial class PropertyPanel : UserControl
    {
        /// <summary>Creates the properties panel and loads its XAML.</summary>
        public PropertyPanel()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void ColorSwatch_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Control { DataContext: AttributeFieldViewModel field })
            {
                return;
            }

            TutorialColorPickerDialog dialog = new(field.Value, field.CanApplyCustomColor);
            Optional<string> result = await dialog.ShowAsync();
            if (result.GetValueOrDefault() is { } value)
            {
                field.Value = value;
            }
        }
    }
}
