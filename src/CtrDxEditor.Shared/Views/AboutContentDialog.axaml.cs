using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using AvaloniaDialogs.Views;

using CtrDxEditor.Localization;

namespace CtrDxEditor.Views
{
    /// <summary>In-app About dialog for browser and other single-view hosts.</summary>
    public partial class AboutContentDialog : BaseDialog
    {
        /// <summary>Creates the About dialog and fills in the localized app name and build version.</summary>
        public AboutContentDialog()
        {
            AvaloniaXamlLoader.Load(this);
            this.FindControl<TextBlock>("TitleText")!.Text = Localizer.Get("Window.Title");
            this.FindControl<TextBlock>("VersionText")!.Text =
                Localizer.Format("Dialog.About.Version", AppVersion.Display);
        }

        /// <summary>Closes the About dialog.</summary>
        /// <param name="sender">Close button that raised the event.</param>
        /// <param name="e">Click event data.</param>
        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
