using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Geometry for the pump's rotation dial: reading/formatting the <c>angle</c> attribute, mapping a
    /// drag point to an angle (snapped to 15° or free), placing the knob, and hit-testing the ring and
    /// knob. Pure and UI-free, like <see cref="GrabRadius"/> / <see cref="GrabRail"/>.
    ///
    /// The game stores <c>angle</c> in degrees and renders the pump rotated by <c>angle + 90</c>
    /// (LoadPumps: <c>rotation = angle + DEG_90</c>). Positive is clockwise in the game's Y-down
    /// projection, which Avalonia's Y-down screen space matches. Level space is likewise Y-down, so a
    /// screen-direction angle computed with <see cref="Math.Atan2"/> is clockwise-positive here too.
    /// </summary>
    public static class PumpRotation
    {
        /// <summary>Degrees added to the stored angle to get the on-screen rotation (game DEG_90).</summary>
        public const double DisplayOffset = 90;

        /// <summary>The snap increment in degrees.</summary>
        public const double SnapStep = 15;

        /// <summary>The stored <c>angle</c> in degrees, or 0 when missing/unparseable.</summary>
        public static double StoredAngle(LevelObject pump)
        {
            return double.TryParse(
                pump.GetAttr("angle"), NumberStyles.Float, CultureInfo.InvariantCulture, out double a)
                ? a
                : 0;
        }

        /// <summary>The pump's on-screen rotation in degrees (<see cref="StoredAngle"/> + 90).</summary>
        public static double DisplayDegrees(LevelObject pump)
        {
            return StoredAngle(pump) + DisplayOffset;
        }

        /// <summary>Wraps <paramref name="deg"/> into the half-open signed range (-180, 180].</summary>
        public static double Normalize(double deg)
        {
            double m = deg % 360;
            if (m <= -180)
            {
                m += 360;
            }
            else if (m > 180)
            {
                m -= 360;
            }
            return m;
        }

        /// <summary>Rounds <paramref name="deg"/> to the nearest 15°.</summary>
        public static double Snap(double deg)
        {
            return Math.Round(deg / SnapStep, MidpointRounding.AwayFromZero) * SnapStep;
        }

        /// <summary>
        /// The stored angle for a drag to <paramref name="point"/> around <paramref name="center"/>.
        /// The on-screen direction is <c>atan2(dy, dx)</c> (clockwise-positive, Y-down); subtracting the
        /// 90° display offset yields the stored angle. Snapped to 15° when <paramref name="snap"/>,
        /// otherwise rounded to whole degrees. Always normalized to (-180, 180].
        /// </summary>
        public static double AngleFromPoint(Vec2 center, Vec2 point, bool snap)
        {
            double dir = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180 / Math.PI;
            double stored = dir - DisplayOffset;
            stored = snap ? Snap(stored) : Math.Round(stored);
            return Normalize(stored);
        }

        /// <summary>The knob's level-space position for a given stored angle and ring radius.</summary>
        public static Vec2 KnobPosition(Vec2 center, double storedAngle, double radius)
        {
            double dir = (storedAngle + DisplayOffset) * Math.PI / 180;
            return new Vec2(center.X + (radius * Math.Cos(dir)), center.Y + (radius * Math.Sin(dir)));
        }

        /// <summary>Whether <paramref name="point"/> is within <paramref name="tolerance"/> of the ring edge.</summary>
        public static bool OnRing(Vec2 center, double radius, Vec2 point, double tolerance)
        {
            return Math.Abs(GrabRadius.Distance(center, point) - radius) <= tolerance;
        }

        /// <summary>Whether <paramref name="point"/> is within <paramref name="tolerance"/> of the knob.</summary>
        public static bool OnKnob(Vec2 center, double storedAngle, double radius, Vec2 point, double tolerance)
        {
            return GrabRadius.Distance(KnobPosition(center, storedAngle, radius), point) <= tolerance;
        }

        /// <summary>Formats a dial-produced angle as invariant whole degrees for the XML attribute.</summary>
        public static string Format(double deg)
        {
            return ((long)Math.Round(deg)).ToString(CultureInfo.InvariantCulture);
        }
    }
}
