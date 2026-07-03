using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;
using CtrDxEditor.Views;

namespace CtrDxEditor
{
    /// <summary>Avalonia application root for startup, content setup, and main window wiring.</summary>
    public partial class App : Application
    {
        /// <inheritdoc />
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <inheritdoc />
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow window = new();
                desktop.MainWindow = window;
                window.Opened += async (_, _) => await StartAsync(window, desktop);
            }
            base.OnFrameworkInitializationCompleted();
        }

        private static async Task StartAsync(MainWindow window, IClassicDesktopStyleApplicationLifetime desktop)
        {
            FileSettingsStore settings = new(ContentRoot.SettingsPath);
            string? resolved = ContentLocation.Resolve(
                AppContext.BaseDirectory, (await settings.LoadAsync()).ContentPath);

            if (resolved is null)
            {
                IContentInstaller installer = new FolderContentInstaller(ContentRoot.DefaultContentDir);
                ContentSetupViewModel vm = new(installer, ContentRoot.DefaultContentDir, async contentPath =>
                {
                    await SaveContentPathAsync(settings, contentPath);
                    await ShowEditorAsync(window, contentPath);
                });
                ContentSetupDialog dialog = new() { DataContext = vm };
                _ = await dialog.ShowAsync();
                if (window.DataContext is null)
                {
                    desktop.Shutdown();
                }
                return;
            }

            await ShowEditorAsync(window, resolved);
        }

        private static async Task ShowEditorAsync(MainWindow window, string contentRoot)
        {
            SpriteCache sprites = new(new FolderContentStore(contentRoot));
            await sprites.PreloadAsync();
            window.DataContext = new EditorViewModel(sprites);
        }

        private static async Task SaveContentPathAsync(FileSettingsStore settings, string contentPath)
        {
            EditorSettings s = await settings.LoadAsync();
            s.ContentPath = contentPath;
            await settings.SaveAsync(s);
        }
    }
}
