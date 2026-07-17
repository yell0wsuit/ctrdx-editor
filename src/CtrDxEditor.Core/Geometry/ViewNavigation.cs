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

        /// <summary>Returns a view transform for the requested scroll offsets, clamped to scaled content bounds.</summary>
        /// <param name="view">The current view transform.</param>
        /// <param name="levelWidth">The level width in level units.</param>
        /// <param name="levelHeight">The level height in level units.</param>
        /// <param name="viewportWidth">The viewport width in screen pixels.</param>
        /// <param name="viewportHeight">The viewport height in screen pixels.</param>
        /// <param name="offsetX">The requested horizontal scroll offset in screen pixels, clamped to the content.</param>
        /// <param name="offsetY">The requested vertical scroll offset in screen pixels, clamped to the content.</param>
        /// <returns>The scrolled transform at the same zoom. An axis whose content is smaller than the viewport is centered instead of scrolled.</returns>
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
            double maxX = Math.Max(0, contentWidth - viewportWidth);
            double maxY = Math.Max(0, contentHeight - viewportHeight);
            double panX = contentWidth <= viewportWidth ? (viewportWidth - contentWidth) / 2 : -Math.Clamp(offsetX, 0, maxX);
            double panY = contentHeight <= viewportHeight ? (viewportHeight - contentHeight) / 2 : -Math.Clamp(offsetY, 0, maxY);
            return new ViewTransform(view.Zoom, panX, panY);
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
