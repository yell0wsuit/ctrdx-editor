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
        private const double PollenSpacing = 44.0;

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
            bool retrace = MoverPath.IsRetrace(grab.GetAttr("path"));
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (!retrace || i % 3 == 0)
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
