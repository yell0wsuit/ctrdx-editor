using System.Threading.Tasks;

using Avalonia.Data;

using CtrDxEditor.Localization;

namespace CtrDxEditor.Views
{
    /// <summary>
    /// The shared "unsaved changes" confirmation for actions that discard the open level (new, open, close,
    /// and quitting the app). The header and body are fixed; only the proceed button varies per action.
    /// </summary>
    internal static class UnsavedChangesPrompt
    {
        /// <summary>
        /// Shows the unsaved-changes prompt with an action-specific proceed button, and returns true when the
        /// user chooses to discard. The dialog is shown in the editor's dialog host, so a view must be attached.
        /// </summary>
        public static async Task<bool> ConfirmDiscardAsync(string proceedKey)
        {
            ConfirmDialog dialog = new()
            {
                Header = Localizer.Get("Dialog.Unsaved.Header"),
                Message = Localizer.Get("Dialog.Unsaved.Body"),
                PositiveText = Localizer.Get(proceedKey),
                NegativeText = Localizer.Get("Dialog.Common.Cancel"),
            };
            Optional<bool> confirmed = await dialog.ShowAsync();
            return confirmed.GetValueOrDefault();
        }
    }
}
