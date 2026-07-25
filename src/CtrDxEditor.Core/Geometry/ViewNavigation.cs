using System;

namespace CtrDxEditor.Core.Geometry
{
    /// <summary>Viewport navigation helpers for zooming and scrolling a level view.</summary>
    public static class ViewNavigation
    {
        /// <summary>Zooms around a screen-space anchor so the anchored level point remains under the pointer.</summary>
        /// <param name="view">The current view transform.</param>
        /// <param name="factor">Multiplied into the current zoom; above 1 zooms in, below 1 zooms out.</param>
        /// <param name="anchor">The screen-space point to hold fixed, typically the pointer.</param>
        /// <param name="minZoom">Lower clamp for the resulting zoom.</param>
        /// <param name="maxZoom">Upper clamp for the resulting zoom.</param>
        /// <returns>The zoomed transform, panned so the level point under <paramref name="anchor"/> stays put.</returns>
        public static ViewTransform ZoomBy(
            ViewTransform view,
            double factor,
            Vec2 anchor,
            double minZoom,
            double maxZoom)
        {
            double newZoom = Math.Clamp(view.Zoom * factor, minZoom, maxZoom);
            double ratio = newZoom / view.Zoom;
            double panX = anchor.X - ((anchor.X - view.PanX) * ratio);
            double panY = anchor.Y - ((anchor.Y - view.PanY) * ratio);
            return new ViewTransform(newZoom, panX, panY);
        }

        /// <summary>The overlap sliver in screen pixels kept between the level and the viewport when panning.</summary>
        public const double OverscrollKeep = 64;

        /// <summary>Returns a view transform for the requested scroll offsets, clamped to the overscroll bounds.</summary>
        /// <param name="view">The current view transform.</param>
        /// <param name="levelWidth">The level width in level units.</param>
        /// <param name="levelHeight">The level height in level units.</param>
        /// <param name="viewportWidth">The viewport width in screen pixels.</param>
        /// <param name="viewportHeight">The viewport height in screen pixels.</param>
        /// <param name="offsetX">The requested horizontal scroll offset in screen pixels, clamped to the range.</param>
        /// <param name="offsetY">The requested vertical scroll offset in screen pixels, clamped to the range.</param>
        /// <returns>
        /// The scrolled transform at the same zoom. Both axes pan freely, stopping only when the level and the
        /// viewport would overlap by less than the keep sliver, so any part of the level can reach screen center.
        /// </returns>
        public static ViewTransform ScrollTo(
            ViewTransform view,
            double levelWidth,
            double levelHeight,
            double viewportWidth,
            double viewportHeight,
            double offsetX,
            double offsetY)
        {
            double contentWidth = Math.Max(0, levelWidth * view.Zoom);
            double contentHeight = Math.Max(0, levelHeight * view.Zoom);
            return new ViewTransform(
                view.Zoom,
                PanForOffset(contentWidth, viewportWidth, offsetX),
                PanForOffset(contentHeight, viewportHeight, offsetY));
        }

        /// <summary>Clamps a view's pan to the overscroll bounds on both axes for the given level and viewport.</summary>
        /// <param name="view">The view transform to clamp.</param>
        /// <param name="levelWidth">The level width in level units.</param>
        /// <param name="levelHeight">The level height in level units.</param>
        /// <param name="viewportWidth">The viewport width in screen pixels.</param>
        /// <param name="viewportHeight">The viewport height in screen pixels.</param>
        /// <returns>
        /// The same transform with each axis pan brought inside its bounds, so an axis whose content fits the
        /// viewport is recentered and one larger than the viewport keeps at least the keep sliver on screen.
        /// </returns>
        public static ViewTransform ClampPan(
            ViewTransform view,
            double levelWidth,
            double levelHeight,
            double viewportWidth,
            double viewportHeight)
        {
            double contentWidth = Math.Max(0, levelWidth * view.Zoom);
            double contentHeight = Math.Max(0, levelHeight * view.Zoom);
            return new ViewTransform(
                view.Zoom,
                ClampPanAxis(contentWidth, viewportWidth, view.PanX),
                ClampPanAxis(contentHeight, viewportHeight, view.PanY));
        }

