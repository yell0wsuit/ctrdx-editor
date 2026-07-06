using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Geometry for the auto-catch grab radius: reading it off a grab, edge hit-testing for the
    /// draggable resize affordance, and mapping a drag point back to a new radius. All lengths are
    /// in level units. The game only draws the circle when radius is positive (auto-catch on);
    /// missing / -1 / non-positive all mean "no radius".
    /// </summary>
    public static class GrabRadius
    {
        /// <summary>Smallest radius the editor lets a drag produce, so the circle never collapses.</summary>
        public const double Min = 1;

        /// <summary>The grab's auto-catch radius in level units, or null when auto-catch is off.</summary>
        public static double? Of(LevelObject grab)
        {
            return double.TryParse(
                       grab.GetAttr("radius"), NumberStyles.Float, CultureInfo.InvariantCulture, out double r)
                   && r > 0
                ? r
                : null;
        }

        /// <summary>Straight-line distance between two points.</summary>
        public static double Distance(Vec2 a, Vec2 b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        /// <summary>Whether <paramref name="point"/> lies within <paramref name="tolerance"/> of the circle's edge.</summary>
        public static bool OnEdge(Vec2 center, double radius, Vec2 point, double tolerance)
        {
            return Math.Abs(Distance(center, point) - radius) <= tolerance;
        }

        /// <summary>New radius from a drag <paramref name="point"/>, clamped to at least <see cref="Min"/>.</summary>
        public static double FromDrag(Vec2 center, Vec2 point)
        {
            return Math.Max(Min, Distance(center, point));
        }
    }
}
