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
        /// Chords shorter than this are degenerate: the hook sits on its target, so there is no cord shape
        /// to solve against and the drag falls back to plain distance from the hook.
        /// </summary>
        public const double MinChord = 1;

        /// <summary>
        /// A drag can only start on the stretch of cord between <see cref="MinParameter"/> and
        /// <see cref="MaxParameter"/>. Near an endpoint the cord barely moves for a large length change, so
        /// a drag anchored there would swing the length wildly for a pixel of travel.
        /// </summary>
        public const double MinParameter = 0.15;

        /// <summary>The far end of the grabbable stretch described on <see cref="MinParameter"/>.</summary>
        public const double MaxParameter = 0.85;

        // How finely the drawn cord is sampled when looking for its lowest point.
        private const int KnobSamples = 32;

        // How finely the drawn cord is walked when hit-testing; consecutive samples are joined into
        // segments, so this is a shape-fidelity knob rather than a hit-accuracy one.
        private const int CordSamples = 32;

        // Bisection budget. The bracket is at most a few thousand level units wide, so this lands well
        // inside the whole-unit precision the length attribute stores.
        private const int SolveIterations = 20;

        /// <summary>Resolved rope geometry in level space.</summary>
        /// <param name="Hook">The grab's hook position, in level units.</param>
        /// <param name="Target">The bound candy or light bulb's position, in level units.</param>
        /// <param name="Chord">The straight-line distance between the two, in level units.</param>
        /// <param name="Length">The authored rest length, in level units.</param>
        /// <param name="Knob">The drag knob's position on the drawn cord, in level units.</param>
        /// <param name="KnobParameter">The curve parameter <paramref name="Knob"/> was found at.</param>
        /// <param name="Taut">Whether the rope has no slack (<paramref name="Length"/> is at most <paramref name="Chord"/>).</param>
        public readonly record struct Geometry(
            Vec2 Hook, Vec2 Target, double Chord, double Length, Vec2 Knob, double KnobParameter, bool Taut);

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
            (Vec2 knob, double knobT) = KnobPoint(hook, target, length);
            return new Geometry(hook, target, chord, length, knob, knobT, length <= chord);
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
        /// Classifies what part of the rope <paramref name="point"/> is over and reports the curve
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
            // The knob carries its own parameter rather than deriving one: it must round-trip exactly, or
            // a press with no movement would solve for a different point on the cord and shift the length.
            if (Distance(point, g.Knob) <= knobTolerance)
            {
                return (Handle.Knob, g.KnobParameter);
            }

            Vec2[] controls = RopeStripBuilder.ControlPoints(g.Hook, g.Target, g.Length);
            Vec2 previous = RopeStripBuilder.CalcPathBezier(controls, 0);
            for (int i = 1; i <= CordSamples; i++)
            {
                double nextT = (double)i / CordSamples;
                Vec2 next = RopeStripBuilder.CalcPathBezier(controls, nextT);
                if (SegmentDistance(point, previous, next) <= cordTolerance)
                {
                    double previousT = (double)(i - 1) / CordSamples;
                    double t = previousT
                        + ((nextT - previousT) * SegmentFraction(point, previous, next));

                    // Refuse the ends rather than clamping into range: clamping would anchor the drag to a
                    // parameter the cursor is not actually on, which shifts the length the moment you press.
                    return t is < MinParameter or > MaxParameter
                        ? (Handle.None, 0)
                        : (Handle.Cord, t);
                }
                previous = next;
            }

            return (Handle.None, 0);
        }

        /// <summary>
        /// The rest length that puts the drawn cord under <paramref name="point"/>: bisects length until
        /// the cord's own Y at curve parameter <paramref name="t"/> reaches the point's. Comparing the
        /// curve against the cursor directly, rather than against the chord, keeps the solve in the same
        /// parameterization the cord is drawn in, so pressing without moving is a no-op. Floors at the
        /// chord distance, because every shorter rope draws as the same straight line — use
        /// <see cref="SolveTaut"/> to reach that range. A degenerate chord has no curve to measure, so it
        /// reads plain distance from the hook instead; note that is unclamped above, unlike
        /// <see cref="SolveTaut"/>, since there is no meaningful taut ceiling when the chord is zero.
        /// </summary>
        /// <param name="g">The rope geometry, from <see cref="Of"/>.</param>
        /// <param name="t">The curve parameter recorded when the drag began, from <see cref="HitTest"/>.</param>
        /// <param name="point">The drag position, in level units.</param>
        /// <returns>The new rest length in level units.</returns>
        public static double Solve(Geometry g, double t, Vec2 point)
        {
            if (g.Chord < MinChord)
            {
                return Math.Max(MinLength, Distance(point, g.Hook));
            }

            double wanted = point.Y;
            if (wanted <= CurveY(g, g.Chord, t))
            {
                return g.Chord;
            }

            // The two-segment path through the cursor is a lower bound on the arc length of any curve
            // through those three points, so it is a good first guess; double until it actually brackets.
            double low = g.Chord;
            double high = Math.Max(low, Distance(point, g.Hook) + Distance(point, g.Target));
            for (int i = 0; i < SolveIterations && CurveY(g, high, t) < wanted; i++)
            {
                high = low + Math.Max(1, (high - low) * 2);
            }

            for (int i = 0; i < SolveIterations; i++)
            {
                double mid = (low + high) / 2;
                if (CurveY(g, mid, t) < wanted)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }
            return (low + high) / 2;
        }

        /// <summary>
        /// The rest length for a below-taut drag: distance from the hook normalized by the drag's fixed
        /// curve parameter, then clamped into [<see cref="MinLength"/>, the chord distance]. Normalizing
        /// makes this mapping meet <see cref="Solve"/> at the same point on the taut chord, so toggling Alt
        /// does not jump. Every length in that range draws as the same straight cord, so the number is the
        /// only feedback and the canvas shows it beside the cursor.
        /// </summary>
        /// <param name="g">The rope geometry, from <see cref="Of"/>.</param>
        /// <param name="t">The fixed curve parameter recorded when the drag began.</param>
        /// <param name="point">The drag position, in level units.</param>
        /// <returns>The new rest length in level units.</returns>
        public static double SolveTaut(Geometry g, double t, Vec2 point)
        {
            double parameter = Math.Clamp(t, MinParameter, MaxParameter);
            return Math.Clamp(Distance(point, g.Hook) / parameter, MinLength, Math.Max(MinLength, g.Chord));
        }

        // The knob sits where the drawn cord hangs furthest below its chord, and carries the curve
        // parameter it was found at so a drag from it solves for that exact point. Sampling beats solving:
        // the cord is a bezier over a variable number of controls, so its low point has no closed form.
        // Ties lose to the midpoint, which keeps a taut rope's knob centered instead of drifting to an end.
        private static (Vec2 Point, double T) KnobPoint(Vec2 hook, Vec2 target, double length)
        {
            Vec2[] controls = RopeStripBuilder.ControlPoints(hook, target, length);
            Vec2 best = RopeStripBuilder.CalcPathBezier(controls, 0.5);
            double bestT = 0.5;
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
                    bestT = t;
                }
            }
            return (best, bestT);
        }

        // The chord's own Y at parameter t, which the knob search measures sag against.
        private static double ChordY(Vec2 hook, Vec2 target, double t)
        {
            return hook.Y + ((target.Y - hook.Y) * t);
        }

        private static double Distance(Vec2 a, Vec2 b)
        {
            return GrabRadius.Distance(a, b);
        }

        // The drawn cord's Y at curve parameter t, for a candidate rest length.
        private static double CurveY(Geometry g, double length, double t)
        {
            return RopeStripBuilder.CalcPathBezier(
                RopeStripBuilder.ControlPoints(g.Hook, g.Target, length), t).Y;
        }

        // How far along a segment the closest point to `point` lies, as a 0-1 fraction.
        private static double SegmentFraction(Vec2 point, Vec2 a, Vec2 b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double lengthSquared = (dx * dx) + (dy * dy);
            return lengthSquared <= 0
                ? 0
                : Math.Clamp((((point.X - a.X) * dx) + ((point.Y - a.Y) * dy)) / lengthSquared, 0, 1);
        }

        // Shortest distance from a point to a line segment, used to walk the drawn cord.
        private static double SegmentDistance(Vec2 point, Vec2 a, Vec2 b)
        {
            double f = SegmentFraction(point, a, b);
            return Distance(point, new Vec2(a.X + ((b.X - a.X) * f), a.Y + ((b.Y - a.Y) * f)));
        }
    }
}
