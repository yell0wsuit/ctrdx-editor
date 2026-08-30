using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Localization;
using CtrDxEditor.Playtest;
using CtrDxEditor.ViewModels;

namespace CtrDxEditor.Views
{
    // Playtest commands: hand the open level to Cut the Rope: DX through the active head's launcher.
    // The desktop launcher owns the temp file and executable resolution; the browser launcher owns its
    // game window and channel. This file deals with confirmation, location recovery where applicable,
    // and reporting failures.
    //
    // Clicking Play while a game is already running is not an error: the launcher rewrites the level
    // file, the game's own watcher notices, and it reloads in place. No second process, no toast -
    // the game flashes its restart dim, which is better feedback than a notification in a window the
    // user is not looking at.
    public partial class MainView
    {
        private bool _playtestEventsHooked;

        private async void Playtest_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not EditorViewModel vm || vm.Playtest is not { } launcher || !vm.HasDocument)
            {
                return;
            }

            // Warn about the same problems that block a save. This applies to reloads too: pushing a
            // broken level into a running game is exactly as unhelpful as launching one.
            if (vm.Document is { } doc)
            {
                IReadOnlyList<LevelWarning> warnings = LevelValidator.Validate(doc);
                if (warnings.Count > 0
                    && !await ConfirmValidationAsync(warnings, "Dialog.Validation.PlayPrompt", "Dialog.Validation.PlayProceed"))
                {
                    return;
                }
            }

            // Only a head that needs an executable is asked for one. Beyond being meaningless in a
            // browser, this call must not happen there for a second reason: it awaits a dialog, and
            // window.open is only permitted while the user's click is still on the stack. Keeping
            // the browser's warning-free path free of any real await is what makes launching work.
            string? executable = null;
            if (launcher.RequiresLocation)
            {
                executable = await EnsureDxExecutableAsync(vm);
                if (executable is null)
                {
                    return; // No path chosen; the user cancelled.
                }
            }

            if (vm.ToXml() is not { } xml)
            {
                return;
            }

            HookPlaytestEvents(launcher);

