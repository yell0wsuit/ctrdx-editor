using System;
using System.Globalization;
using System.Threading.Tasks;

using Avalonia.Data;

using CtrDxEditor.Localization;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    /// <summary>Offers a stored unsaved-work snapshot at startup, restoring or discarding it.</summary>
    internal static class RecoveryPrompt
    {
        /// <summary>
        /// Shows the prompt when a snapshot is stored. Restore loads it; Discard clears it; dismissing the
        /// dialog keeps it for the next launch. A snapshot that fails to restore is logged and cleared.
        /// </summary>
        /// <param name="vm">The editor that receives a restored level.</param>
        public static async Task RunAsync(EditorViewModel vm)
        {
            if (vm.Recovery is not { } store || await store.LoadAsync() is not { } snapshot)
            {
                return;
            }

            ConfirmDialog dialog = new()
            {
                Header = Localizer.Get("Dialog.Recovery.Header"),
                Message = Localizer.Format(
                    "Dialog.Recovery.Body",
                    snapshot.FileName ?? Localizer.Get("Dialog.Recovery.Untitled"),
                    snapshot.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)),
                PositiveText = Localizer.Get("Dialog.Recovery.Restore"),
                NegativeText = Localizer.Get("Dialog.Recovery.Discard"),
                // Restoring discards nothing, so the confirming button keeps the neutral style.
                IsDestructive = false,
            };
            Optional<bool> choice = await dialog.ShowAsync();
            if (!choice.HasValue)
            {
                return;
            }

            if (choice.Value)
            {
                try
                {
                    vm.RestoreSnapshot(snapshot);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CtrDx] Recovered level could not be restored; discarding it.\n{ex}");
                }
            }

            await vm.ClearRecoveryAsync();
        }
    }
}
