using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Parses, previews, and serializes DX mover path strings.</summary>
    public static class MoverPath
    {
        /// <summary>DX allocates 100 points for non-R paths and prepends the authored start point.</summary>
        public const int MaxStoredPlainOffsetPoints = 99;

        /// <summary>Returns true when the object has a path with positive movement speed.</summary>
        public static bool HasActiveMovement(LevelObject obj)
        {
            return RawMoveSpeed(obj) > 0
                && Points(new Vec2(obj.X, obj.Y), obj.GetAttr("path")).Length > 1;
        }

        /// <summary>Computes absolute path points for a DX mover path.</summary>
        public static Vec2[] Points(Vec2 start, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return [start];
            }

            path = path.Trim();
            return IsCircularPath(path) ? CircularPoints(start, path) : PlainPoints(start, path);
        }

        /// <summary>Returns the object point plus editable canonical waypoints; retrace paths expose their outbound half.</summary>
        public static Vec2[] CanonicalPoints(Vec2 start, string? path)
        {
            Vec2[] full = Points(start, path);
            if (!IsRetrace(path))
            {
                return full;
            }

            int storedPointCount = full.Length - 1;
            int outboundPointCount = (storedPointCount + 1) / 2;
            return full[..(outboundPointCount + 1)];
        }

        /// <summary>Computes the live-preview position for a DX mover path.</summary>
        public static Vec2 PreviewPosition(Vec2 start, string? path, int moveSpeed, double elapsedSeconds)
        {
            if (moveSpeed <= 0)
            {
                return start;
            }

            Vec2[] points = Points(start, path);
            if (points.Length == 0)
            {
                return start;
            }

            if (points.Length == 1 || elapsedSeconds <= 0)
            {
                return points[0];
            }

            double remaining = moveSpeed * elapsedSeconds;
            int index = 0;
            while (remaining > 0)
            {
                Vec2 point = points[index];
                Vec2 target = points[(index + 1) % points.Length];
                double dx = target.X - point.X;
                double dy = target.Y - point.Y;
                double distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance <= 0)
                {
                    index = (index + 1) % points.Length;
                    continue;
                }

                if (remaining <= distance)
                {
                    double t = remaining / distance;
                    return new Vec2(point.X + (dx * t), point.Y + (dy * t));
                }

                remaining -= distance;
                index = (index + 1) % points.Length;
            }

            return points[index];
        }

        /// <summary>Computes the live-preview position for a level object's mover data.</summary>
        public static Vec2 PreviewPosition(LevelObject obj, double elapsedSeconds)
        {
            return PreviewPosition(new Vec2(obj.X, obj.Y), obj.GetAttr("path"), RawMoveSpeed(obj), elapsedSeconds);
        }

        /// <summary>Serializes canonical absolute points to a plain DX offset path; retrace appends the reversed interior.</summary>
        public static string Serialize(Vec2 start, IReadOnlyList<Vec2> canonicalAbsolutePoints, bool retrace)
        {
            if (canonicalAbsolutePoints.Count <= 1)
            {
                return string.Empty;
            }

            int canonicalPointCount = Math.Min(canonicalAbsolutePoints.Count, MaxCanonicalPointCount(retrace));
            List<Vec2> stored = [.. canonicalAbsolutePoints.Skip(1).Take(canonicalPointCount - 1)];
            if (retrace && canonicalPointCount > 2)
            {
                for (int i = canonicalPointCount - 2; i >= 1; i--)
                {
                    stored.Add(canonicalAbsolutePoints[i]);
                }
            }

            return string.Join(",", stored.SelectMany(p =>
            {
                Vec2 offset = new(p.X - start.X, p.Y - start.Y);
                return new[] { Format(offset.X), Format(offset.Y) };
            }));
        }

        /// <summary>Compatibility wrapper for serializing a looping or out-and-back plain path.</summary>
        public static string SerializePlain(Vec2 start, IReadOnlyList<Vec2> absolutePoints, bool loop)
        {
            return Serialize(start, absolutePoints, retrace: !loop);
        }

        /// <summary>Returns true when a plain path stores an odd palindrome of out-and-back offsets.</summary>
        public static bool IsRetrace(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || IsCircularPath(path))
            {
                return false;
            }

            Vec2[] points = Points(new Vec2(0, 0), path);
            int storedPointCount = points.Length - 1;
            if (storedPointCount < 3 || storedPointCount % 2 == 0)
            {
                return false;
            }

            for (int i = 1; i <= storedPointCount / 2; i++)
            {
                if (points[i] != points[^i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Returns the canonical waypoint index (>= 1) under the point, or -1.</summary>
        public static int HitCanonicalPoint(Vec2 start, string? path, Vec2 point, double tolerance)
        {
            if (string.IsNullOrWhiteSpace(path) || IsCircularPath(path))
            {
                return -1;
            }

            Vec2[] pts = CanonicalPoints(start, path);
            double toleranceSquared = tolerance * tolerance;
            for (int i = 1; i < pts.Length; i++)
            {
                double dx = pts[i].X - point.X;
                double dy = pts[i].Y - point.Y;
                if ((dx * dx) + (dy * dy) <= toleranceSquared)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Whether one more canonical waypoint can fit without truncating the serialized DX path.</summary>
        public static bool CanAddCanonicalPoint(Vec2 start, string? path)
        {
            return !IsCircularPath(path)
                && CanonicalPoints(start, path).Length < MaxCanonicalPointCount(IsRetrace(path));
        }

        /// <summary>Moves one canonical waypoint and re-serializes, preserving retrace/circuit state.</summary>
        public static string MoveCanonicalPoint(Vec2 start, string? path, int index, Vec2 newPoint)
        {
            Vec2[] pts = CanonicalPoints(start, path);
            if (index <= 0 || index >= pts.Length || IsCircularPath(path))
            {
                return path ?? string.Empty;
            }

            pts[index] = newPoint;
            return Serialize(start, pts, IsRetrace(path));
        }

        /// <summary>Inserts a canonical waypoint after <paramref name="segmentIndex"/> and re-serializes.</summary>
        public static string InsertCanonicalPoint(Vec2 start, string? path, int segmentIndex, Vec2 newPoint)
        {
            Vec2[] pts = CanonicalPoints(start, path);
            if (segmentIndex < 0 || segmentIndex >= pts.Length - 1 || IsCircularPath(path))
            {
                return path ?? string.Empty;
            }
            if (!CanAddCanonicalPoint(start, path))
            {
                return path ?? string.Empty;
            }

            List<Vec2> list = [.. pts];
            list.Insert(segmentIndex + 1, newPoint);
            return Serialize(start, list, IsRetrace(path));
        }

        /// <summary>Appends a canonical waypoint, preserving retrace/circuit state.</summary>
        public static string AppendCanonicalPoint(Vec2 start, string? path, Vec2 newPoint)
        {
            if (IsCircularPath(path) || !CanAddCanonicalPoint(start, path))
            {
                return path ?? string.Empty;
            }

            Vec2[] pts = CanonicalPoints(start, path);
            List<Vec2> list = [.. pts];
            list.Add(newPoint);
            return Serialize(start, list, IsRetrace(path));
        }

        /// <summary>Removes a single canonical waypoint (index >= 1) and re-serializes, preserving retrace/circuit state.</summary>
        public static string DeleteCanonicalPoint(Vec2 start, string? path, int index)
        {
            Vec2[] pts = CanonicalPoints(start, path);
            if (index <= 0 || index >= pts.Length || IsCircularPath(path))
            {
                return path ?? string.Empty;
            }

            List<Vec2> list = [.. pts];
            list.RemoveAt(index);
            return Serialize(start, list, IsRetrace(path));
        }

        /// <summary>Re-serializes the canonical points as an out-and-back retrace or a plain circuit.</summary>
        public static string SetRetrace(Vec2 start, string? path, bool retrace)
        {
            if (string.IsNullOrWhiteSpace(path) || IsCircularPath(path))
            {
                return path ?? string.Empty;
            }

            Vec2[] points = CanonicalPoints(start, path);
            return retrace && points.Length > MaxCanonicalPointCount(retrace: true)
                ? path
                : Serialize(start, points, retrace);
        }

        private static int MaxCanonicalPointCount(bool retrace)
        {
            return retrace
                ? (MaxStoredPlainOffsetPoints + 3) / 2
                : MaxStoredPlainOffsetPoints + 1;
        }

        /// <summary>Returns true when <paramref name="path"/> is DX circular movement syntax.</summary>
        public static bool IsCircularPath(string? path)
        {
            return path is { Length: > 2 }
                && path[0] == 'R'
                && (path[1] == 'C' || path[1] == 'W')
                && int.TryParse(path[2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int radius)
                && radius > 0;
        }

        /// <summary>Returns true when the circular path traverses clockwise.</summary>
        public static bool IsCircularClockwise(string? path)
        {
            return !IsCircularPath(path) || path![1] == 'C';
        }

        /// <summary>Returns the circular path radius, or <paramref name="fallback"/> when not circular.</summary>
        public static int CircularRadius(string? path, int fallback)
        {
            return IsCircularPath(path) && int.TryParse(path![2..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int radius)
                ? radius
                : fallback;
        }

        private static Vec2[] CircularPoints(Vec2 start, string path)
        {
            int radius = CircularRadius(path, fallback: 0);
            int pointCount = radius / 2;
            if (pointCount <= 0)
            {
                return [start];
            }

            Vec2[] points = new Vec2[pointCount];
            double angleStep = Math.Tau / pointCount;
            if (!IsCircularClockwise(path))
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

        private static Vec2[] PlainPoints(Vec2 start, string path)
        {
            if (path[^1] == ',')
            {
                path = path[..^1];
            }

            string[] parts = path.Split(',');
            if (parts.Length % 2 != 0)
            {
                return [start];
            }

            List<Vec2> points = [start];
            for (int i = 0; i < parts.Length; i += 2)
            {
                double x = ParseDoubleOrZero(parts[i]);
                double y = ParseDoubleOrZero(parts[i + 1]);
                points.Add(new Vec2(start.X + x, start.Y + y));
            }

            return [.. points];
        }

        private static int RawMoveSpeed(LevelObject obj)
        {
            return double.TryParse(obj.GetAttr("moveSpeed"), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? Math.Abs((int)value)
                : 0;
        }

        private static double ParseDoubleOrZero(string value)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                ? parsed
                : 0.0;
        }

        private static string Format(double value)
        {
            return Math.Abs(value) < 0.0000001
                ? "0"
                : value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
