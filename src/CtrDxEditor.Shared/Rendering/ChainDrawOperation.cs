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
    /// tint (half the links stay white, the rest take one of three shades).
    /// </summary>
    /// <remarks>
    /// This needs its own draw operation for the same reason the glow does: the tint is a per-vertex
    /// colour multiply, which Avalonia's <see cref="DrawingContext"/> cannot express - pushing opacity
    /// instead would fade a link toward the background rather than darken it. The game's tint is not
    /// even flat across a sprite: a link keeps its fourth corner white, so the shade falls off across
    /// the quad. That is a colour mesh rather than a single multiply, so each sprite is drawn as two
    /// textured triangles whose vertex colours modulate the atlas, exactly as the game's vertex arrays
    /// do. On a non-Skia backend the chain is simply not drawn, like the cord strips above it.
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

        // Bungee.BuildQuadIndices: two triangles per sprite over the game's four-corner order.
        private static readonly ushort[] QuadIndices = [0, 1, 2, 3, 2, 1];

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

            // Vertex texture coordinates address the whole atlas, so one shader covers both quads.
            using SKShader shader = SKShader.CreateImage(
                image, SKShaderTileMode.Clamp, SKShaderTileMode.Clamp, sampling);
            using SKPaint paint = new() { IsAntialias = true, Shader = shader };

            SKPoint[] positions = new SKPoint[4];
            SKPoint[] texs = new SKPoint[4];
            SKColor[] colors = new SKColor[4];

            foreach (ChainSprite sprite in links)
            {
                IntRect frame = sprite.QuadIndex == ChainSpritePlanner.MidpointQuad ? midpointFrame : linkFrame;

                // Atlas pixels are level units scaled by MapScale, exactly as for every other sprite.
                float halfW = (float)(frame.W / SpritePlacement.MapScale / 2);
                float halfH = (float)(frame.H / SpritePlacement.MapScale / 2);

                // The game's corner order, so its fourth (shaded) corner lands where it does there.
                positions[0] = new SKPoint(-halfW, -halfH);
                positions[1] = new SKPoint(halfW, -halfH);
                positions[2] = new SKPoint(-halfW, halfH);
                positions[3] = new SKPoint(halfW, halfH);

                texs[0] = new SKPoint(frame.X, frame.Y);
                texs[1] = new SKPoint(frame.X + frame.W, frame.Y);
                texs[2] = new SKPoint(frame.X, frame.Y + frame.H);
                texs[3] = new SKPoint(frame.X + frame.W, frame.Y + frame.H);

                SKColor tint = Tint(sprite.Tint, opacity);
                colors[0] = tint;
                colors[1] = tint;
                colors[2] = tint;
                colors[3] = Tint(sprite.CornerTint, opacity);

                int linkSave = canvas.Save();
                canvas.Translate((float)sprite.Center.X, (float)sprite.Center.Y);
                canvas.RotateRadians((float)sprite.Rotation);

                // Positions are rotated by the canvas; texs stay in atlas space, which is what lets
                // Skia derive the per-triangle mapping back onto the shader.
                using SKVertices vertices = SKVertices.CreateCopy(
                    SKVertexMode.Triangles, positions, texs, colors, QuadIndices);
                canvas.DrawVertices(vertices, SKBlendMode.Modulate, paint);
                canvas.RestoreToCount(linkSave);
            }

            canvas.RestoreToCount(save);
        }

        // The game multiplies the sprite by a straight RGBA vertex colour; modulating a
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
