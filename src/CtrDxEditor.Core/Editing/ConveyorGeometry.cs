using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Pure canvas geometry and resize/rotate hit-testing for conveyor belts. (x,y) is the centre of one
    /// end (the anchor); the belt extends <c>length</c> along (cos a, -sin a) with <c>a = angle</c> in
    /// radians (level space is y-down, so a positive angle points upward on screen). Modeled on
    /// <see cref="SpikeResize"/> and <see cref="GrabRail"/>. Rotation is handled here (not the generic
    /// rotation dial) because the belt's angle is counter-clockwise while a RotationSpec offset is
    /// clockwise-only.
    /// </summary>
    public static class ConveyorGeometry
    {
        /// <summary>Resolved belt geometry in level units.</summary>
        /// <param name="Anchor">The centre of the anchored end (the object's (x,y)).</param>
        /// <param name="Far">The centre of the far end.</param>
        /// <param name="Length">The belt length in level units.</param>
        /// <param name="Width">The belt thickness in level units.</param>
        /// <param name="AngleDeg">The belt angle in degrees (counter-clockwise, y-up convention).</param>
        public readonly record struct Shape(Vec2 Anchor, Vec2 Far, double Length, double Width, double AngleDeg);

        /// <summary>Which conveyor handle a point is over.</summary>
        public enum Handle
        {
            /// <summary>No conveyor handle.</summary>
            None,

            /// <summary>The far end: dragging rewrites length and angle together.</summary>
            FarEnd,

            /// <summary>A side midpoint: dragging rewrites the belt thickness (width).</summary>
            Width,
        }

        /// <summary>The belt's geometry, or null when <paramref name="belt"/> is not a conveyor.</summary>
        /// <param name="belt">The object to resolve.</param>
        /// <returns>The resolved <see cref="Shape"/>, or null for non-transporters.</returns>
        public static Shape? Of(LevelObject belt)
        {
            if (!ConveyorObject.IsConveyor(belt.Type))
            {
                return null;
            }

            double length = Attr(belt, "length");
            double width = Attr(belt, "width");
            double angleDeg = Attr(belt, "angle");
            double a = angleDeg * Math.PI / 180.0;
            Vec2 anchor = new(belt.X, belt.Y);
            Vec2 far = new(anchor.X + (length * Math.Cos(a)), anchor.Y - (length * Math.Sin(a)));
            return new Shape(anchor, far, length, width, angleDeg);
        }

        /// <summary>Axis-aligned selection/marquee box over the four belt corners.</summary>
        /// <param name="s">The belt shape.</param>
        /// <returns>The axis-aligned bounds enclosing the belt rectangle.</returns>
        public static LevelBounds Bounds(Shape s)
        {
            double a = s.AngleDeg * Math.PI / 180.0;
            // Perpendicular unit vector to dir=(cos a, -sin a).
            double px = Math.Sin(a);
            double py = Math.Cos(a);
            double hw = s.Width / 2.0;
            Vec2[] corners =
            [
                new(s.Anchor.X + (px * hw), s.Anchor.Y + (py * hw)),
                new(s.Anchor.X - (px * hw), s.Anchor.Y - (py * hw)),
                new(s.Far.X + (px * hw), s.Far.Y + (py * hw)),
                new(s.Far.X - (px * hw), s.Far.Y - (py * hw)),
            ];
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (Vec2 c in corners)
            {
                minX = Math.Min(minX, c.X);
                minY = Math.Min(minY, c.Y);
                maxX = Math.Max(maxX, c.X);
                maxY = Math.Max(maxY, c.Y);
            }
            return new LevelBounds(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>Classifies whether <paramref name="point"/> is over the far-end or a width handle.</summary>
        /// <param name="s">The belt shape.</param>
        /// <param name="point">The level-space point to classify.</param>
        /// <param name="endTolerance">Hit radius for the far-end handle.</param>
        /// <param name="widthTolerance">Hit radius for the width handles.</param>
        /// <returns>The handle under the point, or <see cref="Handle.None"/>.</returns>
        public static Handle HitTest(Shape s, Vec2 point, double endTolerance, double widthTolerance)
        {
            if (Distance(point, s.Far) <= endTolerance)
            {
                return Handle.FarEnd;
            }

            (double along, double perp) = Local(s, point);
            double hw = s.Width / 2.0;
            // Width handles sit at the two side midpoints (along = length/2, perp = +-hw).
            bool nearMid = Math.Abs(along - (s.Length / 2.0)) <= Math.Max(widthTolerance, s.Length / 4.0);
            return nearMid && Math.Abs(Math.Abs(perp) - hw) <= widthTolerance ? Handle.Width : Handle.None;
        }

        /// <summary>Rewrites <c>length</c> and <c>angle</c> from a far-end drag, pivoting on the anchor.</summary>
        /// <param name="belt">The conveyor object to modify.</param>
        /// <param name="point">The new far-end position in level space.</param>
        public static void ApplyFarEndDrag(LevelObject belt, Vec2 point)
        {
            double dx = point.X - belt.X;
            double dy = point.Y - belt.Y;
            double length = Math.Sqrt((dx * dx) + (dy * dy));
            // Level space is y-down; angle is measured CCW so negate dy.
            double angleDeg = Math.Atan2(-dy, dx) * 180.0 / Math.PI;
            belt.SetAttr("length", Whole(length));
            belt.SetAttr("angle", Whole(Normalize(angleDeg)));
        }

        /// <summary>Rewrites <c>width</c> from a side drag (perpendicular distance x2, min 1).</summary>
        /// <param name="belt">The conveyor object to modify.</param>
        /// <param name="point">The dragged side-handle position in level space.</param>
        public static void ApplyWidthDrag(LevelObject belt, Vec2 point)
        {
            if (Of(belt) is not { } s)
            {
                return;
            }
            (_, double perp) = Local(s, point);
            double width = Math.Max(1, Math.Abs(perp) * 2.0);
            belt.SetAttr("width", Whole(width));
        }

        // Local coordinates: along the belt axis (0 at anchor) and perpendicular to it.
        private static (double Along, double Perp) Local(Shape s, Vec2 point)
        {
            double a = s.AngleDeg * Math.PI / 180.0;
            double dx = point.X - s.Anchor.X;
            double dy = point.Y - s.Anchor.Y;
            double cos = Math.Cos(a);
            double sin = Math.Sin(a);
            // dir=(cos,-sin), perp=(sin,cos).
            double along = (dx * cos) - (dy * sin);
            double perp = (dx * sin) + (dy * cos);
            return (along, perp);
        }

        private static double Distance(Vec2 a, Vec2 b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static double Attr(LevelObject belt, string name)
        {
            return double.TryParse(belt.GetAttr(name), NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
        }

        private static double Normalize(double deg)
        {
            double d = deg % 360.0;
            if (d < 0)
            {
                d += 360.0;
            }
            return d;
        }

        private static string Whole(double value)
        {
            return ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
        }
    }
}
