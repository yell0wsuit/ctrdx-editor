using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using CtrDxEditor.Content;
using CtrDxEditor.ViewModels;
using CtrDxEditor.Views;

namespace CtrDxEditor
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                string? contentPath = ContentRoot.TryResolve();
                if (contentPath is not null)
                {
                    desktop.MainWindow = ShowEditor(contentPath);
                }
                else
                {
                    desktop.MainWindow = ShowSetup(desktop);
                }
            }
            base.OnFrameworkInitializationCompleted();
        }

        private static MainWindow ShowEditor(string contentPath) =>
            new() { DataContext = new EditorViewModel(contentPath) };

        private static ContentSetupWindow ShowSetup(IClassicDesktopStyleApplicationLifetime desktop)
        {
            ContentSetupViewModel vm = new(
                ContentRoot.DefaultContentDir,
                ContentDownloader.DownloadAsync,
                SaveContentPath);

            ContentSetupWindow setup = new(vm);
            vm.Completed += () =>
            {
                MainWindow editor = ShowEditor(vm.ResolvedContentPath!);
                desktop.MainWindow = editor;
                editor.Show();
                setup.Close();
            };
            return setup;
        }

        private static void SaveContentPath(string contentPath)
        {
            EditorSettings settings = EditorSettings.Load(ContentRoot.SettingsPath);
            settings.ContentPath = contentPath;
            settings.Save(ContentRoot.SettingsPath);
        }
    }
}
