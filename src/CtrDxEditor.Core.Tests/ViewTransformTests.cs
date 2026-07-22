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

        /// <summary>Verifies that viewport scrolling clamps offsets to the overscroll range.</summary>
        [Fact]
        public void ScrollToClampsOffsetsToOverscrollRange()
        {
            ViewTransform t = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 2.0, PanX: 0, PanY: 0),
                levelWidth: 640,
                levelHeight: 960,
                viewportWidth: 300,
                viewportHeight: 400,
                offsetX: 1200,
                offsetY: -50);

            // X: content 1280, keep 64, panMax 236, range 1452 -> 236 - 1200.
            Assert.Equal(-964, t.PanX);

            // Y: content 1920, keep 64, panMax 336, negative offset clamps to 0.
            Assert.Equal(336, t.PanY);
        }

        /// <summary>Verifies that content smaller than the viewport is locked to its centered position.</summary>
        [Fact]
        public void ScrollToCentersContentSmallerThanViewport()
        {
            ViewTransform t = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 0.5, PanX: 0, PanY: 0),
                levelWidth: 320,
                levelHeight: 240,
                viewportWidth: 400,
                viewportHeight: 300,
                offsetX: 100,
                offsetY: 100);

            // Content 160x120 fits the 400x300 viewport, so both axes lock to center regardless of offset.
            Assert.Equal(120, t.PanX);
            Assert.Equal(90, t.PanY);
        }

        /// <summary>Verifies that panning to the maximum offset leaves exactly the keep sliver on screen.</summary>
        [Fact]
        public void ScrollToStopsWithKeepSliverAtMaximumOffset()
        {
            ViewTransform t = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 1.0, PanX: 0, PanY: 0),
                levelWidth: 1000,
                levelHeight: 1000,
                viewportWidth: 400,
                viewportHeight: 400,
                offsetX: 99999,
                offsetY: 99999);

            // Content spans [-936, 64], leaving a 64px sliver against the viewport's left edge.
            Assert.Equal(-936, t.PanX);
            Assert.Equal(-936, t.PanY);
        }

        /// <summary>Verifies that panning to the zero offset leaves exactly the keep sliver on the opposite edge.</summary>
        [Fact]
        public void ScrollToStopsWithKeepSliverAtZeroOffset()
        {
            ViewTransform t = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 1.0, PanX: 0, PanY: 0),
                levelWidth: 1000,
                levelHeight: 1000,
                viewportWidth: 400,
                viewportHeight: 400,
                offsetX: 0,
                offsetY: 0);

            // Content spans [336, 1336], leaving a 64px sliver against the viewport's right edge.
            Assert.Equal(336, t.PanX);
            Assert.Equal(336, t.PanY);
        }

        /// <summary>Verifies that an axis whose content fits the viewport is locked to center at every offset.</summary>
        [Fact]
        public void ScrollToLocksContentToCenterWhenItFitsViewport()
        {
            ViewTransform atZero = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 1.0, PanX: 0, PanY: 0),
                levelWidth: 40,
                levelHeight: 40,
                viewportWidth: 400,
                viewportHeight: 400,
                offsetX: 0,
                offsetY: 0);

            ViewTransform atMax = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 1.0, PanX: 0, PanY: 0),
                levelWidth: 40,
                levelHeight: 40,
                viewportWidth: 400,
                viewportHeight: 400,
                offsetX: 360,
                offsetY: 360);

            // Content 40 fits the 400 viewport, so both extremes resolve to the centered pan (400-40)/2 = 180.
            Assert.Equal(180, atZero.PanX);
            Assert.Equal(180, atMax.PanX);
        }

        /// <summary>Verifies that the overscroll bounds hold at both ends of the zoom clamp range.</summary>
        [Fact]
        public void ScrollToHoldsBoundsAtZoomExtremes()
        {
            ViewTransform farOut = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 0.1, PanX: 0, PanY: 0),
                levelWidth: 1000,
                levelHeight: 1000,
                viewportWidth: 400,
                viewportHeight: 400,
                offsetX: 99999,
                offsetY: 99999);

            ViewTransform farIn = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 10.0, PanX: 0, PanY: 0),
                levelWidth: 1000,
                levelHeight: 1000,
                viewportWidth: 400,
                viewportHeight: 400,
                offsetX: 99999,
                offsetY: 99999);

            // Zoomed out: content 100 fits the 400 viewport, so it locks to center (400-100)/2 = 150.
            Assert.Equal(150, farOut.PanX);

            // Zoomed in: content 10000 > viewport, keep 64, leaving a 64px sliver against the left edge.
            Assert.Equal(-9936, farIn.PanX);
        }

        /// <summary>Verifies that the scroll range stays non-negative when the viewport is narrower than the keep sliver.</summary>
        [Fact]
        public void ComputeScrollRangeStaysNonNegativeForTinyViewport()
        {
            ScrollRange range = ViewNavigation.ComputeScrollRange(contentSize: 500, viewportSize: 10, pan: 0);

            Assert.True(range.Maximum >= 0);
            Assert.InRange(range.Value, 0, range.Maximum);
        }

        /// <summary>Verifies that the scroll range round-trips the pan produced by scrolling to an offset.</summary>
        [Fact]
        public void ComputeScrollRangeRoundTripsScrollToOffset()
        {
            ViewTransform t = ViewNavigation.ScrollTo(
                new ViewTransform(Zoom: 1.0, PanX: 0, PanY: 0),
                levelWidth: 1000,
                levelHeight: 1000,
                viewportWidth: 400,
                viewportHeight: 400,
                offsetX: 500,
                offsetY: 500);

            ScrollRange range = ViewNavigation.ComputeScrollRange(contentSize: 1000, viewportSize: 400, pan: t.PanX);

            Assert.Equal(500, range.Value);
            Assert.InRange(range.Value, 0, range.Maximum);
        }

        /// <summary>Verifies that an axis whose content fits the viewport reports an empty scroll range.</summary>
        [Fact]
        public void ComputeScrollRangeIsZeroWhenContentFitsViewport()
        {
            ScrollRange range = ViewNavigation.ComputeScrollRange(contentSize: 200, viewportSize: 400, pan: 100);

            Assert.Equal(0, range.Maximum);
            Assert.Equal(0, range.Value);
        }

        /// <summary>Verifies that clamping recenters an axis whose content fits the viewport.</summary>
        [Fact]
        public void ClampPanCentersAxisWhoseContentFitsViewport()
        {
            ViewTransform clamped = ViewNavigation.ClampPan(
                new ViewTransform(Zoom: 1.0, PanX: 40, PanY: 0),
                levelWidth: 200,
                levelHeight: 200,
                viewportWidth: 400,
                viewportHeight: 400);

            // Content 200 fits the 400 viewport, so an off-center pan snaps to (400-200)/2 = 100.
            Assert.Equal(100, clamped.PanX);
            Assert.Equal(100, clamped.PanY);
        }

        /// <summary>Verifies that clamping pulls an overscrolled axis back inside the keep-sliver bounds.</summary>
        [Fact]
        public void ClampPanClampsOverscrolledAxisIntoBounds()
        {
            ViewTransform clamped = ViewNavigation.ClampPan(
                new ViewTransform(Zoom: 1.0, PanX: 5000, PanY: -5000),
                levelWidth: 1000,
                levelHeight: 1000,
                viewportWidth: 400,
                viewportHeight: 400);

            // Content 1000 > viewport 400: overscroll bounds are [64 - 1000, 400 - 64] = [-936, 336].
            Assert.Equal(336, clamped.PanX);
            Assert.Equal(-936, clamped.PanY);
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
