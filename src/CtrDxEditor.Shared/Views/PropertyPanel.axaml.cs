using Avalonia.Controls;
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

        private void PropertyHelp_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: AttributeFieldViewModel field } || !field.HasHelp)
            {
                return;
            }

            MessageDialog dialog = new()
            {
                Header = field.Label,
                Message = field.HelpText!,
            };
            _ = dialog.ShowAsync();
        }
    }
}
