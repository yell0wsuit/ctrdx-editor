using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Controls
{
    /// <summary>A small preview that paints a short slack rope in a given skin's colors.</summary>
    public sealed class RopeSwatch : Control
    {
        /// <summary>Rope skin index to preview.</summary>
        public static readonly StyledProperty<int> SkinProperty =
            AvaloniaProperty.Register<RopeSwatch, int>(nameof(Skin));

        static RopeSwatch()
        {
            AffectsRender<RopeSwatch>(SkinProperty);
        }

        /// <summary>Rope skin index to preview.</summary>
        public int Skin { get => GetValue(SkinProperty); set => SetValue(SkinProperty, value); }

        /// <inheritdoc />
        public override void Render(DrawingContext context)
        {
            base.Render(context);
            double w = Bounds.Width;
            double h = Bounds.Height;
            if (w <= 0 || h <= 0)
            {
                return;
            }

            // A short slack rope across the swatch; length > span so it sags a little.
            Vec2 a = new(0, 0);
            Vec2 b = new(50, 0);
            RopeVisual visual = RopeStripBuilder.Build(a, b, 60, Skin);
            RopeDrawColors colors = RopePalette.GetDrawColors(Skin, 50, 60);

            IReadOnlyList<Vec2> pts = visual.SamplePoints;
            if (pts.Count < 2)
            {
                return;
            }

            // Map the sampled catenary points into the swatch rect (with a small margin).
            const double margin = 4;
            const double spanX = 50;
            double spanY = 1;
            foreach (Vec2 p in pts)
            {
                spanY = Math.Max(spanY, p.Y);
            }

            Point Map(Vec2 p)
            {
                return new Point(
                    margin + (p.X / spanX * (w - (2 * margin))),
                    margin + (p.Y / spanY * (h - (2 * margin)) * 0.6));
            }

            StreamGeometry geo = new();
            using (StreamGeometryContext gc = geo.Open())
            {
                gc.BeginFigure(Map(pts[0]), false);
                for (int i = 1; i < pts.Count; i++)
                {
                    gc.LineTo(Map(pts[i]));
                }
            }

            static Color ToColor(RopeRgb c)
            {
                return Color.FromRgb(
                    (byte)(Math.Clamp(c.R, 0, 1) * 255),
                    (byte)(Math.Clamp(c.G, 0, 1) * 255),
                    (byte)(Math.Clamp(c.B, 0, 1) * 255));
            }

            context.DrawGeometry(null, new Pen(new SolidColorBrush(ToColor(colors.Base1)), 4), geo);
            context.DrawGeometry(null, new Pen(new SolidColorBrush(ToColor(colors.Base2)), 2), geo);
        }
    }
}
