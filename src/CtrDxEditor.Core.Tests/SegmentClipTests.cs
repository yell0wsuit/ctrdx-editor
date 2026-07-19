using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for clipping screen-space segments to the visible viewport.</summary>
    public class SegmentClipTests
    {
        private static readonly IntSize Viewport = new(800, 600);

        /// <summary>Verifies that a segment wholly inside the viewport is kept unchanged.</summary>
        [Fact]
        public void SegmentInsideViewportIsUnchanged()
        {
            Vec2 a = new(100, 100);
            Vec2 b = new(700, 500);

            Assert.True(SegmentClip.ToViewport(ref a, ref b, Viewport));
            Assert.Equal(new Vec2(100, 100), a);
            Assert.Equal(new Vec2(700, 500), b);
        }

        /// <summary>Verifies that a segment entirely off-screen is rejected so it is never drawn.</summary>
        [Fact]
        public void SegmentEntirelyOffScreenIsRejected()
        {
            Vec2 a = new(-50_000, 100);
            Vec2 b = new(-10_000, 500);

            Assert.False(SegmentClip.ToViewport(ref a, ref b, Viewport));
        }

        /// <summary>Verifies that a segment straddling the viewport is trimmed to the visible span.</summary>
        [Fact]
        public void SegmentCrossingViewportIsTrimmedToBounds()
        {
            Vec2 a = new(-100_000, 300);
            Vec2 b = new(100_000, 300);

            Assert.True(SegmentClip.ToViewport(ref a, ref b, Viewport));
            Assert.True(a.X >= -SegmentClip.Padding - 0.001);
            Assert.True(b.X <= 800 + SegmentClip.Padding + 0.001);
            Assert.Equal(300, a.Y, precision: 6);
            Assert.Equal(300, b.Y, precision: 6);
        }

        /// <summary>
        /// Verifies that clipping preserves direction: the kept span still runs a to b, so the arrow
        /// chevrons drawn along a clipped path keep pointing the way the object travels.
        /// </summary>
        [Fact]
        public void ClippedSegmentKeepsOriginalDirection()
        {
            Vec2 a = new(100_000, 300);
            Vec2 b = new(-100_000, 300);

            Assert.True(SegmentClip.ToViewport(ref a, ref b, Viewport));
            Assert.True(a.X > b.X);
        }

        /// <summary>Verifies that a diagonal segment passing only through a corner is kept.</summary>
        [Fact]
        public void DiagonalThroughCornerIsKept()
        {
            Vec2 a = new(-1000, 1600);
            Vec2 b = new(1600, -1000);

            Assert.True(SegmentClip.ToViewport(ref a, ref b, Viewport));
        }

        /// <summary>Verifies that a degenerate zero-length segment outside the viewport is rejected.</summary>
        [Fact]
        public void ZeroLengthSegmentOffScreenIsRejected()
        {
            Vec2 a = new(-9999, -9999);
            Vec2 b = new(-9999, -9999);

            Assert.False(SegmentClip.ToViewport(ref a, ref b, Viewport));
        }
    }
}
