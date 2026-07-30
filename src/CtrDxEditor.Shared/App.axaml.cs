using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

using CtrDxEditor.Content;
using CtrDxEditor.Startup;
using CtrDxEditor.ViewModels;
using CtrDxEditor.Views;

namespace CtrDxEditor
{
    /// <summary>Avalonia application root: platform-neutral startup driven by an injected <see cref="PlatformStartup"/>.</summary>
    /// <remarks>Creates the application with its platform services.</remarks>
    public partial class App(PlatformStartup startup) : Application
    {
        private readonly PlatformStartup _startup = startup;

        /// <summary>Parameterless constructor required by Avalonia's XAML runtime loader (previewer/hot reload); never used for actual app startup.</summary>
        public App() : this(null!)
        {
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <inheritdoc />
        public override void OnFrameworkInitializationCompleted()
        {
            if (_startup is null)
            {
                // The parameterless constructor exists only for the previewer and hot reload.
                SafeAreaProbe.PlatformSource = null;
                base.OnFrameworkInitializationCompleted();
                return;
            }

            // The browser head reads insets from CSS env(); desktop leaves this null and falls back to
            // InsetsManager, which reports nothing there anyway.
            SafeAreaProbe.PlatformSource = _startup.SafeAreaInsets;

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow window = new();
                desktop.MainWindow = window;
                // Ends any running playtest and clears its temp directory on quit. Best-effort
                // inside the launcher, so a failed kill or delete can never block shutdown.
                desktop.ShutdownRequested += (_, _) => _startup.Playtest?.Dispose();
                window.Opened += async (_, _) => await StartAsync(window, allowQuit: true, desktop);
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                MainView view = new();
                // Single-view has no window "Opened"; start once attached to the visual tree.
                // Subscribe before assigning MainView: the browser lifetime attaches the view to
                // the visual tree synchronously inside the setter, so subscribing after would miss the event.
                view.AttachedToVisualTree += async (_, _) => await StartAsync(view, allowQuit: false, desktop: null);
                singleView.MainView = view;
            }
            base.OnFrameworkInitializationCompleted();
        }

