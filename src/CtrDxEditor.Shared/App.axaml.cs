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
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow window = new();
                desktop.MainWindow = window;
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

            ContentSetupViewModel vm = new(_startup.Installer, async () =>
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
