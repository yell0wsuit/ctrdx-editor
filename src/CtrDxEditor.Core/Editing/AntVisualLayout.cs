using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>One deterministic ant sprite placement along a conveyor path.</summary>
    /// <param name="PathOffset">Distance from the beginning of the path.</param>
    /// <param name="Position">Absolute position in level coordinates.</param>
    /// <param name="HeadingDeg">Blended path heading in degrees.</param>
    /// <param name="Scale">Final endpoint-fade and base-variant scale.</param>
    /// <param name="Opacity">Endpoint-fade opacity from zero to one.</param>
    /// <param name="Frame">Walk-animation atlas frame from zero to five.</param>
    /// <param name="BaseScale">Deterministic per-ant scale variant.</param>
    public sealed record AntVisual(
        double PathOffset,
        Vec2 Position,
        double HeadingDeg,
        double Scale,
        double Opacity,
        int Frame,
        double BaseScale);

    /// <summary>One entrance or exit hole placement for an open ant path.</summary>
    /// <param name="Position">Absolute endpoint position.</param>
    /// <param name="HeadingDeg">Heading of the adjacent path segment.</param>
    /// <param name="Start">True for the entrance hole; false for the exit.</param>
    public sealed record AntHoleVisual(Vec2 Position, double HeadingDeg, bool Start);

    /// <summary>Immutable, deterministic visual composition for an ant conveyor.</summary>
    /// <param name="Ants">Ant sprite placements in stable index order.</param>
    /// <param name="Holes">Open-path entrance and exit holes.</param>
    /// <param name="Bounds">Selection and artwork bounds.</param>
    /// <param name="Closed">Whether the path has an explicit terminal anchor.</param>
    public sealed record AntVisualLayout(
        IReadOnlyList<AntVisual> Ants,
        IReadOnlyList<AntHoleVisual> Holes,
        LevelBounds Bounds,
        bool Closed)
    {
        private const double AntSpacing = 35;
        private const double FadeDistance = 15;
        private const double StartHoleHalfWidth = 15.5;
        private const double BoundsPadding = 36;
        private const double WalkFrameSeconds = 0.05;
        private const int WalkFrameCount = 6;
        private static readonly double[] BaseScales = [0.9, 1.05, 0.95, 1.1, 1.0];

        /// <summary>Builds a static or elapsed-time ant layout from relative path data.</summary>
        public static AntVisualLayout Build(
            Vec2 anchor,
            string? path,
            double moveSpeed,
            double? elapsedSeconds)
        {
            Vec2[] points = RelativePolyline.Points(anchor, path);
            bool closed = AntPath.IsClosed(path);
            List<Segment> segments = BuildSegments(points);
            double pathLength = 0;
            foreach (Segment segment in segments)
            {
                pathLength += segment.Length;
            }

            LevelBounds bounds = AntPath.Bounds(points, BoundsPadding);
            if (segments.Count == 0 || pathLength <= 0)
            {
                return new AntVisualLayout([], [], bounds, closed);
            }

            LinkSegments(segments, closed);
            IReadOnlyList<AntHoleVisual> holes = closed
                ? []
                :
                [
                    new AntHoleVisual(
                        new Vec2(
                            segments[0].Start.X - (segments[0].DirectionX * StartHoleHalfWidth),
                            segments[0].Start.Y - (segments[0].DirectionY * StartHoleHalfWidth)),
                        segments[0].HeadingDeg,
                        Start: true),
                    new AntHoleVisual(segments[^1].End, segments[^1].HeadingDeg, Start: false),
                ];

            int antCount = (int)(pathLength / AntSpacing);
            List<AntVisual> ants = [with(antCount)];
            double elapsed = elapsedSeconds is double value && double.IsFinite(value) ? Math.Max(0, value) : 0;
            double speed = double.IsFinite(moveSpeed) ? moveSpeed : 0;
            int framePhase = elapsedSeconds.HasValue
                ? (int)(Math.Floor(Math.Max(0, elapsed) / WalkFrameSeconds) % WalkFrameCount)
                : 0;

            double travel = speed * elapsed;
            for (int i = 0; i < antCount; i++)
            {
                double offset = (i * AntSpacing) + (closed ? 0 : -StartHoleHalfWidth) + travel;
                if (closed)
                {
                    AddVisual(
                        ants,
                        segments,
                        pathLength,
                        elapsedSeconds.HasValue ? Wrap(offset, pathLength) : offset,
                        closed: true,
                        creationIdentity: i,
                        framePhase);
                }
                else if (speed <= 0 || offset < pathLength)
                {
                    AddVisual(
                        ants,
                        segments,
                        pathLength,
                        offset,
                        closed: false,
                        creationIdentity: i,
                        framePhase);
                }
            }

            if (!closed && elapsedSeconds.HasValue && speed > 0 && travel >= AntSpacing)
            {
                AddRespawnedAnts(ants, segments, pathLength, travel, antCount, framePhase);
            }

            return new AntVisualLayout(ants, holes, bounds, closed);
        }

        private static void AddRespawnedAnts(
            List<AntVisual> ants,
            IReadOnlyList<Segment> segments,
            double pathLength,
            double travel,
            int initialAntCount,
            int framePhase)
        {
            double lastSpawn = Math.Floor(travel / AntSpacing);
            double firstVisibleSpawn = Math.Max(
                1,
                Math.Floor((travel - StartHoleHalfWidth - pathLength) / AntSpacing) + 1);
            double spawnCount = lastSpawn - firstVisibleSpawn + 1;
            if (spawnCount <= 0)
            {
                return;
            }

            // At extreme elapsed values adjacent spawn indices lose double precision. Reconstruct the
            // same visible lattice from its phase; ordinary preview times retain game creation order below.
            if (spawnCount > (pathLength / AntSpacing) + 2 || (firstVisibleSpawn + 1) == firstVisibleSpawn)
            {
                double lowest = Wrap(travel, AntSpacing) - StartHoleHalfWidth;
                double highest = lowest + ((Math.Ceiling((pathLength - lowest) / AntSpacing) - 1) * AntSpacing);
                for (double offset = highest; offset >= lowest; offset -= AntSpacing)
                {
                    double creationIdentity = initialAntCount + firstVisibleSpawn - 1;
                    AddVisual(ants, segments, pathLength, offset, closed: false, creationIdentity, framePhase);
                    firstVisibleSpawn++;
                }
                return;
            }

            for (double spawn = firstVisibleSpawn; spawn <= lastSpawn; spawn++)
            {
                AddVisual(
                    ants,
                    segments,
                    pathLength,
                    travel - (spawn * AntSpacing) - StartHoleHalfWidth,
                    closed: false,
                    creationIdentity: initialAntCount + spawn - 1,
                    framePhase);
            }
        }

        private static void AddVisual(
            List<AntVisual> ants,
            IReadOnlyList<Segment> segments,
            double pathLength,
            double offset,
            bool closed,
            double creationIdentity,
            int framePhase)
        {
            Vec2 position = PositionForOffset(segments, pathLength, offset);
            double heading = HeadingForOffset(segments, pathLength, offset);
            double opacity = closed ? 1 : EndpointFade(offset, pathLength, StartHoleHalfWidth);
            double edgeScale = closed ? 1 : (opacity * 0.8) + 0.2;
            int variantIdentity = (int)Wrap(creationIdentity, 30);
            double baseScale = BaseScales[variantIdentity % BaseScales.Length];
            ants.Add(new AntVisual(
                offset,
                position,
                heading,
                edgeScale * baseScale,
                opacity,
                (variantIdentity + framePhase) % WalkFrameCount,
                baseScale));
        }

        private static List<Segment> BuildSegments(Vec2[] points)
        {
            List<Segment> segments = [];
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Vec2 start = points[i];
                Vec2 end = points[i + 1];
                double dx = end.X - start.X;
                double dy = end.Y - start.Y;
                double length = Math.Sqrt((dx * dx) + (dy * dy));
                if (length <= 0)
                {
                    continue;
                }

                segments.Add(new Segment(
                    start,
                    end,
                    length,
                    dx / length,
                    dy / length,
                    NormalizeHeading(Math.Atan2(dy, dx) * 180 / Math.PI)));
            }

            return segments;
        }

        private static void LinkSegments(IReadOnlyList<Segment> segments, bool closed)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                segments[i].Previous = i > 0 ? segments[i - 1] : closed ? segments[^1] : null;
                segments[i].Next = i + 1 < segments.Count ? segments[i + 1] : closed ? segments[0] : null;
            }
        }

        private static Vec2 PositionForOffset(IReadOnlyList<Segment> segments, double pathLength, double offset)
        {
            Segment segment = SegmentForOffset(segments, pathLength, offset, out double segmentStart);
            double local = offset - segmentStart;
            return new Vec2(segment.Start.X + (segment.DirectionX * local), segment.Start.Y + (segment.DirectionY * local));
        }

        private static double HeadingForOffset(IReadOnlyList<Segment> segments, double pathLength, double offset)
        {
            Segment segment = SegmentForOffset(segments, pathLength, offset, out double segmentStart);
            double local = offset - segmentStart;
            double heading = segment.HeadingDeg;
            double distanceToEnd = segment.Length - local;
            if (segment.Next != null && distanceToEnd < FadeDistance)
            {
                double t = 1 - (distanceToEnd / FadeDistance);
                return NormalizeHeading(LerpHeading(heading, segment.Next.HeadingDeg, t * 0.5));
            }

            if (segment.Previous != null && local < FadeDistance)
            {
                double t = 1 - (local / FadeDistance);
                heading = LerpHeading(heading, segment.Previous.HeadingDeg, t * 0.5);
            }

            return NormalizeHeading(heading);
        }

        private static Segment SegmentForOffset(
            IReadOnlyList<Segment> segments,
            double pathLength,
            double offset,
            out double segmentStart)
        {
            if (offset < 0)
            {
                segmentStart = 0;
                return segments[0];
            }

            double accumulated = 0;
            foreach (Segment segment in segments)
            {
                if (offset < accumulated + segment.Length)
                {
                    segmentStart = accumulated;
                    return segment;
                }

                accumulated += segment.Length;
            }

            Segment last = segments[^1];
            segmentStart = pathLength - last.Length;
            return last;
        }

        private static double EndpointFade(double offset, double pathLength, double startOffset)
        {
            double distance = Math.Min(startOffset + offset, Math.Abs(pathLength - offset));
            return Math.Clamp(distance / FadeDistance, 0, 1);
        }

        private static double Wrap(double value, double length)
        {
            double wrapped = value % length;
            return wrapped < 0 ? wrapped + length : wrapped;
        }

        private static double LerpHeading(double from, double to, double t)
        {
            double delta = to - from;
            if (delta > 180)
            {
                delta -= 360;
            }
            else if (delta < -180)
            {
                delta += 360;
            }

            return from + (delta * t);
        }

        private static double NormalizeHeading(double heading)
        {
            double normalized = heading % 360;
            return normalized < 0 ? normalized + 360 : normalized;
        }

        private sealed class Segment(
            Vec2 start,
            Vec2 end,
            double length,
            double directionX,
            double directionY,
            double headingDeg)
        {
            public Vec2 Start { get; } = start;
            public Vec2 End { get; } = end;
            public double Length { get; } = length;
            public double DirectionX { get; } = directionX;
            public double DirectionY { get; } = directionY;
            public double HeadingDeg { get; } = headingDeg;
            public Segment? Previous { get; set; }
            public Segment? Next { get; set; }
        }
    }
}
