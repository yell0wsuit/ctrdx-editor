using System;

using Avalonia;
using Avalonia.Threading;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>View navigation: fit-to-view, zoom, scroll, and scrollbar state sync.</summary>
    public sealed partial class LevelCanvas
    {
        /// <summary>Runs a queued fit-to-view once the control has non-zero bounds, then clears the pending flag.</summary>
        private void TryFit()
        {
            if (_pendingFit && Bounds is { Width: > 0, Height: > 0 })
            {
                FitToView();
                _pendingFit = false;
            }
        }

        /// <summary>Scales and centers the level to fit the current viewport with a small margin.</summary>
        public void FitToView()
        {
            LevelDocument? doc = Document;
            if (doc is null || doc.Width <= 0 || doc.Height <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
            {
                return;
            }
            const double margin = 24;
            double zoom = Math.Min(
                (Bounds.Width - (2 * margin)) / doc.Width,
                (Bounds.Height - (2 * margin)) / doc.Height);
            if (zoom <= 0)
            {
                zoom = 1;
            }
            double panX = (Bounds.Width - (doc.Width * zoom)) / 2;
            double panY = (Bounds.Height - (doc.Height * zoom)) / 2;
            View = new ViewTransform(zoom, panX, panY);
            UpdateScrollState();
        }

        /// <summary>Zooms about the viewport center (for menu/keyboard zoom).</summary>
        /// <param name="factor">Multiplicative zoom factor; greater than 1 zooms in, less than 1 zooms out.</param>
        public void ZoomBy(double factor)
        {
            ZoomBy(factor, new Point(Bounds.Width / 2, Bounds.Height / 2));
        }

        /// <summary>Zooms about a screen-space point in this canvas.</summary>
        /// <param name="factor">Multiplicative zoom factor; greater than 1 zooms in, less than 1 zooms out.</param>
        /// <param name="anchor">Screen-space point that stays fixed under the cursor while zooming.</param>
        public void ZoomBy(double factor, Point anchor)
        {
            ViewTransform zoomed = ViewNavigation.ZoomBy(View, factor, new Vec2(anchor.X, anchor.Y), 0.1, 10.0);
            if (Document is { } doc)
            {
                zoomed = ViewNavigation.ClampPan(zoomed, doc.Width, doc.Height, Bounds.Width, Bounds.Height);
            }
            View = zoomed;
            MarkScrollActivity();
            UpdateScrollState();
        }

        /// <summary>Scrolls the level viewport by screen-space pixels.</summary>
        /// <param name="deltaX">Horizontal scroll amount in screen pixels.</param>
        /// <param name="deltaY">Vertical scroll amount in screen pixels.</param>
        public void ScrollBy(double deltaX, double deltaY)
        {
            ScrollTo(HorizontalScrollValue + deltaX, VerticalScrollValue + deltaY);
        }

        /// <summary>Scrolls to an absolute screen-space offset, clamped to the scrollable range.</summary>
        /// <param name="offsetX">Target horizontal scroll offset in screen pixels.</param>
        /// <param name="offsetY">Target vertical scroll offset in screen pixels.</param>
        private void ScrollTo(double offsetX, double offsetY)
        {
            if (Document is not { } doc)
            {
                return;
            }

            View = ViewNavigation.ScrollTo(View, doc.Width, doc.Height, Bounds.Width, Bounds.Height, offsetX, offsetY);
            MarkScrollActivity();
            UpdateScrollState();
        }

        /// <summary>Shows the scrollbar overlay and restarts the idle countdown that hides it again.</summary>
        private void MarkScrollActivity()
        {
            IsScrollActive = true;
            if (_scrollIdleTimer is null)
            {
                _scrollIdleTimer = new DispatcherTimer { Interval = ScrollIdleDelay };
                _scrollIdleTimer.Tick += (_, _) =>
                {
                    _scrollIdleTimer.Stop();
                    IsScrollActive = false;
                };
            }

            _scrollIdleTimer.Stop();
            _scrollIdleTimer.Start();
        }

        /// <summary>Recomputes the scrollbar viewport, maximum, and value from the current document and view, without recursing.</summary>
        private void UpdateScrollState()
        {
            LevelDocument? doc = Document;
            double viewportWidth = Math.Max(0, Bounds.Width);
            double viewportHeight = Math.Max(0, Bounds.Height);
            ScrollRange horizontal = default;
            ScrollRange vertical = default;

            if (doc is not null)
            {
                horizontal = ViewNavigation.ComputeScrollRange(doc.Width * View.Zoom, viewportWidth, View.PanX);
                vertical = ViewNavigation.ComputeScrollRange(doc.Height * View.Zoom, viewportHeight, View.PanY);
            }

            _syncingScroll = true;
            try
            {
                HorizontalScrollViewport = viewportWidth;
                VerticalScrollViewport = viewportHeight;
                HorizontalScrollMaximum = horizontal.Maximum;
                VerticalScrollMaximum = vertical.Maximum;
                HorizontalScrollValue = horizontal.Value;
                VerticalScrollValue = vertical.Value;
            }
            finally
            {
                _syncingScroll = false;
            }
        }
    }
}
