using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CtrDxEditor.Views
{
    /// <summary>Cross-platform Help commands shared by the expanded menu and compact drawer.</summary>
    public partial class MainView
    {
        /// <summary>Opens the Usage Guide in the host appropriate to the current application lifetime.</summary>
        /// <param name="sender">Menu item or compact row that raised the event.</param>
        /// <param name="e">Click event data.</param>
        private void UsageGuide_Click(object? sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is Window owner)
            {
                new UsageGuideWindow().Show(owner);
                return;
            }

            _ = new UsageGuideDialog().ShowAsync();
        }

        /// <summary>Opens About in a desktop window or an in-app browser dialog.</summary>
        /// <param name="sender">Menu item or compact row that raised the event.</param>
        /// <param name="e">Click event data.</param>
        private void About_Click(object? sender, RoutedEventArgs e)
        {
            _ = TopLevel.GetTopLevel(this) is Window owner
                ? new AboutDialog().ShowDialog(owner)
                : new AboutContentDialog().ShowAsync();
        }
    }
}
