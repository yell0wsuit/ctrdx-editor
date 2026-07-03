using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for view zoom and pan coordinate transforms.</summary>
    public class ViewTransformTests
    {
        /// <summary>Verifies that level-to-screen conversion applies zoom before pan.</summary>
        [Fact]
        public void LevelToScreenAppliesZoomThenPan()
        {
            ViewTransform t = new(Zoom: 2.0, PanX: 10, PanY: 5);

            Assert.Equal(new Vec2(210, 105), t.LevelToScreen(new Vec2(100, 50)));
        }

        /// <summary>Verifies that screen-to-level conversion inverts level-to-screen conversion.</summary>
        [Fact]
        public void ScreenToLevelInvertsLevelToScreen()
        {
            ViewTransform t = new(Zoom: 1.7, PanX: -33, PanY: 12);
            Vec2 level = new(164, 146);

            Vec2 round = t.ScreenToLevel(t.LevelToScreen(level));

            Assert.Equal(level.X, round.X, precision: 9);
            Assert.Equal(level.Y, round.Y, precision: 9);
        }
    }
}
