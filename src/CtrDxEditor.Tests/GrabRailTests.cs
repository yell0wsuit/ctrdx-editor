using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the movable-rail grab geometry: resolution, hook sliding, and end resizing.</summary>
    public class GrabRailTests
    {
        private static LevelObject Grab(int x, int y, string moveLength, string moveOffset, bool vertical)
        {
            return new LevelObject(new XElement(
                "grab",
                new XAttribute("x", x),
                new XAttribute("y", y),
                new XAttribute("moveLength", moveLength),
                new XAttribute("moveOffset", moveOffset),
                new XAttribute("moveVertical", vertical ? "true" : "false")));
        }

        /// <summary>moveLength &gt; 0 is movable; -1 and 0 are fixed hooks.</summary>
        [Theory]
        [InlineData("100", true)]
        [InlineData("-1", false)]
        [InlineData("0", false)]
        public void IsMovableTracksMoveLength(string moveLength, bool expected)
        {
            Assert.Equal(expected, GrabRail.IsMovable(Grab(0, 0, moveLength, "0", false)));
        }

        /// <summary>A horizontal rail spans [x - offset, x + length - offset] with the hook at x.</summary>
        [Fact]
        public void HorizontalGeometryStraddlesTheHook()
        {
            GrabRail.Geometry g = GrabRail.Of(Grab(200, 50, "100", "30", false))!.Value;
            Assert.Equal(new Vec2(170, 50), g.Start);
            Assert.Equal(new Vec2(270, 50), g.End);
            Assert.Equal(new Vec2(200, 50), g.Hook);
            Assert.False(g.Vertical);
        }

        /// <summary>A vertical rail runs along y with the same offset math.</summary>
        [Fact]
        public void VerticalGeometryRunsAlongY()
        {
            GrabRail.Geometry g = GrabRail.Of(Grab(80, 200, "120", "40", true))!.Value;
            Assert.Equal(new Vec2(80, 160), g.Start);
            Assert.Equal(new Vec2(80, 280), g.End);
            Assert.True(g.Vertical);
        }

        /// <summary>Sliding clamps the hook between the rail ends and reports the matching offset.</summary>
        [Fact]
        public void SlideHookClampsToTheRail()
        {
            GrabRail.Geometry g = GrabRail.Of(Grab(200, 50, "100", "30", false))!.Value; // rail [170, 270]

            (double hookAxis, double offset) = GrabRail.SlideHook(g, new Vec2(250, 50));
            Assert.Equal(250, hookAxis);
            Assert.Equal(80, offset);

            // Past the far end clamps to the end (offset == length).
            (hookAxis, offset) = GrabRail.SlideHook(g, new Vec2(999, 50));
            Assert.Equal(270, hookAxis);
            Assert.Equal(100, offset);

            // Past the near end clamps to the start (offset == 0).
            (hookAxis, offset) = GrabRail.SlideHook(g, new Vec2(-999, 50));
            Assert.Equal(170, hookAxis);
            Assert.Equal(0, offset);
        }

        /// <summary>Dragging the far end changes length only, never shorter than the hook's offset.</summary>
        [Fact]
        public void ResizeEndKeepsHookOnRail()
        {
            GrabRail.Geometry g = GrabRail.Of(Grab(200, 50, "100", "30", false))!.Value; // start 170, hook offset 30

            Assert.Equal(130, GrabRail.ResizeEnd(g, new Vec2(300, 50)));   // 300 - 170
            Assert.Equal(30, GrabRail.ResizeEnd(g, new Vec2(150, 50)));    // can't shrink past the hook (offset 30)
        }

        /// <summary>Hit-testing routes the ends, hook, and bar; empty space misses.</summary>
        [Fact]
        public void HitTestClassifiesRailParts()
        {
            GrabRail.Geometry g = GrabRail.Of(Grab(200, 50, "100", "30", false))!.Value; // start 170, hook 200, end 270

            Assert.Equal(GrabRail.Handle.ResizeStart, GrabRail.HitTest(g, new Vec2(170, 50), 5, 10, 12));
            Assert.Equal(GrabRail.Handle.ResizeEnd, GrabRail.HitTest(g, new Vec2(270, 50), 5, 10, 12));
            Assert.Equal(GrabRail.Handle.SlideHook, GrabRail.HitTest(g, new Vec2(200, 50), 5, 10, 12));
            Assert.Equal(GrabRail.Handle.MoveBar, GrabRail.HitTest(g, new Vec2(240, 55), 5, 10, 12));
            Assert.Equal(GrabRail.Handle.None, GrabRail.HitTest(g, new Vec2(240, 200), 5, 10, 12));
        }

        /// <summary>When the hook sits on an end, sliding wins over resizing that end.</summary>
        [Fact]
        public void HitTestPrefersHookWhenItSitsOnAnEnd()
        {
            GrabRail.Geometry g = GrabRail.Of(Grab(200, 50, "100", "0", false))!.Value; // hook == start (200,50)

            Assert.Equal(GrabRail.Handle.SlideHook, GrabRail.HitTest(g, new Vec2(200, 50), 5, 10, 12));
        }

        /// <summary>Dragging the near end changes both offset and length; offset never goes negative.</summary>
        [Fact]
        public void ResizeStartMovesOffsetAndLength()
        {
            GrabRail.Geometry g = GrabRail.Of(Grab(200, 50, "100", "30", false))!.Value; // start 170, end 270, hook 200

            (double offset, double length) = GrabRail.ResizeStart(g, new Vec2(150, 50));
            Assert.Equal(50, offset);   // 200 - 150
            Assert.Equal(120, length);  // 270 - 150

            // Dragging past the hook clamps the start at the hook (offset 0).
            (offset, length) = GrabRail.ResizeStart(g, new Vec2(260, 50));
            Assert.Equal(0, offset);
            Assert.Equal(70, length);   // 270 - 200
        }
    }
}
