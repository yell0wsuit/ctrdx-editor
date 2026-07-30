using System;
using System.IO;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Guards the two rules a drag badge is easy to get wrong: it must not appear for a plain click, and
    /// it must sit on the thing it reports. Both regressed once because a shared helper meant something
    /// subtly different than it looked like it did.
    /// </summary>
    public class DragReadoutGatingTests
    {
        /// <summary>
        /// A click that never moves is not a drag. This is the case <see cref="TouchInput.ExceedsDragSlop"/>
        /// cannot cover: for mouse it reports true on any movement at all, so it can never separate a click
        /// from a drag on the desktop.
        /// </summary>
        [Fact]
        public void AStationaryPressIsNotArmed()
        {
            Assert.False(DragReadout.IsArmed(new Vec2(400, 300), new Vec2(400, 300)));
        }

        /// <summary>Sub-threshold jitter during a click stays unarmed, which is the whole point.</summary>
        [Theory]
        [InlineData(1.0, 0.0)]
        [InlineData(0.0, -2.0)]
        [InlineData(1.5, 1.5)]
        public void JitterUnderTheThresholdIsNotArmed(double dx, double dy)
        {
            Assert.False(DragReadout.IsArmed(new Vec2(400, 300), new Vec2(400 + dx, 300 + dy)));
        }

        /// <summary>Travel past the threshold arms the badge, in any direction.</summary>
        [Theory]
        [InlineData(12.0, 0.0)]
        [InlineData(0.0, -9.0)]
        [InlineData(-6.0, 6.0)]
        public void TravelPastTheThresholdArms(double dx, double dy)
        {
            Assert.True(DragReadout.IsArmed(new Vec2(400, 300), new Vec2(400 + dx, 300 + dy)));
        }

        /// <summary>
        /// The water attribute is a depth measured up from the level's bottom edge, not a Y coordinate.
        /// Anchoring a badge at the raw value puts it near the top of a tall level and moves it the wrong
        /// way as the water rises.
        /// </summary>
        [Fact]
        public void WaterIsADepthNotAYCoordinate()
        {
            LevelBounds band = Assert.NotNull(WaterGeometry.Band(1024, 1600, 170));

            Assert.Equal(1430, band.Y, precision: 3);
        }

        /// <summary>Raising the water moves its surface up the level, never down.</summary>
        [Fact]
        public void DeeperWaterRaisesItsSurface()
        {
            LevelBounds shallow = Assert.NotNull(WaterGeometry.Band(1024, 1600, 100));
            LevelBounds deep = Assert.NotNull(WaterGeometry.Band(1024, 1600, 400));

            Assert.True(deep.Y < shallow.Y, "Deeper water must have a higher surface, not a lower one.");
        }

        /// <summary>The readout gates on its own arm distance, not on the mouse-permissive slop flag.</summary>
        [Fact]
        public void ReadoutGatesOnArmDistance()
        {
            string source = ReadReadout();

            int method = source.IndexOf("private void DrawDragReadout", StringComparison.Ordinal);
            int gate = source.IndexOf("_readoutArmed", method, StringComparison.Ordinal);
            int draw = source.IndexOf("BadgeRenderer.DrawReadout", method, StringComparison.Ordinal);

            Assert.True(gate > method, "DrawDragReadout must gate on _readoutArmed.");
            Assert.True(gate < draw, "The arm gate must precede drawing.");
        }

        /// <summary>Each new press disarms the badge, so the previous drag cannot leak into the next click.</summary>
        [Fact]
        public void EachPressDisarmsTheBadge()
        {
            string input = File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared", "Rendering", "LevelCanvas.Input.cs"));

            Assert.Contains("_readoutArmed = false;", input, StringComparison.Ordinal);
        }

        /// <summary>
        /// The water badge anchors off the shared band geometry rather than recomputing the surface, which
        /// is how the inversion got in. <c>HitsWaterHandle</c> already resolves the surface this way.
        /// </summary>
        [Fact]
        public void WaterAnchorUsesTheSharedBandGeometry()
        {
            Assert.Contains("WaterGeometry.Band", ReadReadout(), StringComparison.Ordinal);
        }

        private static string ReadReadout()
        {
            return File.ReadAllText(SourcePath(
                "CtrDxEditor.Shared", "Rendering", "LevelCanvas.DragReadout.cs"));
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
