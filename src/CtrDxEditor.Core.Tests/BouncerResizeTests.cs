using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for bouncer canvas resize geometry.</summary>
    public class BouncerResizeTests
    {
        /// <summary>Width class maps to the resting draw-quad trimmed widths (obj_bouncer.json quads 0 / 5).</summary>
        [Theory]
        [InlineData(1, 196.0)]
        [InlineData(2, 304.0)]
        public void WidthForSizeMatchesGameQuads(int size, double expected)
        {
            Assert.Equal(expected, BouncerResize.WidthForSize(size));
        }

        /// <summary>Nearest size snaps a dragged width to the closer of small/large (midpoint 250).</summary>
        [Theory]
        [InlineData(150, 1)]
        [InlineData(196, 1)]
        [InlineData(240, 1)]
        [InlineData(260, 2)]
        [InlineData(304, 2)]
        [InlineData(500, 2)]
        public void NearestSizeSnaps(double targetWidth, int expected)
        {
            Assert.Equal(expected, BouncerResize.NearestSize(targetWidth));
        }

        /// <summary>Dragging an end handle far out grows the bouncer to large and renames the element.</summary>
        [Fact]
        public void ApplyDragGrowsToLarge()
        {
            LevelObject bouncer = new(XElement.Parse("""<bouncer1 x="0" y="0" size="1" />"""));
            // scale/mapScale = 1: target width = |along|*2 = 400 -> large.
            BouncerResize.ApplyDrag(bouncer, new Vec2(200, 0), scale: SpritePlacement.MapScale);
            Assert.Equal("2", bouncer.GetAttr("size"));
            Assert.Equal("bouncer2", bouncer.Type);
        }

        /// <summary>Dragging an end handle in shrinks the bouncer to small and renames the element.</summary>
        [Fact]
        public void ApplyDragShrinksToSmall()
        {
            LevelObject bouncer = new(XElement.Parse("""<bouncer2 x="0" y="0" size="2" />"""));
            // target width = |along|*2 = 100 -> small.
            BouncerResize.ApplyDrag(bouncer, new Vec2(50, 0), scale: SpritePlacement.MapScale);
            Assert.Equal("1", bouncer.GetAttr("size"));
            Assert.Equal("bouncer1", bouncer.Type);
        }

        /// <summary>A point on the right end handle of a small bouncer is classified as ResizeEnd.</summary>
        [Fact]
        public void HitTestFindsEndHandle()
        {
            LevelObject bouncer = new(XElement.Parse("""<bouncer1 x="0" y="0" size="1" />"""));
            // Small half-width at scale/mapScale=1 is 98. A point near +98 on the axis is the end handle.
            SpikeResize.Handle handle = BouncerResize.HitTest(
                bouncer, new Vec2(98, 0), scale: SpritePlacement.MapScale, tolerance: 6, thickness: 12);
            Assert.Equal(SpikeResize.Handle.ResizeEnd, handle);
        }

        /// <summary>A point at the center is not a resize hit.</summary>
        [Fact]
        public void HitTestMissesInterior()
        {
            LevelObject bouncer = new(XElement.Parse("""<bouncer1 x="0" y="0" size="1" />"""));
            SpikeResize.Handle handle = BouncerResize.HitTest(
                bouncer, new Vec2(0, 0), scale: SpritePlacement.MapScale, tolerance: 6, thickness: 12);
            Assert.Equal(SpikeResize.Handle.None, handle);
        }
    }
}
