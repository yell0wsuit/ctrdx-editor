using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Canvas editing for a grab's rope rest length. The rope is a scalar attribute with no position of
    /// its own, so the gesture reads a length back out of where the cord was dragged: the drawn cord's
    /// vertical drop below the hook-to-target chord rises monotonically with the rest length, which makes
    /// it invertible by bisection. All lengths are in level units. This helper is pure and UI-free.
    /// </summary>
    public static class RopeLength
    {
        /// <summary>Shortest rope a drag may produce, so a rope never collapses to nothing.</summary>
        public const double MinLength = 1;

        /// <summary>
        /// Chords shorter than this are degenerate: the hook sits on its target, so a chord parameter
        /// carries no information and the drag falls back to plain distance from the hook.
        /// </summary>
        public const double MinChord = 1;

        /// <summary>
        /// Chord parameters are clamped into [<see cref="MinParameter"/>, <see cref="MaxParameter"/>].
        /// Near an endpoint the cord barely moves for a large length change, so an unclamped drag there
        /// would swing the length wildly for a pixel of travel.
        /// </summary>
        public const double MinParameter = 0.15;

        /// <summary>The far end of the clamp described on <see cref="MinParameter"/>.</summary>
        public const double MaxParameter = 0.85;

        // How finely the drawn cord is sampled when looking for its lowest point.
        private const int KnobSamples = 32;

        // How finely the drawn cord is walked when hit-testing; consecutive samples are joined into
        // segments, so this is a shape-fidelity knob rather than a hit-accuracy one.
        private const int CordSamples = 32;

        /// <summary>Resolved rope geometry in level space.</summary>
        /// <param name="Hook">The grab's hook position, in level units.</param>
        /// <param name="Target">The bound candy or light bulb's position, in level units.</param>
        /// <param name="Chord">The straight-line distance between the two, in level units.</param>
        /// <param name="Length">The authored rest length, in level units.</param>
        /// <param name="Knob">The drag knob's position on the drawn cord, in level units.</param>
        /// <param name="Taut">Whether the rope has no slack (<paramref name="Length"/> is at most <paramref name="Chord"/>).</param>
        public readonly record struct Geometry(
            Vec2 Hook, Vec2 Target, double Chord, double Length, Vec2 Knob, bool Taut);

        /// <summary>What part of a rope a point is over, so the canvas can route a drag.</summary>
        public enum Handle
        {
            /// <summary>Nothing interactive under the point.</summary>
            None,

            /// <summary>The knob at the cord's lowest point: dragging changes the rest length.</summary>
            Knob,

            /// <summary>The cord itself: dragging changes the rest length from where it was grabbed.</summary>
            Cord,
        }

        /// <summary>
        /// Resolves the editable rope geometry for a grab, or null when it has no authored rope. A gun or
        /// auto-catch grab resolves to no target in <see cref="RopeResolver"/> and so lands here as null,
        /// which matches the property panel greying out its Length field.
        /// </summary>
        /// <param name="grab">The grab whose rope is being edited.</param>
        /// <param name="rope">The resolved rope target, from <see cref="RopeResolver.Resolve"/>.</param>
        /// <returns>The rope geometry, or null when there is no rope to edit.</returns>
        public static Geometry? Of(LevelObject grab, RopeTarget rope)
        {
            if (grab.Type != "grab" || rope.Target is not { } bound)
            {
                return null;
            }

            Vec2 hook = new(grab.X, grab.Y);
            Vec2 target = new(bound.X, bound.Y);
            double chord = Distance(hook, target);
            double length = ReadLength(grab);
            return new Geometry(hook, target, chord, length, KnobPoint(hook, target, length), length <= chord);
        }

        /// <summary>The grab's authored rest length, or 0 when the attribute is missing or unparsable.</summary>
        /// <param name="grab">The grab to read.</param>
        /// <returns>The rest length in level units.</returns>
        public static double ReadLength(LevelObject grab)
        {
            return double.TryParse(
                grab.GetAttr("length"), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                ? v
                : 0;
        }

        /// <summary>
        /// Classifies what part of the rope <paramref name="point"/> is over and reports the chord
        /// parameter the resulting drag should be anchored to. The knob wins over the cord, since it sits
        /// on it. Tolerances are in level units, so the caller converts screen pixels via the zoom.
        /// </summary>
        /// <param name="g">The rope geometry, from <see cref="Of"/>.</param>
        /// <param name="point">The position to test, in level units; typically the cursor.</param>
        /// <param name="knobTolerance">The hit radius for the knob, in level units.</param>
        /// <param name="cordTolerance">How far off the cord still counts as the cord, in level units.</param>
        /// <returns>The handle under the point, and the parameter to drag it with.</returns>
        public static (Handle Handle, double Parameter) HitTest(
            Geometry g, Vec2 point, double knobTolerance, double cordTolerance)
        {
            if (Distance(point, g.Knob) <= knobTolerance)
            {
                return (Handle.Knob, Parameter(g, g.Knob));
            }

            Vec2[] controls = RopeStripBuilder.ControlPoints(g.Hook, g.Target, g.Length);
            Vec2 previous = RopeStripBuilder.CalcPathBezier(controls, 0);
            for (int i = 1; i <= CordSamples; i++)
            {
                Vec2 next = RopeStripBuilder.CalcPathBezier(controls, (double)i / CordSamples);
                if (SegmentDistance(point, previous, next) <= cordTolerance)
                {
                    return (Handle.Cord, Parameter(g, point));
                }
                previous = next;
            }

            return (Handle.None, 0);
        }

        /// <summary>
        /// The chord parameter for a point: its projection onto the hook-to-target chord, normalized so 0
        /// is the hook and 1 is the target, then clamped into the usable middle of the rope. A degenerate
        /// chord has no direction to project onto, so it reports the midpoint.
        /// </summary>
        /// <param name="g">The rope geometry, from <see cref="Of"/>.</param>
        /// <param name="point">The position to project, in level units.</param>
        /// <returns>The clamped chord parameter.</returns>
        public static double Parameter(Geometry g, Vec2 point)
        {
            if (g.Chord < MinChord)
            {
                return 0.5;
            }

            double dx = g.Target.X - g.Hook.X;
            double dy = g.Target.Y - g.Hook.Y;
            double t = (((point.X - g.Hook.X) * dx) + ((point.Y - g.Hook.Y) * dy)) / (g.Chord * g.Chord);
            return Math.Clamp(t, MinParameter, MaxParameter);
        }

        // The knob sits where the drawn cord hangs furthest below its chord. Sampling beats solving: the
        // cord is a bezier over a variable number of controls, so its low point has no closed form. Ties
        // lose to the midpoint, which keeps a taut rope's knob centered instead of drifting to an end.
        private static Vec2 KnobPoint(Vec2 hook, Vec2 target, double length)
        {
            Vec2[] controls = RopeStripBuilder.ControlPoints(hook, target, length);
            Vec2 best = RopeStripBuilder.CalcPathBezier(controls, 0.5);
            double bestDrop = best.Y - ChordY(hook, target, 0.5);
            for (int i = 1; i < KnobSamples; i++)
            {
                double t = (double)i / KnobSamples;
                Vec2 p = RopeStripBuilder.CalcPathBezier(controls, t);
                double drop = p.Y - ChordY(hook, target, t);
                if (drop > bestDrop)
                {
                    bestDrop = drop;
                    best = p;
                }
            }
            return best;
        }

        // The chord's own Y at parameter t, which the cord's drop is measured against.
        private static double ChordY(Vec2 hook, Vec2 target, double t)
        {
            return hook.Y + ((target.Y - hook.Y) * t);
        }

        private static double Distance(Vec2 a, Vec2 b)
        {
            return GrabRadius.Distance(a, b);
        }

        // Shortest distance from a point to a line segment, used to walk the drawn cord.
        private static double SegmentDistance(Vec2 point, Vec2 a, Vec2 b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lengthSquared = (dx * dx) + (dy * dy);
            if (lengthSquared <= 0)
            {
                return Distance(point, a);
            }

            double t = Math.Clamp((((point.X - a.X) * dx) + ((point.Y - a.Y) * dy)) / lengthSquared, 0, 1);
            return Distance(point, new Vec2(a.X + (dx * t), a.Y + (dy * t)));
        }
    }
}
