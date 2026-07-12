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
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));
            string controller = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PaletteDragController.cs"));

            // The view routes lost capture to the controller and funnels detachment through the same Cancel().
            Assert.Contains("PointerCaptureLostEvent, _paletteDrag.OnPointerCaptureLost", view, StringComparison.Ordinal);
            Assert.Contains("_paletteDrag.Cancel();", view, StringComparison.Ordinal);
            // The controller's capture-lost handler and its cleanup share the same Cancel() entry point.
            Assert.Contains("public void OnPointerCaptureLost", controller, StringComparison.Ordinal);
            Assert.Contains("public void Cancel()", controller, StringComparison.Ordinal);
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
