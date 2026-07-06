using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the auto-catch grab radius geometry: reading, edge hit-testing, and drag mapping.</summary>
    public class GrabRadiusTests
    {
        private static LevelObject Grab(string? radius)
        {
            XElement e = new("grab", new XAttribute("x", "0"), new XAttribute("y", "0"));
            if (radius is not null)
            {
                e.SetAttributeValue("radius", radius);
            }
            return new LevelObject(e);
        }

        /// <summary>A positive radius reads back; missing, -1, and 0 all mean auto-catch is off.</summary>
        [Theory]
        [InlineData("100", 100.0)]
        [InlineData(null, null)]
        [InlineData("-1", null)]
        [InlineData("0", null)]
        public void OfReadsPositiveRadiusOnly(string? attr, double? expected)
        {
            Assert.Equal(expected, GrabRadius.Of(Grab(attr)));
        }

        /// <summary>A point on the circle edge, within tolerance, hits; the center and far points miss.</summary>
        [Fact]
        public void OnEdgeDetectsTheRing()
        {
            Vec2 center = new(100, 100);

            Assert.True(GrabRadius.OnEdge(center, 50, new Vec2(151, 100), 3));   // just outside, within tol
            Assert.True(GrabRadius.OnEdge(center, 50, new Vec2(148, 100), 3));   // just inside, within tol
            Assert.False(GrabRadius.OnEdge(center, 50, new Vec2(100, 100), 3));  // center
            Assert.False(GrabRadius.OnEdge(center, 50, new Vec2(160, 100), 3));  // well outside the ring
        }

        /// <summary>Dragging maps to the distance from center, floored at the minimum.</summary>
        [Fact]
        public void FromDragUsesDistanceAndClampsToMin()
        {
            Vec2 center = new(100, 100);

            Assert.Equal(80, GrabRadius.FromDrag(center, new Vec2(180, 100)));
            Assert.Equal(GrabRadius.Min, GrabRadius.FromDrag(center, new Vec2(100, 100)));
        }
    }
}
