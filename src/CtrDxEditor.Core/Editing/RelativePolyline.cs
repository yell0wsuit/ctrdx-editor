using System;
using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Parses and edits polylines whose stored vertices are offsets from an absolute anchor.</summary>
    public static class RelativePolyline
    {
        /// <summary>Maximum offset vertices supported by the game path representation.</summary>
        public const int MaxStoredOffsetPoints = 99;

        /// <summary>Returns the anchor followed by every valid, complete relative coordinate pair.</summary>
        public static Vec2[] Points(Vec2 anchor, string? path)
        {
            List<Vec2> points = [anchor];
            if (string.IsNullOrWhiteSpace(path))
            {
                return [.. points];
            }

            string[] parts = path.Split(',');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double x)
                    || !double.TryParse(parts[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                {
                    continue;
                }

                points.Add(new Vec2(anchor.X + x, anchor.Y + y));
            }

            return [.. points];
        }

        /// <summary>Serializes absolute vertices after the anchor as invariant relative coordinate pairs.</summary>
        public static string Serialize(
            Vec2 anchor,
            IReadOnlyList<Vec2> absolutePoints,
            int maxStoredPoints = MaxStoredOffsetPoints)
        {
            if (absolutePoints.Count <= 1 || maxStoredPoints <= 0)
            {
                return string.Empty;
            }

            int storedPointCount = Math.Min(absolutePoints.Count - 1, maxStoredPoints);
            List<string> values = [with(storedPointCount * 2)];
            for (int i = 1; i <= storedPointCount; i++)
            {
                Vec2 offset = absolutePoints[i] - anchor;
                values.Add(Format(offset.X));
                values.Add(Format(offset.Y));
            }

            return string.Join(",", values);
        }

        /// <summary>Returns the first editable vertex within tolerance of a point, or -1.</summary>
        public static int HitPoint(
            Vec2 anchor,
            string? path,
            Vec2 point,
            double tolerance,
            int firstEditableIndex = 1)
        {
            Vec2[] points = Points(anchor, path);
            double toleranceSquared = tolerance * tolerance;
            for (int i = Math.Max(0, firstEditableIndex); i < points.Length; i++)
            {
                double dx = points[i].X - point.X;
                double dy = points[i].Y - point.Y;
                if ((dx * dx) + (dy * dy) <= toleranceSquared)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Returns whether another stored offset can be added without truncation.</summary>
        public static bool CanAddPoint(
            Vec2 anchor,
            string? path,
            int maxStoredPoints = MaxStoredOffsetPoints)
        {
            return Points(anchor, path).Length - 1 < Math.Max(0, maxStoredPoints);
        }

        /// <summary>Moves an existing non-anchor vertex and normalizes the path.</summary>
        public static string MovePoint(Vec2 anchor, string? path, int index, Vec2 newPoint)
        {
            Vec2[] points = Points(anchor, path);
            if (index <= 0 || index >= points.Length)
            {
                return path ?? string.Empty;
            }

            points[index] = newPoint;
            return Serialize(anchor, points);
        }

        /// <summary>Inserts a vertex after an existing segment's starting vertex.</summary>
        public static string InsertPoint(Vec2 anchor, string? path, int segmentIndex, Vec2 newPoint)
        {
            Vec2[] points = Points(anchor, path);
            if (segmentIndex < 0 || segmentIndex >= points.Length - 1 || !CanAddPoint(anchor, path))
            {
                return path ?? string.Empty;
            }

            List<Vec2> edited = [.. points];
            edited.Insert(segmentIndex + 1, newPoint);
            return Serialize(anchor, edited);
        }

        /// <summary>Appends a vertex when the stored point limit allows it.</summary>
        public static string AppendPoint(Vec2 anchor, string? path, Vec2 newPoint)
        {
            if (!CanAddPoint(anchor, path))
            {
                return path ?? string.Empty;
            }

            List<Vec2> edited = [.. Points(anchor, path), newPoint];
            return Serialize(anchor, edited);
        }

        /// <summary>Deletes a non-anchor vertex without crossing the requested minimum total point count.</summary>
        public static string DeletePoint(
            Vec2 anchor,
            string? path,
            int index,
            int minimumPointCount = 1)
        {
            Vec2[] points = Points(anchor, path);
            if (index <= 0 || index >= points.Length || points.Length <= Math.Max(1, minimumPointCount))
            {
                return path ?? string.Empty;
            }

            List<Vec2> edited = [.. points];
            edited.RemoveAt(index);
            return Serialize(anchor, edited);
        }

        private static string Format(double value)
        {
            return value == 0
                ? "0"
                : value == Math.Truncate(value)
                ? value.ToString("0", CultureInfo.InvariantCulture)
                : value.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}
