using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Content
{
    /// <summary>One resolved layer ready to draw: its atlas bitmap and the frame within it.</summary>
    public readonly record struct SpriteLayerDraw(Bitmap Bitmap, AtlasFrame Frame);

    /// <summary>
    /// A fully resolved, composited object sprite: ordered layers plus per-object scale.
    /// <paramref name="Variants"/> are the resolved decorative back-layer choices (one is drawn per
    /// placed instance, behind <paramref name="Layers"/>); empty for objects without variants.
    /// </summary>
    public sealed record ObjectSprite(
        IReadOnlyList<SpriteLayerDraw> Layers,
        double Scale,
        IReadOnlyList<SpriteLayerDraw>? Variants = null)
    {
        /// <summary>Resolved decorative back-layer variants; empty when the object has none.</summary>
        public IReadOnlyList<SpriteLayerDraw> Variants { get; init; } = Variants ?? [];
    }

    /// <summary>The rope Christmas lights atlas: its bitmap and the light bulb frames.</summary>
    public sealed record ChristmasLightsArt(Bitmap Bitmap, IReadOnlyList<AtlasFrame> Frames);

    /// <summary>Reads sprite atlases from a preloaded platform content store.</summary>
    public sealed class SpriteCache(IContentStore store, string imageExtension = ".png")
    {
        private const string XmasLightsJson = "images/christmas_lights.json";
        private const string XmasLightsImageBase = "images/christmas_lights";

        private readonly Dictionary<string, Bitmap> _bitmaps = [];
        private readonly Dictionary<string, Atlas> _atlases = [];
        private readonly Dictionary<string, Bitmap?> _thumbnails = [];

        /// <summary>Creates a sprite cache for a desktop content folder.</summary>
        public SpriteCache(string contentRoot)
            : this(new FolderContentStore(contentRoot))
        {
            PreloadAsync().GetAwaiter().GetResult();
        }

        /// <summary>Loads every statically-known atlas image and frame table into memory once.</summary>
        public async Task PreloadAsync()
        {
            foreach (VisualDescriptor v in VisualDescriptorMap.ByElement.Values)
            {
                foreach (SpriteLayer layer in AllLayers(v))
                {
                    string imagePath = layer.AtlasImageBasePath + imageExtension;
                    if (!_bitmaps.ContainsKey(imagePath))
                    {
                        byte[] bytes = await store.ReadBytesAsync(imagePath);
                        using MemoryStream ms = new(bytes);
                        _bitmaps[imagePath] = new Bitmap(ms);
                    }
                    if (!_atlases.ContainsKey(layer.AtlasJsonRelPath))
                    {
                        string json = await store.ReadTextAsync(layer.AtlasJsonRelPath);
                        _atlases[layer.AtlasJsonRelPath] = new Atlas(AtlasJsonLoader.ParseFrames(json));
                    }
                }
            }

            // Seasonal rope lights. Optional: a bundle without the atlas just renders
            // ropes bare, so a load failure must not break the whole preload.
            try
            {
                string imagePath = XmasLightsImageBase + imageExtension;
                if (!_bitmaps.ContainsKey(imagePath))
                {
                    byte[] bytes = await store.ReadBytesAsync(imagePath);
                    using MemoryStream ms = new(bytes);
                    _bitmaps[imagePath] = new Bitmap(ms);
                }
                if (!_atlases.ContainsKey(XmasLightsJson))
                {
                    string json = await store.ReadTextAsync(XmasLightsJson);
                    _atlases[XmasLightsJson] = new Atlas(AtlasJsonLoader.ParseFrames(json));
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>The rope Christmas lights art, or null when the bundle doesn't include it.</summary>
        public ChristmasLightsArt? GetChristmasLights()
        {
            Bitmap? bitmap = LoadBitmap(XmasLightsImageBase + imageExtension);
            Atlas? atlas = LoadAtlas(XmasLightsJson);
            return bitmap is null || atlas is null || atlas.Frames.Count == 0
                ? null
                : new ChristmasLightsArt(bitmap, atlas.Frames);
        }

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

        private RenderTargetBitmap? BuildThumbnail(string element)
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

        /// <summary>Returns the resolved sprite layers for an object element, or null when unavailable.</summary>
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
                Bitmap? bitmap = LoadBitmap(layer.AtlasImageBasePath + imageExtension);
                AtlasFrame? frame = LoadAtlas(layer.AtlasJsonRelPath)?.Find(layer.FrameName);
                if (bitmap is not null && frame is not null)
                {
                    layers.Add(new SpriteLayerDraw(bitmap, frame));
                }
            }

            List<SpriteLayerDraw> variants = new(v.RandomBackLayers.Count);
            foreach (SpriteLayer layer in v.RandomBackLayers)
            {
                Bitmap? bitmap = LoadBitmap(layer.AtlasImageBasePath + imageExtension);
                AtlasFrame? frame = LoadAtlas(layer.AtlasJsonRelPath)?.Find(layer.FrameName);
                if (bitmap is not null && frame is not null)
                {
                    variants.Add(new SpriteLayerDraw(bitmap, frame));
                }
            }

            return layers.Count == 0 ? null : new ObjectSprite(layers, v.Scale, variants);
        }

        private static IEnumerable<SpriteLayer> AllLayers(VisualDescriptor descriptor)
        {
            foreach (SpriteLayer layer in descriptor.Layers)
            {
                yield return layer;
            }
            foreach (SpriteLayer layer in descriptor.RandomBackLayers)
            {
                yield return layer;
            }
        }

        private Bitmap? LoadBitmap(string relPath)
        {
            return _bitmaps.TryGetValue(relPath, out Bitmap? bmp) ? bmp : null;
        }

        private Atlas? LoadAtlas(string relPath)
        {
            return _atlases.TryGetValue(relPath, out Atlas? atlas) ? atlas : null;
        }
    }
}
