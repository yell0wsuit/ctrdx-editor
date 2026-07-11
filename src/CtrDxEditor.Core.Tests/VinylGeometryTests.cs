using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for vinyl (rotatedCircle) canvas geometry: size, disc radius, and handle rotation.</summary>
    public class VinylGeometryTests
    {
        private static LevelObject Vinyl(int x, int y, int size, double handleAngle, bool oneHandle)
        {
            XElement e = new("rotatedCircle",
                new XAttribute("x", x), new XAttribute("y", y),
                new XAttribute("size", size),
                new XAttribute("handleAngle", handleAngle),
                new XAttribute("oneHandle", oneHandle ? "true" : "false"));
            return new LevelObject(e);
        }

        /// <summary>Disc radius equals the size attribute in level units.</summary>
        [Fact]
        public void DiscRadiusEqualsSize()
        {
            Assert.Equal(110, VinylGeometry.DiscRadius(Vinyl(0, 0, 110, 0, false)));
        }

        /// <summary>At handleAngle 0 the right handle sits at +x and the left at -x, radius = size.</summary>
        [Fact]
        public void HandlesSitOnOppositeEdgesAtZeroAngle()
        {
            LevelObject v = Vinyl(100, 200, 50, 0, false);
            Vec2 right = VinylGeometry.HandlePosition(v, VinylGeometry.Handle.Right);
            Vec2 left = VinylGeometry.HandlePosition(v, VinylGeometry.Handle.Left);
            Assert.Equal(150, right.X, 3);
            Assert.Equal(200, right.Y, 3);
            Assert.Equal(50, left.X, 3);
            Assert.Equal(200, left.Y, 3);
        }

        /// <summary>Dragging the right handle to a point writes that point's angle from center.</summary>
        [Fact]
        public void AngleForRightHandleIsAtan2FromCenter()
        {
            LevelObject v = Vinyl(0, 0, 50, 0, false);
            double deg = VinylGeometry.AngleFor(v, VinylGeometry.Handle.Right, new Vec2(0, 10));
            Assert.Equal(90, deg, 3);
        }

        /// <summary>Dragging either handle to the same screen direction yields the same handleAngle.</summary>
        [Fact]
        public void LeftHandleDragMapsToSameAngleAsRight()
        {
            LevelObject v = Vinyl(0, 0, 50, 0, false);
            double right = VinylGeometry.AngleFor(v, VinylGeometry.Handle.Right, new Vec2(10, 10));
            double left = VinylGeometry.AngleFor(v, VinylGeometry.Handle.Left, new Vec2(-10, -10));
            Assert.Equal(45, right, 3);
            Assert.Equal(45, left, 3);
        }

        /// <summary>HitTest finds the right handle near its position and misses far away.</summary>
        [Fact]
        public void HitTestFindsHandleWithinTolerance()
        {
            LevelObject v = Vinyl(0, 0, 100, 0, false);
            Assert.Equal(VinylGeometry.Handle.Right, VinylGeometry.HitTest(v, new Vec2(100, 0), 10));
            Assert.Equal(VinylGeometry.Handle.Left, VinylGeometry.HitTest(v, new Vec2(-100, 0), 10));
            Assert.Equal(VinylGeometry.Handle.None, VinylGeometry.HitTest(v, new Vec2(0, 0), 10));
        }

        /// <summary>A one-handle disc only exposes the right handle.</summary>
        [Fact]
        public void OneHandleHidesLeftHandleFromHitTest()
        {
            LevelObject v = Vinyl(0, 0, 100, 0, true);
            Assert.Equal(VinylGeometry.Handle.None, VinylGeometry.HitTest(v, new Vec2(-100, 0), 10));
            Assert.Equal(VinylGeometry.Handle.Right, VinylGeometry.HitTest(v, new Vec2(100, 0), 10));
        }
    }
}
