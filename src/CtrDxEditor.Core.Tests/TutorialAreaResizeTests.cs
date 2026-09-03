using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Dragging an area's corners keeps the rectangle normalized.</summary>
    public class TutorialAreaResizeTests
    {
        [Fact]
        public void DraggingTopLeftMovesOriginAndKeepsOppositeCorner()
        {
            TutorialArea area = new(100, 100, 50, 50);

            TutorialArea dragged = TutorialAreaResize.DragCorner(area, corner: 0, to: new Vec2(120, 110));

            Assert.Equal(120, dragged.X);
            Assert.Equal(110, dragged.Y);
            Assert.Equal(30, dragged.Width);
            Assert.Equal(40, dragged.Height);
        }

        /// <summary>Dragging a corner past its opposite flips the rectangle rather than inverting it.</summary>
        [Fact]
        public void DraggingPastTheOppositeCornerStaysPositive()
        {
            TutorialArea area = new(100, 100, 50, 50);

            TutorialArea dragged = TutorialAreaResize.DragCorner(area, corner: 2, to: new Vec2(80, 70));

            Assert.True(dragged.Width > 0);
            Assert.True(dragged.Height > 0);
            Assert.Equal(80, dragged.X);
            Assert.Equal(70, dragged.Y);
        }

        /// <summary>Dragging a corner exactly onto its opposite still yields a usable (non-degenerate) rectangle.</summary>
        [Fact]
        public void DraggingOntoTheOppositeCornerStaysNonDegenerate()
        {
            TutorialArea area = new(100, 100, 50, 50);

            TutorialArea dragged = TutorialAreaResize.DragCorner(area, corner: 1, to: new Vec2(100, 150));

            Assert.True(dragged.Width > 0);
            Assert.True(dragged.Height > 0);
        }

        /// <summary>Corners are ordered clockwise from top-left, independent of the drag helper's own math.</summary>
        [Fact]
        public void CornersAreOrderedClockwiseFromTopLeft()
        {
            TutorialArea area = new(10, 20, 30, 40);

            Vec2[] corners = TutorialAreaResize.Corners(area);

            Assert.Equal(new Vec2(10, 20), corners[0]);
            Assert.Equal(new Vec2(40, 20), corners[1]);
            Assert.Equal(new Vec2(40, 60), corners[2]);
            Assert.Equal(new Vec2(10, 60), corners[3]);
        }

        /// <summary>A point on a corner, within tolerance, hits that corner's index.</summary>
        [Theory]
        [InlineData(10, 20, 0)]
        [InlineData(40, 20, 1)]
        [InlineData(40, 60, 2)]
        [InlineData(10, 60, 3)]
        public void HitCornerFindsTheNearestCornerWithinTolerance(double x, double y, int expectedCorner)
        {
            TutorialArea area = new(10, 20, 30, 40);

            int hit = TutorialAreaResize.HitCorner(area, new Vec2(x, y), tolerance: 2);

            Assert.Equal(expectedCorner, hit);
        }

        /// <summary>A point far from every corner, including the rectangle's own interior, misses entirely.</summary>
        [Fact]
        public void HitCornerMissesTheInterior()
        {
            TutorialArea area = new(10, 20, 30, 40);

            int hit = TutorialAreaResize.HitCorner(area, new Vec2(25, 40), tolerance: 2);

            Assert.Equal(-1, hit);
        }

        /// <summary>A point just outside the tolerance radius of a corner misses.</summary>
        [Fact]
        public void HitCornerMissesJustOutsideTolerance()
        {
            TutorialArea area = new(10, 20, 30, 40);

            int hit = TutorialAreaResize.HitCorner(area, new Vec2(13.5, 20), tolerance: 3);

            Assert.Equal(-1, hit);
        }
    }
}
