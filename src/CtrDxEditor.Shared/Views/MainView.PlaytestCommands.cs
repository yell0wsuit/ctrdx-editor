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
    // Playtest commands: hand the open level to Cut the Rope: DX via --level. The launcher owns the
    // temp file and the macOS .app resolution, so this file only deals with confirmation, the
    // first-run executable picker, and reporting failures.
    //
    // Clicking Play while a game is already running is not an error: the launcher rewrites the level
    // file, the game's own watcher notices, and it reloads in place. No second process, no toast -
    // the game flashes its restart dim, which is better feedback than a notification in a window the
    // user is not looking at.
    public partial class MainView
    {
        private bool _playtestExitHooked;

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

            string? executable = await EnsureDxExecutableAsync(vm);
            if (executable is null)
            {
                return; // No path chosen; the user cancelled.
            }

            if (vm.ToXml() is not { } xml)
            {
                return;
            }

            HookPlaytestExit(launcher);

            try
            {
                // Only a cold launch is announced. A reload returns false: the game flashes its own
                // restart dim, and a toast in a window the user has just switched away from would
                // report something they cannot see.
                if (launcher.Play(executable, xml))
                {
                    Notifications()?.Show(new Notification(
                        Localizer.Get("Notification.Playtest.Launching"),
                        string.Empty,
                        NotificationType.Information));
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
        // stderr diagnostic, which is how it reports a level it could not load.
        private void HookPlaytestExit(IPlaytestLauncher launcher)
        {
            if (_playtestExitHooked)
            {
                return;
            }
            _playtestExitHooked = true;

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
            if (vm.Settings is { } store)
            {
                await store.SaveAsync(vm.CurrentSettingsSnapshot);
            }
            return path;
        }

    }
}
