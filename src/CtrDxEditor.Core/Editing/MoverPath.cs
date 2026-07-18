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
        public const int MaxStoredPlainOffsetPoints = RelativePolyline.MaxStoredOffsetPoints;

        /// <summary>Returns true when the object has a path with positive movement speed.</summary>
        public static bool HasActiveMovement(LevelObject obj)
        {
            return RawMoveSpeed(obj) > 0
                && Points(new Vec2(obj.X, obj.Y), obj.GetAttr("path")).Length > 1;
        }

        /// <summary>
        /// True when a plain (non-circular) path actually translates the object — at least one stored point
        /// differs from the start. The spin-carrier path (<c>"0,0"</c>) that hosts <c>rotateSpeed</c> does not, so
        /// movement-mode detection can tell a real polyline apart from a bare spinner.
        /// </summary>
        public static bool IsPolylineMovement(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || IsCircularPath(path))
            {
                return false;
            }

            Vec2 origin = new(0, 0);
            Vec2[] points = Points(origin, path);
            for (int i = 1; i < points.Length; i++)
            {
                if (points[i] != origin)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Computes absolute path points for a DX mover path.</summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <returns>Absolute points, never empty: a blank path yields <paramref name="start"/> alone.</returns>
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
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <returns>
        /// The editable waypoints. For a retrace path this is the outbound half only, since the return leg is
        /// implied and must not be presented as separately editable.
        /// </returns>
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
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <param name="moveSpeed">Units per second; zero or negative parks the object at <paramref name="start"/>.</param>
        /// <param name="elapsedSeconds">Seconds since the preview began. The path loops, so this is not clamped.</param>
        /// <returns>The interpolated position along the looping path.</returns>
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
            // A spin-carrier path ("0,0"), or any run of coincident
            // points, advances the target without moving. Count
            // consecutive zero-length steps and stop after
            // one full lap so the object stays put (only rotateSpeed spins it) instead of looping forever.
            int noProgressSteps = 0;
            int maxNoProgressSteps = points.Length + 1;
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
                    if (++noProgressSteps > maxNoProgressSteps)
                    {
                        break;
                    }
                    continue;
                }

                noProgressSteps = 0;
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
        /// <param name="obj">The mover, supplying its own position, <c>path</c>, and speed.</param>
        /// <param name="elapsedSeconds">Seconds since the preview began. The path loops, so this is not clamped.</param>
        /// <returns>The interpolated position along the looping path.</returns>
        public static Vec2 PreviewPosition(LevelObject obj, double elapsedSeconds)
        {
            return PreviewPosition(new Vec2(obj.X, obj.Y), obj.GetAttr("path"), RawMoveSpeed(obj), elapsedSeconds);
        }

        /// <summary>Serializes canonical absolute points to a plain DX offset path; retrace appends the reversed interior.</summary>
        /// <param name="start">The object's absolute position, subtracted from each point to produce offsets.</param>
        /// <param name="canonicalAbsolutePoints">Waypoints including <paramref name="start"/> at index 0. Excess points beyond the DX limit are dropped.</param>
        /// <param name="retrace">When true the interior is mirrored to walk the path back; when false it loops as a circuit.</param>
        /// <returns>The serialized path, or an empty string when there are too few points to move along.</returns>
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

            return RelativePolyline.Serialize(start, [start, .. stored]);
        }

        /// <summary>Compatibility wrapper for serializing a looping or out-and-back plain path.</summary>
        /// <param name="start">The object's absolute position, subtracted from each point to produce offsets.</param>
        /// <param name="absolutePoints">Waypoints including <paramref name="start"/> at index 0.</param>
        /// <param name="loop">True to circuit back to the start; false to retrace the path in reverse.</param>
        /// <returns>The serialized path, or an empty string when there are too few points to move along.</returns>
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

        /// <summary>
        /// True when out-and-back retrace produces a different path from a plain circuit — i.e. the path has at
        /// least two waypoints. A single straight segment is inherently back-and-forth, so retrace is a no-op.
        /// </summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        public static bool CanRetrace(Vec2 start, string? path)
        {
            return !IsCircularPath(path) && CanonicalPoints(start, path).Length >= 3;
        }

        /// <summary>Returns the canonical waypoint index (>= 1) under the point, or -1.</summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <param name="point">The absolute position to test, typically the cursor.</param>
        /// <param name="tolerance">The hit radius in the same units as <paramref name="point"/>.</param>
        /// <returns>
        /// The waypoint index, always 1 or greater since index 0 is the object itself and is not separately
        /// draggable, or -1 when nothing is under <paramref name="point"/>.
        /// </returns>
        public static int HitCanonicalPoint(Vec2 start, string? path, Vec2 point, double tolerance)
        {
            return string.IsNullOrWhiteSpace(path) || IsCircularPath(path)
                ? -1
                : RelativePolyline.HitPoint(start, CanonicalPath(start, path), point, tolerance);
        }

        /// <summary>Whether one more canonical waypoint can fit without truncating the serialized DX path.</summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        public static bool CanAddCanonicalPoint(Vec2 start, string? path)
        {
            return !IsCircularPath(path)
                && RelativePolyline.CanAddPoint(
                    start,
                    CanonicalPath(start, path),
                    MaxCanonicalPointCount(IsRetrace(path)) - 1);
        }

        /// <summary>Moves one canonical waypoint and re-serializes, preserving retrace/circuit state.</summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <param name="index">The waypoint to move, 1-based as returned by <see cref="HitCanonicalPoint"/>.</param>
        /// <param name="newPoint">The waypoint's new absolute position.</param>
        /// <returns>The re-serialized path, or <paramref name="path"/> unchanged when the move does not apply.</returns>
        public static string MoveCanonicalPoint(Vec2 start, string? path, int index, Vec2 newPoint)
        {
            if (IsCircularPath(path))
            {
                return path ?? string.Empty;
            }

            bool retrace = IsRetrace(path);
            string canonicalPath = CanonicalPath(start, path);
            string edited = RelativePolyline.MovePoint(start, canonicalPath, index, newPoint);
            return edited == canonicalPath
                ? path ?? string.Empty
                : Serialize(start, RelativePolyline.Points(start, edited), retrace);
        }

        /// <summary>Inserts a canonical waypoint after <paramref name="segmentIndex"/> and re-serializes.</summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <param name="segmentIndex">The 0-based segment to insert after; the new point lands at <c>segmentIndex + 1</c>.</param>
        /// <param name="newPoint">The new waypoint's absolute position.</param>
        /// <returns>The re-serialized path, or <paramref name="path"/> unchanged when the edit does not apply.</returns>
        public static string InsertCanonicalPoint(Vec2 start, string? path, int segmentIndex, Vec2 newPoint)
        {
            if (IsCircularPath(path))
            {
                return path ?? string.Empty;
            }
            if (!CanAddCanonicalPoint(start, path))
            {
                return path ?? string.Empty;
            }

            bool retrace = IsRetrace(path);
            string canonicalPath = CanonicalPath(start, path);
            string edited = RelativePolyline.InsertPoint(start, canonicalPath, segmentIndex, newPoint);
            return edited == canonicalPath
                ? path ?? string.Empty
                : Serialize(start, RelativePolyline.Points(start, edited), retrace);
        }

        /// <summary>Appends a canonical waypoint, preserving retrace/circuit state.</summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <param name="newPoint">The new waypoint's absolute position.</param>
        /// <returns>The re-serialized path, or <paramref name="path"/> unchanged when the edit does not apply.</returns>
        public static string AppendCanonicalPoint(Vec2 start, string? path, Vec2 newPoint)
        {
            if (IsCircularPath(path) || !CanAddCanonicalPoint(start, path))
            {
                return path ?? string.Empty;
            }

            bool retrace = IsRetrace(path);
            string edited = RelativePolyline.AppendPoint(start, CanonicalPath(start, path), newPoint);
            return Serialize(start, RelativePolyline.Points(start, edited), retrace);
        }

        /// <summary>Removes a single canonical waypoint (index >= 1) and re-serializes, preserving retrace/circuit state.</summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <param name="index">The waypoint to remove, 1-based; index 0 is the object itself and cannot be deleted.</param>
        /// <returns>The re-serialized path, or <paramref name="path"/> unchanged when the edit does not apply.</returns>
        public static string DeleteCanonicalPoint(Vec2 start, string? path, int index)
        {
            if (IsCircularPath(path))
            {
                return path ?? string.Empty;
            }

            bool retrace = IsRetrace(path);
            string canonicalPath = CanonicalPath(start, path);
            string edited = RelativePolyline.DeletePoint(start, canonicalPath, index);
            return edited == canonicalPath
                ? path ?? string.Empty
                : Serialize(start, RelativePolyline.Points(start, edited), retrace);
        }

        /// <summary>Re-serializes the canonical points as an out-and-back retrace or a plain circuit.</summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <param name="retrace">True to walk the path back; false to close it into a circuit.</param>
        /// <returns>The re-serialized path, or <paramref name="path"/> unchanged when retracing would overflow the DX point limit.</returns>
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

        /// <summary>
        /// Returns true when a plain path forms a closed loop with a defined winding: not circular, not an
        /// out-and-back retrace, and at least three non-collinear canonical points (so it encloses an area).
        /// Travel direction is only meaningful for such loops.
        /// </summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        public static bool IsClosedLoop(Vec2 start, string? path)
        {
            if (IsCircularPath(path) || IsRetrace(path))
            {
                return false;
            }

            Vec2[] points = CanonicalPoints(start, path);
            return points.Length >= 3 && Math.Abs(SignedArea(points)) > 0.0001;
        }

        /// <summary>Returns true when the closed loop winds clockwise on screen (level Y is screen-down).</summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        public static bool IsClockwise(Vec2 start, string? path)
        {
            return IsClosedLoop(start, path) && SignedArea(CanonicalPoints(start, path)) > 0;
        }

        /// <summary>
        /// Reverses the loop's traversal direction (the stored point order) when needed so it winds the
        /// requested way, and re-serializes. The game has no direction flag, so direction is the point order.
        /// </summary>
        /// <param name="start">The object's absolute position, which becomes the first point.</param>
        /// <param name="path">A DX path string of offsets from <paramref name="start"/>; null or blank means no path.</param>
        /// <param name="clockwise">The desired on-screen winding; level Y runs screen-down.</param>
        /// <returns>The re-serialized path, or <paramref name="path"/> unchanged when it is not a closed loop or already winds that way.</returns>
        public static string SetClockwise(Vec2 start, string? path, bool clockwise)
        {
            if (!IsClosedLoop(start, path) || IsClockwise(start, path) == clockwise)
            {
                return path ?? string.Empty;
            }

            Vec2[] points = CanonicalPoints(start, path);
            List<Vec2> reversed = [points[0]];
            for (int i = points.Length - 1; i >= 1; i--)
            {
                reversed.Add(points[i]);
            }

            return Serialize(start, reversed, retrace: false);
        }

        /// <summary>
        /// Winding used for the direction checkbox's displayed value: the signed area of the canonical points,
        /// computed whether or not the path is currently a loop or an out-and-back. Because the canonical order
        /// is preserved across a retrace toggle, the shown check state stays stable instead of flipping. A
        /// degenerate (collinear) path defaults to clockwise.
        /// </summary>
        public static bool IsCanonicalClockwise(Vec2 start, string? path)
        {
            return IsCircularPath(path) || SignedArea(CanonicalPoints(start, path)) >= 0;
        }

        /// <summary>Signed polygon area (shoelace) of a closed loop; positive is clockwise in screen space.</summary>
        private static double SignedArea(Vec2[] points)
        {
            double sum = 0.0;
            for (int i = 0; i < points.Length; i++)
            {
                Vec2 a = points[i];
                Vec2 b = points[(i + 1) % points.Length];
                sum += (a.X * b.Y) - (b.X * a.Y);
            }

            return sum / 2.0;
        }

        private static int MaxCanonicalPointCount(bool retrace)
        {
            return retrace
                ? (MaxStoredPlainOffsetPoints + 3) / 2
                : MaxStoredPlainOffsetPoints + 1;
        }

        private static string CanonicalPath(Vec2 start, string? path)
        {
            return RelativePolyline.Serialize(start, CanonicalPoints(start, path));
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
            return RelativePolyline.Points(start, path);
        }

        private static int RawMoveSpeed(LevelObject obj)
        {
            return double.TryParse(obj.GetAttr("moveSpeed"), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? Math.Abs((int)value)
                : 0;
        }

    }
}
