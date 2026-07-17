using System;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Resolved rotation-dial data for an ordinary object or one active hand segment.</summary>
    /// <param name="Spec">Angle mapping used by dial geometry.</param>
    /// <param name="Center">Dial pivot in level coordinates.</param>
    /// <param name="StoredAngle">Current XML angle in degrees.</param>
    /// <param name="HandSegmentIndex">Active 1-based hand segment, or 0 for an ordinary object.</param>
    public readonly record struct RotationDialTarget(
        RotationSpec Spec,
        Vec2 Center,
        double StoredAngle,
        int HandSegmentIndex);

    /// <summary>Pure target resolution and writes shared by rotation-dial rendering and input.</summary>
    public static class RotationDialTargetResolver
    {
        /// <summary>Resolves a hand segment target or passes an ordinary object's registered target through.</summary>
        public static RotationDialTarget? Resolve(
            LevelObject obj, int activeHandSegment, RotationSpec? ordinarySpec)
        {
            if (HandObject.IsHand(obj.Type))
            {
                int count = HandObject.SegmentCount(obj);
                if (activeHandSegment < 1 || activeHandSegment > count)
                {
                    return null;
                }

                RotationSpec spec = HandGeometry.SegmentSpec(activeHandSegment);
                return new RotationDialTarget(
                    spec,
                    HandGeometry.Joint(obj, activeHandSegment - 1),
                    HandObject.Angle(obj, activeHandSegment),
                    activeHandSegment);
            }

            return ordinarySpec is null
                ? null
                : new RotationDialTarget(
                    ordinarySpec,
                    ObjectRotation.Center(obj, ordinarySpec),
                    ObjectRotation.StoredAngle(obj, ordinarySpec),
                    0);
        }

        /// <summary>Clamps transient hand-segment state to the live chain, or clears it for another object.</summary>
        public static int ClampActiveHandSegment(LevelObject? obj, int requested)
        {
            if (obj is null || !HandObject.IsHand(obj.Type) || requested < 1)
            {
                return 0;
            }

            int count = HandObject.SegmentCount(obj);
            return count > 0 ? Math.Min(requested, count) : 0;
        }

        /// <summary>Writes a dial-produced angle through the target object's canonical writer.</summary>
        public static void ApplyAngle(LevelObject obj, RotationDialTarget target, double angle, Vec2 stableCenter)
        {
            if (target.HandSegmentIndex > 0 && HandObject.IsHand(obj.Type))
            {
                HandObject.SetAngle(obj, target.HandSegmentIndex, angle);
            }
            else if (target.Spec.CenterKind == RotationCenterKind.ConveyorMidpoint)
            {
                ConveyorGeometry.ApplyRotationAroundCenter(obj, angle, stableCenter);
            }
            else
            {
                obj.SetAttr(target.Spec.AttributeName, ObjectRotation.Format(angle));
            }
        }

        /// <summary>Whether a dial hit wins over coincident hand art; length joints deliberately take priority.</summary>
        public static bool DialHasPriority(ObjectRotation.Handle dial, HandGeometry.HandleKind handHandle)
        {
            return dial != ObjectRotation.Handle.None && handHandle != HandGeometry.HandleKind.Joint;
        }
    }

    /// <summary>
    /// Draws the rotation dial for a rotatable object: a ring, a tick every 15° of a turn, and a knob at
    /// the object's facing. Mirrors <see cref="GrabRenderer"/> (static, UI owns the invocation). The
    /// geometry lives in <see cref="ObjectRotation"/>; this class only paints and exposes the screen-space
    /// sizes the canvas needs to hit-test against the same ring.
    /// </summary>
    internal static class RotationDialRenderer
    {
        /// <summary>Ring radius in screen pixels (constant on screen; converted to level units via zoom).</summary>
        public const double RadiusPx = 96;

        /// <summary>Pointer tolerance to the ring edge, in screen pixels.</summary>
        public const double RingTolerancePx = 8;

        /// <summary>Pointer tolerance to the knob, in screen pixels.</summary>
        public const double KnobTolerancePx = 14;

        private const double KnobPx = 7;
        private static readonly Pen RingPen = new(new SolidColorBrush(Color.FromArgb(200, 90, 200, 255)), 1.5);
        private static readonly Pen TickPen = new(new SolidColorBrush(Color.FromArgb(140, 90, 200, 255)), 1);
        private static readonly IBrush KnobBrush = new SolidColorBrush(Color.FromArgb(230, 90, 200, 255));
        private static readonly IBrush KnobActiveBrush = new SolidColorBrush(Color.FromArgb(255, 255, 210, 90));

        /// <summary>
        /// Draws the dial for <paramref name="obj"/> using <paramref name="spec"/>. <paramref name="active"/>
        /// highlights the knob while a rotation drag is in progress. All sizes are screen-constant via zoom.
        /// </summary>
        public static void Draw(DrawingContext ctx, ViewTransform v, LevelObject obj, RotationSpec spec, bool active)
        {
            Draw(
                ctx,
                v,
                ObjectRotation.Center(obj, spec),
                ObjectRotation.StoredAngle(obj, spec),
                spec,
                active);
        }

        /// <summary>Draws a dial using an explicit pivot and stored angle.</summary>
        public static void Draw(
            DrawingContext ctx,
            ViewTransform v,
            Vec2 center,
            double storedAngle,
            RotationSpec spec,
            bool active)
        {
            if (v.Zoom <= 0)
            {
                return;
            }

            double radius = RadiusPx / v.Zoom;
            Vec2 cs = v.LevelToScreen(center);
            const double rs = RadiusPx; // screen radius = level radius * zoom = px constant
            ctx.DrawEllipse(null, RingPen, new Point(cs.X, cs.Y), rs, rs);

            for (int i = 0; i < 24; i++)
            {
                double a = i * 15 * Math.PI / 180;
                double dx = Math.Cos(a), dy = Math.Sin(a);
                double inner = i % 6 == 0 ? rs - 10 : rs - 5; // longer ticks at cardinals
                ctx.DrawLine(TickPen,
                    new Point(cs.X + (dx * inner), cs.Y + (dy * inner)),
                    new Point(cs.X + (dx * rs), cs.Y + (dy * rs)));
            }

            Vec2 knob = v.LevelToScreen(ObjectRotation.KnobPosition(center, storedAngle, spec, radius));
            ctx.DrawEllipse(active ? KnobActiveBrush : KnobBrush, null, new Point(knob.X, knob.Y), KnobPx, KnobPx);
        }
    }
}
