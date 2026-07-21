using System.Runtime.Versioning;

using Avalonia;
using Avalonia.Browser;

using CtrDxEditor;
using CtrDxEditor.Browser.Content;
using CtrDxEditor.Content;
using CtrDxEditor.Startup;

[assembly: SupportedOSPlatform("browser")]

await IndexedDb.ImportAsync();
await WebCrypto.ImportAsync();
await SafeAreaInterop.ImportAsync();
await BuildAvaloniaApp().StartBrowserAppAsync("out");

AppBuilder BuildAvaloniaApp()
{
    IndexedDbSettingsStore settings = new();
    IndexedDbContentStore contentStore = new();
    PlatformStartup startup = new()
    {
        Settings = settings,
        Installer = new BrowserContentInstaller(".webp"),
        InstalledStore = () => new IndexedDbContentStore(),
        SpriteImageExtension = ".webp",
        DownloadSizeLabel = "30 MB",
        ManualDownloadUrl = ContentDownloader.WebpAssetsUrl,
        // The asset host doesn't send CORS headers, so an in-app fetch fails in the browser;
        // offer only manual download + zip upload there.
        AllowDirectDownload = false,
        ResolveInstalled = async () =>
            await contentStore.IsPopulatedAsync() ? contentStore : null,
        SafeAreaInsets = () =>
        {
            double[] i = SafeAreaInterop.ReadInsets();
            return new Thickness(i[0], i[1], i[2], i[3]);
        },
    };
    return AppBuilder.Configure(() => new App(startup)).WithInterFont();
}
