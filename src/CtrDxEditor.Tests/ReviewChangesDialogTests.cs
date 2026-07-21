using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Verifies source-level layout rules for the read-only Review Changes dialog.</summary>
    public class ReviewChangesDialogTests
    {
        /// <summary>Long XML lines wrap within the viewport and grow the vertically scrolling diff.</summary>
        [Fact]
        public void LongLinesWrapInsteadOfScrollingHorizontally()
        {
            string view = File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared", "Views", "ReviewChangesDialog.axaml"));

            Assert.Contains(
                "<Setter Property=\"HorizontalContentAlignment\" Value=\"Stretch\" />",
                view,
                StringComparison.Ordinal);
            Assert.Contains(
                "<Setter Property=\"TextWrapping\" Value=\"Wrap\" />",
                view,
                StringComparison.Ordinal);
            Assert.Equal(
                2,
                view.Split("ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\"", StringSplitOptions.None).Length - 1);
            Assert.Equal(
                2,
                view.Split("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", StringSplitOptions.None).Length - 1);
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
