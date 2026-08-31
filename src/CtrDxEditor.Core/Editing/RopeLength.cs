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
        /// <param name="Physics">The level's physics model, which sets how the cord is subdivided.</param>
        public readonly record struct Geometry(
            Vec2 Hook, Vec2 Target, double Chord, double Length, Vec2 Knob, double KnobParameter, bool Taut,
            RopePhysics Physics);

        /// <summary>
        /// The fixed state of an in-progress rope drag, captured when the press landed. Both solvers work
        /// as an offset from this, so a press that never moves cannot change the length whatever the cord
        /// happens to look like.
        /// </summary>
        /// <param name="Parameter">The curve parameter the drag is anchored to, from <see cref="HitTest"/>.</param>
        /// <param name="Origin">Where the press landed, in level units.</param>
        /// <param name="Length">The rope's rest length when the press landed, in level units.</param>
        public readonly record struct Drag(double Parameter, Vec2 Origin, double Length);

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
        /// <param name="physics">The level's physics model, which sets how the cord is subdivided.</param>
        /// <returns>The rope geometry, or null when there is no rope to edit.</returns>
        public static Geometry? Of(LevelObject grab, RopeTarget rope, RopePhysics physics)
        {
            if (grab.Type != "grab" || rope.Target is not { } bound)
            {
                return null;
            }

            Vec2 hook = new(grab.X, grab.Y);
            Vec2 target = new(bound.X, bound.Y);
            double chord = Distance(hook, target);
            double length = ReadLength(grab);
            (Vec2 knob, double knobT) = KnobPoint(hook, target, length, physics);
            return new Geometry(hook, target, chord, length, knob, knobT, length <= chord, physics);
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

            Vec2[] controls = RopeStripBuilder.ControlPoints(g.Hook, g.Target, g.Length, g.Physics);
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
        /// Captures the fixed state of a drag as the press lands, so the solvers can work as an offset from
        /// it rather than reading a length straight out of the cursor position.
        /// </summary>
        /// <param name="g">The rope geometry, from <see cref="Of"/>.</param>
        /// <param name="parameter">The curve parameter under the press, from <see cref="HitTest"/>.</param>
        /// <param name="origin">Where the press landed, in level units.</param>
        /// <returns>The drag state to pass to <see cref="Solve"/> and <see cref="SolveTaut"/>.</returns>
        public static Drag BeginDrag(Geometry g, double parameter, Vec2 origin)
        {
            return new Drag(parameter, origin, g.Length);
        }

        /// <summary>
        /// The rest length for a drag: the rope's length at the press, shifted by however much the cord
        /// mapping has moved since. Relative rather than absolute, because a rope at or below its chord
        /// draws as the same straight line at every length - an absolute mapping cannot tell 50 from 200 and
        /// would snap a short rope taut the moment it was touched. Working from the press instead means a
        /// press that does not move changes nothing, and a drag grows the rope from wherever it already was.
        /// Only the floor survives from the absolute form: dragging back toward the chord stops at taut,
        /// since below that the cord stops responding - use <see cref="SolveTaut"/> for that range.
        /// </summary>
        /// <param name="g">The rope geometry, from <see cref="Of"/>.</param>
        /// <param name="drag">The drag state, from <see cref="BeginDrag"/>.</param>
        /// <param name="point">The drag position, in level units.</param>
        /// <returns>The new rest length in level units.</returns>
        public static double Solve(Geometry g, Drag drag, Vec2 point)
        {
            double moved = SagLength(g, drag.Parameter, point) - SagLength(g, drag.Parameter, drag.Origin);
            return Math.Max(MinLength, drag.Length + moved);
        }

        /// <summary>
        /// The rest length for an Alt drag, the one that reaches below taut: the length at the press shifted
        /// by how much further from the hook the cursor has travelled. Normalizing by the drag's parameter
        /// keeps a step of cursor travel worth about a step of rope. Relative for the same reason as
        /// <see cref="Solve"/>, and with no ceiling, so releasing Alt part-way through a drag never jumps.
        /// Every length at or below the chord draws the same straight cord, so the badge is the only
        /// feedback this range has.
        /// </summary>
        /// <param name="g">The rope geometry, from <see cref="Of"/>.</param>
        /// <param name="drag">The drag state, from <see cref="BeginDrag"/>.</param>
        /// <param name="point">The drag position, in level units.</param>
        /// <returns>The new rest length in level units.</returns>
        public static double SolveTaut(Geometry g, Drag drag, Vec2 point)
        {
            double parameter = Math.Clamp(drag.Parameter, MinParameter, MaxParameter);
            double moved = (Distance(point, g.Hook) - Distance(drag.Origin, g.Hook)) / parameter;
            return Math.Max(MinLength, drag.Length + moved);
        }

        // The rest length whose drawn cord passes through `point` at curve parameter t: bisects on the
        // cord's own Y, which rises monotonically with length. This is the absolute mapping the public
        // solvers difference against; on its own it cannot represent a taut rope, since every length at or
        // below the chord draws the same line and so reports the chord.
        private static double SagLength(Geometry g, double t, Vec2 point)
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

        // The knob sits where the drawn cord hangs furthest below its chord, and carries the curve
        // parameter it was found at so a drag from it solves for that exact point. Sampling beats solving:
        // the cord is a bezier over a variable number of controls, so its low point has no closed form.
        // Ties lose to the midpoint, which keeps a taut rope's knob centered instead of drifting to an end.
        private static (Vec2 Point, double T) KnobPoint(Vec2 hook, Vec2 target, double length, RopePhysics physics)
        {
            Vec2[] controls = RopeStripBuilder.ControlPoints(hook, target, length, physics);
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
                RopeStripBuilder.ControlPoints(g.Hook, g.Target, length, g.Physics), t).Y;
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
