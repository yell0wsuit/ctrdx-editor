using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

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

            IContentStore? installed = await _startup.ResolveInstalled();
            if (installed is not null && await TryShowEditorAsync(root, installed))
            {
                return;
            }

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

        private async Task<bool> TryShowEditorAsync(Control root, IContentStore store)
        {
            try
            {
                SpriteCache sprites = new(store, _startup.SpriteImageExtension);
                await Task.Run(sprites.PreloadAsync);
                EditorSettings initial = await _startup.Settings.LoadAsync();
                EditorViewModel editor = new(sprites, _startup.Settings, initial, _startup.Playtest);
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
