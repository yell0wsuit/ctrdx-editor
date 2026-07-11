using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Deterministic DX bee animation and pollen geometry for moving rope hooks.</summary>
    public static class GrabBeeRenderer
    {
        private const double PollenSpacing = 44.0 / SpritePlacement.MapScale;
        private const double BeeAnchorX = -6.0 / SpritePlacement.MapScale;
        private const double BeeAnchorY = -58.0 / SpritePlacement.MapScale;

        /// <summary>Base quad enlargement applied by the DX pollen drawer before particle scaling.</summary>
        public const double PollenQuadScale = 1.5;

        /// <summary>Current deterministic draw state for one pollen particle.</summary>
        /// <param name="ScaleX">Horizontal particle scale.</param>
        /// <param name="ScaleY">Vertical particle scale.</param>
        /// <param name="Alpha">Particle opacity.</param>
        public readonly record struct PollenVisual(double ScaleX, double ScaleY, double Alpha);

        /// <summary>Whether the game attaches a bee to this grab.</summary>
        /// <param name="grab">Grab whose mover state is inspected.</param>
        /// <returns><see langword="true"/> when the grab has active movement.</returns>
        public static bool HasBee(LevelObject grab)
        {
            return grab.Type == "grab" && MoverPath.HasActiveMovement(grab);
        }

        /// <summary>Returns deterministic pollen positions along the grab mover path.</summary>
        /// <param name="grab">Grab whose movement path supplies pollen geometry.</param>
        /// <returns>Pollen positions in level coordinates, or an empty list when pollen is hidden or movement is inactive.</returns>
        public static IReadOnlyList<Vec2> PollenPoints(LevelObject grab)
        {
            if (!HasBee(grab) || IsTrue(grab.GetAttr("hidePath")))
            {
                return [];
            }

            Vec2[] points = MoverPath.Points(new Vec2(grab.X, grab.Y), grab.GetAttr("path"));
            List<Vec2> result = [];
            bool sparse = grab.GetAttr("path")?.StartsWith('R') == true;
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (!sparse || i % 3 == 0)
                {
                    AddSegment(result, points[i], points[i + 1]);
                }
            }
            if (points.Length > 2)
            {
                AddSegment(result, points[0], points[^1]);
            }
            return result;
        }

        /// <summary>Static middle wings, or the game's 2→4 ping-pong animation during preview.</summary>
        /// <param name="elapsedSeconds">Elapsed animation-preview time, or null for the static middle frame.</param>
        /// <returns>The visual descriptor key for the current wing frame.</returns>
        public static string WingSpriteKey(double? elapsedSeconds)
        {
            if (elapsedSeconds is not double seconds)
            {
                return "grab_bee_wing_1";
            }
            int phase = (int)Math.Floor(Math.Max(0, seconds) / 0.03) % 4;
            return phase switch
            {
                0 => "grab_bee_wing_0",
                1 or 3 => "grab_bee_wing_1",
                _ => "grab_bee_wing_2",
            };
        }

        /// <summary>Whether a grab's authored rope remains visible for the current preview state.</summary>
        /// <param name="grab">Grab whose active movement state is inspected.</param>
        /// <param name="animationPreviewSeconds">Elapsed preview time, or null when this grab is not previewing.</param>
        /// <returns><see langword="false"/> only for an actively moving grab during its animation preview.</returns>
        public static bool ShouldDrawRope(LevelObject grab, double? animationPreviewSeconds)
        {
            return animationPreviewSeconds is null || !MoverPath.HasActiveMovement(grab);
        }

        /// <summary>Offsets the bee above the grab point so its parent hook remains visible beneath it.</summary>
        /// <param name="grabPosition">Preview-aware grab position in level coordinates.</param>
        /// <returns>The bee anchor derived from the fallback offset used by DX <c>Grab.SetBee</c>.</returns>
        public static Vec2 BeeAnchor(Vec2 grabPosition)
        {
            return new Vec2(grabPosition.X + BeeAnchorX, grabPosition.Y + BeeAnchorY);
        }

        /// <summary>Computes the game's scale and alpha motion using deterministic per-index initialization.</summary>
        /// <param name="index">Stable particle index along all rendered grab paths.</param>
        /// <param name="elapsedSeconds">Elapsed preview time, or null for the deterministic initial state.</param>
        /// <returns>Independent axis scales and alpha for drawing the particle.</returns>
        public static PollenVisual PollenVisualAt(int index, double? elapsedSeconds)
        {
            double[] options = [0.3, 0.3, 0.5, 0.5, 0.6];
            double endScaleX = options[Math.Abs(index) % options.Length];
            double endScaleY = endScaleX;
            if ((index & 1) == 0)
            {
                endScaleX *= 1.1;
            }
            else
            {
                endScaleY *= 1.1;
            }

            double scaleOffset = Math.Min(1 - endScaleX, 1 - endScaleY);
            double startScaleX = endScaleX + scaleOffset;
            double startScaleY = endScaleY + scaleOffset;
            double phase = DeterministicPhase(index);
            double seconds = Math.Max(0, elapsedSeconds ?? 0);
            double scaleX = PingPong(startScaleX * phase, endScaleX, startScaleX, seconds);
            double scaleY = PingPong(startScaleY * phase, endScaleY, startScaleY, seconds);
            double alpha = PingPong(0.3 + (0.7 * phase), 0.3, 1.0, seconds);
            return new PollenVisual(scaleX, scaleY, alpha);
        }

        /// <summary>Derives a reproducible replacement for the game's random initial particle phase.</summary>
        /// <param name="index">Stable particle index.</param>
        /// <returns>A deterministic value in the half-open interval [0, 1).</returns>
        private static double DeterministicPhase(int index)
        {
            double phase = 0.37 + (Math.Abs(index) * 0.6180339887498949);
            return phase - Math.Floor(phase);
        }

        /// <summary>Advances one scalar through the game's initial target and subsequent ping-pong targets.</summary>
        /// <param name="initial">Deterministic initial value.</param>
        /// <param name="firstTarget">Target used by the particle's first update.</param>
        /// <param name="otherTarget">Alternate target used after reaching <paramref name="firstTarget"/>.</param>
        /// <param name="seconds">Elapsed time at one unit per second.</param>
        /// <returns>The scalar value after the requested elapsed time.</returns>
        private static double PingPong(double initial, double firstTarget, double otherTarget, double seconds)
        {
            double firstDistance = Math.Abs(firstTarget - initial);
            if (seconds <= firstDistance)
            {
                return MoveTowards(initial, firstTarget, seconds);
            }

            seconds -= firstDistance;
            double range = Math.Abs(otherTarget - firstTarget);
            if (range <= 0)
            {
                return firstTarget;
            }
            double cycle = seconds % (range * 2);
            return cycle <= range
                ? MoveTowards(firstTarget, otherTarget, cycle)
                : MoveTowards(otherTarget, firstTarget, cycle - range);
        }

        /// <summary>Moves a scalar toward a target without overshooting.</summary>
        /// <param name="value">Starting value.</param>
        /// <param name="target">Destination value.</param>
        /// <param name="distance">Maximum absolute movement.</param>
        /// <returns>The moved value, clamped to <paramref name="target"/>.</returns>
        private static double MoveTowards(double value, double target, double distance)
        {
            return target >= value
                ? Math.Min(target, value + distance)
                : Math.Max(target, value - distance);
        }

        /// <summary>Appends pollen particles at the game's fixed spacing along one path segment.</summary>
        /// <param name="result">Destination particle collection.</param>
        /// <param name="a">Segment start in level coordinates.</param>
        /// <param name="b">Segment end in level coordinates.</param>
        private static void AddSegment(List<Vec2> result, Vec2 a, Vec2 b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double length = Math.Sqrt((dx * dx) + (dy * dy));
            int count = (int)(length / PollenSpacing);
            for (int i = 0; i <= count; i++)
            {
                double distance = i * PollenSpacing;
                double t = length <= 0 ? 0 : distance / length;
                result.Add(new Vec2(a.X + (dx * t), a.Y + (dy * t)));
            }
        }

        /// <summary>Parses an optional XML boolean using the game's false-by-default convention.</summary>
        /// <param name="value">Raw XML attribute value.</param>
        /// <returns><see langword="true"/> only when the value parses as true.</returns>
        private static bool IsTrue(string? value)
        {
            return bool.TryParse(value, out bool result) && result;
        }
    }
}
