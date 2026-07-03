using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;
using CtrDxEditor.Views;

namespace CtrDxEditor
{
    /// <summary>Avalonia application root: platform-neutral startup driven by an injected <see cref="PlatformStartup"/>.</summary>
    public partial class App : Application
    {
        private readonly PlatformStartup _startup;

        /// <summary>Creates the application with its platform services.</summary>
        public App(PlatformStartup startup) => _startup = startup;

        /// <inheritdoc />
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        /// <inheritdoc />
        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow window = new();
                desktop.MainWindow = window;
                window.Opened += async (_, _) => await StartAsync(window, allowQuit: true, desktop);
            }
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                MainView view = new();
                singleView.MainView = view;
                // Single-view has no window "Opened"; start once attached to the visual tree.
                view.AttachedToVisualTree += async (_, _) => await StartAsync(view, allowQuit: false, desktop: null);
            }
            base.OnFrameworkInitializationCompleted();
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
            if (installed is not null)
            {
                await ShowEditorAsync(root, installed);
                return;
            }

            // TODO(Task 6): switch to the parameterless-completion ContentSetupViewModel constructor
            // and drop the unused downloadContentDir/contentPath parameters below.
            ContentSetupViewModel vm = new(_startup.Installer, string.Empty, async _ =>
                await ShowEditorAsync(root, _startup.InstalledStore()));
            ContentSetupDialog dialog = new() { DataContext = vm };
            _ = await dialog.ShowAsync();

            // Desktop may quit if the user dismissed setup without installing; the browser never quits.
            if (allowQuit && root.DataContext is null)
            {
                desktop?.Shutdown();
            }
        }

        private static async Task ShowEditorAsync(Control root, IContentStore store)
        {
            SpriteCache sprites = new(store);
            await sprites.PreloadAsync();
            root.DataContext = new EditorViewModel(sprites);
        }
    }
}
