using System.Threading.Tasks;

using Avalonia.Data;

using CtrDxEditor.Localization;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>
    /// The startup prompt shown when the installed asset bundle predates the objects this editor build
    /// knows about.
    /// </summary>
    /// <remarks>
    /// Purely an offer. The tolerant sprite preload already keeps a stale bundle working - the objects
    /// it has no art for are simply absent from the palette - so declining costs the user nothing beyond
    /// those objects, and they are asked again next launch.
    /// </remarks>
    internal static class AssetUpdatePrompt
    {
        /// <summary>
        /// Offers to re-download the asset bundle, running the normal content install when accepted.
        /// </summary>
        /// <param name="setup">Builds the content setup view model for the re-download.</param>
        public static async Task ShowAsync(ContentSetupViewModel setup)
        {
            ConfirmDialog dialog = new()
            {
                Header = Localizer.Get("Dialog.AssetUpdate.Header"),
                Message = Localizer.Get("Dialog.AssetUpdate.Body"),
                PositiveText = Localizer.Get("Dialog.AssetUpdate.Download"),
                NegativeText = Localizer.Get("Dialog.Update.Later"),
                // Re-downloading replaces content with a newer copy of the same thing; nothing is lost.
                IsDestructive = false,
            };

            Optional<bool> accepted = await dialog.ShowAsync();
            if (!accepted.GetValueOrDefault())
            {
                return;
            }

            ContentSetupDialog install = new() { DataContext = setup };

            if (setup.AllowDownload)
            {
                _ = setup.DownloadCommand.ExecuteAsync(null);
            }

            _ = await install.ShowAsync();
        }
    }
}
