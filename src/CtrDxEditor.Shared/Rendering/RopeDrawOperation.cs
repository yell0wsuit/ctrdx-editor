using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using SkiaSharp;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Renders rope triangle strips through SkiaSharp, reproducing the game's
    /// per-vertex color gradients (Avalonia's DrawingContext cannot express them).
    /// Vertices are level-space; the view transform is applied on the Skia canvas
    /// so rope width zooms with the art.
    /// </summary>
    internal sealed class RopeDrawOperation(Rect bounds, ViewTransform view, IReadOnlyList<RopeStrip> strips, double opacity = 1.0)
        : ICustomDrawOperation
    {
        /// <inheritdoc />
        public Rect Bounds { get; } = bounds;

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public bool HitTest(Point p)
        {
            return false;
        }

        /// <inheritdoc />
        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        /// <inheritdoc />
        public void Render(ImmediateDrawingContext context)
        {
            ISkiaSharpApiLeaseFeature? leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
            {
                return; // Non-Skia backend: ropes simply aren't drawn.
            }
            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;
            int save = canvas.Save();
            canvas.Translate((float)view.PanX, (float)view.PanY);
            canvas.Scale((float)view.Zoom);
            using SKPaint paint = new() { IsAntialias = true };
            foreach (RopeStrip strip in strips)
            {
                SKPoint[] points = new SKPoint[strip.Points.Length];
                SKColor[] colors = new SKColor[strip.Colors.Length];
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = new SKPoint((float)strip.Points[i].X, (float)strip.Points[i].Y);
                    colors[i] = ToSKColor(strip.Colors[i], opacity);
                }
                using SKVertices vertices = SKVertices.CreateCopy(SKVertexMode.TriangleStrip, points, colors);
                // Dst keeps the interpolated vertex colors (the paint contributes nothing).
                canvas.DrawVertices(vertices, SKBlendMode.Dst, paint);
            }
            canvas.RestoreToCount(save);
        }

        // opacity fades the whole rope uniformly: Avalonia's PushOpacity does not reach a custom draw
        // operation, so an invisible grab's rope has to be dimmed here at the vertex alpha instead.
        private static SKColor ToSKColor(RopeRgba c, double opacity)
        {
            return new SKColor(
                (byte)Math.Clamp(c.R * 255, 0, 255),
                (byte)Math.Clamp(c.G * 255, 0, 255),
                (byte)Math.Clamp(c.B * 255, 0, 255),
                (byte)Math.Clamp(c.A * opacity * 255, 0, 255));
        }
    }
}
