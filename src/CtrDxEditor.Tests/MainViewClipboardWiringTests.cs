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
            Assert.Contains("ReadClipboardText", view, StringComparison.Ordinal);
            Assert.Contains("TryGetTextAsync", view, StringComparison.Ordinal);
            Assert.Contains("SetTextAsync", view, StringComparison.Ordinal);

            string viewModel = SourceText("CtrDxEditor.Shared", "ViewModels", "EditorViewModel.cs");
            Assert.DoesNotContain("TopLevel", viewModel, StringComparison.Ordinal);
            Assert.DoesNotContain("IClipboard", viewModel, StringComparison.Ordinal);
        }

        /// <summary>Cut, Copy and Paste all go through the clipboard-aware entry points.</summary>
        [Fact]
        public void ClipboardCommandsUseTheAsyncEntryPoints()
        {
            string commands = SourceText("CtrDxEditor.Shared", "Views", "MainView.Commands.cs");

            Assert.Contains("CutSelectionAsync()", commands, StringComparison.Ordinal);
            Assert.Contains("CopySelectionAsync()", commands, StringComparison.Ordinal);
            Assert.Contains("PasteFromClipboardAsync(", commands, StringComparison.Ordinal);
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

        /// <summary>A refused paste raises a toast rather than silently doing nothing.</summary>
        [Fact]
        public void RejectedPasteRaisesAWarningToast()
        {
            string commands = SourceText("CtrDxEditor.Shared", "Views", "MainView.Commands.cs");

            Assert.Contains("PasteOutcome.InvalidXml", commands, StringComparison.Ordinal);
            Assert.Contains("Notification.Paste.InvalidXml", commands, StringComparison.Ordinal);
            Assert.Contains("NotificationType.Warning", commands, StringComparison.Ordinal);

            string strings = File.ReadAllText(LocalizationPath());
            Assert.Contains("\"Notification.Paste.InvalidXml\"", strings, StringComparison.Ordinal);
        }

        /// <summary>Paste's enabled state refreshes on attachment and window activation only.</summary>
        /// <remarks>
        /// Activation covers leaving to copy elsewhere and coming back. Reading again whenever Edit opens
        /// adds lifecycle and async churn without making the inherently cached answer exact.
        /// </remarks>
        [Fact]
        public void PasteStateRefreshesOnAttachmentAndActivation()
        {
            string view = SourceText("CtrDxEditor.Shared", "Views", "MainView.axaml.cs");

            Assert.Contains("clipboardWindow.Activated += Window_Activated;", view, StringComparison.Ordinal);
            Assert.Contains("clipboardWindow.Activated -= Window_Activated;", view, StringComparison.Ordinal);
            Assert.Contains("RefreshSystemClipboardStateAsync()", view, StringComparison.Ordinal);
            Assert.DoesNotContain("EditMenu_SubmenuOpened", view, StringComparison.Ordinal);

            int wire = view.IndexOf("private void WireObjectMutated()", StringComparison.Ordinal);
            int propertyHandler = view.IndexOf("private void ViewModel_PropertyChanged", wire, StringComparison.Ordinal);
            Assert.True(wire >= 0 && propertyHandler > wire);
            ReadOnlySpan<char> wiring = view.AsSpan(wire, propertyHandler - wire);
            Assert.Contains("if (VisualRoot is not null)", wiring, StringComparison.Ordinal);
            Assert.Contains("RefreshClipboardState();", wiring, StringComparison.Ordinal);

            string markup = SourceText("CtrDxEditor.Shared", "Views", "MainView.axaml");
            Assert.DoesNotContain("SubmenuOpened=\"EditMenu_SubmenuOpened\"", markup, StringComparison.Ordinal);

            string commands = SourceText("CtrDxEditor.Shared", "Views", "MainView.Commands.cs");
            Assert.DoesNotContain("RefreshClipboardState();", commands, StringComparison.Ordinal);

            // The browser must never take the read, since it is permission-gated there.
            string viewModel = SourceText("CtrDxEditor.Shared", "ViewModels", "EditorViewModel.cs");
            int refresh = viewModel.IndexOf(
                "public async Task RefreshSystemClipboardStateAsync()",
                StringComparison.Ordinal);
            Assert.True(refresh >= 0);
            Assert.Contains(
                "OperatingSystem.IsBrowser()",
                viewModel.AsSpan(refresh, 300),
                StringComparison.Ordinal);
        }

        private static string SourceText(params string[] parts)
        {
            return File.ReadAllText(Path.Combine([SourceRoot(), .. parts]));
        }

        private static string LocalizationPath()
        {
            return Path.Combine(
                Directory.GetParent(SourceRoot())!.FullName,
                "resources",
                "localization",
                "en.json");
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
