namespace CtrDxEditor.Core.Geometry
{
    /// <summary>
    /// Clips screen-space segments to the visible viewport before they reach the renderer.
    /// <para>
    /// Movement paths use sentinel offsets (±9999 and larger) to mean "travel off the map", so a single
    /// authored segment can span hundreds of thousands of screen pixels once zoom is applied. Handing that
    /// to a dashed or dotted pen is what costs: the dash path effect tessellates the whole run into
    /// on/off pieces before rasterization ever gets to reject the off-screen ones, so the work scales with
    /// the segment's full length rather than with the part you can actually see. Trimming each segment to
    /// the viewport first makes the cost proportional to the visible span instead.
    /// </para>
    /// </summary>
    public static class SegmentClip
    {
        /// <summary>
        /// Slack in pixels kept around the viewport. A stroke is drawn centered on the segment and can
        /// carry caps and arrow barbs, so trimming exactly at the edge could shave pixels that belong
        /// on-screen; this keeps a margin wider than any overlay stroke.
        /// </summary>
        public const double Padding = 16.0;

        /// <summary>
        /// Trims <paramref name="a"/>→<paramref name="b"/> to the padded viewport, preserving direction.
        /// </summary>
        /// <param name="a">The segment start in screen pixels; moved to the first visible point.</param>
        /// <param name="b">The segment end in screen pixels; moved to the last visible point.</param>
        /// <param name="viewport">The viewport size in screen pixels.</param>
        /// <returns>
        /// True when some part of the segment is visible and <paramref name="a"/>/<paramref name="b"/> now
        /// bound that part; false when the segment misses the viewport entirely and should not be drawn.
        /// </returns>
        public static bool ToViewport(ref Vec2 a, ref Vec2 b, IntSize viewport)
        {
            double minX = -Padding;
            double minY = -Padding;
            double maxX = viewport.W + Padding;
            double maxY = viewport.H + Padding;

            // Cohen-Sutherland: push each endpoint onto the boundary it violates until both are inside
            // (accept) or both sit outside the same edge (reject).
            double x0 = a.X, y0 = a.Y, x1 = b.X, y1 = b.Y;
            int code0 = OutCode(x0, y0, minX, minY, maxX, maxY);
            int code1 = OutCode(x1, y1, minX, minY, maxX, maxY);

            while (true)
            {
                if ((code0 | code1) == 0)
                {
                    a = new Vec2(x0, y0);
                    b = new Vec2(x1, y1);
                    return true;
                }

                if ((code0 & code1) != 0)
                {
                    return false;
                }

                // An endpoint can sit outside more than one edge at once (a corner), so the guards are
                // bit tests taken in order rather than cases on the code's value: pull the point onto the
                // first violated boundary, then re-test on the next pass until it lands inside.
                int outside = code0 != 0 ? code0 : code1;
                (double x, double y) = outside switch
                {
                    _ when (outside & Bottom) != 0 => (x0 + ((x1 - x0) * (maxY - y0) / (y1 - y0)), maxY),
                    _ when (outside & Top) != 0 => (x0 + ((x1 - x0) * (minY - y0) / (y1 - y0)), minY),
                    _ when (outside & Right) != 0 => (maxX, y0 + ((y1 - y0) * (maxX - x0) / (x1 - x0))),
                    _ => (minX, y0 + ((y1 - y0) * (minX - x0) / (x1 - x0))),
                };

                if (outside == code0)
                {
                    x0 = x;
                    y0 = y;
                    code0 = OutCode(x0, y0, minX, minY, maxX, maxY);
                }
                else
                {
                    x1 = x;
                    y1 = y;
                    code1 = OutCode(x1, y1, minX, minY, maxX, maxY);
                }
            }
        }

        private const int Left = 1;
        private const int Right = 2;
        private const int Top = 4;
        private const int Bottom = 8;

        private static int OutCode(double x, double y, double minX, double minY, double maxX, double maxY)
        {
            int code = 0;
            if (x < minX)
            {
                code |= Left;
            }
            else if (x > maxX)
            {
                code |= Right;
            }

            if (y < minY)
            {
                code |= Top;
            }
            else if (y > maxY)
            {
                code |= Bottom;
            }

            return code;
        }
    }
}
