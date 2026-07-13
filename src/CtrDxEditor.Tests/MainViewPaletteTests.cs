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

        /// <summary>The palette overlays monochrome tutorial thumbnails with a theme-aware alpha mask.</summary>
        [Fact]
        public void TutorialPaletteIconUsesDarkThemeMask()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml"));
            string theme = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Styles", "EditorTheme.axaml"));

            Assert.Contains("IsVisible=\"{Binding InvertOnDarkTheme}\"", view, StringComparison.Ordinal);
            Assert.Contains("EditorBrush.TutorialPaletteOverlay", view, StringComparison.Ordinal);
            Assert.Contains("<ImageBrush Source=\"{Binding Icon}\" Stretch=\"Uniform\"", view, StringComparison.Ordinal);
            Assert.Contains("<Color x:Key=\"EditorColor.TutorialPaletteOverlay\">#00FFFFFF</Color>", theme, StringComparison.Ordinal);
            Assert.Contains("<Color x:Key=\"EditorColor.TutorialPaletteOverlay\">#FFFFFFFF</Color>", theme, StringComparison.Ordinal);
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
