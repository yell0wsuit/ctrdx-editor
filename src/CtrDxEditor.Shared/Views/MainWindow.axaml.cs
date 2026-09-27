using System.Globalization;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CtrDxEditor.Localization;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>Desktop window shell that hosts the editor <see cref="MainView"/>.</summary>
    public partial class MainWindow : Window
    {
        // Set once the user has confirmed discarding unsaved changes, so the re-issued Close proceeds.
        private bool _confirmedClose;

        /// <summary>Creates the main editor window.</summary>
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            // Overrides the plain title from XAML; the version is only known at runtime.
            Title = string.Create(
                CultureInfo.InvariantCulture, $"{Localizer.Get("Window.Title")} v{AppVersion.Display}");
        }

        /// <inheritdoc />
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            // Warn before quitting with unsaved level changes. OnClosing is synchronous, so cancel this close,
            // ask asynchronously, and re-close once confirmed (guarded by _confirmedClose to avoid a loop).
            if (_confirmedClose || e.Cancel || DataContext is not EditorViewModel { IsModified: true })
            {
                return;
            }

            e.Cancel = true;
            // A user close (the close button, or Cmd+Q, which bypasses the in-app chords) while a dialog is
            // up would stack a second prompt over it - possibly another unsaved-changes prompt from Close or
            // Open. Refuse it instead, as the in-app shortcuts already stand down; the user answers the open
            // dialog first. Programmatic and OS-shutdown closes still prompt.
            if (!e.IsProgrammatic && e.CloseReason != WindowCloseReason.OSShutdown
                && Content is MainView { IsDialogOpen: true })
            {
                return;
            }

            _ = PromptThenCloseAsync();
        }

        private async Task PromptThenCloseAsync()
        {
            if (await UnsavedChangesPrompt.ConfirmDiscardAsync("Dialog.Unsaved.Quit"))
            {
                _confirmedClose = true;
                Close();
            }
        }
    }
}
