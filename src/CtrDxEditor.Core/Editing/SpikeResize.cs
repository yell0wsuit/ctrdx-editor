using System;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Canvas resize geometry for regular spike strips.</summary>
    public static class SpikeResize
    {
        private static readonly double[] StaticWidths = [214, 335, 455, 568];
        private static readonly double[] ToggledWidths = [204, 321, 446, 561];

        /// <summary>Which spike resize handle a point is over.</summary>
        public enum Handle
        {
            /// <summary>No spike resize handle.</summary>
            None,

            /// <summary>The left/start end of the spike strip, in local spike coordinates.</summary>
            ResizeStart,

            /// <summary>The right/end end of the spike strip, in local spike coordinates.</summary>
            ResizeEnd,
        }

        /// <summary>Returns the DX atlas width used for a spike size and toggle state.</summary>
        public static double WidthForSize(int size, bool toggled)
        {
            int index = Math.Clamp(size, 1, 4) - 1;
            return toggled ? ToggledWidths[index] : StaticWidths[index];
        }

        /// <summary>Current spike strip width in level units for the object's size/toggle state and sprite scale.</summary>
        public static double Width(LevelObject spike, double scale, double mapScale = SpritePlacement.MapScale)
        {
            return WidthForSize(Size(spike), SpikeObject.IsToggled(spike)) * scale / mapScale;
        }

        /// <summary>Classifies whether <paramref name="point"/> is on one of the selected spike's end handles.</summary>
        public static Handle HitTest(LevelObject spike, Vec2 point, double scale, double tolerance, double thickness)
        {
            if (!SpikeObject.IsSpike(spike.Type))
            {
                return Handle.None;
            }

            (double along, double perp) = LocalCoordinates(spike, point);
            double half = Width(spike, scale) / 2.0;
            return Math.Abs(perp) > thickness ? Handle.None
                : Math.Abs(along + half) <= tolerance
                ? Handle.ResizeStart
                : Math.Abs(along - half) <= tolerance ? Handle.ResizeEnd : Handle.None;
        }

        /// <summary>Resizes the spike to the nearest supported size for a drag point.</summary>
        public static void ApplyDrag(LevelObject spike, Vec2 point, double scale)
        {
            (double along, _) = LocalCoordinates(spike, point);
            double targetWidth = Math.Abs(along) * 2.0 * SpritePlacement.MapScale / scale;
            int size = NearestSize(targetWidth, SpikeObject.IsToggled(spike));
            SpikeObject.SetSize(spike, size.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>Returns the supported size whose DX width is closest to <paramref name="targetWidth"/>.</summary>
        public static int NearestSize(double targetWidth, bool toggled)
        {
            double[] widths = toggled ? ToggledWidths : StaticWidths;
            int best = 1;
            double bestDistance = double.MaxValue;
            for (int i = 0; i < widths.Length; i++)
            {
                double distance = Math.Abs(widths[i] - targetWidth);
                if (distance < bestDistance)
                {
                    best = i + 1;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static (double Along, double Perp) LocalCoordinates(LevelObject spike, Vec2 point)
        {
            double angle = RotationTable.For(spike.Type) is { } spec
                ? ObjectRotation.DisplayDegrees(spike, spec) * Math.PI / 180.0
                : 0;
            double dx = point.X - spike.X;
            double dy = point.Y - spike.Y;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return ((dx * cos) + (dy * sin), (-dx * sin) + (dy * cos));
        }

        private static int Size(LevelObject spike)
        {
            return int.TryParse(SpikeObject.Size(spike), out int size) ? size : 1;
        }
    }
}
