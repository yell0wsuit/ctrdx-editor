using System;

using Avalonia;

using CtrDxEditor.Content;
using CtrDxEditor.Desktop.Playtest;
using CtrDxEditor.Playtest;
using CtrDxEditor.Startup;

namespace CtrDxEditor.Desktop
{
    internal static class Program
    {
        /// <summary>Application entry point.</summary>
        [STAThread]
        public static void Main(string[] args)
        {
            _ = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        /// <summary>Creates the configured Avalonia application builder.</summary>
        public static AppBuilder BuildAvaloniaApp()
        {
            const string spriteExt = ".png";

            // Clear playtest temp directories left behind by editor sessions that never shut down
            // cleanly, or that quit while a game was still playing. Directories owned by a live
            // process (a second editor instance) are spared.
            PlaytestTempStore.SweepStale();

            FileSettingsStore settings = new(ContentRoot.SettingsPath);
            PlatformStartup startup = new()
            {
                Settings = settings,
                Installer = new FolderContentInstaller(ContentRoot.DefaultContentDir, spriteExt),
                InstalledStore = () => new FolderContentStore(ContentRoot.DefaultContentDir),
                SpriteImageExtension = spriteExt,
                DownloadSizeLabel = "340 MB",
                ManualDownloadUrl = ContentDownloader.AssetsUrl,
                Playtest = new ProcessPlaytestLauncher(),
                ResolveInstalled = async () =>
                {
                    string? resolved = ContentLocation.Resolve(
                        [UserDataDirectory.Current, AppContext.BaseDirectory],
                        (await settings.LoadAsync()).ContentPath);
                    return resolved is null ? null : new FolderContentStore(resolved);
                },
            };
            return AppBuilder.Configure(() => new App(startup))
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
        }
    }
}
