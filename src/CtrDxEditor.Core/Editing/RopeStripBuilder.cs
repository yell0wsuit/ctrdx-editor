using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>An RGBA color with straight (non-premultiplied) 0-1 channels. Channels may exceed 1 (clamped at raster time).</summary>
    public readonly record struct RopeRgba(double R, double G, double B, double A);

    /// <summary>One triangle strip of a rendered rope: parallel position/color arrays in strip order.</summary>
    public sealed record RopeStrip(Vec2[] Points, RopeRgba[] Colors);

    /// <summary>
    /// Builds the triangle strips that draw a rope exactly like the game's
    /// <c>Bungee.DrawBungee</c> / <c>DrawAntialiasedLineContinued</c> (default skin,
    /// alpha 1, not highlighted). Coordinates are level-space; the game's pixel
    /// constants (half-width 5, 1-unit edge fade) scale with the view like sprite art.
    /// </summary>
    public static class RopeStripBuilder
    {
        // Desktop game constants: BUNGEE_REST_LEN, BungeeDrawSamplePoints, main strip half-width.
        private const double RestLength = 105;
        private const int SamplesPerSegment = 4;
        private const double HalfWidth = 5;

        /// <summary>
        /// Evaluates the game's <c>DrawHelper.CalcPathBezier</c>: a de Casteljau reduction
        /// where every input point is a control point (the curve only interpolates the ends).
        /// </summary>
        public static Vec2 CalcPathBezier(IReadOnlyList<Vec2> controls, double t)
        {
            int n = controls.Count;
            if (n == 0)
            {
                return default;
            }

            if (n == 1)
            {
                return controls[0];
            }

            Vec2[] scratch = new Vec2[n];
            for (int i = 0; i < n; i++)
            {
                scratch[i] = controls[i];
            }

            for (int level = n - 1; level >= 1; level--)
            {
                for (int i = 0; i < level; i++)
                {
                    scratch[i] = new Vec2(
                        (scratch[i].X * (1 - t)) + (scratch[i + 1].X * t),
                        (scratch[i].Y * (1 - t)) + (scratch[i + 1].Y * t));
                }
            }

            return scratch[0];
        }
    }
}
