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

        // How finely the drawn cord is sampled when looking for its lowest point.
        private const int KnobSamples = 32;

        /// <summary>Resolved rope geometry in level space.</summary>
        /// <param name="Hook">The grab's hook position, in level units.</param>
        /// <param name="Target">The bound candy or light bulb's position, in level units.</param>
        /// <param name="Chord">The straight-line distance between the two, in level units.</param>
        /// <param name="Length">The authored rest length, in level units.</param>
        /// <param name="Knob">The drag knob's position on the drawn cord, in level units.</param>
        /// <param name="Taut">Whether the rope has no slack (<paramref name="Length"/> is at most <paramref name="Chord"/>).</param>
        public readonly record struct Geometry(
            Vec2 Hook, Vec2 Target, double Chord, double Length, Vec2 Knob, bool Taut);

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
    }
}
