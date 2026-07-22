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

        /// <summary>Save is hidden on heads that cannot overwrite the opened file; Save As stays everywhere.</summary>
        [Fact]
        public void SaveIsHiddenWhereInPlaceSaveIsUnsupported()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string viewModel = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "ViewModels", "EditorViewModel.cs"));

            Assert.Contains("IsVisible=\"{Binding CanSaveInPlace}\"", MenuItem(view, "Save_Click"), StringComparison.Ordinal);

            // The positive assertion proves the slice really is the Save As element, so the negative one
            // below cannot pass just because the extraction returned something empty or unrelated.
            string saveAs = MenuItem(view, "SaveAs_Click");
            Assert.Contains("IsEnabled=\"{Binding HasDocument}\"", saveAs, StringComparison.Ordinal);
            Assert.DoesNotContain("IsVisible", saveAs, StringComparison.Ordinal);
            // The browser is the head without in-place save; Chromium could write back, but Safari cannot.
            Assert.Contains("CanSaveInPlace { get; } = !OperatingSystem.IsBrowser()", viewModel, StringComparison.Ordinal);
        }

        /// <summary>
        /// The Ctrl+S chord is gated on the same capability as the menu item, so it is inert on heads where
        /// Save is hidden rather than redirecting to a command the user did not press.
        /// </summary>
        [Fact]
        public void SaveChordIsGatedOnTheSameCapabilityAsTheMenuItem()
        {
            string shortcuts = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.Shortcuts.cs"));

            Assert.Contains("EditorShortcut.Save when DataContext is EditorViewModel { HasDocument: true, CanSaveInPlace: true }",
                shortcuts, StringComparison.Ordinal);
            // Save As keeps its own chord on every head, so saving stays reachable by keyboard.
            Assert.Contains("case EditorShortcut.SaveAs when DataContext is EditorViewModel { HasDocument: true }:",
                shortcuts, StringComparison.Ordinal);
        }

        /// <summary>
        /// Save still falls back to Save As at the handler, so the header's access key cannot reach a write
        /// the platform cannot perform even though the chord no longer routes there.
        /// </summary>
        [Fact]
        public void SaveFallsBackToSaveAsAtTheHandler()
        {
            string commands = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.FileCommands.cs"));

            Assert.Contains("_currentLevelFile is null || !vm.CanSaveInPlace", commands, StringComparison.Ordinal);
        }

        // The opening tag of the MenuItem carrying the given Click handler, so attribute assertions do not
        // depend on the order the attributes are written in or on where the line happens to wrap.
        private static string MenuItem(string xaml, string clickHandler)
        {
            int click = xaml.IndexOf($"Click=\"{clickHandler}\"", StringComparison.Ordinal);
            Assert.True(click >= 0, $"No MenuItem with Click=\"{clickHandler}\".");
            return xaml[xaml.LastIndexOf('<', click)..xaml.IndexOf('>', click)];
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
