using System;
using System.IO;

using CtrDxEditor.Content;
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
