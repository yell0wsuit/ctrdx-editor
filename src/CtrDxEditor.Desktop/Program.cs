using System;

using Avalonia;

using CtrDxEditor.Content;
using CtrDxEditor.Desktop.Platform;
using CtrDxEditor.Desktop.Playtest;
using CtrDxEditor.Playtest;
using CtrDxEditor.Startup;
using CtrDxEditor.Update;

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
                DownloadSizeLabel = "310 MB",
                ManualDownloadUrl = ContentDownloader.AssetsUrl,
                Playtest = new ProcessPlaytestLauncher(),
                Attention = new NativeUserAttention(),
                CheckForUpdate = () => GitHubUpdateChecker.IsUpdateAvailableAsync(AppVersion.Display),
                RepointContentLocation = async () =>
                {
                    // The installer always writes to DefaultContentDir, but resolution prefers a
                    // configured path. Without this, a hand-set path holding older-but-valid content
                    // keeps winning, so the install just made is never loaded.
                    EditorSettings saved = await settings.LoadAsync();
                    if (ContentLocation.ShouldRepoint(saved.ContentPath, ContentRoot.DefaultContentDir))
                    {
                        saved.ContentPath = ContentRoot.DefaultContentDir;
                        await settings.SaveAsync(saved);
                    }
                },
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
                .WithBundledInterFont()
                .LogToTrace();
        }
    }
}
