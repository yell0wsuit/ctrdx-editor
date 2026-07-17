using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Geometry for a movable-rail grab: the hook rides a straight horizontal or vertical rail. In the
    /// game (Grab.SetMoveLengthVerticalOffset) a grab with <c>moveLength &gt; 0</c> becomes a movable
    /// hook whose travel range is <c>[pos - moveOffset, pos + moveLength - moveOffset]</c>, where
    /// <c>pos</c> is the hook's axis coordinate (x for a horizontal rail, y for a vertical one) and the
    /// hook rests at the object's (x, y). The constraint <c>0 &lt;= moveOffset &lt;= moveLength</c> keeps
    /// the hook on the rail. All lengths are in level units. This helper is pure and UI-free.
    /// </summary>
    public static class GrabRail
    {
        /// <summary>Shortest rail a drag may produce, so a movable grab never collapses to length 0.</summary>
        public const double MinLength = 1;

        /// <summary>Whether the grab is a movable-rail hook (moveLength &gt; 0), not a fixed hook.</summary>
        public static bool IsMovable(LevelObject grab)
        {
            return MoveLength(grab) > 0;
        }

        /// <summary>The rail length in level units (0 when not movable).</summary>
        public static double MoveLength(LevelObject grab)
        {
            return Read(grab, "moveLength");
        }

        /// <summary>The hook's rest offset from the rail's start, in level units.</summary>
        public static double MoveOffset(LevelObject grab)
        {
            return Read(grab, "moveOffset");
        }

        /// <summary>Whether the rail runs vertically rather than horizontally.</summary>
        public static bool Vertical(LevelObject grab)
        {
            return bool.TryParse(grab.GetAttr("moveVertical"), out bool v) && v;
        }

        /// <summary>Resolved rail geometry in level space: the two ends, the hook, and orientation.</summary>
        /// <param name="Start">The near cap, in level units.</param>
        /// <param name="End">The far cap, in level units.</param>
        /// <param name="Hook">The hook's rest position, in level units.</param>
        /// <param name="Vertical">Whether the rail runs vertically rather than horizontally.</param>
        /// <param name="Length">The rail length in level units.</param>
        /// <param name="Offset">The hook's distance from <paramref name="Start"/> along the rail.</param>
        public readonly record struct Geometry(
            Vec2 Start, Vec2 End, Vec2 Hook, bool Vertical, double Length, double Offset);

        /// <summary>What part of a rail a point is over, so the canvas can route a drag.</summary>
        public enum Handle
        {
            /// <summary>Nothing interactive under the point.</summary>
            None,

            /// <summary>The near (start) cap: dragging resizes from that end.</summary>
            ResizeStart,

            /// <summary>The far (end) cap: dragging resizes the length.</summary>
            ResizeEnd,

            /// <summary>The hook: dragging slides it along the rail.</summary>
            SlideHook,

            /// <summary>The rail bar: dragging moves the whole grab.</summary>
            MoveBar,
        }

        /// <summary>
        /// Classifies what part of the rail <paramref name="point"/> is over. The end caps win (they are
        /// small targets) unless the hook sits on that end, where sliding wins; then the hook; then the bar
        /// itself. Tolerances are in level units, so the caller converts screen pixels via the zoom.
        /// </summary>
        /// <param name="g">The rail geometry, from <see cref="Of"/>.</param>
        /// <param name="point">The position to test, in level units; typically the cursor.</param>
        /// <param name="endTolerance">The hit radius for the end caps, in level units.</param>
        /// <param name="hookTolerance">The hit radius for the hook, in level units.</param>
        /// <param name="barThickness">How far off-axis still counts as the bar, in level units.</param>
        public static Handle HitTest(Geometry g, Vec2 point, double endTolerance, double hookTolerance, double barThickness)
        {
            bool onHook = Distance(point, g.Hook) <= hookTolerance;
            if (!onHook && Distance(point, g.Start) <= endTolerance)
            {
                return Handle.ResizeStart;
            }
            if (!onHook && Distance(point, g.End) <= endTolerance)
            {
                return Handle.ResizeEnd;
            }
            if (onHook)
            {
                return Handle.SlideHook;
            }

            double along = Axis(point, g.Vertical);
            double perp = Math.Abs(g.Vertical ? point.X - g.Hook.X : point.Y - g.Hook.Y);
            double lo = Math.Min(Axis(g.Start, g.Vertical), Axis(g.End, g.Vertical));
            double hi = Math.Max(Axis(g.Start, g.Vertical), Axis(g.End, g.Vertical));
            return perp <= barThickness && along >= lo && along <= hi ? Handle.MoveBar : Handle.None;
        }

        /// <summary>Resolves the rail geometry for a movable grab, or null when it is a fixed hook.</summary>
        public static Geometry? Of(LevelObject grab)
        {
            if (!IsMovable(grab))
            {
                return null;
            }

            double len = MoveLength(grab);
            double off = Math.Clamp(MoveOffset(grab), 0, len);
            bool vert = Vertical(grab);
            Vec2 hook = new(grab.X, grab.Y);
            Vec2 start = vert ? new Vec2(hook.X, hook.Y - off) : new Vec2(hook.X - off, hook.Y);
            Vec2 end = vert ? new Vec2(hook.X, start.Y + len) : new Vec2(start.X + len, hook.Y);
            return new Geometry(start, end, hook, vert, len, off);
        }

        /// <summary>
        /// Slides the hook to <paramref name="point"/>: the rail ends stay put, the hook moves along the
        /// axis clamped between them. Returns the new hook axis coordinate and the matching offset.
        /// </summary>
        /// <param name="g">The rail geometry, from <see cref="Of"/>.</param>
        /// <param name="point">The drag position, in level units.</param>
        public static (double HookAxis, double Offset) SlideHook(Geometry g, Vec2 point)
        {
            double start = Axis(g.Start, g.Vertical);
            double hookAxis = Math.Clamp(Axis(point, g.Vertical), start, start + g.Length);
            return (hookAxis, hookAxis - start);
        }

        /// <summary>
        /// Resizes by dragging the far (End) cap: the start and the hook stay put, so only the length
        /// changes. The length can't drop below the hook's offset (or <see cref="MinLength"/>), keeping
        /// the hook on the rail. Returns the new length.
        /// </summary>
        /// <param name="g">The rail geometry, from <see cref="Of"/>.</param>
        /// <param name="point">The drag position, in level units.</param>
        public static double ResizeEnd(Geometry g, Vec2 point)
        {
            double start = Axis(g.Start, g.Vertical);
            double floor = Math.Max(MinLength, g.Offset);
            return Math.Max(floor, Axis(point, g.Vertical) - start);
        }

        /// <summary>
        /// Resizes by dragging the near (Start) cap: the end and the hook stay put, so both the offset and
        /// the length change. The start can't pass the hook (offset stays &gt;= 0) and the length stays at
        /// least <see cref="MinLength"/>. Returns the new offset and length.
        /// </summary>
        public static (double Offset, double Length) ResizeStart(Geometry g, Vec2 point)
        {
            double end = Axis(g.End, g.Vertical);
            double hookAxis = Axis(g.Hook, g.Vertical);
            double newStart = Math.Min(Axis(point, g.Vertical), Math.Min(hookAxis, end - MinLength));
            return (hookAxis - newStart, end - newStart);
        }

        private static double Axis(Vec2 p, bool vertical)
        {
            return vertical ? p.Y : p.X;
        }

        private static double Distance(Vec2 a, Vec2 b)
        {
            return GrabRadius.Distance(a, b);
        }

        private static double Read(LevelObject grab, string name)
        {
            return double.TryParse(
                grab.GetAttr(name), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)
                ? v
                : 0;
        }
    }
}
