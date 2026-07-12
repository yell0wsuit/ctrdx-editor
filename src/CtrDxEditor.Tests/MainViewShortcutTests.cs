using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the shortcut routing wiring that can't be exercised through headless input.</summary>
    public class MainViewShortcutTests
    {
        /// <summary>Menu chords are handled at the TopLevel in the tunnel phase so focus can't make them unreliable.</summary>
        [Fact]
        public void MenuChordsAreHandledGloballyAtTopLevel()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Shortcuts.cs"));

            // Registered on the TopLevel, tunnel phase, seeing even already-handled keys.
            Assert.Contains(
                "KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true",
                source, StringComparison.Ordinal);
            // Unregistered when the view detaches, since the TopLevel differs per attach.
            Assert.Contains("RemoveHandler(KeyDownEvent, OnTopLevelKeyDown)", source, StringComparison.Ordinal);
        }

        /// <summary>The two routing phases feed the two resolvers: bubble handles Delete/Space, tunnel the chords.</summary>
        [Fact]
        public void RoutingPhasesUseTheMatchingResolver()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Shortcuts.cs"));

            // The focused bubble handler routes to the local resolver (Delete/Space).
            Assert.Contains("EditorShortcuts.ResolveLocal(", source, StringComparison.Ordinal);
            // The global TopLevel handler routes to the command-chord resolver.
            Assert.Contains("EditorShortcuts.ResolveCommand(", source, StringComparison.Ordinal);
        }

        private static string SourcePath(params string[] parts)
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return Path.Combine([path, .. parts]);
        }
    }
}
