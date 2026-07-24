using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using CtrDxEditor.Localization;

namespace CtrDxEditor.Views
{
    /// <summary>Small "About" window: the app icon, name, and build version, shown from the macOS app menu.</summary>
    public partial class AboutDialog : Window
    {
        /// <summary>Creates the About window and fills in the app name and version.</summary>
        public AboutDialog()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<TextBlock>("TitleText")!.Text = Localizer.Get("Window.Title");
            this.FindControl<TextBlock>("VersionText")!.Text =
                Localizer.Format("Dialog.About.Version", AppVersion.Display);
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
