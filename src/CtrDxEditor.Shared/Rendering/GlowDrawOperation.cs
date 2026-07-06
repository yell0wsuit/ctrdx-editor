using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using SkiaSharp;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Renders light-bulb lit-glow halos through SkiaSharp so they use the game's additive blend
    /// (SKBlendMode.Plus at 0.6 alpha, matching LightBulb.cs blendingMode=2 and the glow color alpha). The
    /// halo is the 01_light.png glow quad stretched to 1.5x litRadius (LightBulb.ApplyGlowScale's 1.5f
    /// multiplier) and centered on the bulb, drawn in level space with the view transform applied on the
    /// Skia canvas so it zooms with the art. On a non-Skia backend the glow is simply not drawn.
    /// </summary>
    internal sealed class GlowDrawOperation(
        Rect bounds,
        ViewTransform view,
        Bitmap atlas,
        IntRect frame,
        IReadOnlyList<(Vec2 Center, double Radius)> bulbs)
        : ICustomDrawOperation
    {
        // One SKImage per atlas bitmap for the process lifetime, keyed weakly so it is collected with the
        // bitmap. Converting the already-decoded Avalonia bitmap avoids decoding the atlas a second time.
        private static readonly ConditionalWeakTable<Bitmap, SKImage> ImageCache = [];

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
                return; // Non-Skia backend: the glow simply isn't drawn (same as ropes).
            }
            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;
            SKImage image = ImageFor(atlas);

            int save = canvas.Save();
            canvas.Translate((float)view.PanX, (float)view.PanY);
            canvas.Scale((float)view.Zoom);

            SKRect src = new(frame.X, frame.Y, frame.X + frame.W, frame.Y + frame.H);
            SKSamplingOptions sampling = new(SKFilterMode.Linear);
            using SKPaint paint = new()
            {
                IsAntialias = true,
                BlendMode = SKBlendMode.Plus,           // additive, matching the game's blendingMode=2
                Color = new SKColor(255, 255, 255, 153), // white @ ~0.6 alpha (game glow alpha)
            };

            foreach ((Vec2 center, double radius) in bulbs)
            {
                (double halfW, double halfH) = GlowQuad.DestRadii(radius, frame.W, frame.H);
                SKRect dest = new(
                    (float)(center.X - halfW), (float)(center.Y - halfH),
                    (float)(center.X + halfW), (float)(center.Y + halfH));
                canvas.DrawImage(image, src, dest, sampling, paint);
            }

            canvas.RestoreToCount(save);
        }

        // Snapshots the Avalonia bitmap's pixels into a standalone SKImage (BGRA premultiplied, as Avalonia
        // decodes PNGs). FromPixelCopy copies, so the temporary SKBitmap is safe to dispose.
        private static SKImage ImageFor(Bitmap bmp)
        {
            if (ImageCache.TryGetValue(bmp, out SKImage? cached))
            {
                return cached;
            }
            PixelSize size = bmp.PixelSize;
            SKImageInfo info = new(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using SKBitmap sk = new(info);
            bmp.CopyPixels(new PixelRect(0, 0, size.Width, size.Height), sk.GetPixels(), info.BytesSize, info.RowBytes);
            SKImage image = SKImage.FromPixelCopy(info, sk.GetPixels(), info.RowBytes);
            ImageCache.Add(bmp, image);
            return image;
        }
    }
}
