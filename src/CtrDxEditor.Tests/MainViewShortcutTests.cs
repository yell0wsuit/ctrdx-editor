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

        /// <summary>Edit menus and shortcuts share document-aware command availability.</summary>
        [Fact]
        public void EditCommandsUseDocumentAwareAvailability()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string shortcuts = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Shortcuts.cs"));

            Assert.Contains("IsEnabled=\"{Binding CanCutSelection}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanCopySelection}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanPaste}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanSelectAllObjects}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanDeleteSelection}\"", view, StringComparison.Ordinal);
            Assert.Contains("IsEnabled=\"{Binding CanToggleAnimationPreview}\"", view, StringComparison.Ordinal);
            Assert.Contains("{ CanCopySelection: true } copyVm", shortcuts, StringComparison.Ordinal);
            Assert.Contains("{ CanCutSelection: true } cutVm", shortcuts, StringComparison.Ordinal);
            Assert.Contains("{ CanPaste: true } pasteVm", shortcuts, StringComparison.Ordinal);
            Assert.Contains("{ CanSelectAllObjects: true } selectVm", shortcuts, StringComparison.Ordinal);
            Assert.Contains("{ HasDocument: true } deleteVm", shortcuts, StringComparison.Ordinal);
            Assert.Contains("else if (deleteVm.CanDeleteSelection)", shortcuts, StringComparison.Ordinal);
            Assert.Contains("{ CanToggleAnimationPreview: true } previewVm", shortcuts, StringComparison.Ordinal);
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
