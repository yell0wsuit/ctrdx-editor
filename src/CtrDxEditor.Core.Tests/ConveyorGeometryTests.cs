using System.Globalization;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests conveyor on-canvas geometry: shape, bounds, hit-testing and drags.</summary>
    public class ConveyorGeometryTests
    {
        private static LevelObject Belt(double x, double y, double length, double width, double angle)
        {
            XElement e = new("transporter");
            e.SetAttributeValue("x", x.ToString(CultureInfo.InvariantCulture));
            e.SetAttributeValue("y", y.ToString(CultureInfo.InvariantCulture));
            e.SetAttributeValue("length", length.ToString(CultureInfo.InvariantCulture));
            e.SetAttributeValue("width", width.ToString(CultureInfo.InvariantCulture));
            e.SetAttributeValue("angle", angle.ToString(CultureInfo.InvariantCulture));
            return new LevelObject(e);
        }

        /// <summary>Of returns null for a non-transporter object.</summary>
        [Fact]
        public void OfReturnsNullForNonTransporter()
        {
            Assert.Null(ConveyorGeometry.Of(new LevelObject(new XElement("grab"))));
        }

        /// <summary>At angle 0 the belt extends along +X.</summary>
        [Fact]
        public void FarEndAtAngleZeroExtendsAlongPositiveX()
        {
            ConveyorGeometry.Shape s = ConveyorGeometry.Of(Belt(100, 200, 250, 50, 0))!.Value;
            Assert.Equal(100, s.Anchor.X, 3);
            Assert.Equal(200, s.Anchor.Y, 3);
            Assert.Equal(350, s.Far.X, 3);   // 100 + 250*cos0
            Assert.Equal(200, s.Far.Y, 3);   // 200 - 250*sin0
        }

        /// <summary>At angle 90 the belt extends upward on screen (smaller y).</summary>
        [Fact]
        public void FarEndAtAngle90ExtendsUpwardOnScreen()
        {
            ConveyorGeometry.Shape s = ConveyorGeometry.Of(Belt(100, 200, 250, 50, 90))!.Value;
            Assert.Equal(100, s.Far.X, 3);   // 100 + 250*cos90
            Assert.Equal(-50, s.Far.Y, 3);   // 200 - 250*sin90 (up = smaller y)
        }

        /// <summary>Bounds cover both ends grown by half the width to each side.</summary>
        [Fact]
        public void BoundsCoverBothEndsPlusHalfWidth()
        {
            ConveyorGeometry.Shape s = ConveyorGeometry.Of(Belt(100, 200, 250, 50, 0))!.Value;
            LevelBounds b = ConveyorGeometry.Bounds(s);
            Assert.Equal(100, b.X, 3);
            Assert.Equal(175, b.Y, 3);       // 200 - 25
            Assert.Equal(250, b.W, 3);
            Assert.Equal(50, b.H, 3);
        }

        /// <summary>Hit-testing the far end returns the FarEnd handle.</summary>
        [Fact]
        public void HitTestFindsFarEndHandle()
        {
            ConveyorGeometry.Shape s = ConveyorGeometry.Of(Belt(100, 200, 250, 50, 0))!.Value;
            Assert.Equal(ConveyorGeometry.Handle.FarEnd,
                ConveyorGeometry.HitTest(s, new Vec2(350, 200), endTolerance: 10, widthTolerance: 10));
        }

        /// <summary>Hit-testing a side midpoint returns the Width handle.</summary>
        [Fact]
        public void HitTestFindsWidthHandleAtSideMidpoint()
        {
            ConveyorGeometry.Shape s = ConveyorGeometry.Of(Belt(100, 200, 250, 50, 0))!.Value;
            // Side midpoint at along=125, perp=+25 => (225, 225).
            Assert.Equal(ConveyorGeometry.Handle.Width,
                ConveyorGeometry.HitTest(s, new Vec2(225, 225), endTolerance: 10, widthTolerance: 10));
        }

        /// <summary>Hit-testing far from any handle returns None.</summary>
        [Fact]
        public void HitTestReturnsNoneAwayFromHandles()
        {
            ConveyorGeometry.Shape s = ConveyorGeometry.Of(Belt(100, 200, 250, 50, 0))!.Value;
            Assert.Equal(ConveyorGeometry.Handle.None,
                ConveyorGeometry.HitTest(s, new Vec2(-500, -500), endTolerance: 10, widthTolerance: 10));
        }

        /// <summary>Dragging the far end rewrites length and angle together.</summary>
        [Fact]
        public void FarEndDragRewritesLengthAndAngle()
        {
            LevelObject belt = Belt(100, 200, 250, 50, 0);
            ConveyorGeometry.ApplyFarEndDrag(belt, new Vec2(100, 100)); // straight up: length 100, angle 90
            Assert.Equal("100", belt.GetAttr("length"));
            Assert.Equal("90", belt.GetAttr("angle"));
        }

        /// <summary>A width drag rewrites width from twice the perpendicular distance.</summary>
        [Fact]
        public void WidthDragRewritesWidthFromPerpendicularDistance()
        {
            LevelObject belt = Belt(100, 200, 250, 50, 0);
            ConveyorGeometry.ApplyWidthDrag(belt, new Vec2(225, 240)); // perp = 40 => width 80
            Assert.Equal("80", belt.GetAttr("width"));
        }

        /// <summary>A width drag clamps the width to a minimum of 1.</summary>
        [Fact]
        public void WidthDragClampsToMinimumOne()
        {
            LevelObject belt = Belt(100, 200, 250, 50, 0);
            ConveyorGeometry.ApplyWidthDrag(belt, new Vec2(225, 200)); // perp = 0 => clamp to 1
            Assert.Equal("1", belt.GetAttr("width"));
        }
    }
}
