using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
            catch (Exception ex) when (ex is InvalidOperationException or IOException
                or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                // A bad bundle, a missing binary, quarantine, permissions: all reach the user as the
                // OS or resolver described them, rather than as an unhandled async void crash.
                Notifications()?.Show(new Notification(
                    Localizer.Get("Notification.Playtest.Failed"),
                    ex.Message,
                    NotificationType.Error));
            }
        }

        private async void SetDxLocation_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is EditorViewModel vm)
            {
                _ = await PickDxExecutableAsync(vm);
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
        private async Task<string?> EnsureDxExecutableAsync(EditorViewModel vm)
        {
            string? stored = vm.CurrentSettingsSnapshot.DxExecutablePath;

            // A .app is a directory and a dev build is a plain file, so both existence checks apply.
            if (!string.IsNullOrWhiteSpace(stored) && (File.Exists(stored) || Directory.Exists(stored)))
            {
                return stored;
            }

            ConfirmDialog dialog = new()
            {
                Header = Localizer.Get("Dialog.Playtest.Locate.Header"),
                Message = Localizer.Get("Dialog.Playtest.Locate.Body"),
                PositiveText = Localizer.Get("Dialog.Playtest.Locate.Browse"),
                NegativeText = Localizer.Get("Dialog.Common.Cancel"),
            };
            Optional<bool> confirmed = await dialog.ShowAsync();
            return confirmed.GetValueOrDefault() ? await PickDxExecutableAsync(vm) : null;
        }

        // Shows the file picker and persists the pick. Returns the chosen path, or null if cancelled.
        private async Task<string?> PickDxExecutableAsync(EditorViewModel vm)
        {
            IReadOnlyList<IStorageFile> files = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = Localizer.Get("Dialog.Playtest.Picker.Title"),
                    AllowMultiple = false,
                    FileTypeFilter = [DxExecutableFileType()],
                });

            if (files.Count != 1 || files[0].TryGetLocalPath() is not { } path)
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

        // The macOS entry is the load-bearing one: NSOpenPanel descends into .app bundles instead of
        // selecting them unless the filter declares the bundle type. Declaring the Unix-executable
        // type alongside it keeps the extensionless development binary selectable too.
        private static FilePickerFileType DxExecutableFileType()
        {
            return new FilePickerFileType(Localizer.Get("Dialog.FileType.Executable"))
            {
                Patterns = OperatingSystem.IsWindows() ? ["*.exe"] : ["*"],
                AppleUniformTypeIdentifiers = ["com.apple.application-bundle", "public.unix-executable"],
            };
        }
    }
}
