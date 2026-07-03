using System.Threading.Tasks;

using Avalonia;
using Avalonia.Browser;

using CtrDxEditor;
using CtrDxEditor.Content;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]

internal sealed partial class Program
{
    private static Task Main()
    {
        return BuildAvaloniaApp().StartBrowserAppAsync("out");
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        PlatformStartup startup = new()
        {
            Settings = null!,
            Installer = null!,
            InstalledStore = null!,
            ResolveInstalled = () => Task.FromResult<IContentStore?>(null),
        };
        return AppBuilder.Configure(() => new App(startup)).WithInterFont();
    }
}
