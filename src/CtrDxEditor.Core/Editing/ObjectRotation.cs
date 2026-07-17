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
    /// Most game objects render <c>angle + DisplayOffset</c>, with positive clockwise in the Y-down
    /// projection. A spec may set <see cref="RotationSpec.StoredAngleSign"/> to -1 for an object such as the
    /// conveyor whose XML stores positive angles counter-clockwise. Level and screen space remain Y-down.
    /// </summary>
    public static class ObjectRotation
    {
        /// <summary>Resolves the pivot shared by dial drawing, hit-testing, and pointer angle conversion.</summary>
        /// <param name="obj">The rotatable object.</param>
        /// <param name="spec">Its rotation mapping and center policy.</param>
        /// <returns>The rotation center in level coordinates.</returns>
        public static Vec2 Center(LevelObject obj, RotationSpec spec)
        {
            return spec.CenterKind == RotationCenterKind.ConveyorMidpoint
                && ConveyorGeometry.Of(obj) is { } belt
                ? new Vec2(
                    (belt.Anchor.X + belt.Far.X) / 2,
                    (belt.Anchor.Y + belt.Far.Y) / 2)
                : new Vec2(obj.X, obj.Y);
        }

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
        /// <param name="obj">The object carrying the angle attribute.</param>
        /// <param name="spec">The object's rotation convention, mapping the stored attribute to an on-screen angle.</param>
        /// <returns>The raw stored angle in degrees.</returns>
        public static double StoredAngle(LevelObject obj, RotationSpec spec)
        {
            return double.TryParse(
                obj.GetAttr(spec.AttributeName), NumberStyles.Float, CultureInfo.InvariantCulture, out double a)
                ? a
                : 0;
        }

        /// <summary>The object's on-screen rotation in degrees (stored × sign + display offset).</summary>
        /// <param name="obj">The object carrying the angle attribute.</param>
        /// <param name="spec">The object's rotation convention, mapping the stored attribute to an on-screen angle.</param>
        /// <returns>The on-screen angle in degrees, clockwise-positive with Y running screen-down.</returns>
        public static double DisplayDegrees(LevelObject obj, RotationSpec spec)
        {
            return (StoredAngle(obj, spec) * spec.StoredAngleSign) + spec.DisplayOffset;
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
        /// <param name="deg">The angle in degrees.</param>
        /// <param name="step">The snap increment in degrees.</param>
        /// <returns>The snapped angle; halfway cases round away from zero.</returns>
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
        /// <param name="center">The object's center, in level units.</param>
        /// <param name="point">The drag position, in level units.</param>
        /// <param name="spec">The object's rotation convention, mapping the stored attribute to an on-screen angle.</param>
        /// <param name="snap">True to snap to the spec's step; false to round to whole degrees.</param>
        /// <returns>The angle to store, normalized to (-180, 180].</returns>
        public static double AngleFromPoint(Vec2 center, Vec2 point, RotationSpec spec, bool snap)
        {
            double dir = Math.Atan2(point.Y - center.Y, point.X - center.X) * 180 / Math.PI;
            double stored = (dir - spec.DisplayOffset) / spec.StoredAngleSign;
            stored = snap ? Snap(stored, spec.SnapStep) : Math.Round(stored);
            return Normalize(stored);
        }

        /// <summary>The knob's level-space position for a stored angle and ring radius.</summary>
        /// <param name="center">The object's center, in level units.</param>
        /// <param name="storedAngle">The angle as stored on the object, not the on-screen angle.</param>
        /// <param name="spec">The object's rotation convention, mapping the stored attribute to an on-screen angle.</param>
        /// <param name="radius">The dial ring radius, in level units.</param>
        /// <returns>The knob's position, in level units.</returns>
        public static Vec2 KnobPosition(Vec2 center, double storedAngle, RotationSpec spec, double radius)
        {
            double dir = ((storedAngle * spec.StoredAngleSign) + spec.DisplayOffset) * Math.PI / 180;
            return new Vec2(center.X + (radius * Math.Cos(dir)), center.Y + (radius * Math.Sin(dir)));
        }

        /// <summary>Whether <paramref name="point"/> is within <paramref name="tolerance"/> of the ring edge.</summary>
        /// <param name="center">The object's center, in level units.</param>
        /// <param name="radius">The dial ring radius, in level units.</param>
        /// <param name="point">The position to test, in level units; typically the cursor.</param>
        /// <param name="tolerance">The hit distance in level units, so callers convert screen pixels via the zoom.</param>
        public static bool OnRing(Vec2 center, double radius, Vec2 point, double tolerance)
        {
            return Math.Abs(GrabRadius.Distance(center, point) - radius) <= tolerance;
        }

        /// <summary>Whether <paramref name="point"/> is within <paramref name="tolerance"/> of the knob.</summary>
        /// <param name="center">The object's center, in level units.</param>
        /// <param name="storedAngle">The angle as stored on the object, not the on-screen angle.</param>
        /// <param name="spec">The object's rotation convention, mapping the stored attribute to an on-screen angle.</param>
        /// <param name="radius">The dial ring radius, in level units.</param>
        /// <param name="point">The position to test, in level units; typically the cursor.</param>
        /// <param name="tolerance">The hit distance in level units, so callers convert screen pixels via the zoom.</param>
        public static bool OnKnob(Vec2 center, double storedAngle, RotationSpec spec, double radius, Vec2 point, double tolerance)
        {
            return GrabRadius.Distance(KnobPosition(center, storedAngle, spec, radius), point) <= tolerance;
        }

        /// <summary>
        /// Classifies what part of the dial <paramref name="point"/> is over. The knob wins (it is the
        /// primary target); then the ring bar; then nothing. Tolerances are in level units, so the caller
        /// converts screen pixels via the zoom.
        /// </summary>
        /// <param name="center">The object's center, in level units.</param>
        /// <param name="storedAngle">The angle as stored on the object, not the on-screen angle.</param>
        /// <param name="spec">The object's rotation convention, mapping the stored attribute to an on-screen angle.</param>
        /// <param name="radius">The dial ring radius, in level units.</param>
        /// <param name="point">The position to test, in level units; typically the cursor.</param>
        /// <param name="ringTolerance">The hit distance from the ring edge, in level units.</param>
        /// <param name="knobTolerance">The hit radius around the knob, in level units.</param>
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
