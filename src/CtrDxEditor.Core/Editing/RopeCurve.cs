using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Static catenary approximation of a hanging rope. Given two endpoints and a rest
    /// length, produces the polyline a slack cable settles to under gravity (+Y is down),
    /// or a straight line when taut. Closed-form - no physics simulation.
    /// </summary>
    public static class RopeCurve
    {
        // Below this horizontal span (level units) the catenary degenerates (a -> 0,
        // cosh overflows); fall back to a vertical fold instead.
        private const double MinSpan = 1e-3;

        /// <summary>
        /// Samples the rope between <paramref name="a"/> and <paramref name="b"/> in level
        /// space. Taut (<paramref name="length"/> &lt;= straight distance) -> the two endpoints;
        /// otherwise a catenary whose parameter is solved from <paramref name="length"/> so
        /// its arc length equals the rope length.
        /// </summary>
        /// <param name="a">First endpoint (e.g. the grab).</param>
        /// <param name="b">Second endpoint (e.g. the target).</param>
        /// <param name="length">Rope rest length, in level units.</param>
        /// <param name="segments">Number of segments (points = segments + 1).</param>
        public static IReadOnlyList<Vec2> Sample(Vec2 a, Vec2 b, double length, int segments = 20)
        {
            if (segments < 1)
            {
                segments = 1;
            }

            Vec2 chordVec = b - a;
            double chord = Math.Sqrt((chordVec.X * chordVec.X) + (chordVec.Y * chordVec.Y));

            if (length <= chord)
            {
                return [a, b];
            }

            // Orient left -> right so the catenary math assumes increasing x; reverse at the end.
            bool swapped = b.X < a.X;
            Vec2 p0 = swapped ? b : a;
            Vec2 p1 = swapped ? a : b;

            double h = p1.X - p0.X;   // horizontal span >= 0
            double v = p1.Y - p0.Y;   // vertical gap (+Y down)

            Vec2[] pts = h < MinSpan
                ? BuildVerticalFold(p0, p1, length, segments)
                : BuildCatenary(p0, h, v, length, segments);

            if (swapped)
            {
                Array.Reverse(pts);
            }
            return pts;
        }

        private static Vec2[] BuildCatenary(Vec2 p0, double h, double v, double length, int segments)
        {
            // sqrt(length^2 - v^2) = 2a*sinh(h/2a). Sub u = h/2a -> sinh(u)/u = D/h.
            double d = Math.Sqrt(Math.Max(0, (length * length) - (v * v)));
            double u = SolveSinhOverU(d / h);
            double a = h / (2 * u);

            double x0 = (h / 2) + (a * Math.Asinh(v / d)); // vertex x, relative to p0.X
            double lambda = p0.Y + (a * Math.Cosh(-x0 / a)); // so y(0) == p0.Y

            Vec2[] pts = new Vec2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                double x = (double)i / segments * h;
                double y = lambda - (a * Math.Cosh((x - x0) / a)); // minus-cosh -> sags toward +Y
                pts[i] = new Vec2(p0.X + x, y);
            }

            // Pin exact endpoints against tiny numeric drift.
            pts[0] = p0;
            pts[segments] = new Vec2(p0.X + h, p0.Y + v);
            return pts;
        }

        private static Vec2[] BuildVerticalFold(Vec2 p0, Vec2 p1, double length, int segments)
        {
            // Endpoints share ~the same x. Drop straight down and back so the path length ~= length.
            double drop = (length - Math.Abs(p1.Y - p0.Y)) / 2;
            double lowY = Math.Max(p0.Y, p1.Y) + drop;

            Vec2[] pts = new Vec2[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double y = t <= 0.5
                    ? p0.Y + ((lowY - p0.Y) * (t / 0.5))
                    : lowY + ((p1.Y - lowY) * ((t - 0.5) / 0.5));
                pts[i] = new Vec2(p0.X, y);
            }

            pts[0] = p0;
            pts[segments] = p1;
            return pts;
        }

        // Solves sinh(u)/u = r for u > 0. The ratio rises monotonically from 1 (u -> 0) to infinity.
        private static double SolveSinhOverU(double r)
        {
            if (r <= 1)
            {
                return 1e-6; // effectively straight
            }

            double lo = 1e-6;
            double hi = 1;
            while (Math.Sinh(hi) / hi < r && hi < 1e4)
            {
                hi *= 2;
            }
            for (int i = 0; i < 60; i++)
            {
                double mid = (lo + hi) / 2;
                if (Math.Sinh(mid) / mid < r)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }
            return (lo + hi) / 2;
        }
    }
}
