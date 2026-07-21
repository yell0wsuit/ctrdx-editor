using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// The locate dialog's drop route. The rules it applies live in <see cref="Playtest.DxExecutableResolver"/>
    /// and are tested directly there; what is asserted here is that the dialog actually applies them,
    /// which has to be read off the source because DragEventArgs cannot be constructed in a unit test.
    /// </summary>
    public class PlaytestLocateDropTests
    {
        /// <summary>
        /// A dropped path clears the same bar as a typed one, and on macOS must additionally be a bundle.
        /// Without this a dropped level XML would be accepted and stored as the game path, failing later
        /// at launch rather than at the point the user got it wrong.
        /// </summary>
        [Fact]
        public void DroppedPathIsValidatedBeforeItIsAccepted()
        {
            string dialog = SourceText("PlaytestLocateDialog.axaml.cs");

            Assert.Contains("IsMacOS && !DxExecutableResolver.IsBundleOrBundleContainer(path)", dialog, StringComparison.Ordinal);
            Assert.Contains("return DxExecutableResolver.TryResolve(path, out _, out error);", dialog, StringComparison.Ordinal);

            int drop = dialog.IndexOf("void Host_Drop(", StringComparison.Ordinal);
            Assert.True(drop >= 0, "Host_Drop should exist.");

            int guard = dialog.IndexOf("if (IsAcceptableDrop(path, out string? error))", drop, StringComparison.Ordinal);
            int close = dialog.IndexOf("Close(path);", drop, StringComparison.Ordinal);
            Assert.True(guard >= 0 && guard < close, "Host_Drop should validate before closing with the path.");
        }

        /// <summary>A refused drop says why; the drag cursor alone does not explain the refusal.</summary>
        [Fact]
        public void RefusedDropReportsAnError()
        {
            string view = SourceText("PlaytestLocateDialog.axaml");

            // The error line has to sit outside the macOS-only block, or a drop refused on Windows or
            // Linux would set a message that is never displayed.
            int macOnly = view.IndexOf("IsVisible=\"{Binding IsMacOS}\"", StringComparison.Ordinal);
            int endOfMacOnly = view.IndexOf("</StackPanel>", macOnly, StringComparison.Ordinal);
            int error = view.IndexOf("Text=\"{Binding ErrorMessage}\"", StringComparison.Ordinal);

            Assert.True(error > endOfMacOnly, "The error text should be outside the macOS-only panel.");
        }

        private static string SourceText(string fileName)
        {
            return File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", fileName));
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
