using System;
using System.IO;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Structural guards on the readout's wiring into the canvas, which headless pointer input cannot
    /// drive. They pin the ordering and coverage rules the spec depends on.
    /// </summary>
    public class DragReadoutWiringTests
    {
        /// <summary>The readout draws last, so no handle or outline can paint over it.</summary>
        [Fact]
        public void ReadoutDrawsAfterEveryHandle()
        {
            string source = ReadRendering();

            int rope = source.IndexOf("DrawRopeLengthHandle(context", StringComparison.Ordinal);
            int readout = source.IndexOf("DrawDragReadout(context", StringComparison.Ordinal);

            Assert.True(readout >= 0, "DrawLevelContent never calls DrawDragReadout.");
            Assert.True(readout > rope, "The readout must draw after the handles it sits above.");
        }

        /// <summary>The rope handle no longer draws its own badge; the shared readout covers it.</summary>
        [Fact]
        public void RopeHandleDelegatesItsBadge()
        {
            string source = ReadRendering();

            int method = source.IndexOf("private void DrawRopeLengthHandle", StringComparison.Ordinal);
            int end = source.IndexOf("\n        private", method + 1, StringComparison.Ordinal);
            string body = source[method..(end < 0 ? source.Length : end)];

            Assert.DoesNotContain("BadgeRenderer.DrawValue", body, StringComparison.Ordinal);
            Assert.DoesNotContain("DrawValueBadge", body, StringComparison.Ordinal);
        }

        /// <summary>Every drag kind the resolver defines is reachable from the canvas's mapping.</summary>
        [Theory]
        [InlineData("DragKind.Move")]
        [InlineData("DragKind.Rotate")]
        [InlineData("DragKind.Radius")]
        [InlineData("DragKind.RopeLength")]
        [InlineData("DragKind.RailOffset")]
        [InlineData("DragKind.RailResize")]
        [InlineData("DragKind.ConveyorLength")]
        [InlineData("DragKind.ConveyorWidth")]
        [InlineData("DragKind.StripSize")]
        [InlineData("DragKind.HandJoint")]
        [InlineData("DragKind.HandBase")]
        [InlineData("DragKind.VinylAngle")]
        [InlineData("DragKind.PolylinePoint")]
        [InlineData("DragKind.TutorialWidth")]
        [InlineData("DragKind.Water")]
        public void EveryDragKindIsMapped(string kind)
        {
            Assert.Contains(kind, ReadReadout(), StringComparison.Ordinal);
        }

        /// <summary>The readout is gated on a real drag, so hovering never shows a badge.</summary>
        [Fact]
        public void ReadoutRequiresAnActiveDrag()
        {
            string source = ReadReadout();

            int method = source.IndexOf("private void DrawDragReadout", StringComparison.Ordinal);
            int gate = source.IndexOf("AnyDragActive", method, StringComparison.Ordinal);
            int draw = source.IndexOf("BadgeRenderer.DrawReadout", method, StringComparison.Ordinal);

            Assert.True(gate > method, "DrawDragReadout must consult AnyDragActive.");
            Assert.True(gate < draw, "The drag gate must precede drawing.");
        }

        /// <summary>A press that has not cleared the slop threshold is still a tap, and shows no badge.</summary>
        [Fact]
        public void ReadoutRequiresClearedSlop()
        {
            Assert.Contains("_slopCleared", ReadReadout(), StringComparison.Ordinal);
        }

        /// <summary>The canvas passes its own size so the plate can clamp into the viewport.</summary>
        [Fact]
        public void ReadoutPassesCanvasBounds()
        {
            Assert.Contains("Bounds.Size", ReadReadout(), StringComparison.Ordinal);
        }

        /// <summary>A hand dial passes the active segment so the resolver reads the angle being edited.</summary>
        [Fact]
        public void HandRotationPassesTheActiveSegment()
        {
            Assert.Contains(
                "(DragKind.Rotate, _handActiveSegment",
                ReadReadout(),
                StringComparison.Ordinal);
        }

        /// <summary>Rail readouts follow the hook or end cap that the pointer is dragging.</summary>
        [Fact]
        public void RailReadoutTracksTheActiveHandle()
        {
            string source = ReadReadout();

            Assert.Contains("GrabRail.Of(obj)", source, StringComparison.Ordinal);
            Assert.Contains("_railDrag switch", source, StringComparison.Ordinal);
            Assert.Contains("GrabRail.Handle.SlideHook => rail.Hook", source, StringComparison.Ordinal);
            Assert.Contains("GrabRail.Handle.ResizeStart => rail.Start", source, StringComparison.Ordinal);
            Assert.Contains("GrabRail.Handle.ResizeEnd => rail.End", source, StringComparison.Ordinal);
        }

        /// <summary>Rotation readouts follow the dial knob computed from the live rotation target.</summary>
        [Fact]
        public void RotationReadoutTracksTheDialKnob()
        {
            string source = ReadReadout();

            Assert.Contains("EditableRotationTarget(obj)", source, StringComparison.Ordinal);
            Assert.Contains("RotationDialRenderer.RadiusPx", source, StringComparison.Ordinal);
            Assert.Contains("ObjectRotation.KnobPosition", source, StringComparison.Ordinal);
        }

        private static string ReadRendering()
        {
            return File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.Rendering.cs"));
        }

        private static string ReadReadout()
        {
            return File.ReadAllText(SourcePath("CtrDxEditor.Shared", "Rendering", "LevelCanvas.DragReadout.cs"));
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
