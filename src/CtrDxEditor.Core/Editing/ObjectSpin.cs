using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Helpers for the game's mover-backed spin and orbit attributes, exposed as Spin in the editor.</summary>
    public static class ObjectSpin
    {
        /// <summary>Default spin speed written when enabling spin on an object without an existing speed.</summary>
        public const int DefaultSpeed = 70;

        /// <summary>Minimal non-moving path required for DX to construct a rotating mover.</summary>
        public const string StaticPath = "0,0";

        /// <summary>Default radius used when enabling DX circular orbit paths.</summary>
        public const int DefaultOrbitRadius = 30;

        /// <summary>Whether <paramref name="obj"/> has active in-place spin data.</summary>
        /// <param name="obj">Object whose spin-related mover attributes are inspected.</param>
        /// <returns><see langword="true"/> when <paramref name="obj"/> stores non-zero rotateSpeed.</returns>
        public static bool IsSpinning(LevelObject obj)
        {
            return IsRotatingInPlace(obj);
        }

        /// <summary>Whether the object has active rotateSpeed-backed in-place spin.</summary>
        /// <param name="obj">Object whose <c>rotateSpeed</c> attribute is inspected.</param>
        /// <returns><see langword="true"/> when <paramref name="obj"/> stores a non-zero rotate speed.</returns>
        public static bool IsRotatingInPlace(LevelObject obj)
        {
            return RawSpeed(obj) != 0;
        }

        /// <summary>Whether the object uses DX circular mover syntax (<c>RC</c>/<c>RW</c>) with speed.</summary>
        /// <param name="obj">Object whose <c>path</c> and <c>moveSpeed</c> attributes are inspected.</param>
        /// <returns><see langword="true"/> when the object has a circular path and non-zero move speed.</returns>
        public static bool IsOrbital(LevelObject obj)
        {
            return IsCircularPath(obj.GetAttr("path")) && RawMoveSpeed(obj) > 0;
        }

        /// <summary>The positive whole-number speed magnitude shown in the editor.</summary>
        /// <param name="obj">Object whose <c>rotateSpeed</c> or orbital <c>moveSpeed</c> attribute is inspected.</param>
        /// <returns>The absolute whole-number speed, or zero when absent or invalid.</returns>
        public static int SpinSpeed(LevelObject obj)
        {
            return Math.Abs(RawSpeed(obj));
        }

        /// <summary>Whether the stored self-spin direction is clockwise. Zero defaults to clockwise for new spins.</summary>
        /// <param name="obj">Object whose spin-related mover attributes are inspected.</param>
        /// <returns><see langword="true"/> for non-negative rotate speeds.</returns>
        public static bool SpinClockwise(LevelObject obj)
        {
            return RawSpeed(obj) >= 0;
        }

        /// <summary>Whether the stored orbital path direction is clockwise.</summary>
        /// <param name="obj">Object whose <c>path</c> attribute is inspected.</param>
        /// <returns><see langword="true"/> for <c>RC</c> paths, and as the default for new orbit paths.</returns>
        public static bool OrbitClockwise(LevelObject obj)
        {
            string? path = obj.GetAttr("path");
            return !IsCircularPath(path) || path![1] == 'C';
        }

        /// <summary>Writes or clears spin data, storing direction as the sign of <paramref name="speed"/>.</summary>
        /// <param name="obj">Object whose spin attributes are updated.</param>
        /// <param name="enabled">Whether spin should remain enabled.</param>
        /// <param name="speed">Positive whole-number spin speed magnitude.</param>
        /// <param name="clockwise">Whether the stored speed should be positive.</param>
        public static void SetSpin(LevelObject obj, bool enabled, int speed, bool clockwise)
        {
            if (!enabled || speed <= 0)
            {
                obj.RemoveAttr("rotateSpeed");
                return;
            }

            int signed = clockwise ? speed : -speed;
            obj.SetAttr("rotateSpeed", signed.ToString(CultureInfo.InvariantCulture));
            if (string.IsNullOrEmpty(obj.GetAttr("path")))
            {
                obj.SetAttr("path", StaticPath);
            }
        }

        /// <summary>The positive radius encoded in a DX circular path, defaulting when no valid path exists.</summary>
        /// <param name="obj">Object whose <c>path</c> attribute is inspected.</param>
        /// <returns>The positive <c>RC</c>/<c>RW</c> radius, or <see cref="DefaultOrbitRadius"/>.</returns>
        public static int OrbitRadius(LevelObject obj)
        {
            string? path = obj.GetAttr("path");
            return IsCircularPath(path) && int.TryParse(path![2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int radius) && radius > 0
                ? radius
                : DefaultOrbitRadius;
        }

        /// <summary>The positive whole-number movement speed encoded in DX <c>moveSpeed</c>.</summary>
        /// <param name="obj">Object whose <c>moveSpeed</c> attribute is inspected.</param>
        /// <returns>The absolute whole-number movement speed, or zero when absent or invalid.</returns>
        public static int OrbitSpeed(LevelObject obj)
        {
            return RawMoveSpeed(obj);
        }

        /// <summary>Writes or clears orbit data using DX circular path syntax (<c>RC</c>/<c>RW</c>).</summary>
        /// <param name="obj">Object whose orbital spin attributes are updated.</param>
        /// <param name="enabled">Whether orbital spin should remain enabled.</param>
        /// <param name="radius">Positive circular path radius.</param>
        /// <param name="clockwise">Whether the path should use clockwise <c>RC</c> direction.</param>
        public static void SetOrbital(LevelObject obj, bool enabled, int radius, bool clockwise)
        {
            if (!enabled || radius <= 0)
            {
                if (IsCircularPath(obj.GetAttr("path")))
                {
                    obj.RemoveAttr("path");
                    obj.RemoveAttr("moveSpeed");
                }
                return;
            }

            obj.SetAttr("path", $"{(clockwise ? "RC" : "RW")}{radius.ToString(CultureInfo.InvariantCulture)}");
            int moveSpeed = RawMoveSpeed(obj);
            if (moveSpeed == 0)
            {
                moveSpeed = SpinSpeed(obj);
            }
            obj.SetAttr("moveSpeed", (moveSpeed > 0 ? moveSpeed : DefaultSpeed).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Writes the DX <c>moveSpeed</c> used to advance along an orbital path.</summary>
        /// <param name="obj">Object whose orbital movement speed is updated.</param>
        /// <param name="speed">Positive whole-number movement speed magnitude.</param>
        public static void SetOrbitSpeed(LevelObject obj, int speed)
        {
            if (speed <= 0)
            {
                obj.RemoveAttr("moveSpeed");
                return;
            }

            obj.SetAttr("moveSpeed", speed.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Computes the signed live-preview rotation angle for elapsed playback time.</summary>
        /// <param name="obj">Object whose <c>rotateSpeed</c> attribute drives preview rotation.</param>
        /// <param name="elapsedSeconds">Elapsed preview time in seconds.</param>
        /// <returns>Signed clockwise-positive degrees to add to the object's authored rotation.</returns>
        public static double PreviewDegrees(LevelObject obj, double elapsedSeconds)
        {
            return RawSpeed(obj) * elapsedSeconds;
        }

        /// <summary>Computes the live-preview position for DX circular orbit paths.</summary>
        /// <param name="obj">Object whose <c>RC</c>/<c>RW</c> path and <c>moveSpeed</c> are inspected.</param>
        /// <param name="elapsedSeconds">Elapsed preview time in seconds.</param>
        /// <returns>The preview position, or the authored position when no active orbit exists.</returns>
        public static Vec2 PreviewPosition(LevelObject obj, double elapsedSeconds)
        {
            Vec2 authored = new(obj.X, obj.Y);
            string? path = obj.GetAttr("path");
            int moveSpeed = RawMoveSpeed(obj);
            if (!IsCircularPath(path) || moveSpeed <= 0)
            {
                return authored;
            }

            int radius = OrbitRadius(obj);
            int pointsCount = radius / 2;
            if (pointsCount <= 0)
            {
                return authored;
            }

            Vec2[] points = BuildCircularPath(authored, radius, path![1] == 'C', pointsCount);
            if (points.Length == 1 || elapsedSeconds <= 0)
            {
                return points[0];
            }

            double remaining = moveSpeed * elapsedSeconds;
            int index = 0;
            while (remaining > 0)
            {
                Vec2 start = points[index];
                Vec2 end = points[(index + 1) % points.Length];
                double dx = end.X - start.X;
                double dy = end.Y - start.Y;
                double distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance <= 0)
                {
                    index = (index + 1) % points.Length;
                    continue;
                }

                if (remaining <= distance)
                {
                    double t = remaining / distance;
                    return new Vec2(start.X + (dx * t), start.Y + (dy * t));
                }

                remaining -= distance;
                index = (index + 1) % points.Length;
            }

            return points[index];
        }

        private static int RawSpeed(LevelObject obj)
        {
            return double.TryParse(obj.GetAttr("rotateSpeed"), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? (int)value
                : 0;
        }

        private static int RawMoveSpeed(LevelObject obj)
        {
            return double.TryParse(obj.GetAttr("moveSpeed"), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? Math.Abs((int)value)
                : 0;
        }

        private static Vec2[] BuildCircularPath(Vec2 start, int radius, bool clockwise, int pointsCount)
        {
            Vec2[] points = new Vec2[pointsCount];
            double angleStep = Math.Tau / pointsCount;
            if (!clockwise)
            {
                angleStep = -angleStep;
            }

            double theta = 0.0;
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vec2(
                    start.X + (radius * Math.Cos(theta)),
                    start.Y + (radius * Math.Sin(theta)));
                theta += angleStep;
            }

            return points;
        }

        private static bool IsCircularPath(string? path)
        {
            return path is { Length: > 2 }
                && path[0] == 'R'
                && (path[1] == 'C' || path[1] == 'W')
                && int.TryParse(path[2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int radius)
                && radius > 0;
        }
    }
}
