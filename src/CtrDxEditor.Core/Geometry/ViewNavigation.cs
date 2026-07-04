using System;

namespace CtrDxEditor.Core.Geometry
{
    /// <summary>Viewport navigation helpers for zooming and scrolling a level view.</summary>
    public static class ViewNavigation
    {
        /// <summary>Zooms around a screen-space anchor so the anchored level point remains under the pointer.</summary>
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
    }
}
