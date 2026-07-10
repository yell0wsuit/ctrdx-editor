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

        /// <summary>Serializes absolute points to a plain DX offset path, optionally mirroring back home.</summary>
        public static string SerializePlain(Vec2 start, IReadOnlyList<Vec2> absolutePoints, bool loop)
        {
            if (absolutePoints.Count <= 1)
            {
                return "0,0";
            }

            List<Vec2> stored = [.. absolutePoints.Skip(1)];
            if (!loop && absolutePoints.Count > 2)
            {
                for (int i = absolutePoints.Count - 2; i >= 1; i--)
                {
                    stored.Add(absolutePoints[i]);
                }
            }

            if (stored.Count > MaxStoredPlainOffsetPoints)
            {
                stored.RemoveRange(MaxStoredPlainOffsetPoints, stored.Count - MaxStoredPlainOffsetPoints);
            }

            return string.Join(",", stored.SelectMany(p =>
            {
                Vec2 offset = new(p.X - start.X, p.Y - start.Y);
                return new[] { Format(offset.X), Format(offset.Y) };
            }));
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
