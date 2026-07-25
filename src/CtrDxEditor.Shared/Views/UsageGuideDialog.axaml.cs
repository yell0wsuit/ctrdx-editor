using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using AvaloniaDialogs.Views;

namespace CtrDxEditor.Views
{
    /// <summary>Full-surface browser dialog host for the shared Usage Guide.</summary>
    public partial class UsageGuideDialog : BaseDialog
    {
        /// <summary>Creates the in-app guide dialog.</summary>
        public UsageGuideDialog()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>Closes the in-app guide and returns to the editor.</summary>
        /// <param name="sender">Close button that raised the event.</param>
        /// <param name="e">Click event data.</param>
        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
