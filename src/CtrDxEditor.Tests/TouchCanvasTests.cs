using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests canvas touch wiring that headless pointer input cannot drive directly.</summary>
    public class TouchCanvasTests
    {
        /// <summary>Hit tolerance is resolved through the shared helper rather than hardcoded per site.</summary>
        [Fact]
        public void HitTestingUsesTheToleranceHelper()
        {
            string input = ReadInput();

            Assert.Contains("private double HitTolerance(double basePx)", input, StringComparison.Ordinal);
            Assert.Contains("TouchInput.Tolerance(basePx, _lastPointerWasTouch)", input, StringComparison.Ordinal);
        }

        /// <summary>
        /// No hit-test site keeps a raw pixel tolerance. Any left behind stays mouse-sized and is
        /// unreachable by a fingertip, which is invisible on desktop and only shows up on a device.
        /// </summary>
        [Fact]
        public void NoHitToleranceBypassesTheHelper()
        {
            string input = ReadInput();

            Assert.DoesNotContain("9 / View.Zoom", input, StringComparison.Ordinal);
            Assert.DoesNotContain("6 / View.Zoom", input, StringComparison.Ordinal);
            Assert.DoesNotContain("7 / View.Zoom", input, StringComparison.Ordinal);
            Assert.DoesNotContain("12 / View.Zoom", input, StringComparison.Ordinal);
            Assert.DoesNotContain("18 / View.Zoom", input, StringComparison.Ordinal);
            Assert.DoesNotContain("22 / View.Zoom", input, StringComparison.Ordinal);
        }

        /// <summary>Every pointer press records its type, so tolerance reflects the pointer in use.</summary>
        [Fact]
        public void PointerPressRecordsPointerType()
        {
            string input = ReadInput();

            Assert.Contains("_lastPointerWasTouch = e.Pointer.Type == PointerType.Touch;", input, StringComparison.Ordinal);
        }

        /// <summary>A touch drag only begins once movement clears the slop threshold.</summary>
        [Fact]
        public void ObjectDragWaitsForSlop()
        {
            string input = ReadInput();

            Assert.Contains("TouchInput.ExceedsDragSlop", input, StringComparison.Ordinal);
            Assert.Contains("_slopCleared", input, StringComparison.Ordinal);
        }

        /// <summary>The slop gate resets on each press so it cannot leak between gestures.</summary>
        [Fact]
        public void SlopResetsOnPress()
        {
            string input = ReadInput();

            Assert.Contains("_slopCleared = false;", input, StringComparison.Ordinal);
        }

        /// <summary>Touch has no hover, so handles must render from selection alone.</summary>
        [Fact]
        public void HandlesRenderWithoutHoverOnTouch()
        {
            string rendering = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Rendering.cs"));

            Assert.Contains("ShowHandlesWithoutHover", rendering, StringComparison.Ordinal);
        }

        /// <summary>The touch toggle enables angle snapping independently while Alt remains available on desktop.</summary>
        [Fact]
        public void RotationSnapToggleOrAltEnablesAngleSnapping()
        {
            string input = ReadInput();

            Assert.Contains(
                "RotationSnapEnabled || mods.HasFlag(KeyModifiers.Alt)",
                input,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// The water surface handle is the one handle whose existence is gated on hover, so touch must
        /// force it visible or the water line cannot be dragged on a phone at all.
        /// </summary>
        [Fact]
        public void WaterHandleDrawsWithoutHoverOnTouch()
        {
            string rendering = File.ReadAllText(
                SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Rendering.cs"));

            Assert.Contains(
                "(_waterHandleHovered || _waterDrag || ShowHandlesWithoutHover)",
                rendering,
                StringComparison.Ordinal);
        }

        private static string ReadInput()
        {
            return File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));
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
