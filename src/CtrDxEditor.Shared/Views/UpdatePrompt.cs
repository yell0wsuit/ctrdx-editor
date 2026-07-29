using System;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Data;

using CtrDxEditor.Localization;
using CtrDxEditor.Update;

namespace CtrDxEditor.Views
{
    /// <summary>
    /// The startup prompt shown when GitHub has published a release newer than the running build.
    /// </summary>
    /// <remarks>
    /// The editor does not update itself, so the prompt's only outcome is opening the releases page;
    /// declining simply closes it, and the next launch asks again.
    /// </remarks>
    internal static class UpdatePrompt
    {
        /// <summary>
        /// Offers the release page and opens it when accepted. Shown in the editor's dialog host, so a
        /// view must already be attached.
        /// </summary>
        /// <param name="root">Control whose top level owns the launcher used to open the page.</param>
        public static async Task ShowAsync(Control root)
        {
            ConfirmDialog dialog = new()
            {
                Header = Localizer.Get("Dialog.Update.Header"),
                Message = Localizer.Get("Dialog.Update.Body"),
                PositiveText = Localizer.Get("Dialog.Common.Yes"),
                NegativeText = Localizer.Get("Dialog.Update.Later"),
                // Opening a web page discards nothing, so the confirming button keeps the neutral style.
                IsDestructive = false,
            };

            Optional<bool> accepted = await dialog.ShowAsync();
            if (!accepted.GetValueOrDefault() || TopLevel.GetTopLevel(root) is not { } top)
            {
                return;
            }

            _ = await top.Launcher.LaunchUriAsync(new Uri(GitHubUpdateChecker.ReleasesUrl));
        }
    }
}
