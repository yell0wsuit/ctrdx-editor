using System;
using System.Collections.Concurrent;
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
        // Non-default candy skins (index 1..) are loaded on demand, not during preload. Concurrent so an
        // off-thread picker preload and the UI-thread canvas render can both resolve skins without racing.
        private readonly ConcurrentDictionary<int, Bitmap?> _candyBitmaps = new();
        private readonly ConcurrentDictionary<int, Atlas?> _candyAtlases = new();
        // Concurrent so the UI-thread canvas render and an off-thread thumbnail preload can both
        // resolve backgrounds without racing the cache.
        private readonly ConcurrentDictionary<int, Bitmap?> _backgrounds = new();
        private readonly ConcurrentDictionary<int, Bitmap?> _backgroundsP2 = new();
        private readonly ConcurrentDictionary<int, Bitmap?> _backgroundThumbnails = new();

        /// <summary>
        /// Per-background secondary (p2) layer Y offset in internal pixels, from the game's pack config
        /// (<c>boxBackgroundP2Y</c> in ctroriginal_packs.json). Index is the background id (1..17);
        /// only bgr_01..bgr_11 ship a p2 layer, so ids 12..17 are 0 (no p2).
        /// </summary>
        private static readonly int[] BackgroundP2Y =
        [
            0,     // (id 0 unused)
            1120,  // bgr_01
            1044,  // bgr_02
            945,   // bgr_03
            960,   // bgr_04
            780,   // bgr_05
            951,   // bgr_06
            1102,  // bgr_07
            1118,  // bgr_08
            975,   // bgr_09
            991,   // bgr_10
            802,   // bgr_11
        ];

        /// <summary>
        /// The secondary background's Y offset in internal pixels for the given id, or 0 when the
        /// background has no p2 layer (ids &lt;= 0 or 12..17).
        /// </summary>
        public static int GetBackgroundP2Y(int id)
        {
            return id >= 1 && id < BackgroundP2Y.Length ? BackgroundP2Y[id] : 0;
        }

        /// <summary>
        /// The earth decoration's center in internal pixels (the game's <c>earthBgPosition</c>) for the
        /// given background id, or null when it has no earth layer. Only the cosmic box (bgr_08) does.
        /// </summary>
        public static Vec2? GetEarthBgPosition(int id)
        {
            return id == 8 ? new Vec2(1284, 724) : null;
        }

        /// <summary>
        /// The earth decoration art (obj_star_idle quad 23) drawn on top of the cosmic-box background,
        /// or null when the atlas isn't loaded. Center-anchored via <see cref="SpritePlacement"/>.
        /// </summary>
        public SpriteLayerDraw? GetEarthArt()
        {
            Bitmap? bitmap = LoadBitmap("images/obj_star_idle" + imageExtension);
            AtlasFrame? frame = LoadAtlas("images/obj_star_idle.json")?.Find("frame_0058.png");
            return bitmap is null || frame is null ? null : new SpriteLayerDraw(bitmap, frame);
        }

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

        /// <summary>Decode width for the canvas background; the full p1 art (~2560px) is far larger than
        /// needed for a decorative backdrop, so downscaling keeps memory in check.</summary>
        private const int BackgroundDecodeWidth = 1280;

        /// <summary>Decode width for the dialog's background thumbnails - small, since many are held at once.</summary>
        private const int BackgroundThumbnailWidth = 192;

        /// <summary>
        /// Decodes the p1 background image for the given decoration id (1..17 = bgr_01..bgr_17) at
        /// canvas resolution, cached for the process lifetime. Returns null for id &lt;= 0
        /// (Blank/Random-unresolved) or a missing/unreadable file.
        /// </summary>
        public Bitmap? GetBackground(int id)
        {
            return LoadBackground(id, BackgroundDecodeWidth, _backgrounds);
        }

        /// <summary>
        /// Decodes the secondary (p2) background image for the given decoration id, or null when the
        /// background has no p2 layer (see <see cref="GetBackgroundP2Y"/>). p2 is a full-width overlay
        /// the game draws once, near the bottom of tall levels.
        /// </summary>
        public Bitmap? GetBackgroundP2(int id)
        {
            return GetBackgroundP2Y(id) <= 0 ? null : LoadBackground(id, BackgroundDecodeWidth, _backgroundsP2, "_p2");
        }

        /// <summary>
        /// Decodes a small thumbnail of the p1 background for the New Level dialog's picker, cached
        /// separately from the full-size canvas bitmaps. Returns null for id &lt;= 0 or a missing file.
        /// </summary>
        public Bitmap? GetBackgroundThumbnail(int id)
        {
            return LoadBackground(id, BackgroundThumbnailWidth, _backgroundThumbnails);
        }

        /// <remarks>
        /// The <see cref="IContentStore"/> read is dispatched through <c>Task.Run</c> so the awaited
        /// continuation never marshals back to a UI SynchronizationContext. Calling this synchronously
        /// from the UI thread therefore blocks only on the file read/decode rather than deadlocking
        /// against its own continuation.
        /// </remarks>
        private Bitmap? LoadBackground(int id, int decodeWidth, ConcurrentDictionary<int, Bitmap?> cache, string suffix = "_p1")
        {
            if (id <= 0)
            {
                return null;
            }
            if (cache.TryGetValue(id, out Bitmap? cached))
            {
                return cached;
            }

            Bitmap? bmp = null;
            try
            {
                string rel = $"images/backgrounds/bgr_{id:D2}{suffix}.png";
                byte[] bytes = Task.Run(() => store.ReadBytesAsync(rel)).GetAwaiter().GetResult();
                using MemoryStream ms = new(bytes);
                bmp = Bitmap.DecodeToWidth(ms, decodeWidth);
            }
            catch (Exception ex) when (ex is IOException or FileNotFoundException or InvalidOperationException)
            {
                bmp = null;
            }

            cache[id] = bmp;
            return bmp;
        }

        /// <summary>
        /// A small composited preview of an object's sprite, for the palette (and the candy-skin picker).
        /// Cached per (element, candy skin). <paramref name="candySkin"/> only affects candy elements.
        /// Call on the UI thread; warm non-default candy skins first with <see cref="PreloadCandySkin"/>.
        /// </summary>
        public Bitmap? GetThumbnail(string element, int candySkin = 0)
        {
            string key = candySkin == 0 ? element : $"{element}#{candySkin}";
            if (_thumbnails.TryGetValue(key, out Bitmap? cached))
            {
                return cached;
            }

            Bitmap? thumb = BuildThumbnail(element, candySkin);
            _thumbnails[key] = thumb;
            return thumb;
        }

        /// <summary>Smallest source-frame side (px) a layer must have to count as real art in a thumbnail;
        /// below this a frame is a placeholder that would only distort the crop bounds.</summary>
        private const int MinThumbnailFrameSide = 8;

        private RenderTargetBitmap? BuildThumbnail(string element, int candySkin)
        {
            ObjectSprite? sprite = GetSprite(element, candySkin);
            if (sprite is null || sprite.Layers.Count == 0)
            {
                return null;
            }

            // Some skins pad an unused layer with a tiny placeholder frame parked in a corner (e.g. candy
            // skins whose "top" quad is a 3x3 sprite at 0,0). Folding that into the crop bounds would
            // balloon the preview and shove the real art off-center, so drop such layers from the thumbnail.
            List<SpriteLayerDraw> drawn = new(sprite.Layers.Count);
            foreach (SpriteLayerDraw layer in sprite.Layers)
            {
                if (layer.Frame.Frame.W >= MinThumbnailFrameSide && layer.Frame.Frame.H >= MinThumbnailFrameSide)
                {
                    drawn.Add(layer);
                }
            }
            if (drawn.Count == 0)
            {
                drawn.AddRange(sprite.Layers);
            }

            // Lay the layers out in pixel space (mapScale 1) centered at the origin, then take the union
            // of their drawn rects so the preview is cropped to the visible art.
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach (SpriteLayerDraw layer in drawn)
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
                foreach (SpriteLayerDraw layer in drawn)
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

        /// <summary>
        /// Returns the resolved sprite layers for an object element, or null when unavailable. For candy
        /// elements the layers are drawn from the given <paramref name="candySkin"/>'s atlas (resolved by
        /// quad index); the parameter is ignored for every other element.
        /// </summary>
        public ObjectSprite? GetSprite(string element, int candySkin = 0)
        {
            VisualDescriptor? v = VisualDescriptorMap.For(element);
            if (v is null)
            {
                return null;
            }

            // Candy frames are addressed by quad index against the active skin's atlas; everything else
            // resolves against its own preloaded atlas by frame name (or quad, if the layer specifies one).
            bool isCandy = element is "candy" or "candyL" or "candyR";
            (Bitmap? candyBitmap, Atlas? candyAtlas) = isCandy ? LoadCandySkin(candySkin) : (null, null);

            List<SpriteLayerDraw> layers = new(v.Layers.Count);
            foreach (SpriteLayer layer in v.Layers)
            {
                Bitmap? bitmap = isCandy ? candyBitmap : LoadBitmap(layer.AtlasImageBasePath + imageExtension);
                Atlas? atlas = isCandy ? candyAtlas : LoadAtlas(layer.AtlasJsonRelPath);
                AtlasFrame? frame = ResolveFrame(atlas, layer);
                if (bitmap is not null && frame is not null)
                {
                    layers.Add(new SpriteLayerDraw(bitmap, frame));
                }
            }

            List<SpriteLayerDraw> variants = new(v.RandomBackLayers.Count);
            foreach (SpriteLayer layer in v.RandomBackLayers)
            {
                Bitmap? bitmap = LoadBitmap(layer.AtlasImageBasePath + imageExtension);
                AtlasFrame? frame = ResolveFrame(LoadAtlas(layer.AtlasJsonRelPath), layer);
                if (bitmap is not null && frame is not null)
                {
                    variants.Add(new SpriteLayerDraw(bitmap, frame));
                }
            }

            return layers.Count == 0 ? null : new ObjectSprite(layers, v.Scale, variants);
        }

        private static AtlasFrame? ResolveFrame(Atlas? atlas, SpriteLayer layer)
        {
            return layer.Quad is int quad ? atlas?.At(quad) : atlas?.Find(layer.FrameName);
        }

        /// <summary>
        /// Returns the atlas bitmap and frame table for a candy skin, loading and caching the non-default
        /// skins (index &gt;= 1) on first use. Skin 0 uses the atlas preloaded from the candy descriptor.
        /// Safe to call off the UI thread (used to warm the picker's thumbnails).
        /// </summary>
        private (Bitmap? Bitmap, Atlas? Atlas) LoadCandySkin(int skin)
        {
            if (skin <= 0)
            {
                return (LoadBitmap(CandySkins.ResourceBase(0) + imageExtension), LoadAtlas(CandySkins.JsonPath(0)));
            }
            Bitmap? bmp = _candyBitmaps.GetOrAdd(skin, LoadCandyBitmap);
            Atlas? atlas = _candyAtlases.GetOrAdd(skin, LoadCandyAtlas);
            return (bmp, atlas);
        }

        private Bitmap? LoadCandyBitmap(int skin)
        {
            try
            {
                byte[] bytes = Task.Run(() => store.ReadBytesAsync(CandySkins.ResourceBase(skin) + imageExtension))
                    .GetAwaiter().GetResult();
                using MemoryStream ms = new(bytes);
                return new Bitmap(ms);
            }
            catch (Exception ex) when (ex is IOException or FileNotFoundException or InvalidOperationException)
            {
                return null;
            }
        }

        private Atlas? LoadCandyAtlas(int skin)
        {
            try
            {
                string json = Task.Run(() => store.ReadTextAsync(CandySkins.JsonPath(skin))).GetAwaiter().GetResult();
                return new Atlas(AtlasJsonLoader.ParseFrames(json));
            }
            catch (Exception ex) when (ex is IOException or FileNotFoundException or InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Decodes and caches a candy skin's atlas without building a sprite, so callers can warm a skin
        /// off the UI thread before compositing its (UI-thread) thumbnail. No-op for skin 0.
        /// </summary>
        public void PreloadCandySkin(int skin)
        {
            _ = LoadCandySkin(skin);
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
