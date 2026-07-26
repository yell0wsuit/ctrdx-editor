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

        /// <summary>The guide tells the user Copy and Paste reach the system clipboard.</summary>
        [Fact]
        public void GuideDocumentsTheSystemClipboard()
        {
            string strings = File.ReadAllText(LocalizationPath());
            int key = strings.IndexOf(
                "\"Guide.Article.clipboard-lock-hide.Clipboard\"",
                StringComparison.Ordinal);

            Assert.True(key >= 0);
            int end = strings.IndexOf('\n', key);
            Assert.Contains("XML", strings[key..end], StringComparison.Ordinal);
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
