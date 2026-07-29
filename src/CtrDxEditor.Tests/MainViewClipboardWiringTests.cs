using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Guards the view-side clipboard wiring, which headless layout cannot drive.</summary>
    public class MainViewClipboardWiringTests
    {
        /// <summary>The view supplies the platform clipboard; the view model never reaches for a TopLevel.</summary>
        [Fact]
        public void ClipboardAccessLivesInTheView()
        {
            string view = SourceText("CtrDxEditor.Shared", "Views", "MainView.axaml.cs");

            Assert.Contains("WriteClipboardText", view, StringComparison.Ordinal);
            Assert.Contains("SetTextAsync", view, StringComparison.Ordinal);

            string viewModel = SourceText("CtrDxEditor.Shared", "ViewModels", "EditorViewModel.cs");
            Assert.DoesNotContain("TopLevel", viewModel, StringComparison.Ordinal);
            Assert.DoesNotContain("IClipboard", viewModel, StringComparison.Ordinal);
        }

        /// <summary>Nothing anywhere reads the system clipboard back in.</summary>
        /// <remarks>
        /// The system clipboard is an outbound channel only. Reading it is what forced Paste's enabled
        /// state to be a guess - a cached observation on the desktop, an unfalsifiable assumption in the
        /// browser, where the read is permission-gated and prompts. Paste answers to the in-app buffer,
        /// so there is nothing left to guess at.
        /// </remarks>
        [Fact]
        public void TheSystemClipboardIsNeverReadBack()
        {
            foreach (string source in new[] { "MainView.axaml.cs", "MainView.Commands.cs", "MainView.Shortcuts.cs" })
            {
                string view = SourceText("CtrDxEditor.Shared", "Views", source);
                Assert.DoesNotContain("TryGetTextAsync", view, StringComparison.Ordinal);
                Assert.DoesNotContain("ReadClipboardText", view, StringComparison.Ordinal);
            }

            string viewModel = SourceText("CtrDxEditor.Shared", "ViewModels", "EditorViewModel.cs");
            Assert.DoesNotContain("ReadClipboardText", viewModel, StringComparison.Ordinal);
            Assert.DoesNotContain("RefreshSystemClipboardStateAsync", viewModel, StringComparison.Ordinal);
        }

        /// <summary>Cut and Copy go through the clipboard-aware entry points; Paste uses the buffer.</summary>
        [Fact]
        public void ClipboardCommandsUseTheAsyncEntryPoints()
        {
            string commands = SourceText("CtrDxEditor.Shared", "Views", "MainView.Commands.cs");

            Assert.Contains("CutSelectionAsync()", commands, StringComparison.Ordinal);
            Assert.Contains("CopySelectionAsync()", commands, StringComparison.Ordinal);
            Assert.Contains("PasteAt(", commands, StringComparison.Ordinal);
        }

        /// <summary>Keyboard shortcuts share the same clipboard-aware command handlers as menu clicks.</summary>
        [Fact]
        public void ClipboardShortcutsUseTheClipboardAwareHandlers()
        {
            string shortcuts = SourceText("CtrDxEditor.Shared", "Views", "MainView.Shortcuts.cs");

            Assert.Contains("Copy_Click(", shortcuts, StringComparison.Ordinal);
            Assert.Contains("Cut_Click(", shortcuts, StringComparison.Ordinal);
            Assert.Contains("Paste_Click(", shortcuts, StringComparison.Ordinal);
            Assert.DoesNotContain("copyVm.CopySelection()", shortcuts, StringComparison.Ordinal);
            Assert.DoesNotContain("cutVm.CutSelection()", shortcuts, StringComparison.Ordinal);
            Assert.DoesNotContain("pasteVm.PasteAt(", shortcuts, StringComparison.Ordinal);
        }

        /// <summary>No window-activation plumbing survives, since there is no cached state to refresh.</summary>
        [Fact]
        public void PasteNeedsNoActivationRefresh()
        {
            string view = SourceText("CtrDxEditor.Shared", "Views", "MainView.axaml.cs");

            Assert.DoesNotContain("Window_Activated", view, StringComparison.Ordinal);
            Assert.DoesNotContain("RefreshClipboardState", view, StringComparison.Ordinal);
            Assert.DoesNotContain("EditMenu_SubmenuOpened", view, StringComparison.Ordinal);

            string markup = SourceText("CtrDxEditor.Shared", "Views", "MainView.axaml");
            Assert.DoesNotContain("SubmenuOpened=\"EditMenu_SubmenuOpened\"", markup, StringComparison.Ordinal);
        }

        private static string SourceText(params string[] parts)
        {
            return File.ReadAllText(Path.Combine([SourceRoot(), .. parts]));
        }

        private static string SourceRoot()
        {
            string path = AppContext.BaseDirectory;
            while (Path.GetFileName(path) != "src")
            {
                path = Directory.GetParent(path)?.FullName
                       ?? throw new InvalidOperationException("Could not locate src directory.");
            }

            return path;
        }
    }
}
