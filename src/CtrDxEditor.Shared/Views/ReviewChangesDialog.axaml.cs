using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using AvaloniaDialogs.Views;

namespace CtrDxEditor.Views
{
    /// <summary>
    /// Read-only diff of the live level against its saved baseline, in split or unified view. Returns
    /// nothing - reverting is the undo stack's job, so the dialog never mutates the document.
    /// </summary>
    public partial class ReviewChangesDialog : BaseDialog
    {
        /// <summary>Creates the dialog. Set <see cref="Avalonia.StyledElement.DataContext"/> to a
        /// <see cref="ViewModels.ReviewChangesViewModel"/> before showing it.</summary>
        public ReviewChangesDialog()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Close_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
