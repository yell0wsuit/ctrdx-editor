using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
