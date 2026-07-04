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

        /// <summary>Verifies that viewport scrolling clamps offsets to the scaled level content.</summary>
        [Fact]
        public void ScrollToClampsOffsetsToContentSize()
        {
            ViewTransform t = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 2.0, PanX: 0, PanY: 0),
                levelWidth: 640,
                levelHeight: 960,
                viewportWidth: 300,
                viewportHeight: 400,
                offsetX: 1200,
                offsetY: -50);

            Assert.Equal(-980, t.PanX);
            Assert.Equal(0, t.PanY);
        }

        /// <summary>Verifies that scrolling centers content on axes smaller than the viewport.</summary>
        [Fact]
        public void ScrollToCentersContentWhenSmallerThanViewport()
        {
            ViewTransform t = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 0.5, PanX: 0, PanY: 0),
                levelWidth: 320,
                levelHeight: 240,
                viewportWidth: 400,
                viewportHeight: 300,
                offsetX: 100,
                offsetY: 100);

            Assert.Equal(120, t.PanX);
            Assert.Equal(90, t.PanY);
        }

        /// <summary>Verifies that pointer-centered zoom preserves the level point under the pointer.</summary>
        [Fact]
        public void ZoomByKeepsAnchorLevelPointUnderPointer()
        {
            ViewTransform t = new(Zoom: 2.0, PanX: -40, PanY: -80);
            Vec2 anchor = new(120, 140);
            Vec2 before = t.ScreenToLevel(anchor);

            ViewTransform zoomed = ViewNavigation.ZoomBy(t, factor: 1.25, anchor, minZoom: 0.1, maxZoom: 10.0);

            Assert.Equal(before.X, zoomed.ScreenToLevel(anchor).X, precision: 9);
            Assert.Equal(before.Y, zoomed.ScreenToLevel(anchor).Y, precision: 9);
        }

        /// <summary>Verifies that cumulative pinch scale is converted to the next incremental zoom factor.</summary>
        [Fact]
        public void PinchScaleToZoomFactorUsesScaleRatio()
        {
            Assert.Equal(1.25, ViewNavigation.PinchScaleToZoomFactor(previousScale: 1.2, currentScale: 1.5));
        }

        /// <summary>Verifies that touchpad magnify deltas are bounded before becoming zoom factors.</summary>
        [Fact]
        public void MagnifyDeltaToZoomFactorClampsExtremeDeltas()
        {
            Assert.Equal(1.1, ViewNavigation.MagnifyDeltaToZoomFactor(0.1));
            Assert.Equal(0.5, ViewNavigation.MagnifyDeltaToZoomFactor(-10));
            Assert.Equal(2.0, ViewNavigation.MagnifyDeltaToZoomFactor(10));
        }
    }
}
