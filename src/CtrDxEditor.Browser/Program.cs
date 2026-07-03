using Avalonia;
using Avalonia.Browser;

using CtrDxEditor;
using CtrDxEditor.Browser.Content;
using CtrDxEditor.Startup;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]

await IndexedDb.ImportAsync();
await BuildAvaloniaApp().StartBrowserAppAsync("out");

AppBuilder BuildAvaloniaApp()
{
    IndexedDbSettingsStore settings = new();
    IndexedDbContentStore contentStore = new();
    PlatformStartup startup = new()
    {
        Settings = settings,
        Installer = new BrowserContentInstaller(),
        InstalledStore = () => new IndexedDbContentStore(),
        ResolveInstalled = async () =>
            await contentStore.IsPopulatedAsync() ? contentStore : null,
    };
    return AppBuilder.Configure(() => new App(startup)).WithInterFont();
}
