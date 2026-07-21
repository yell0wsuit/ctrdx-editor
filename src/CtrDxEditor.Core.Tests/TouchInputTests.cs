using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the pointer-type-dependent hit tolerance and drag threshold rules.</summary>
    public class TouchInputTests
    {
        /// <summary>Mouse tolerance is unchanged, so desktop hit-testing cannot drift.</summary>
        [Theory]
        [InlineData(9)]
        [InlineData(6)]
        [InlineData(22)]
        public void MouseToleranceIsUnscaled(double basePx)
        {
            Assert.Equal(basePx, TouchInput.Tolerance(basePx, isTouch: false));
        }

        /// <summary>Touch tolerance grows so a fingertip can land on a handle drawn for a cursor.</summary>
        [Fact]
        public void TouchToleranceIsScaledUp()
        {
            Assert.Equal(22.5, TouchInput.Tolerance(9, isTouch: true));
        }

        /// <summary>The scale turns the common 9px handle into a target near the 44pt touch guideline.</summary>
        [Fact]
        public void TouchToleranceReachesTouchTargetSize()
        {
            // Tolerance is a radius, so the usable target is twice it.
            Assert.True(TouchInput.Tolerance(9, isTouch: true) * 2 >= 44);
        }

        /// <summary>A small wobble during a tap is not a drag, so tapping cannot nudge an object.</summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, 0)]
        [InlineData(0, 5)]
        [InlineData(5, 5)]
        public void SmallTouchMovementIsNotADrag(double dx, double dy)
        {
            Assert.False(TouchInput.ExceedsDragSlop(new Vec2(100, 100), new Vec2(100 + dx, 100 + dy), isTouch: true));
        }

        /// <summary>Movement past the threshold becomes a drag.</summary>
        [Theory]
        [InlineData(11, 0)]
        [InlineData(0, -12)]
        [InlineData(20, 20)]
        public void LargeTouchMovementIsADrag(double dx, double dy)
        {
            Assert.True(TouchInput.ExceedsDragSlop(new Vec2(100, 100), new Vec2(100 + dx, 100 + dy), isTouch: true));
        }

        /// <summary>
        /// The mouse has no slop: a mouse-down-then-move of one pixel is a deliberate drag, and adding a
        /// threshold there would make precise desktop nudging feel broken.
        /// </summary>
        [Fact]
        public void MouseHasNoSlop()
        {
            Assert.True(TouchInput.ExceedsDragSlop(new Vec2(100, 100), new Vec2(101, 100), isTouch: false));
            Assert.False(TouchInput.ExceedsDragSlop(new Vec2(100, 100), new Vec2(100, 100), isTouch: false));
        }
    }
}
