using System;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
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
            if (v.Zoom <= 0)
            {
                return;
            }

            Vec2 c = ObjectRotation.Center(obj, spec);
            double radius = RadiusPx / v.Zoom;
            Vec2 cs = v.LevelToScreen(c);
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

            Vec2 knob = v.LevelToScreen(ObjectRotation.KnobPosition(c, ObjectRotation.StoredAngle(obj, spec), spec, radius));
            ctx.DrawEllipse(active ? KnobActiveBrush : KnobBrush, null, new Point(knob.X, knob.Y), KnobPx, KnobPx);
        }
    }
}
