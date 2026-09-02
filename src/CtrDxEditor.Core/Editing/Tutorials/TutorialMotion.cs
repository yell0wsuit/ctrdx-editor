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

            double speed = Positive(o.GetAttr("moveSpeed"), 100.0);
            double[] legs = new double[offsets.Length];
            Vec2 previous = new(0, 0);
            for (int leg = 0; leg < offsets.Length; leg++)
            {
                legs[leg] = Distance(previous, offsets[leg]) / speed;
                previous = offsets[leg];
            }

            return new TutorialMotion(offsets, eases, legs, NonNegative(o.GetAttr("moveDelay"), 0.0));
        }

        /// <summary>The eased fraction of a leg travelled, integrating the game's constant acceleration.</summary>
        public static double EaseProgress(TutorialEase ease, double progress)
        {
            return ease switch
            {
                TutorialEase.In => progress * progress,
                TutorialEase.Out => 1 - ((1 - progress) * (1 - progress)),
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
}
