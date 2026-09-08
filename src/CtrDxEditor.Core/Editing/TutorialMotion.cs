using System;
using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Easing applied to one leg of an authored tutorial path.</summary>
    public enum TutorialEase
    {
        /// <summary>Constant speed across the leg.</summary>
        None,

        /// <summary>Slow start.</summary>
        In,

        /// <summary>Slow end.</summary>
        Out,
    }

    /// <summary>Which motion system a tutorial prompt uses.</summary>
    public enum TutorialMotionMode
    {
        /// <summary>No path, so <c>CTRMover.FromXml</c> returns null and the prompt never moves.</summary>
        None,

        /// <summary>A bare path runs the shared mover, looping forever at constant speed.</summary>
        Looping,

        /// <summary>Path plus ease/moveDelay/repeat drives the prompt off the timeline instead.</summary>
        Timed,
    }

    /// <summary>
    /// Authored travel for one tutorial prompt in Timed mode, expressed as eased legs rather than a
    /// looping mover. Mirrors the game's TutorialMotion, evaluated in closed form so preview needs no
    /// timeline runtime.
    /// </summary>
    public sealed class TutorialMotion
    {
        /// <summary>The game's moveSpeed default (TutorialPromptLoader.Parse), world units per second.</summary>
        private const double DefaultMoveSpeed = 100.0;

        private TutorialMotion(
            IReadOnlyList<Vec2> offsets,
            IReadOnlyList<TutorialEase> eases,
            IReadOnlyList<double> legSeconds,
            double moveDelay)
        {
            Offsets = offsets;
            Eases = eases;
            LegSeconds = legSeconds;
            MoveDelay = moveDelay;

            double travel = moveDelay;
            foreach (double leg in legSeconds)
            {
                travel += leg;
            }

            TravelSeconds = travel;
        }

        /// <summary>Absolute offsets from the anchor, one per leg's end point.</summary>
        public IReadOnlyList<Vec2> Offsets { get; }

        /// <summary>Easing applied to each leg, one entry per <see cref="Offsets"/>.</summary>
        public IReadOnlyList<TutorialEase> Eases { get; }

        /// <summary>Seconds each leg takes to travel, one entry per <see cref="Offsets"/>.</summary>
        public IReadOnlyList<double> LegSeconds { get; }

        /// <summary>Seconds the prompt holds at the anchor before travel starts.</summary>
        public double MoveDelay { get; }

        /// <summary>Total seconds one pass of travel occupies, including the leading delay.</summary>
        public double TravelSeconds { get; }

        /// <summary>
        /// Anchor-relative midpoint of each leg, paired with that leg's ease. A polyline drawn as a plain
        /// line looks identical whether a leg eases in, out, or not at all - <c>PositionAt</c> is where the
        /// difference actually shows, but that needs a running preview. This gives the canvas a static point
        /// per leg to mark instead, one entry per <see cref="Offsets"/> in the same order.
        /// </summary>
        public IReadOnlyList<(Vec2 Midpoint, TutorialEase Ease)> LegMarkers
        {
            get
            {
                (Vec2, TutorialEase)[] markers = new (Vec2, TutorialEase)[Offsets.Count];
                Vec2 previous = new(0, 0);
                for (int leg = 0; leg < Offsets.Count; leg++)
                {
                    Vec2 midpoint = new(
                        (previous.X + Offsets[leg].X) / 2,
                        (previous.Y + Offsets[leg].Y) / 2);
                    markers[leg] = (midpoint, Eases[leg]);
                    previous = Offsets[leg];
                }

                return markers;
            }
        }

        /// <summary>
        /// Which motion system a prompt uses. The game decides by attribute presence: a bare path
        /// runs the shared mover, and any of ease, moveDelay or repeat switches it to the timeline.
        /// A pathless prompt is inert whatever its speeds say, because CTRMover.FromXml returns null.
        /// </summary>
        public static TutorialMotionMode ModeOf(LevelObject o)
        {
            string? path = o.GetAttr("path");
            if (path is null)
            {
                return TutorialMotionMode.None;
            }

            bool timed = o.GetAttr("ease") is not null
                || o.GetAttr("moveDelay") is not null
                || o.GetAttr("repeat") is not null;
            return timed
                ? TutorialMotionMode.Timed
                : path.Length == 0 ? TutorialMotionMode.None : TutorialMotionMode.Looping;
        }

        /// <summary>The travel curve for a prompt in Timed mode, or null when it is not usable.</summary>
        public static TutorialMotion? Timed(LevelObject o)
        {
            if (ModeOf(o) != TutorialMotionMode.Timed || MoverPath.IsCircularPath(o.GetAttr("path")))
            {
                return null;
            }

            Vec2[] points = RelativePolyline.Points(new Vec2(0, 0), o.GetAttr("path"));
            // Points prepends the anchor, so the offsets are everything after it.
            Vec2[] offsets = [.. points[1..]];
            if (offsets.Length == 0 || !TryEases(o.GetAttr("ease"), offsets.Length, out TutorialEase[] eases))
            {
                return null;
            }

            double speed = Positive(o.GetAttr("moveSpeed"), DefaultMoveSpeed);
            double[] legs = new double[offsets.Length];
            Vec2 previous = new(0, 0);
            for (int leg = 0; leg < offsets.Length; leg++)
            {
                legs[leg] = Distance(previous, offsets[leg]) / speed;
                previous = offsets[leg];
            }

            return new TutorialMotion(offsets, eases, legs, NonNegative(o.GetAttr("moveDelay"), 0.0));
        }

        /// <summary>
        /// Seconds authored travel takes for one pass in Timed mode, evaluated at the game's float
        /// precision instead of <see cref="TravelSeconds"/>'s double. Mirrors the game's own
        /// TutorialMotion.Parse arithmetic exactly - float moveSpeed, float path components, MathF.Sqrt -
        /// so a comparison against <see cref="TutorialTiming.PassSecondsAtGameFloatPrecision"/> lands on
        /// the same side of the rounding boundary the game does. Returns null when the prompt is not in
        /// Timed mode, its path is circular, or moveSpeed, moveDelay or a path component fails the
        /// game's own strict float parse (non-finite, unparseable, or out of range).
        /// </summary>
        public static float? TravelSecondsAtGameFloatPrecision(LevelObject o)
        {
            if (ModeOf(o) != TutorialMotionMode.Timed || MoverPath.IsCircularPath(o.GetAttr("path")))
            {
                return null;
            }

            if (!GameFloat.TryPositive(o.GetAttr("moveSpeed"), (float)DefaultMoveSpeed, out float speed)
                || !GameFloat.TryNonNegative(o.GetAttr("moveDelay"), 0f, out float moveDelay))
            {
                return null;
            }

            string path = o.GetAttr("path")!;
            string trimmed = path.EndsWith(',') ? path[..^1] : path;
            string[] parts = trimmed.Split(',');
            if (parts.Length == 0 || parts.Length % 2 != 0)
            {
                return null;
            }

            float travel = moveDelay;
            float previousX = 0f;
            float previousY = 0f;
            for (int pair = 0; pair < parts.Length; pair += 2)
            {
                if (!GameFloat.TryPathComponent(parts[pair], out float x)
                    || !GameFloat.TryPathComponent(parts[pair + 1], out float y))
                {
                    return null;
                }

                float dx = x - previousX;
                float dy = y - previousY;
                travel += MathF.Sqrt((dx * dx) + (dy * dy)) / speed;
                previousX = x;
                previousY = y;
            }

            return travel;
        }

        /// <summary>The eased fraction of a leg travelled, integrating the game's constant acceleration.</summary>
        public static double EaseProgress(TutorialEase ease, double progress)
        {
            return ease switch
            {
                TutorialEase.In => progress * progress,
                TutorialEase.Out => 1 - ((1 - progress) * (1 - progress)),
                TutorialEase.None => progress,
                _ => progress,
            };
        }

        /// <summary>Where the prompt sits at an elapsed time, anchored at its authored position.</summary>
        public Vec2 PositionAt(double seconds, Vec2 anchor)
        {
            double elapsed = seconds - MoveDelay;
            if (elapsed <= 0)
            {
                return anchor;
            }

            Vec2 previous = new(0, 0);
            for (int leg = 0; leg < Offsets.Count; leg++)
            {
                double duration = LegSeconds[leg];
                if (elapsed < duration && duration > 0)
                {
                    double eased = EaseProgress(Eases[leg], elapsed / duration);
                    return new Vec2(
                        anchor.X + previous.X + ((Offsets[leg].X - previous.X) * eased),
                        anchor.Y + previous.Y + ((Offsets[leg].Y - previous.Y) * eased));
                }

                elapsed -= duration;
                previous = Offsets[leg];
            }

            return new Vec2(anchor.X + previous.X, anchor.Y + previous.Y);
        }

        /// <summary>
        /// Parses a comma-separated per-leg ease list, matching the game's ParseEases: a single value
        /// applies to every leg, otherwise the count must match the leg count exactly. A null ease
        /// (no attribute authored) fills every leg with <see cref="TutorialEase.None"/>.
        /// </summary>
        private static bool TryEases(string? ease, int legs, out TutorialEase[] eases)
        {
            if (ease is null)
            {
                eases = new TutorialEase[legs];
                return true;
            }

            string[] parts = ease.Split(',');
            TutorialEase[] parsed = new TutorialEase[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                if (!TryEase(parts[i], out parsed[i]))
                {
                    eases = [];
                    return false;
                }
            }

            if (parsed.Length == 1 && legs != 1)
            {
                eases = new TutorialEase[legs];
                Array.Fill(eases, parsed[0]);
                return true;
            }

            if (parsed.Length != legs)
            {
                eases = [];
                return false;
            }

            eases = parsed;
            return true;
        }

        private static bool TryEase(string value, out TutorialEase ease)
        {
            switch (value)
            {
                case "none":
                    ease = TutorialEase.None;
                    return true;
                case "in":
                    ease = TutorialEase.In;
                    return true;
                case "out":
                    ease = TutorialEase.Out;
                    return true;
                default:
                    ease = TutorialEase.None;
                    return false;
            }
        }

        private static double Distance(Vec2 a, Vec2 b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static double Positive(string? value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                && double.IsFinite(parsed)
                && parsed > 0
                    ? parsed
                    : fallback;
        }

        private static double NonNegative(string? value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                && double.IsFinite(parsed)
                && parsed >= 0
                    ? parsed
                    : fallback;
        }
    }

    /// <summary>
    /// Writes the attribute combination that selects a tutorial prompt's <see cref="TutorialMotionMode"/>.
    /// Kept in Core, apart from the panel view model, so the writes are unit-testable without a UI.
    /// </summary>
    public static class TutorialMotionEditor
    {
        /// <summary>Path seeded when a mode needs one and none usable is authored.</summary>
        private const string DefaultPath = "100,0";

        private const string DefaultMoveSpeed = "100";

        /// <summary>
        /// Removes the attributes the target mode does not read and seeds what it needs: a default
        /// path when entering a mode with none authored (or with a circular one while entering
        /// Timed), <c>moveSpeed="100"</c> when entering Looping with no speed (the shared DX mover
        /// otherwise reads an absent speed as zero), and <c>ease="none"</c> when entering Timed with
        /// no ease. Existing authored speeds are preserved.
        /// </summary>
        /// <param name="o">The tutorial prompt whose motion attributes are updated.</param>
        /// <param name="mode">The motion mode to switch to.</param>
        public static void SetMode(LevelObject o, TutorialMotionMode mode)
        {
            switch (mode)
            {
                case TutorialMotionMode.None:
                    o.RemoveAttr("path");
                    o.RemoveAttr("ease");
                    o.RemoveAttr("moveDelay");
                    o.RemoveAttr("moveSpeed");
                    o.RemoveAttr("repeat");
                    o.RemoveAttr("rotateSpeed");
                    break;

                case TutorialMotionMode.Looping:
                    o.RemoveAttr("ease");
                    o.RemoveAttr("moveDelay");
                    o.RemoveAttr("repeat");
                    if (string.IsNullOrEmpty(o.GetAttr("path")))
                    {
                        o.SetAttr("path", DefaultPath);
                    }
                    if (o.GetAttr("moveSpeed") is null)
                    {
                        o.SetAttr("moveSpeed", DefaultMoveSpeed);
                    }
                    break;

                case TutorialMotionMode.Timed:
                    o.RemoveAttr("rotateSpeed");
                    if (!HasTimeableOffset(o.GetAttr("path")))
                    {
                        o.SetAttr("path", DefaultPath);
                    }
                    if (o.GetAttr("ease") is null)
                    {
                        o.SetAttr("ease", "none");
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Whether a path is usable for Timed motion: not circular (the timeline can't express an
        /// orbit) and carrying at least one parseable offset (an empty or garbage path parses to
        /// none). A circular path is the common case this rejects, but any path <see cref="TutorialMotion.Timed"/>
        /// could not turn into a leg is replaced the same way.
        /// </summary>
        private static bool HasTimeableOffset(string? path)
        {
            return !string.IsNullOrEmpty(path)
                && !MoverPath.IsCircularPath(path)
                && RelativePolyline.Points(new Vec2(0, 0), path).Length > 1;
        }
    }
}
