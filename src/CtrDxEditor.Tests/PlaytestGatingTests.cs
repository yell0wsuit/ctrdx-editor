using System;
using System.IO;
using System.Text.Json;

using CtrDxEditor.Content;
using CtrDxEditor.Playtest;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Gating for the playtest commands: a level has to be open and a Cut the Rope: DX location has to
    /// have been picked. The view bindings are asserted against the XAML source, which is where the
    /// enabled state actually lives.
    /// </summary>
    public class PlaytestGatingTests
    {
        private const string Level = """
        <map>
            <layer name="settings"><map gridSize="32" width="320" height="480" /></layer>
            <layer name="Objects"><candy x="10" y="10" /></layer>
        </map>
        """;

        private static EditorViewModel Vm(string? dxPath)
        {
            return new EditorViewModel(
                new SpriteCache(new EmptyContentStore()),
                initial: new EditorSettings { DxExecutablePath = dxPath });
        }

        /// <summary>A fresh install has no location, so playing is off even with a level open.</summary>
        [Fact]
        public void OpenLevelWithoutADxLocationCannotPlaytest()
        {
            EditorViewModel vm = Vm(null);
            vm.LoadLevelXml(Level);

            Assert.False(vm.HasDxLocation);
            Assert.False(vm.CanLaunchPlaytest);
        }

        /// <summary>A location alone is not enough; there has to be something to play.</summary>
        [Fact]
        public void DxLocationWithoutALevelCannotPlaytest()
        {
            EditorViewModel vm = Vm("/Applications/Cut the Rope DX.app");

            Assert.True(vm.HasDxLocation);
            Assert.False(vm.CanLaunchPlaytest);
        }

        /// <summary>Both present is the only combination that enables the command.</summary>
        [Fact]
        public void LevelAndDxLocationCanPlaytest()
        {
            EditorViewModel vm = Vm("/Applications/Cut the Rope DX.app");
            vm.LoadLevelXml(Level);

            Assert.True(vm.CanLaunchPlaytest);
        }

        /// <summary>Whitespace is a path nobody set, not a location.</summary>
        [Fact]
        public void BlankDxPathDoesNotCount()
        {
            Assert.False(Vm("   ").HasDxLocation);
        }

        /// <summary>
        /// The snapshot is a plain settings object, so setting a location has to be announced by hand for
        /// the menu item to enable itself without reopening the level.
        /// </summary>
        [Fact]
        public void NotifyingALocationChangeRaisesTheGate()
        {
            EditorViewModel vm = Vm(null);
            vm.LoadLevelXml(Level);

            bool raised = false;
            vm.PropertyChanged += (_, e) =>
                raised |= e.PropertyName == nameof(EditorViewModel.CanLaunchPlaytest);

            vm.CurrentSettingsSnapshot.DxExecutablePath = "/Applications/Cut the Rope DX.app";
            vm.NotifyDxLocationChanged();

            Assert.True(raised);
            Assert.True(vm.CanLaunchPlaytest);
        }

        /// <summary>Opening a level after the location was set has to enable the command too.</summary>
        [Fact]
        public void LoadingALevelRaisesTheGate()
        {
            EditorViewModel vm = Vm("/Applications/Cut the Rope DX.app");

            bool raised = false;
            vm.PropertyChanged += (_, e) =>
                raised |= e.PropertyName == nameof(EditorViewModel.CanLaunchPlaytest);

            vm.LoadLevelXml(Level);

            Assert.True(raised);
        }

        /// <summary>Both the menu item and the compact drawer row bind the same gate.</summary>
        [Fact]
        public void BothPlaytestEntryPointsBindTheGate()
        {
            string xaml = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            int menu = xaml.IndexOf("Click=\"Playtest_Click\"", StringComparison.Ordinal);
            int drawer = xaml.IndexOf("Click=\"DrawerPlaytest_Click\"", StringComparison.Ordinal);
            Assert.True(menu >= 0 && drawer >= 0, "Both playtest entry points should exist.");

            foreach (int start in new[] { menu, drawer })
            {
                Assert.Contains(
                    "IsEnabled=\"{Binding CanLaunchPlaytest}\"",
                    xaml[start..(start + 200)],
                    StringComparison.Ordinal);
            }
        }

        /// <summary>The keyboard chord runs behind the same gate, so it cannot bypass a disabled menu item.</summary>
        [Fact]
        public void ThePlaytestChordRespectsTheGate()
        {
            string shortcuts = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Shortcuts.cs"));

            Assert.Contains(
                "EditorShortcut.Playtest when DataContext is EditorViewModel { CanPlaytest: true, CanLaunchPlaytest: true }",
                shortcuts,
                StringComparison.Ordinal);
        }

        /// <summary>Setting the location must tell the view, or the command stays greyed out until reload.</summary>
        [Fact]
        public void PickingALocationNotifiesTheViewModel()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.PlaytestCommands.cs"));

            int assignment = source.IndexOf("vm.CurrentSettingsSnapshot.DxExecutablePath = path;", StringComparison.Ordinal);
            Assert.True(assignment >= 0, "The pick should store the path on the snapshot.");

            int notify = source.IndexOf("vm.NotifyDxLocationChanged();", StringComparison.Ordinal);
            Assert.True(notify > assignment, "The notification belongs after the path is stored.");
        }

        /// <summary>Builds a view model around a specific launcher, which the existing helper never sets.</summary>
        /// <param name="launcher">The launcher to install, or null for a head without playtest.</param>
        /// <param name="dxPath">Stored executable path, if any.</param>
        /// <returns>A view model with no level loaded.</returns>
        private static EditorViewModel VmWith(IPlaytestLauncher? launcher, string? dxPath = null)
        {
            return new EditorViewModel(
                new SpriteCache(new EmptyContentStore()),
                initial: new EditorSettings { DxExecutablePath = dxPath },
                playtest: launcher);
        }

        /// <summary>
        /// A head whose launcher needs no executable can play as soon as a level is open, and offers
        /// no "set location" command. The browser has nothing to point at, so gating on a stored path
        /// there would disable Play permanently.
        /// </summary>
        [Fact]
        public void LauncherWithoutLocationRequirementCanPlayWithNoStoredPath()
        {
            EditorViewModel vm = VmWith(new StubLauncher { RequiresLocation = false });
            vm.LoadLevelXml(Level);

            Assert.True(vm.CanPlaytest);
            Assert.True(vm.CanLaunchPlaytest);
            Assert.False(vm.CanSetDxLocation);
        }

        /// <summary>A head that does need an executable still gates Play on having picked one.</summary>
        [Fact]
        public void LauncherRequiringLocationStillGatesOnStoredPath()
        {
            EditorViewModel vm = VmWith(new StubLauncher { RequiresLocation = true });
            vm.LoadLevelXml(Level);

            Assert.False(vm.CanLaunchPlaytest);
            Assert.True(vm.CanSetDxLocation);

            vm.CurrentSettingsSnapshot.DxExecutablePath = "/Applications/Cut the Rope DX.app";
            vm.NotifyDxLocationChanged();

            Assert.True(vm.CanLaunchPlaytest);
        }

        /// <summary>A head with no launcher at all offers neither command, whatever is stored.</summary>
        [Fact]
        public void NoLauncherOffersNeitherCommand()
        {
            EditorViewModel vm = VmWith(null, "/Applications/Cut the Rope DX.app");
            vm.LoadLevelXml(Level);

            Assert.False(vm.CanPlaytest);
            Assert.False(vm.CanSetDxLocation);
        }

        /// <summary>
        /// Setting a location has to announce the set-location gate as well as the play gate, or the
        /// menu item's visibility goes stale.
        /// </summary>
        [Fact]
        public void NotifyingALocationChangeRaisesTheSetLocationGate()
        {
            EditorViewModel vm = VmWith(new StubLauncher { RequiresLocation = true });

            bool raised = false;
            vm.PropertyChanged += (_, e) =>
                raised |= e.PropertyName == nameof(EditorViewModel.CanSetDxLocation);

            vm.NotifyDxLocationChanged();

            Assert.True(raised);
        }

        /// <summary>
        /// A browser launcher must reach Play without opening the desktop-only location dialog first.
        /// Besides being meaningless in a browser, that await would spend the gesture needed by
        /// window.open and make the normal launch path popup-blocked.
        /// </summary>
        [Fact]
        public void LocationLookupIsGatedByTheLaunchersRequirement()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.PlaytestCommands.cs"));

            int gate = source.IndexOf("if (launcher.RequiresLocation)", StringComparison.Ordinal);
            int lookup = source.IndexOf("await EnsureDxExecutableAsync(vm)", StringComparison.Ordinal);

            Assert.True(gate >= 0, "Location lookup should be guarded by RequiresLocation.");
            Assert.True(lookup > gate, "The guarded block should contain the location lookup.");
        }

        /// <summary>A blocked browser launch offers a fresh user gesture that retries the same level.</summary>
        [Fact]
        public void BlockedLaunchOffersAnInAppRetry()
        {
            Assert.NotNull(typeof(IBlockableLauncher).GetProperty(nameof(IBlockableLauncher.LastLaunchBlocked)));

            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.PlaytestCommands.cs"));
            Assert.Contains("else if (WasLaunchBlocked(launcher))", source, StringComparison.Ordinal);
            Assert.Contains("onClick: () => _ = launcher.Play(executable, xml)", source, StringComparison.Ordinal);
        }

        /// <summary>A rejected level is surfaced with localized copy and the game's diagnostic.</summary>
        [Fact]
        public void RejectedLevelHasLocalizedNotificationWiring()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.PlaytestCommands.cs"));
            Assert.Contains("launcher.LevelRejected +=", source, StringComparison.Ordinal);
            Assert.Contains("Localizer.Get(\"Notification.Playtest.Rejected\")", source, StringComparison.Ordinal);
            Assert.Contains("args.Message", source, StringComparison.Ordinal);

            string sourceDirectory = Directory.GetParent(SourcePath("CtrDxEditor.Shared"))?.FullName
                ?? throw new InvalidOperationException("Could not locate source directory.");
            string repositoryRoot = Directory.GetParent(sourceDirectory)?.FullName
                ?? throw new InvalidOperationException("Could not locate repository root.");
            string localizationPath = Path.Combine(
                repositoryRoot,
                "resources",
                "localization",
                "en.json");
            using JsonDocument localization = JsonDocument.Parse(File.ReadAllText(localizationPath));
            JsonElement root = localization.RootElement;
            Assert.True(root.TryGetProperty("Notification.Playtest.Rejected", out _));
            Assert.Equal(
                "Your browser blocked the playtest",
                root.GetProperty("Notification.Playtest.Blocked").GetString());
            Assert.Equal(
                "Allow pop-ups for this site, or select this notification to open the playtest.",
                root.GetProperty("Notification.Playtest.BlockedBody").GetString());
            Assert.False(root.TryGetProperty("Notification.Playtest.BlockedAction", out _));
            Assert.Contains(
                "the game opens in a new browser tab or window",
                root.GetProperty("Guide.Article.save-export-playtest.Playtest").GetString(),
                StringComparison.Ordinal);
            Assert.Equal(
                "Browser playtest requires the editor and game to be hosted on the same site, with pop-ups allowed.",
                root.GetProperty("Guide.Article.save-export-playtest.Browser.Warning").GetString());
        }

        /// <summary>A launcher that records calls and launches nothing.</summary>
        private sealed class StubLauncher : IPlaytestLauncher
        {
#pragma warning disable CS0067
            // Declared to satisfy IPlaytestLauncher; a stub that launches nothing never raises them.
            /// <inheritdoc />
            public event EventHandler<PlaytestExitedEventArgs>? Exited;

            /// <inheritdoc />
            public event EventHandler<PlaytestUnsupportedEventArgs>? Unsupported;

            /// <inheritdoc />
            public event EventHandler<PlaytestLevelRejectedEventArgs>? LevelRejected;
#pragma warning restore CS0067

            /// <inheritdoc />
            public bool RequiresLocation { get; init; }

            /// <summary>The XML handed to the most recent <see cref="Play"/> call.</summary>
            public string? LastXml { get; private set; }

            /// <inheritdoc />
            public bool Play(string? executablePath, string levelXml)
            {
                LastXml = levelXml;
                return true;
            }

            /// <inheritdoc />
            public void Dispose()
            {
            }
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine(path, Path.Combine(parts));
        }
    }
}
