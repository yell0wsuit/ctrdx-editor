using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using CtrDxEditor.Core.Geometry;

using SkiaSharp;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Draws one tutorial atlas quad with its ink repainted to <paramref name="color"/>, keeping each
    /// pixel's own alpha, with the view transform applied on the Skia canvas. Sign art is flat black
    /// whose whole shape lives in the alpha channel, so the source RGB carries nothing worth
    /// preserving - this mirrors the game's <c>PremultipliedTint.Apply</c>, which likewise discards
    /// the source color and keeps only alpha. Used both for an authored <c>color</c> and for the
    /// dark-canvas default (white), which is why the un-authored case must still resolve to white to
    /// leave today's dark-canvas rendering unchanged. <paramref name="alpha"/> is a combined 0-1
    /// multiplier (authored <c>opacity</c> times any extra fade-envelope alpha), applied on top of each
    /// pixel's own alpha; it defaults to full so a caller that has not computed one yet draws at full
    /// strength.
    /// </summary>
    internal sealed class TutorialInvertDrawOperation(
        Rect bounds,
        ViewTransform view,
        Bitmap atlas,
        IntRect frame,
        Rect destLevel,
        double rotationDegrees,
        SKColor color,
        double alpha = 1.0)
        : ICustomDrawOperation
    {
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
                return;
            }

            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;
            SKImage image = ImageFor(atlas);

            int save = canvas.Save();
            canvas.Translate((float)view.PanX, (float)view.PanY);
            canvas.Scale((float)view.Zoom);

            float cx = (float)(destLevel.X + (destLevel.Width / 2));
            float cy = (float)(destLevel.Y + (destLevel.Height / 2));
            if (rotationDegrees != 0)
            {
                canvas.RotateDegrees((float)rotationDegrees, cx, cy);
            }

            SKRect src = new(frame.X, frame.Y, frame.X + frame.W, frame.Y + frame.H);
            SKRect dst = new(
                (float)destLevel.X,
                (float)destLevel.Y,
                (float)(destLevel.X + destLevel.Width),
                (float)(destLevel.Y + destLevel.Height));
            using SKPaint paint = new()
            {
                IsAntialias = true,
                ColorFilter = SKColorFilter.CreateColorMatrix(InkMatrix(color, alpha)),
            };
            canvas.DrawImage(image, src, dst, new SKSamplingOptions(SKFilterMode.Linear), paint);
            canvas.RestoreToCount(save);
        }

        /// <summary>
        /// Builds the color matrix that repaints every pixel to <paramref name="color"/> regardless of
        /// its own RGB, scaling its existing alpha by <paramref name="alpha"/>. The zero coefficients on
        /// the R/G/B rows are what discard the source color (matching <c>PremultipliedTint.Apply</c>,
        /// which never reads it either); the offsets are the only thing that reaches the output. The
        /// alpha row keeps the source alpha's shape (coefficient 1) and only scales it, so the sign's
        /// silhouette is unchanged - only its ink color and overall strength are.
        /// </summary>
        internal static float[] InkMatrix(SKColor color, double alpha)
        {
            float a = (float)Math.Clamp(alpha, 0.0, 1.0);
            return
            [
                0, 0, 0, 0, color.Red,
                0, 0, 0, 0, color.Green,
                0, 0, 0, 0, color.Blue,
                0, 0, 0, a, 0,
            ];
        }

        private static SKImage ImageFor(Bitmap bmp)
        {
            if (ImageCache.TryGetValue(bmp, out SKImage? cached))
            {
                return cached;
            }

            PixelSize size = bmp.PixelSize;
            SKImageInfo info = new(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            byte[] pixels = new byte[info.BytesSize];
            GCHandle handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try
            {
                bmp.CopyPixels(
                    new PixelRect(0, 0, size.Width, size.Height),
                    handle.AddrOfPinnedObject(),
                    info.BytesSize,
                    info.RowBytes);
            }
            finally
            {
                handle.Free();
            }

            SKImage image = SKImage.FromPixelCopy(info, pixels, info.RowBytes);
            ImageCache.Add(bmp, image);
            return image;
        }
    }
}