            try
            {
                // Both a cold launch and a reload are announced. The game is never raised on a
                // reload - deliberately, since desktop does not raise it either - so the user is
                // still looking at the editor, where the game's own restart dim is invisible to
                // them. Without this toast, pressing Play on a running game looks like nothing
                // happened at all.
                if (launcher.Play(executable, xml))
                {
                    Notifications()?.Show(new Notification(
                        Localizer.Get("Notification.Playtest.Launching"),
                        string.Empty,
                        NotificationType.Information));
                }
                else if (WasLaunchBlocked(launcher))
                {
                    // Only reachable when a validation dialog was confirmed first: awaiting a real
                    // dialog spends the user gesture that window.open needs, and the browser refuses.
                    // Selecting this notification is a fresh gesture, so the retry succeeds.
                    Notifications()?.Show(new Notification(
                        Localizer.Get("Notification.Playtest.Blocked"),
                        Localizer.Get("Notification.Playtest.BlockedBody"),
                        NotificationType.Warning,
                        onClick: () => _ = launcher.Play(executable, xml)));
                }
                else
                {
                    // "Sent" rather than "reloaded" on purpose: the level either went to a live game
                    // or was stashed for one that is still booting, and this wording is true of both.
                    Notifications()?.Show(new Notification(
                        Localizer.Get("Notification.Playtest.Reloaded"),
                        string.Empty,
                        NotificationType.Success));
                }
            }
            // Catch-all on purpose: this is an async void handler, so anything that escapes is an
            // unobserved exception the user would experience as the command silently doing nothing.
            catch (Exception ex)
            {
                // A bad bundle, a missing binary, quarantine, permissions: all reach the user as the
                // OS or resolver described them, rather than as an unhandled async void crash.
                Notifications()?.Show(new Notification(
                    Localizer.Get("Notification.Playtest.Failed"),
                    ex.Message,
                    NotificationType.Error));
            }
        }

        // Confirms the pick with a toast showing the path, the way saving a screenshot does: closing the
        // dialog is otherwise the command's only visible effect, and the path is what tells the user they
        // picked the bundle they meant. The toast lives here rather than in PickDxExecutableAsync so the
        // first-run pick inside Play stays silent - it already reports itself by launching the game.
        private async void SetDxLocation_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm && await PickDxExecutableAsync(vm) is { } path)
            {
                Notifications()?.Show(new Notification(
                    Localizer.Get("Notification.Playtest.LocationSet"),
                    path,
                    NotificationType.Success));
            }
        }

        // Subscribed once per view. A clean exit is silent; a non-zero one carries the game's own
        // stderr diagnostic, which is how it reports a level it could not load. An unsupported launch is
        // the case a wrong or too-old program was picked: it never handshook, so the level never played.
        private void HookPlaytestEvents(IPlaytestLauncher launcher)
        {
            if (_playtestEventsHooked)
            {
                return;
            }
            _playtestEventsHooked = true;

            launcher.Exited += (_, args) =>
            {
                if (args.ExitCode == 0)
                {
                    return;
                }
                Dispatcher.UIThread.Post(() => Notifications()?.Show(new Notification(
                    Localizer.Get("Notification.Playtest.Failed"),
                    string.IsNullOrWhiteSpace(args.StandardError)
                        ? $"Cut the Rope: DX exited with code {args.ExitCode}."
                        : args.StandardError,
                    NotificationType.Error)));
            };

            launcher.Unsupported += (_, _) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // The game window has the user's focus right now, so a dialog behind it goes unseen.
                    // Flash the taskbar / bounce the dock first, then raise the dialog to acknowledge.
                    (DataContext as EditorViewModel)?.Attention?.Demand();

                    MessageDialog dialog = new()
                    {
                        Header = Localizer.Get("Playtest.Unsupported.Header"),
                        Message = Localizer.Get("Playtest.Unsupported.Body"),
                    };
                    _ = dialog.ShowAsync();
                });
            };

            // The game is still running the level it had before, so this is a notification rather
            // than the dialog an unsupported launch raises: nothing needs acknowledging, and the
            // user is looking at the game window anyway.
            launcher.LevelRejected += (_, args) =>
            {
                Dispatcher.UIThread.Post(() => Notifications()?.Show(new Notification(
                    Localizer.Get("Notification.Playtest.Rejected"),
                    args.Message,
                    NotificationType.Error)));
            };
        }

        // Asked through a narrow capability interface: only the browser head can have a launch
        // blocked, and widening IPlaytestLauncher for it would put a browser concern in every head's
        // contract.
        private static bool WasLaunchBlocked(IPlaytestLauncher launcher)
        {
            return launcher is IBlockableLauncher { LastLaunchBlocked: true };
        }

        // Returns a usable executable path, prompting on first use or when the stored one has gone
        // missing. Returns null when the user cancels.
        private static async Task<string?> EnsureDxExecutableAsync(EditorViewModel vm)
        {
            string? stored = vm.CurrentSettingsSnapshot.DxExecutablePath;

            // A bundle or containing folder is a directory and a dev build is a plain file, so both
            // existence checks apply.
            return !string.IsNullOrWhiteSpace(stored) && (File.Exists(stored) || Directory.Exists(stored))
                ? stored
                : await PickDxExecutableAsync(vm);
        }

        // Shows the locate dialog (drop zone + Browse) and persists the pick. Returns the chosen path,
        // or null if cancelled.
        private static async Task<string?> PickDxExecutableAsync(EditorViewModel vm)
        {
            Optional<string?> result = await new PlaytestLocateDialog().ShowAsync();
            if (result.GetValueOrDefault() is not { } path)
            {
                return null;
            }

            vm.CurrentSettingsSnapshot.DxExecutablePath = path;
            vm.NotifyDxLocationChanged();
            if (vm.Settings is { } store)
            {
                await store.SaveAsync(vm.CurrentSettingsSnapshot);
            }
            return path;
        }

    }
}