        /// <summary>Reframes a view after its viewport changes size, preserving zoom and the level point at center.</summary>
        /// <param name="view">The view before the resize.</param>
        /// <param name="levelWidth">The level width in level units.</param>
        /// <param name="levelHeight">The level height in level units.</param>
        /// <param name="oldViewportWidth">The previous viewport width in screen pixels.</param>
        /// <param name="oldViewportHeight">The previous viewport height in screen pixels.</param>
        /// <param name="newViewportWidth">The new viewport width in screen pixels.</param>
        /// <param name="newViewportHeight">The new viewport height in screen pixels.</param>
        /// <returns>
        /// A transform at the same zoom with the former center level point under the new viewport center,
        /// clamped to recenter content that fits and retain the normal overscroll limits otherwise.
        /// </returns>
        public static ViewTransform ResizeViewport(
            ViewTransform view,
            double levelWidth,
            double levelHeight,
            double oldViewportWidth,
            double oldViewportHeight,
            double newViewportWidth,
            double newViewportHeight)
        {
            Vec2 levelCenter = view.ScreenToLevel(new Vec2(oldViewportWidth / 2, oldViewportHeight / 2));
            ViewTransform reframed = new(
                view.Zoom,
                (newViewportWidth / 2) - (levelCenter.X * view.Zoom),
                (newViewportHeight / 2) - (levelCenter.Y * view.Zoom));
            return ClampPan(reframed, levelWidth, levelHeight, newViewportWidth, newViewportHeight);
        }

        /// <summary>Returns the scrollbar range and position for one axis at the given pan.</summary>
        /// <param name="contentSize">The zoomed content size in screen pixels.</param>
        /// <param name="viewportSize">The viewport size in screen pixels.</param>
        /// <param name="pan">The current pan on this axis in screen pixels.</param>
        /// <returns>The range length and the current offset within it, both non-negative.</returns>
        public static ScrollRange ComputeScrollRange(double contentSize, double viewportSize, double pan)
        {
            (double min, double max) = PanBounds(contentSize, viewportSize);
            double range = Math.Max(0, max - min);
            return new ScrollRange(range, Math.Clamp(max - pan, 0, range));
        }

        /// <summary>Converts a scroll offset on one axis into the pan it represents, clamped to the range.</summary>
        private static double PanForOffset(double contentSize, double viewportSize, double offset)
        {
            (double min, double max) = PanBounds(contentSize, viewportSize);
            double range = Math.Max(0, max - min);
            return max - Math.Clamp(offset, 0, range);
        }

        /// <summary>Clamps one axis's pan to its bounds, recentering when the content fits the viewport.</summary>
        private static double ClampPanAxis(double contentSize, double viewportSize, double pan)
        {
            (double min, double max) = PanBounds(contentSize, viewportSize);
            return Math.Clamp(pan, min, max);
        }

        /// <summary>
        /// Returns the inclusive pan bounds for one axis. Content that fits the viewport collapses to a single
        /// centered pan so the axis cannot scroll; content larger than the viewport must overlap it by at least
        /// <see cref="OverscrollKeep"/> pixels, or by the smaller of the two when either is narrower than that.
        /// </summary>
        private static (double Min, double Max) PanBounds(double contentSize, double viewportSize)
        {
            double content = Math.Max(0, contentSize);
            double viewport = Math.Max(0, viewportSize);
            if (content <= viewport)
            {
                double centered = (viewport - content) / 2;
                return (centered, centered);
            }
            double keep = Math.Min(OverscrollKeep, Math.Min(content, viewport));
            return (keep - content, viewport - keep);
        }

        /// <summary>Converts cumulative pinch scale into an incremental zoom factor.</summary>
        /// <param name="previousScale">The pinch gesture's cumulative scale at the previous event.</param>
        /// <param name="currentScale">The pinch gesture's cumulative scale now.</param>
        /// <returns>The incremental factor to pass to <see cref="ZoomBy"/>; 1 when either scale is non-positive.</returns>
        public static double PinchScaleToZoomFactor(double previousScale, double currentScale)
        {
            return previousScale <= 0 || currentScale <= 0 ? 1 : currentScale / previousScale;
        }

        /// <summary>Converts platform touchpad magnify delta into a bounded zoom factor.</summary>
        public static double MagnifyDeltaToZoomFactor(double delta)
        {
            return Math.Clamp(1 + delta, 0.5, 2.0);
        }
    }
}
