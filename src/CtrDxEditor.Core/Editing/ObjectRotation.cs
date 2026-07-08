using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Geometry for an object's rotation dial: reading/formatting its angle attribute, mapping a drag
    /// point to an angle (snapped or free), placing the knob, and hit-testing the ring and knob. Pure
    /// and UI-free, like <see cref="GrabRadius"/> / <see cref="GrabRail"/>. Object-agnostic: the per-object
    /// attribute name, display offset, and snap step come from a <see cref="RotationSpec"/>.
    ///
    /// The game stores the angle in degrees and renders the object rotated by <c>angle + DisplayOffset</c>.
    /// Positive is clockwise in the game's Y-down projection, which Avalonia's Y-down screen space matches.
    /// Level space is likewise Y-down, so a screen-direction angle from <see cref="Math.Atan2"/> is
    /// clockwise-positive here too.
    /// </summary>
    public static class ObjectRotation
    {
        /// <summary>What part of the dial a point is over, so the canvas can route a drag.</summary>
        public enum Handle
        {
            /// <summary>Nothing interactive under the point.</summary>
            None,

            /// <summary>The ring bar: dragging rotates from that direction.</summary>
            Ring,

            /// <summary>The facing knob: dragging spins the object.</summary>
            Knob,
        }

        /// <summary>The stored angle in degrees, or 0 when missing/unparseable.</summary>
        public static double StoredAngle(LevelObject obj, RotationSpec spec)
        {
            return double.TryParse(
                obj.GetAttr(spec.AttributeName), NumberStyles.Float, CultureInfo.InvariantCulture, out double a)
                ? a
                : 0;
        }

        /// <summary>The object's on-screen rotation in degrees (<see cref="StoredAngle"/> + display offset).</summary>
        public static double DisplayDegrees(LevelObject obj, RotationSpec spec)
        {
            return StoredAngle(obj, spec) + spec.DisplayOffset;
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

        /// <summary>Rounds <paramref name="deg"/> to the nearest <paramref name="step"/> degrees.</summary>
        public static double Snap(double deg, double step)
        {
            return Math.Round(deg / step, MidpointRounding.AwayFromZero) * step;
        }

        /// <summary>
        /// The stored angle for a drag to <paramref name="point"/> around <paramref name="center"/>.
        /// The on-screen direction is <c>atan2(dy, dx)</c> (clockwise-positive, Y-down); subtracting the
        /// display offset yields the stored angle. Snapped to the spec's step when <paramref name="snap"/>,
        /// otherwise rounded to whole degrees. Always normalized to (-180, 180].
        /// </summary>
        public static double AngleFromPoint(Vec2 center, Vec2 point, RotationSpec spec, bool snap)
        {
            double dir = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180 / Math.PI;
            double stored = dir - spec.DisplayOffset;
            stored = snap ? Snap(stored, spec.SnapStep) : Math.Round(stored);
            return Normalize(stored);
        }

        /// <summary>The knob's level-space position for a stored angle and ring radius.</summary>
        public static Vec2 KnobPosition(Vec2 center, double storedAngle, RotationSpec spec, double radius)
        {
            double dir = (storedAngle + spec.DisplayOffset) * Math.PI / 180;
            return new Vec2(center.X + (radius * Math.Cos(dir)), center.Y + (radius * Math.Sin(dir)));
        }

        /// <summary>Whether <paramref name="point"/> is within <paramref name="tolerance"/> of the ring edge.</summary>
        public static bool OnRing(Vec2 center, double radius, Vec2 point, double tolerance)
        {
            return Math.Abs(GrabRadius.Distance(center, point) - radius) <= tolerance;
        }

        /// <summary>Whether <paramref name="point"/> is within <paramref name="tolerance"/> of the knob.</summary>
        public static bool OnKnob(Vec2 center, double storedAngle, RotationSpec spec, double radius, Vec2 point, double tolerance)
        {
            return GrabRadius.Distance(KnobPosition(center, storedAngle, spec, radius), point) <= tolerance;
        }

        /// <summary>
        /// Classifies what part of the dial <paramref name="point"/> is over. The knob wins (it is the
        /// primary target); then the ring bar; then nothing. Tolerances are in level units, so the caller
        /// converts screen pixels via the zoom.
        /// </summary>
        public static Handle HitTest(
            Vec2 center, double storedAngle, RotationSpec spec, double radius, Vec2 point, double ringTolerance, double knobTolerance)
        {
            return OnKnob(center, storedAngle, spec, radius, point, knobTolerance)
                ? Handle.Knob
                : OnRing(center, radius, point, ringTolerance) ? Handle.Ring : Handle.None;
        }

        /// <summary>Formats a dial-produced angle as invariant whole degrees for the XML attribute.</summary>
        public static string Format(double deg)
        {
            return ((long)Math.Round(deg)).ToString(CultureInfo.InvariantCulture);
        }
    }
}
