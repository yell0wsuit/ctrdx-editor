using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Canvas geometry for the vinyl (rotatedCircle) disc: its size, the on-canvas scale that makes the
    /// drawn disc radius equal <c>size</c> level units, and the 1-2 handles ropes attach to. Mirrors the
    /// game's <c>LoadRotatedCircle</c>: handles sit at <c>center ± size</c> then rotate by <c>handleAngle</c>
    /// degrees (clockwise, Y-down); <c>oneHandle</c> keeps only the right handle. UI-free; all lengths in
    /// level units. The disc-body atlas frame width (<see cref="BodyFrameWidth"/>) is a fixed property of
    /// <c>obj_vinil</c>, hardcoded here the same way <see cref="BouncerResize"/> hardcodes its quad widths.
    /// </summary>
    public static class VinylGeometry
    {
        /// <summary>XML element name of the vinyl object.</summary>
        public const string Element = "rotatedCircle";

        /// <summary>Default size used when the attribute is missing or unparsable.</summary>
        public const int DefaultSize = 110;

        /// <summary>Smallest disc radius (level units) a drag may produce.</summary>
        public const int MinSize = 1;

        /// <summary>Drawn width of the disc-body atlas frame (obj_vinil.json quad 0), in atlas pixels.</summary>
        public const double BodyFrameWidth = 1066.0;

        /// <summary>Which disc handle a hit or drag refers to.</summary>
        public enum Handle
        {
            /// <summary>No handle.</summary>
            None,

            /// <summary>The handle at <c>handleAngle</c> (kept when oneHandle is set).</summary>
            Right,

            /// <summary>The handle at <c>handleAngle + 180</c> (hidden when oneHandle is set).</summary>
            Left,
        }

        /// <summary>Whether an element type is the vinyl disc.</summary>
        public static bool IsVinyl(string type)
        {
            return type == Element;
        }

        /// <summary>The disc size (radius) in level units, clamped to at least <see cref="MinSize"/>.</summary>
        public static int Size(LevelObject obj)
        {
            return int.TryParse(obj.GetAttr("size"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int s)
                ? Math.Max(MinSize, s)
                : DefaultSize;
        }

        /// <summary>The handle rotation in degrees (clockwise, Y-down), default 0.</summary>
        public static double HandleAngleDegrees(LevelObject obj)
        {
            return double.TryParse(obj.GetAttr("handleAngle"), NumberStyles.Float, CultureInfo.InvariantCulture, out double d)
                ? d
                : 0.0;
        }

        /// <summary>Whether the disc exposes only the right handle.</summary>
        public static bool OneHandle(LevelObject obj)
        {
            return bool.TryParse(obj.GetAttr("oneHandle"), out bool one) && one;
        }

        /// <summary>The disc radius in level units (equal to <see cref="Size"/>).</summary>
        public static double DiscRadius(LevelObject obj)
        {
            return Size(obj);
        }

        /// <summary>
        /// The per-object sprite scale so the drawn disc-body radius equals <see cref="DiscRadius"/> level
        /// units: body half-width is <c>BodyFrameWidth/2 * scale / mapScale</c>, set equal to size.
        /// </summary>
        public static double LayerScale(LevelObject obj, double mapScale = SpritePlacement.MapScale)
        {
            return Size(obj) * mapScale / (BodyFrameWidth / 2.0);
        }

        /// <summary>World-space position of a disc handle.</summary>
        public static Vec2 HandlePosition(LevelObject obj, Handle h)
        {
            double radial = HandleAngleDegrees(obj) + (h == Handle.Left ? 180.0 : 0.0);
            double rad = radial * Math.PI / 180.0;
            double r = DiscRadius(obj);
            return new Vec2(obj.X + (Math.Cos(rad) * r), obj.Y + (Math.Sin(rad) * r));
        }

        /// <summary>The handle within <paramref name="tolerance"/> of <paramref name="point"/>, nearest first.</summary>
        public static Handle HitTest(LevelObject obj, Vec2 point, double tolerance)
        {
            double right = Distance(HandlePosition(obj, Handle.Right), point);
            double left = OneHandle(obj) ? double.MaxValue : Distance(HandlePosition(obj, Handle.Left), point);
            return right > tolerance && left > tolerance
                ? Handle.None
                : right <= left ? Handle.Right : Handle.Left;
        }

        /// <summary>The <c>handleAngle</c> to store when <paramref name="h"/> is dragged to <paramref name="point"/>.</summary>
        public static double AngleFor(LevelObject obj, Handle h, Vec2 point)
        {
            double deg = Math.Atan2(point.Y - obj.Y, point.X - obj.X) * 180.0 / Math.PI;
            if (h == Handle.Left)
            {
                deg -= 180.0;
            }
            return Normalize(deg);
        }

        /// <summary>Normalizes an angle in degrees to the half-open range (-180, 180].</summary>
        private static double Normalize(double degrees)
        {
            degrees %= 360.0;
            if (degrees <= -180.0)
            {
                degrees += 360.0;
            }
            else if (degrees > 180.0)
            {
                degrees -= 360.0;
            }
            return degrees;
        }

        private static double Distance(Vec2 a, Vec2 b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt((dx * dx) + (dy * dy));
        }
    }
}