        /// <summary>macOS app-menu "About" handler: shows the editor's own About window, owned by the main window.</summary>
        private void OnAboutClick(object? sender, EventArgs e)
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } owner })
            {
                _ = new AboutDialog().ShowDialog(owner);
            }
        }

        private bool _started;

        private async Task StartAsync(Control root, bool allowQuit, IClassicDesktopStyleApplicationLifetime? desktop)
        {
            if (_started)
            {
                return; // AttachedToVisualTree can fire more than once.
            }
            _started = true;

            // Deliberately not awaited: nothing here needs the result, and the import it triggers should
            // overlap the content resolve and sprite preload below rather than delay them.
            _ = WarmUpFilePickerAsync(root);

            IContentStore? installed = await _startup.ResolveInstalled();
            if (installed is null || !await TryShowEditorAsync(root, installed))
            {
                // Either no content is installed, or installed content passed the cheap existence check
                // yet failed to actually load (wrong-platform bundle, corrupt atlas, ...): run setup.
                // Browser setup blocks dismissal because the editor cannot run without content. Keep the
                // loop as a defensive fallback in case the dialog is removed by some mechanism other than
                // its normal close path. Desktop instead quits on dismissal.
                do
                {
                    ContentSetupViewModel vm = new(
                        _startup.Installer,
                        async () => await TryShowEditorAsync(root, _startup.InstalledStore()),
                        allowQuit: allowQuit,
                        allowManualDownload: true,
                        allowDownload: _startup.AllowDirectDownload,
                        downloadSizeLabel: _startup.DownloadSizeLabel,
                        manualDownloadUrl: _startup.ManualDownloadUrl);
                    ContentSetupDialog dialog = new() { DataContext = vm };
                    _ = await dialog.ShowAsync();

                    if (allowQuit && root.DataContext is null)
                    {
                        // Desktop: dismissing setup without installing quits the app.
                        desktop?.Shutdown();
                        return;
                    }
                }
                while (root.DataContext is null);
            }

            // Deliberately not awaited: the editor is already usable, and neither check blocks it.
            // Reached only once the editor is up, so these can never stack on top of the content setup
            // dialog the user must complete first.
            _ = PromptForStaleThingsAsync(root);
        }

        /// <summary>
        /// Runs the startup "you are behind" checks in sequence: outdated assets first, then a newer
        /// editor release.
        /// </summary>
        /// <param name="root">Attached control that hosts the dialogs and owns the URI launcher.</param>
        /// <remarks>
        /// Sequential and awaited so the two prompts cannot appear at once. Assets come first because
        /// they are the ones actively costing the user objects in the palette right now.
        /// </remarks>
        private async Task PromptForStaleThingsAsync(Control root)
        {
            await CheckForStaleAssetsAsync(root);
            await CheckForUpdateAsync(root);
        }

        /// <summary>Offers to re-download the asset bundle when the installed one is out of date.</summary>
        /// <param name="root">Attached control whose data context is replaced if content is reinstalled.</param>
        /// <remarks>
        /// Swallows its own failures: an editor that is already running must not be taken down by a
        /// check about content it is currently managing without.
        /// </remarks>
        private async Task CheckForStaleAssetsAsync(Control root)
        {
            try
            {
                if (await _startup.ResolveInstalled() is not { } installed
                    || !await ContentVersion.IsOutdatedAsync(installed))
                {
                    return;
                }

                ContentSetupViewModel vm = new(
                    _startup.Installer,
                    async () =>
                    {
                        // Before loading it: resolution prefers a configured content path, so a stale
                        // one would keep winning and re-prompt on every launch despite the download.
                        if (_startup.RepointContentLocation is { } repoint)
                        {
                            await repoint();
                        }
                        _ = await TryShowEditorAsync(root, _startup.InstalledStore());
                    },
                    // Optional download: never offer to quit, but always allow backing out - unlike
                    // first-run setup, the editor behind this dialog already works.
                    allowQuit: false,
                    allowManualDownload: true,
                    allowDownload: _startup.AllowDirectDownload,
                    downloadSizeLabel: _startup.DownloadSizeLabel,
                    manualDownloadUrl: _startup.ManualDownloadUrl,
                    canDismiss: true,
                    isUpdate: true);
                // The same dialog first-run setup uses, re-labelled: one Download button, one consent,
                // and the whole install surface (manual download, zip upload, progress, verification,
                // error reporting) already behaves correctly for a re-download.
                ContentSetupDialog dialog = new() { DataContext = vm };
                _ = await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CtrDx] Asset update prompt failed; continuing without it.\n{ex}");
            }
        }

        /// <summary>Offers the release page when GitHub has published a build newer than this one.</summary>
        /// <param name="root">Attached control that hosts the dialog and owns the URI launcher.</param>
        /// <remarks>
        /// Skipped entirely on heads that leave <c>CheckForUpdate</c> null (the browser). Swallows its
        /// own failures, since it runs unawaited and must never bring startup down with an unobserved
        /// exception.
        /// </remarks>
        private async Task CheckForUpdateAsync(Control root)
        {
            try
            {
                if (_startup.CheckForUpdate is { } check && await check())
                {
                    await UpdatePrompt.ShowAsync(root);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CtrDx] Update prompt failed; continuing without it.\n{ex}");
            }
        }

        /// <summary>
        /// Resolves Avalonia's browser storage module up front, so the first file picker opens on one tap.
        /// </summary>
        /// <remarks>
        /// <c>BrowserStorageProvider.OpenFilePickerAsync</c> opens with <c>await AvaloniaModule.ImportStorage()</c>,
        /// a <c>Lazy&lt;Task&gt;</c> around a dynamic import of storage.js. On the very first call that await
        /// genuinely suspends, so the picker's <c>input.click()</c> lands after the tap's user activation has
        /// lapsed - and iOS Safari opens the file sheet only from inside one, which cost the user a wasted
        /// first tap on both the level open and the content zip upload. Later calls await an already-completed
        /// task and resume synchronously, which is why only the first pick per page load misbehaved.
        /// <c>TryGetWellKnownFolderAsync</c> awaits that same import while needing no activation and showing no
        /// UI, so it resolves the Lazy long before the user can reach a picker.
        /// </remarks>
        private static async Task WarmUpFilePickerAsync(Control root)
        {
            // Desktop has a native picker with no module to import, so there is nothing to warm there.
            if (!OperatingSystem.IsBrowser() || TopLevel.GetTopLevel(root)?.StorageProvider is not { } storage)
            {
                return;
            }

            try
            {
                _ = await storage.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
            }
            catch (Exception ex)
            {
                // Caught rather than left to fault this unawaited task: the worst case is the extra tap
                // this exists to avoid, which must never take startup down with an unobserved exception.
                Console.WriteLine($"[CtrDx] File picker warm-up failed; the first pick may need two taps.\n{ex}");
            }
        }

        private async Task<bool> TryShowEditorAsync(Control root, IContentStore store)
        {
            try
            {
                SpriteCache sprites = new(store, _startup.SpriteImageExtension);
                await Task.Run(sprites.PreloadAsync);
                EditorSettings initial = await _startup.Settings.LoadAsync();
                EditorViewModel editor = new(sprites, _startup.Settings, initial, _startup.Playtest, _startup.Attention);
                editor.InitializeDecorationFromSettings();
                root.DataContext = editor;
                return true;
            }
            catch (Exception ex)
            {
                // Content resolved but can't be loaded; surface it and let the caller re-run setup
                // rather than crashing the app with an unhandled exception.
                Console.WriteLine($"[CtrDx] Installed content failed to load; falling back to setup.\n{ex}");
                return false;
            }
        }
    }
}
