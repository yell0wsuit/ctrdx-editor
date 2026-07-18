using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Provides forward-only open and explicitly closed ant-conveyor path semantics.</summary>
    public static class AntPath
    {
        /// <summary>XML element name used by ant conveyors.</summary>
        public const string Element = "ants";

        /// <summary>Visible two-point path used for newly placed ant conveyors.</summary>
        public const string DefaultPath = "120,0";

        /// <summary>Default ant movement speed in level units per second.</summary>
        public const string DefaultMoveSpeed = "100";

        /// <summary>Returns whether an object type is an ant conveyor.</summary>
        public static bool IsAnts(string type)
        {
            return string.Equals(type, Element, StringComparison.Ordinal);
        }

        /// <summary>Returns the anchor and unique editable vertices, excluding a terminal closure marker.</summary>
        public static Vec2[] Points(LevelObject ants)
        {
            Vec2 anchor = Anchor(ants);
            Vec2[] points = RelativePolyline.Points(anchor, ants.GetAttr("path"));
            return IsClosedPoints(points) ? points[..^1] : points;
        }

        /// <summary>Returns whether the last stored vertex explicitly returns to the anchor.</summary>
        public static bool IsClosed(string? path)
        {
            return IsClosedPoints(RelativePolyline.Points(new Vec2(0, 0), path));
        }

        /// <summary>Adds or removes the terminal anchor that represents semantic closure.</summary>
        public static void SetClosed(LevelObject ants, bool closed)
        {
            bool wasClosed = IsClosed(ants.GetAttr("path"));
            if (closed == wasClosed)
            {
                return;
            }

            Vec2 anchor = Anchor(ants);
            List<Vec2> points = [.. RelativePolyline.Points(anchor, ants.GetAttr("path"))];
            if (closed)
            {
                points.Add(anchor);
            }
            else
            {
                points.RemoveAt(points.Count - 1);
            }

            ants.SetAttr("path", RelativePolyline.Serialize(anchor, points));
        }

        /// <summary>Returns whether the semantic closure control can change without exceeding path capacity.</summary>
        public static bool CanSetClosed(LevelObject ants)
        {
            return IsClosed(ants.GetAttr("path")) || CanAddPoint(ants);
        }

        /// <summary>Returns whether another unique vertex can fit while retaining explicit closure.</summary>
        public static bool CanAddPoint(LevelObject ants)
        {
            Vec2 anchor = Anchor(ants);
            return RelativePolyline.CanAddPoint(anchor, ants.GetAttr("path"));
        }

        /// <summary>Returns the editable unique vertex under a point, or -1.</summary>
        public static int HitPoint(LevelObject ants, Vec2 point, double tolerance)
        {
            Vec2 anchor = Anchor(ants);
            return RelativePolyline.HitPoint(
                anchor,
                RelativePolyline.Serialize(anchor, Points(ants)),
                point,
                tolerance);
        }

        /// <summary>Moves one unique non-anchor vertex while preserving closure.</summary>
        public static void MovePoint(LevelObject ants, int index, Vec2 point)
        {
            Vec2 anchor = Anchor(ants);
            Vec2[] points = Points(ants);
            if (index <= 0 || index >= points.Length)
            {
                return;
            }

            points[index] = point;
            Write(ants, anchor, points, IsClosed(ants.GetAttr("path")));
        }

        /// <summary>Inserts a vertex after a segment start, including the final segment of a closed loop.</summary>
        public static void InsertPoint(LevelObject ants, int segmentIndex, Vec2 point)
        {
            if (!CanAddPoint(ants))
            {
                return;
            }

            Vec2 anchor = Anchor(ants);
            List<Vec2> points = [.. Points(ants)];
            bool closed = IsClosed(ants.GetAttr("path"));
            int segmentCount = closed ? points.Count : points.Count - 1;
            if (segmentIndex < 0 || segmentIndex >= segmentCount)
            {
                return;
            }

            points.Insert(segmentIndex + 1, point);
            Write(ants, anchor, points, closed);
        }

        /// <summary>Appends a unique endpoint immediately before any terminal closure marker.</summary>
        public static void AppendPoint(LevelObject ants, Vec2 point)
        {
            if (!CanAddPoint(ants))
            {
                return;
            }

            Vec2 anchor = Anchor(ants);
            List<Vec2> points = [.. Points(ants), point];
            Write(ants, anchor, points, IsClosed(ants.GetAttr("path")));
        }

        /// <summary>Deletes a unique vertex while retaining at least one non-anchor endpoint.</summary>
        public static void DeletePoint(LevelObject ants, int index)
        {
            Vec2 anchor = Anchor(ants);
            List<Vec2> points = [.. Points(ants)];
            if (index <= 0 || index >= points.Count || points.Count <= 2)
            {
                return;
            }

            bool closed = IsClosed(ants.GetAttr("path"));
            points.RemoveAt(index);
            Write(ants, anchor, points, closed);
        }

        /// <summary>Returns axis-aligned bounds around every unique vertex with artwork padding.</summary>
        public static LevelBounds Bounds(LevelObject ants, double padding = 16)
        {
            return Bounds(Points(ants), padding);
        }

        internal static LevelBounds Bounds(IReadOnlyList<Vec2> points, double padding)
        {
            double minX = points[0].X;
            double minY = points[0].Y;
            double maxX = minX;
            double maxY = minY;
            for (int i = 1; i < points.Count; i++)
            {
                minX = Math.Min(minX, points[i].X);
                minY = Math.Min(minY, points[i].Y);
                maxX = Math.Max(maxX, points[i].X);
                maxY = Math.Max(maxY, points[i].Y);
            }

            return new LevelBounds(
                minX - padding,
                minY - padding,
                maxX - minX + (padding * 2),
                maxY - minY + (padding * 2));
        }

        private static Vec2 Anchor(LevelObject ants)
        {
            return new Vec2(ants.X, ants.Y);
        }

        private static bool IsClosedPoints(Vec2[] points)
        {
            return points.Length > 1 && points[^1] == points[0];
        }

        private static void Write(LevelObject ants, Vec2 anchor, IReadOnlyList<Vec2> uniquePoints, bool closed)
        {
            List<Vec2> stored = [.. uniquePoints];
            if (closed)
            {
                stored.Add(anchor);
            }

            ants.SetAttr("path", RelativePolyline.Serialize(anchor, stored));
        }
    }
}
