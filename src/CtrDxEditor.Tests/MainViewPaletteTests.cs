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
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PaletteView.axaml"));

            Assert.Contains("HorizontalContentAlignment=\"Stretch\"", view, StringComparison.Ordinal);
        }

        /// <summary>The palette overlays monochrome tutorial thumbnails with a theme-aware alpha mask.</summary>
        [Fact]
        public void TutorialPaletteIconUsesDarkThemeMask()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PaletteView.axaml"));
            string theme = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Styles", "EditorTheme.axaml"));

            Assert.Contains("IsVisible=\"{Binding InvertOnDarkTheme}\"", view, StringComparison.Ordinal);
            Assert.Contains("EditorBrush.TutorialPaletteOverlay", view, StringComparison.Ordinal);
            Assert.Contains("<ImageBrush Source=\"{Binding Icon}\" Stretch=\"Uniform\"", view, StringComparison.Ordinal);
            Assert.Contains("<Color x:Key=\"EditorColor.TutorialPaletteOverlay\">#00FFFFFF</Color>", theme, StringComparison.Ordinal);
            Assert.Contains("<Color x:Key=\"EditorColor.TutorialPaletteOverlay\">#FFFFFFFF</Color>", theme, StringComparison.Ordinal);
        }

        /// <summary>Hand selection has its own warm theme token instead of reusing cyan object selection.</summary>
        [Fact]
        public void HandSegmentSelectionUsesDedicatedThemeColor()
        {
            string theme = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Styles", "EditorTheme.axaml"));
            string palette = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "CanvasPalette.cs"));

            Assert.Contains("<Color x:Key=\"EditorColor.OverlayHandSegmentSelected\">#BB4D00</Color>", theme, StringComparison.Ordinal);
            Assert.Contains("<Color x:Key=\"EditorColor.OverlayHandSegmentSelected\">#FDC700</Color>", theme, StringComparison.Ordinal);
            Assert.Contains("\"EditorColor.OverlayHandSegmentSelected\"", palette, StringComparison.Ordinal);
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

        /// <summary>
        /// Palette drag-to-place is wired through the extracted view's exposed item host, not a name lookup.
        /// </summary>
        /// <remarks>
        /// <c>FindControl</c> does not cross into a child <c>UserControl</c>'s name scope, so looking up
        /// "PaletteList" from <c>MainView</c> silently returns null once the palette lives in its own control
        /// — and mouse drag-to-place would break with no test catching it. The host must be reached through
        /// the property instead.
        /// </remarks>
        [Fact]
        public void PaletteDragWiresThroughExtractedViewItemHost()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));
            string palette = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PaletteView.axaml.cs"));

            Assert.Contains("FindControl<PaletteView>(\"Palette\")!.ItemsHost", view, StringComparison.Ordinal);
            Assert.Contains("public ItemsControl ItemsHost", palette, StringComparison.Ordinal);
            // A name lookup for the inner control from MainView would resolve to null at runtime.
            Assert.DoesNotContain("FindControl<ItemsControl>(\"PaletteList\")", view, StringComparison.Ordinal);
        }

        /// <summary>The sticky group header logic moved with the markup, into the palette's own name scope.</summary>
        [Fact]
        public void StickyHeaderLogicLivesWithThePaletteMarkup()
        {
            string view = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "MainView.axaml.cs"));
            string palette = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PaletteView.axaml.cs"));

            Assert.Contains("OnPaletteScrollChanged", palette, StringComparison.Ordinal);
            Assert.Contains("StickyHeaderHost", palette, StringComparison.Ordinal);
            Assert.Contains("StickyHeaderText", palette, StringComparison.Ordinal);
            // Left behind in MainView the handler would compile but find nothing at runtime.
            Assert.DoesNotContain("OnPaletteScrollChanged", view, StringComparison.Ordinal);
            Assert.DoesNotContain("StickyHeaderHost", view, StringComparison.Ordinal);
        }

        /// <summary>A touch press neither captures the pointer nor arms a drag.</summary>
        /// <remarks>
        /// Capturing takes the gesture from the palette sheet's <c>ScrollViewer</c> and stops the list
        /// scrolling; the drag path itself is meaningless on touch, because the sheet covers the canvas so
        /// any drop point is one the finger never saw. A tap places at the level center instead.
        /// </remarks>
        [Fact]
        public void TouchPressDoesNotCaptureOrDrag()
        {
            string controller = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PaletteDragController.cs"));

            Assert.Contains("e.Pointer.Type == PointerType.Touch", controller, StringComparison.Ordinal);
            int press = controller.IndexOf("public void OnPointerPressed", StringComparison.Ordinal);
            int moved = controller.IndexOf("public void OnPointerMoved", StringComparison.Ordinal);
            int capture = controller.IndexOf("e.Pointer.Capture(sender", StringComparison.Ordinal);
            Assert.True(press >= 0 && moved > press && capture > press && capture < moved);

            // The touch branch returns before the capture call, so only mouse and pen reach it.
            int touchReturn = controller.IndexOf("if (touch)", press, StringComparison.Ordinal);
            Assert.True(touchReturn > press && touchReturn < capture);
        }

        /// <summary>A touch swipe abandons the pending placement so scrolling never drops an object.</summary>
        [Fact]
        public void TouchSwipeCancelsPendingPlacement()
        {
            string controller = File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Views", "PaletteDragController.cs"));

            int moved = controller.IndexOf("public void OnPointerMoved", StringComparison.Ordinal);
            int released = controller.IndexOf("public void OnPointerReleased", StringComparison.Ordinal);
            Assert.True(moved >= 0 && released > moved);

            // Past the drag threshold, a touch gesture cancels rather than promoting itself to a drag.
            ReadOnlySpan<char> body = controller.AsSpan(moved, released - moved);
            Assert.Contains("if (_touch)", body, StringComparison.Ordinal);
            Assert.Contains("Cancel();", body, StringComparison.Ordinal);
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
