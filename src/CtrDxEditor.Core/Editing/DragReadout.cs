using System;
using System.Collections.Generic;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Which canvas drag is in progress, and therefore which property its badge reports.</summary>
    public enum DragKind
    {
        /// <summary>No drag; nothing to report.</summary>
        None,

        /// <summary>Moving an object's position.</summary>
        Move,

        /// <summary>Turning an object with the rotation dial.</summary>
        Rotate,

        /// <summary>Resizing a grab's catch radius or a bulb's lit radius.</summary>
        Radius,

        /// <summary>Dragging the knob on a grab's rope.</summary>
        RopeLength,

        /// <summary>Sliding the hook along a grab's rail.</summary>
        RailOffset,

        /// <summary>Dragging either end of a grab's rail.</summary>
        RailResize,

        /// <summary>Dragging a conveyor's far end.</summary>
        ConveyorLength,

        /// <summary>Dragging a conveyor's side midpoint.</summary>
        ConveyorWidth,

        /// <summary>Resizing a spike or bouncer strip to a new size class.</summary>
        StripSize,

        /// <summary>Dragging a hand joint, changing that segment's length.</summary>
        HandJoint,

        /// <summary>Dragging a hand's base, moving the whole hand.</summary>
        HandBase,

        /// <summary>Turning a vinyl's handle.</summary>
        VinylAngle,

        /// <summary>Dragging a vertex of a mover or ant polyline.</summary>
        PolylinePoint,

        /// <summary>Dragging the right edge of a tutorial text box.</summary>
        TutorialWidth,

        /// <summary>Dragging the level's waterline.</summary>
        Water,
    }

    /// <summary>
    /// Resolves the live value a drag is changing, so the canvas can show it in a badge. Values are read
    /// back off the object <em>after</em> the drag has applied them, so a badge can never disagree with
    /// what was actually written.
    /// </summary>
    public static class DragReadout
    {
        /// <summary>One label/value pair in a readout badge.</summary>
        /// <param name="AttrKey">The raw XML attribute name, which the renderer localizes for display.</param>
        /// <param name="Value">The formatted value, ready to draw.</param>
        public readonly record struct Entry(string AttrKey, string Value);

        /// <summary>
        /// Screen pixels a pointer must travel before its badge appears. Small enough that a deliberate
        /// drag reads as immediate, large enough to swallow the hand-tremor movement that accompanies an
        /// ordinary click.
        /// </summary>
        public const double ArmDistancePx = 3;

        /// <summary>
        /// Whether a gesture has travelled far enough to read as a drag rather than a click.
        /// </summary>
        /// <remarks>
        /// Deliberately separate from <see cref="TouchInput.ExceedsDragSlop"/>, which exists to decide when
        /// a press starts <em>editing</em> and reports true on any mouse movement at all. That is correct
        /// for editing — desktop drags are pixel-precise — but it cannot tell a click from a drag, so it
        /// would show a badge on every click.
        /// </remarks>
        /// <param name="pressPx">Screen-space position where the press began.</param>
        /// <param name="currentPx">Current screen-space position.</param>
        /// <returns>True once travel exceeds <see cref="ArmDistancePx"/>.</returns>
        public static bool IsArmed(Vec2 pressPx, Vec2 currentPx)
        {
            double dx = currentPx.X - pressPx.X;
            double dy = currentPx.Y - pressPx.Y;
            return (dx * dx) + (dy * dy) > ArmDistancePx * ArmDistancePx;
        }

        /// <summary>
        /// The entries a drag's badge should show, in display order. Returns empty rather than throwing
        /// whenever state is inconsistent — a missing object, an out-of-range index, an absent attribute —
        /// so a stale drag flag can never crash a render pass.
        /// </summary>
        /// <param name="kind">The drag in progress.</param>
        /// <param name="obj">The object being dragged; null for <see cref="DragKind.Water"/>.</param>
        /// <param name="index">
        /// The 1-based hand segment for a hand <see cref="DragKind.HandJoint"/> or
        /// <see cref="DragKind.Rotate"/>; ignored otherwise.
        /// </param>
        /// <param name="point">
        /// Consulted only by <see cref="DragKind.PolylinePoint"/> (the dragged vertex) and
        /// <see cref="DragKind.Water"/> (the height, <c>Y</c> only). Those two are the only kinds whose
        /// value is not derivable from the object alone.
        /// </param>
        /// <returns>The badge entries, or an empty list.</returns>
        public static IReadOnlyList<Entry> For(DragKind kind, LevelObject? obj, int index, Vec2 point)
        {
            return kind == DragKind.Water
                ? [new Entry("water", Whole(point.Y))]
                : kind == DragKind.PolylinePoint
                    ? [new Entry("x", Whole(point.X)), new Entry("y", Whole(point.Y))]
                    : obj is null
                        ? []
                        : kind switch
                        {
                            DragKind.Move or DragKind.HandBase => Position(obj),
                            DragKind.Rotate => Rotation(obj, index),
                            DragKind.Radius => Radius(obj),
                            DragKind.RopeLength or DragKind.ConveyorLength => Attr(obj, "length"),
                            DragKind.RailOffset => RailOffset(obj),
                            DragKind.RailResize => [.. Attr(obj, "moveOffset"), .. Attr(obj, "moveLength")],
                            DragKind.ConveyorWidth or DragKind.TutorialWidth => Attr(obj, "width"),
                            DragKind.StripSize => StripSize(obj),
                            DragKind.HandJoint => HandJoint(obj, index),
                            DragKind.VinylAngle => Attr(obj, "handleAngle"),
                            DragKind.None or DragKind.PolylinePoint or DragKind.Water => [],
                            _ => [],
                        };
        }

        /// <summary>The object's authored position, X before Y.</summary>
        private static Entry[] Position(LevelObject obj)
        {
            return [new Entry("x", Int(obj.X)), new Entry("y", Int(obj.Y))];
        }

        /// <summary>An attribute's current value, or nothing when it is absent or blank.</summary>
        private static Entry[] Attr(LevelObject obj, string key)
        {
            string? value = obj.GetAttr(key);
            return string.IsNullOrEmpty(value) ? [] : [new Entry(key, value)];
        }

        /// <summary>
        /// The displayed rotation, degree-signed. Registered objects use
        /// <see cref="ObjectRotation.DisplayDegrees"/>; hands read the indexed segment's synthesized angle
        /// property, and a ghost bouncer preview reads the ghost's ordinary <c>angle</c> property.
        /// </summary>
        private static Entry[] Rotation(LevelObject obj, int index)
        {
            return HandObject.IsHand(obj.Type)
                ? index < 1 || index > HandObject.SegmentCount(obj)
                    ? []
                    : AngleAttr(obj, HandObject.AngleAttr(index))
                : RotationTable.For(obj.Type) is { } spec
                    ? [new Entry("angle", Whole(ObjectRotation.DisplayDegrees(obj, spec)) + "°")]
                    : AngleAttr(obj, "angle");
        }

        /// <summary>An angle attribute formatted as a degree-signed badge value, or nothing when absent.</summary>
        private static Entry[] AngleAttr(LevelObject obj, string key)
        {
            return double.TryParse(
                obj.GetAttr(key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? [new Entry("angle", Whole(value) + "°")]
                : [];
        }

        /// <summary>The resized ring's own attribute, so a bulb reads "litRadius" and a grab reads "radius".</summary>
        private static Entry[] Radius(LevelObject obj)
        {
            return RadiusRing.Of(obj) is { } ring
                ? [new Entry(ring.Attr, Whole(ring.Radius))]
                : obj.Type == "ghost" && GrabRadius.Of(obj) is double radius
                    ? [new Entry("radius", Whole(radius))]
                    : [];
        }

        /// <summary>
        /// The hook slide writes the offset and moves the grab along the rail's axis, so the badge reports
        /// both — the offset alone would not explain the object visibly changing position.
        /// </summary>
        private static Entry[] RailOffset(LevelObject obj)
        {
            bool vertical = GrabRail.Vertical(obj);
            Entry axis = vertical
                ? new Entry("y", Int(obj.Y))
                : new Entry("x", Int(obj.X));
            return [.. Attr(obj, "moveOffset"), axis];
        }

        /// <summary>The strip's discrete size class, read through the owning type's accessor.</summary>
        private static Entry[] StripSize(LevelObject obj)
        {
            return SpikeObject.IsSpike(obj.Type) ? [new Entry("size", SpikeObject.Size(obj))]
                : BouncerObject.IsBouncer(obj.Type) ? [new Entry("size", BouncerObject.Size(obj))]
                : [];
        }

        /// <summary>The dragged segment's length, or nothing when the index is out of range.</summary>
        private static Entry[] HandJoint(LevelObject obj, int index)
        {
            return index < 1 || index > HandObject.SegmentCount(obj)
                ? []
                : [new Entry("length", Whole(HandObject.Length(obj, index)))];
        }

        /// <summary>Rounds a level-space value to a whole number for display.</summary>
        private static string Whole(double value)
        {
            return ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Formats an already-integral coordinate for display.</summary>
        private static string Int(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
