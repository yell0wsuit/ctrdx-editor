using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Canvas resize geometry for bouncers. Mirrors <see cref="SpikeResize"/> with two size classes and
    /// reuses <see cref="SpikeResize.Handle"/> so the canvas can drive both strips with one drag state.
    /// </summary>
    public static class BouncerResize
    {
        /// <summary>Resting draw-quad trimmed widths for the small and large bouncer (obj_bouncer.json quads 0 / 5).</summary>
        private static readonly double[] Widths = [196.0, 304.0];

        /// <summary>The world-unit width used for a bouncer size class (1 or 2).</summary>
        public static double WidthForSize(int size)
        {
            return Widths[Math.Clamp(size, 1, 2) - 1];
        }

        /// <summary>Current bouncer width in level units for its size and sprite scale.</summary>
        public static double Width(LevelObject bouncer, double scale, double mapScale = SpritePlacement.MapScale)
        {
            return WidthForSize(Size(bouncer)) * scale / mapScale;
        }

        /// <summary>Classifies whether <paramref name="point"/> is on one of the bouncer's end handles.</summary>
        public static SpikeResize.Handle HitTest(LevelObject bouncer, Vec2 point, double scale, double tolerance, double thickness)
        {
            if (!BouncerObject.IsBouncer(bouncer.Type))
            {
                return SpikeResize.Handle.None;
            }

            (double along, double perp) = LocalCoordinates(bouncer, point);
            double half = Width(bouncer, scale) / 2.0;
            return Math.Abs(perp) > thickness ? SpikeResize.Handle.None
                : Math.Abs(along + half) <= tolerance ? SpikeResize.Handle.ResizeStart
                : Math.Abs(along - half) <= tolerance ? SpikeResize.Handle.ResizeEnd
                : SpikeResize.Handle.None;
        }

        /// <summary>Resizes the bouncer to the nearest supported size for a drag point.</summary>
        public static void ApplyDrag(LevelObject bouncer, Vec2 point, double scale)
        {
            (double along, _) = LocalCoordinates(bouncer, point);
            double targetWidth = Math.Abs(along) * 2.0 * SpritePlacement.MapScale / scale;
            int size = NearestSize(targetWidth);
            BouncerObject.SetSize(bouncer, size.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Returns the size class (1 or 2) whose width is closest to <paramref name="targetWidth"/>.</summary>
        public static int NearestSize(double targetWidth)
        {
            int best = 1;
            double bestDistance = double.MaxValue;
            for (int i = 0; i < Widths.Length; i++)
            {
                double distance = Math.Abs(Widths[i] - targetWidth);
                if (distance < bestDistance)
                {
                    best = i + 1;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static (double Along, double Perp) LocalCoordinates(LevelObject bouncer, Vec2 point)
        {
            double angle = RotationTable.For(bouncer.Type) is { } spec
                ? ObjectRotation.DisplayDegrees(bouncer, spec) * Math.PI / 180.0
                : 0;
            double dx = point.X - bouncer.X;
            double dy = point.Y - bouncer.Y;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return ((dx * cos) + (dy * sin), (-dx * sin) + (dy * cos));
        }

        private static int Size(LevelObject bouncer)
        {
            return BouncerObject.Size(bouncer) == "2" ? 2 : 1;
        }
    }
}
