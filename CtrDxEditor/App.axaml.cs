using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
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

                string? contentPath = ContentRoot.TryResolve();
                if (contentPath is not null)
                {
                    window.DataContext = new EditorViewModel(contentPath);
                }
                else
                {
                    // The editor stays uninitialized (null DataContext) behind the setup
                    // dialog until the user provides content. Run once the host window is
                    // shown so the ReactiveDialogHost is in the visual tree.
                    window.Opened += async (_, _) => await RunSetupAsync(window, desktop);
                }
            }
            base.OnFrameworkInitializationCompleted();
        }

        private static async Task RunSetupAsync(
            MainWindow window, IClassicDesktopStyleApplicationLifetime desktop)
        {
            ContentSetupViewModel vm = new(
                ContentRoot.DefaultContentDir,
                ContentDownloader.DownloadAsync,
                SaveContentPath);

            ContentSetupDialog dialog = new() { DataContext = vm };
            Optional<string> result = await dialog.ShowAsync();

            if (result.GetValueOrDefault() is string contentPath)
            {
                window.DataContext = new EditorViewModel(contentPath);
            }
            else
            {
                desktop.Shutdown();
            }
        }

        private static void SaveContentPath(string contentPath)
        {
            EditorSettings settings = EditorSettings.Load(ContentRoot.SettingsPath);
            settings.ContentPath = contentPath;
            settings.Save(ContentRoot.SettingsPath);
        }
    }
}
