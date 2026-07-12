using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests palette view wiring that is difficult to exercise through headless pointer input.</summary>
    public class MainViewPaletteTests
    {
        /// <summary>The palette button stretches its content so the marquee receives a usable viewport.</summary>
        [Fact]
        public void PaletteButtonStretchesMarqueeContent()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));

            Assert.Contains("HorizontalContentAlignment=\"Stretch\"", view, StringComparison.Ordinal);
        }

        /// <summary>Lost pointer capture and view detachment cancel palette drag state through shared cleanup.</summary>
        [Fact]
        public void InterruptedPaletteDragUsesSharedCleanup()
        {
            string source = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));

            Assert.Contains("PointerCaptureLostEvent, PaletteItem_PointerCaptureLost", source, StringComparison.Ordinal);
            Assert.Contains("private void CancelPaletteDrag()", source, StringComparison.Ordinal);
            Assert.Contains("private void PaletteItem_PointerCaptureLost", source, StringComparison.Ordinal);
            Assert.Contains("CancelPaletteDrag();", source, StringComparison.Ordinal);
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
