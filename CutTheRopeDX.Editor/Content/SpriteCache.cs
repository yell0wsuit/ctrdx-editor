using System;
using System.Collections.Generic;
using System.IO;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using CutTheRopeDX.Editor.Core.Atlas;
using CutTheRopeDX.Editor.Core.Editing;
using CutTheRopeDX.Editor.Core.Geometry;

namespace CutTheRopeDX.Editor.Content
{
    /// <summary>One resolved layer ready to draw: its atlas bitmap and the frame within it.</summary>
    public readonly record struct SpriteLayerDraw(Bitmap Bitmap, AtlasFrame Frame);

    /// <summary>A fully resolved, composited object sprite: ordered layers plus per-object scale.</summary>
    public sealed record ObjectSprite(IReadOnlyList<SpriteLayerDraw> Layers, double Scale);

    public sealed class SpriteCache(string contentRoot)
    {
        private readonly Dictionary<string, Bitmap> _bitmaps = [];
        private readonly Dictionary<string, Atlas> _atlases = [];
        private readonly Dictionary<string, Bitmap?> _thumbnails = [];

        /// <summary>A small composited preview of an object's sprite, for the palette. Cached per element.</summary>
        public Bitmap? GetThumbnail(string element)
        {
            if (_thumbnails.TryGetValue(element, out Bitmap? cached))
            {
                return cached;
            }

            Bitmap? thumb = BuildThumbnail(element);
            _thumbnails[element] = thumb;
            return thumb;
        }

        private Bitmap? BuildThumbnail(string element)
        {
            ObjectSprite? sprite = GetSprite(element);
            if (sprite is null || sprite.Layers.Count == 0)
            {
                return null;
            }

            // Lay the layers out in pixel space (mapScale 1) centered at the origin, then take the union
            // of their drawn rects so the preview is cropped to the visible art.
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (SpriteLayerDraw layer in sprite.Layers)
            {
                LevelBounds d = SpritePlacement.Compute(layer.Frame, 0, 0, sprite.Scale, mapScale: 1.0).Dest;
                minX = Math.Min(minX, d.X);
                minY = Math.Min(minY, d.Y);
                maxX = Math.Max(maxX, d.X + d.W);
                maxY = Math.Max(maxY, d.Y + d.H);
            }

            double w = maxX - minX, h = maxY - minY;
            if (w <= 0 || h <= 0)
            {
                return null;
            }

            const double maxDim = 32.0;
            double f = Math.Min(1.0, maxDim / Math.Max(w, h));
            PixelSize size = new(Math.Max(1, (int)Math.Ceiling(w * f)), Math.Max(1, (int)Math.Ceiling(h * f)));

            RenderTargetBitmap rtb = new(size, new Vector(96, 96));
            using (DrawingContext ctx = rtb.CreateDrawingContext())
            {
                foreach (SpriteLayerDraw layer in sprite.Layers)
                {
                    SpriteLayout layout = SpritePlacement.Compute(layer.Frame, 0, 0, sprite.Scale, mapScale: 1.0);
                    Rect src = new(layout.Source.X, layout.Source.Y, layout.Source.W, layout.Source.H);
                    Rect dst = new(
                        (layout.Dest.X - minX) * f, (layout.Dest.Y - minY) * f,
                        layout.Dest.W * f, layout.Dest.H * f);
                    ctx.DrawImage(layer.Bitmap, src, dst);
                }
            }
            return rtb;
        }

        public ObjectSprite? GetSprite(string element)
        {
            VisualDescriptor? v = VisualDescriptorMap.For(element);
            if (v is null)
            {
                return null;
            }

            List<SpriteLayerDraw> layers = new(v.Layers.Count);
            foreach (SpriteLayer layer in v.Layers)
            {
                Bitmap bitmap = LoadBitmap(layer.AtlasPngRelPath);
                AtlasFrame? frame = LoadAtlas(layer.AtlasJsonRelPath).Find(layer.FrameName);
                if (frame is not null)
                {
                    layers.Add(new SpriteLayerDraw(bitmap, frame));
                }
            }

            return layers.Count == 0 ? null : new ObjectSprite(layers, v.Scale);
        }

        private Bitmap LoadBitmap(string relPath)
        {
            if (!_bitmaps.TryGetValue(relPath, out Bitmap? bmp))
            {
                bmp = new Bitmap(Path.Combine(contentRoot, relPath));
                _bitmaps[relPath] = bmp;
            }
            return bmp;
        }

        private Atlas LoadAtlas(string relPath)
        {
            if (!_atlases.TryGetValue(relPath, out Atlas? atlas))
            {
                string json = File.ReadAllText(Path.Combine(contentRoot, relPath));
                atlas = new Atlas(AtlasJsonLoader.ParseFrames(json));
                _atlases[relPath] = atlas;
            }
            return atlas;
        }
    }
}
