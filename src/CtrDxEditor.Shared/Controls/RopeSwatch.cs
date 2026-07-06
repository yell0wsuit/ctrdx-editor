using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Rendering;

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
            if (visual.Strips.Count == 0)
            {
                return;
            }

            // Draw through the exact same triangle-strip path as the editor canvas
            // (RopeDrawOperation), so the swatch matches the in-canvas / in-game rope
            // shading pixel-for-pixel instead of faking it with flat polylines.
            // Fit the level-space rope into the swatch rect with a small margin.
            const double margin = 4;
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (RopeStrip strip in visual.Strips)
            {
                foreach (Vec2 p in strip.Points)
                {
                    minX = Math.Min(minX, p.X);
                    minY = Math.Min(minY, p.Y);
                    maxX = Math.Max(maxX, p.X);
                    maxY = Math.Max(maxY, p.Y);
                }
            }

            double contentW = Math.Max(maxX - minX, 1e-3);
            double contentH = Math.Max(maxY - minY, 1e-3);
            double availW = Math.Max(w - (2 * margin), 1e-3);
            double availH = Math.Max(h - (2 * margin), 1e-3);
            double zoom = Math.Min(availW / contentW, availH / contentH);
            double panX = margin + ((availW - (contentW * zoom)) / 2) - (minX * zoom);
            double panY = margin + ((availH - (contentH * zoom)) / 2) - (minY * zoom);

            ViewTransform view = new(zoom, panX, panY);
            context.Custom(new RopeDrawOperation(new Rect(0, 0, w, h), view, visual.Strips));
        }
    }
}
