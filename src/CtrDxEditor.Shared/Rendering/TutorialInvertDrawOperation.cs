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
    /// Draws one tutorial atlas quad through an RGB-invert color filter, with the view transform applied
    /// on the Skia canvas. Used only for line-art tutorial icons on the dark blank canvas.
    /// </summary>
    internal sealed class TutorialInvertDrawOperation(
        Rect bounds,
        ViewTransform view,
        Bitmap atlas,
        IntRect frame,
        Rect destLevel,
        double rotationDegrees)
        : ICustomDrawOperation
    {
        private static readonly float[] InvertMatrix =
        [
            -1, 0, 0, 0, 1,
            0, -1, 0, 0, 1,
            0, 0, -1, 0, 1,
            0, 0, 0, 1, 0,
        ];

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
                ColorFilter = SKColorFilter.CreateColorMatrix(InvertMatrix),
            };
            canvas.DrawImage(image, src, dst, new SKSamplingOptions(SKFilterMode.Linear), paint);
            canvas.RestoreToCount(save);
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
