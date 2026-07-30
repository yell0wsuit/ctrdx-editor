using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the resizable orbit ring: what exposes one, and what a drag writes back.</summary>
    public class OrbitRingTests
    {
        private static LevelObject Obj(string? path = null, string? moveSpeed = null)
        {
            XElement e = new("star");
            e.SetAttributeValue("x", "100");
            e.SetAttributeValue("y", "100");
            if (path is not null)
            {
                e.SetAttributeValue("path", path);
            }
            if (moveSpeed is not null)
            {
                e.SetAttributeValue("moveSpeed", moveSpeed);
            }
            return new LevelObject(e);
        }

        /// <summary>A travelling circular path is the ring the canvas draws, so it is the one a drag resizes.</summary>
        [Fact]
        public void ClockwiseOrbitExposesItsRadiusAndDirection()
        {
            Assert.Equal((40.0, true), OrbitRing.Of(Obj("RC40", "50")));
        }

        /// <summary>Counter-clockwise paths keep their direction, which the resize must not silently flip.</summary>
        [Fact]
        public void CounterClockwiseOrbitReportsCounterClockwise()
        {
            Assert.Equal((40.0, false), OrbitRing.Of(Obj("RW40", "50")));
        }

        /// <summary>Without a move speed the object never travels, so no ring is drawn and none can be grabbed.</summary>
        [Fact]
        public void CircularPathWithoutSpeedHasNoRing()
        {
            Assert.Null(OrbitRing.Of(Obj("RC40")));
        }

        /// <summary>A polyline is edited by its vertices, not by a ring.</summary>
        [Fact]
        public void PolylinePathHasNoRing()
        {
            Assert.Null(OrbitRing.Of(Obj("100,0", "50")));
        }

        /// <summary>The path stores a whole-number radius, so a drag lands on one.</summary>
        [Fact]
        public void DragRoundsToAWholeRadius()
        {
            Assert.Equal(60, OrbitRing.FromDrag(new Vec2(100, 100), new Vec2(159.6, 100)));
        }

        /// <summary>
        /// Dragging into the centre clamps rather than collapsing the ring: below the minimum a circular
        /// path yields fewer than two points, the orbit stops counting as movement, and the ring the
        /// pointer is holding disappears mid-drag.
        /// </summary>
        [Fact]
        public void DragClampsAtTheSmallestRealOrbit()
        {
            LevelObject orbiter = Obj("RC40", "50");
            OrbitRing.Apply(orbiter, OrbitRing.FromDrag(new Vec2(100, 100), new Vec2(100, 100)));

            Assert.Equal(OrbitRing.Min, OrbitRing.Of(orbiter)?.Radius);
            Assert.True(MoverPath.HasActiveMovement(orbiter));
        }

        /// <summary>Resizing rewrites the radius in place, leaving direction and speed alone.</summary>
        [Fact]
        public void ApplyKeepsDirectionAndSpeed()
        {
            LevelObject orbiter = Obj("RW40", "50");

            OrbitRing.Apply(orbiter, 90);

            Assert.Equal("RW90", orbiter.GetAttr("path"));
            Assert.Equal("50", orbiter.GetAttr("moveSpeed"));
        }
    }
}
