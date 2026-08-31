using System;
using System.Collections.Generic;
using System.IO;
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
    /// Renders a chain rope's links through SkiaSharp, reproducing the game's <c>Bungee.DrawChain</c>:
    /// the same link art at every planned point, rotated onto the curve and multiplied by that link's
    /// tint (half the links stay white, the rest take a grey shade).
    /// </summary>
    /// <remarks>
    /// This needs its own draw operation for the same reason the glow does: the tint is a per-sprite
    /// colour multiply, which Avalonia's <see cref="DrawingContext"/> cannot express - pushing opacity
    /// instead would fade a link toward the background rather than darken it. Skia's modulate colour
    /// filter is exactly the game's vertex-colour multiply, and folding the caller's opacity into that
    /// filter's alpha keeps the pale "invisible grab" rendering working. On a non-Skia backend the
    /// chain is simply not drawn, like the cord strips above it.
    /// </remarks>
    /// <param name="bounds">Control bounds for the custom draw operation.</param>
    /// <param name="view">Level-to-screen transform, applied on the Skia canvas so links zoom with the art.</param>
    /// <param name="atlas">The chain atlas bitmap; both quads come from it.</param>
    /// <param name="linkFrame">Atlas rect of the link drawn at each sampled point.</param>
    /// <param name="midpointFrame">Atlas rect of the link drawn between two samples.</param>
    /// <param name="links">The planned links, in draw order.</param>
    /// <param name="opacity">Alpha multiplier applied to every link.</param>
    internal sealed class ChainDrawOperation(
        Rect bounds,
        ViewTransform view,
        Bitmap atlas,
        IntRect linkFrame,
        IntRect midpointFrame,
        IReadOnlyList<ChainSprite> links,
        double opacity)
        : ICustomDrawOperation
    {
        // One SKImage per atlas bitmap for the process lifetime, keyed weakly so it is collected with
        // the bitmap - the same caching the glow operation uses, and for the same reason: the atlas is
        // already decoded, so decoding it again per frame would be pure waste.
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
            SKSamplingOptions sampling = new(SKFilterMode.Linear);

            int save = canvas.Save();
            canvas.Translate((float)view.PanX, (float)view.PanY);
            canvas.Scale((float)view.Zoom);

            foreach (ChainSprite sprite in links)
            {
                IntRect frame = sprite.QuadIndex == ChainSpritePlanner.MidpointQuad ? midpointFrame : linkFrame;

                // Atlas pixels are level units scaled by MapScale, exactly as for every other sprite.
                float halfW = (float)(frame.W / SpritePlacement.MapScale / 2);
                float halfH = (float)(frame.H / SpritePlacement.MapScale / 2);

                using SKPaint paint = new()
                {
                    IsAntialias = true,
                    ColorFilter = SKColorFilter.CreateBlendMode(Tint(sprite.Tint, opacity), SKBlendMode.Modulate),
                };

                int linkSave = canvas.Save();
                canvas.Translate((float)sprite.Center.X, (float)sprite.Center.Y);
                canvas.RotateRadians((float)sprite.Rotation);
                canvas.DrawImage(
                    image,
                    new SKRect(frame.X, frame.Y, frame.X + frame.W, frame.Y + frame.H),
                    new SKRect(-halfW, -halfH, halfW, halfH),
                    sampling,
                    paint);
                canvas.RestoreToCount(linkSave);
            }

            canvas.RestoreToCount(save);
        }

        // The game multiplies the sprite by a straight RGBA vertex colour; modulate against a
        // premultiplied-alpha image needs the colour premultiplied too, or a tinted link at reduced
        // opacity would keep more colour than alpha and fringe.
        private static SKColor Tint(RopeRgba tint, double opacity)
        {
            double alpha = Math.Clamp(tint.A * opacity, 0, 1);
            return new SKColor(
                Channel(tint.R * alpha),
                Channel(tint.G * alpha),
                Channel(tint.B * alpha),
                Channel(alpha));
        }

        private static byte Channel(double value)
        {
            return (byte)Math.Round(Math.Clamp(value, 0, 1) * 255);
        }

        // Hands the atlas to Skia as encoded PNG rather than reinterpreting its raw bytes. Copying the
        // pixels out means naming a channel order, and Avalonia's decoded order is not ours to assume -
        // guessing BGRA when it hands back RGBA swapped red and blue, which turned this blue-grey chain
        // copper. Encoding once per atlas costs a decode the cache then keeps for the process lifetime,
        // and cannot get the channels wrong.
        private static SKImage ImageFor(Bitmap bmp)
        {
            if (ImageCache.TryGetValue(bmp, out SKImage? cached))
            {
                return cached;
            }

            using MemoryStream stream = new();
            bmp.Save(stream, new PngBitmapEncoderOptions());
            stream.Position = 0;
            SKImage image = SKImage.FromEncodedData(stream);
            ImageCache.Add(bmp, image);
            return image;
        }
    }
}
