using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using SkiaSharp;

namespace CtrDxEditor.Rendering
{
    /// <summary>Lazily loaded gooddog typeface used by the game tutorial font and its level-space metrics.</summary>
    internal static class TutorialFont
    {
        /// <summary>
        /// Tutorial glyph pixel height in level units: the game SmallFont's 72-px height / map scale 3.
        /// The game renders with FontStashSharp, whose size is the glyph pixel height (stb_truetype's
        /// ScaleForPixelHeight = ascent − descent), not the em size. <see cref="CreateFont"/> sizes the
        /// SKFont to this pixel height so the editor matches the game rather than rendering ~30% larger
        /// (SKFont.Size is the em size, and gooddog's em is only ~76% of its ascent−descent height).
        /// </summary>
        public const double FontSizeLevel = 24.0;

        private const double InterFallbackScale = 0.75;

        /// <summary>Line advance including the game's five-pixel line spacing.</summary>
        public const double LineAdvanceLevel = FontSizeLevel + (5.0 / 3.0);

        /// <summary>The game's 25-pixel top spacing converted to level units.</summary>
        public const double TopSpacingLevel = 25.0 / 3.0;

        private static readonly Lock TypefaceCacheLock = new();

        private static TypefaceSelection? TypefaceCache { get; set; }

        private sealed class TypefaceSelection(SKTypeface typeface, bool usesInterFallback)
        {
            public SKTypeface Typeface { get; } = typeface;

            public bool UsesInterFallback { get; } = usesInterFallback;

            public float? EmSize { get; set; }
        }

        /// <summary>Returns the gooddog typeface, or Inter when the game font asset is unavailable.</summary>
        /// <param name="sprites">Sprite cache whose platform content store supplies the font bytes.</param>
        /// <returns>The cached gooddog or fallback typeface.</returns>
        public static SKTypeface GetTypeface(SpriteCache sprites)
        {
            return GetTypefaceSelection(sprites).Typeface;
        }

        private static TypefaceSelection GetTypefaceSelection(SpriteCache sprites)
        {
            lock (TypefaceCacheLock)
            {
                if (TypefaceCache is not null)
                {
                    return TypefaceCache;
                }

                SKTypeface? typeface = null;
                try
                {
                    byte[] bytes = sprites.ReadContentBytes("fonts/gooddog_new-webfont.ttf");
                    using SKData data = SKData.CreateCopy(bytes);
                    typeface = SKTypeface.FromData(data);
                }
                catch (Exception)
                {
                    typeface = null;
                }

                TypefaceCache = typeface is not null
                    ? new TypefaceSelection(typeface, usesInterFallback: false)
                    : new TypefaceSelection(ResolveDefaultTypeface(), usesInterFallback: true);

                return TypefaceCache;
            }
        }

        /// <summary>App-owned Inter regular face, embedded in the shared assembly for every backend.</summary>
        private static readonly Uri InterFontUri =
            new("avares://CtrDxEditor.Shared/Assets/Fonts/Inter/Inter-Regular.ttf");

        /// <summary>
        /// Resolves Inter as an <see cref="SKTypeface"/> for when gooddog is absent, so fallback text
        /// matches the editor on every platform. The asset stream is copied to managed bytes before it is
        /// passed to Skia because package-resource streams are not consistently seekable in browser WASM.
        /// </summary>
        private static SKTypeface ResolveDefaultTypeface()
        {
            StandardAssetLoader assetLoader = new();
            return ResolveDefaultTypeface(() => assetLoader.Open(InterFontUri));
        }

        private static SKTypeface ResolveDefaultTypeface(Func<Stream> openInterFont)
        {
            using Stream stream = openInterFont();
            using MemoryStream buffer = new();
            stream.CopyTo(buffer);
            using SKData data = SKData.CreateCopy(buffer.ToArray());
            return SKTypeface.FromData(data)
                ?? throw new InvalidOperationException("Could not decode the editor's bundled Inter font.");
        }

        /// <summary>
        /// Builds an <see cref="SKFont"/> whose pixel height (descent − ascent) matches the game's
        /// <see cref="FontSizeLevel"/> when gooddog is available. Inter fallback text uses 90% of that
        /// height for a closer visual match. SKFont.Size is the em size, so it is scaled by the font's
        /// pixel-height-to-em ratio, probed once.
        /// </summary>
        /// <param name="sprites">Sprite cache supplying the typeface.</param>
        /// <param name="sizeScale">
        /// Authored <c>size</c> multiplier. The game rasterizes a genuinely bigger face rather than
        /// stretching glyphs drawn at the base size, so this scales the em size actually requested from
        /// Skia instead of scaling a fixed-size result afterward.
        /// </param>
        /// <returns>A font sized for the selected gooddog or Inter typeface.</returns>
        public static SKFont CreateFont(SpriteCache sprites, double sizeScale = 1.0)
        {
            TypefaceSelection selection = GetTypefaceSelection(sprites);
            lock (TypefaceCacheLock)
            {
                if (selection.EmSize is null)
                {
                    using SKFont probe = new(selection.Typeface, 100f);
                    SKFontMetrics metrics = probe.Metrics;
                    float heightPer100 = metrics.Descent - metrics.Ascent;
                    double pixelHeight = TargetPixelHeight(selection.UsesInterFallback);
                    selection.EmSize = heightPer100 > 0
                        ? (float)(pixelHeight * 100f / heightPer100)
                        : (float)pixelHeight;
                }

                return new SKFont(selection.Typeface, selection.EmSize.Value * (float)sizeScale);
            }
        }

        private static double TargetPixelHeight(bool usesInterFallback)
        {
            return usesInterFallback ? FontSizeLevel * InterFallbackScale : FontSizeLevel;
        }
    }

    /// <summary>
    /// Shared wrap-box height arithmetic so painting, hit-testing and auto-width all size the same box
    /// for the same wrapped line count. <see cref="TutorialFont.TopSpacingLevel"/> is added unscaled,
    /// mirroring the game's own <c>GetTopSpacing()</c>, which is likewise added after the size multiplier
    /// rather than scaled by it.
    /// </summary>
    internal static class TutorialTextMetrics
    {
        /// <summary>Total level-space height of a wrapped tutorial text box at <paramref name="look"/>'s scale.</summary>
        /// <param name="lineCount">Number of wrapped lines, as returned by <see cref="TutorialTextLayout.Wrap"/>.</param>
        /// <param name="look">The prompt's authored size and line-height multipliers.</param>
        /// <returns>The box height in level units.</returns>
        public static double HeightLevel(int lineCount, TutorialLook look)
        {
            double fontHeight = TutorialFont.FontSizeLevel * look.Size;
            double lineAdvance = TutorialFont.LineAdvanceLevel * look.Size * look.LineHeight;
            return lineCount > 0
                ? ((lineCount - 1) * lineAdvance) + fontHeight + TutorialFont.TopSpacingLevel
                : fontHeight + TutorialFont.TopSpacingLevel;
        }
    }

    /// <summary>
    /// Draws tutorial text from the game's top-left wrap-box origin in the gooddog font, greedily wrapped
    /// to the authored width. An authored <c>color</c> supersedes the dark-canvas invert (white on dark,
    /// black otherwise); <c>size</c> and <c>lineHeight</c> re-wrap and re-space the lines rather than just
    /// stretching glyphs, matching the game's own re-wrap once those multipliers are known. A non-Skia
    /// backend draws nothing.
    /// </summary>
    internal sealed class TutorialTextDrawOperation(
        Rect bounds,
        ViewTransform view,
        SpriteCache sprites,
        string text,
        double originX,
        double originY,
        double widthLevel,
        bool dark,
        TutorialLook look,
        double alpha = 1.0)
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
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            ISkiaSharpApiLeaseFeature? leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature is null)
            {
                return;
            }

            using ISkiaSharpApiLease lease = leaseFeature.Lease();
            SKCanvas canvas = lease.SkCanvas;

            // A genuinely resized face, not a fixed-size one rescaled after measuring: the game
            // rasterizes text at its own multiplied size for the same reason (crisp glyphs at any scale),
            // and this single font is what both the wrap decision and the painted glyphs measure through,
            // so they cannot disagree about how wide a line is.
            using SKFont font = TutorialFont.CreateFont(sprites, look.Size);
            TutorialColor color = look.EffectiveColor(dark);
            byte a = (byte)Math.Clamp(look.Opacity * alpha * 255.0, 0, 255);
            using SKPaint paint = new()
            {
                IsAntialias = true,
                Color = new SKColor(color.Red, color.Green, color.Blue, a),
            };

            IReadOnlyList<string> lines = TutorialTextLayout.Wrap(text, widthLevel, s => font.MeasureText(s));
            if (lines.Count == 0)
            {
                return;
            }

            int save = canvas.Save();
            canvas.Translate((float)view.PanX, (float)view.PanY);
            canvas.Scale((float)view.Zoom);

            if (look.Angle != 0)
            {
                double heightLevel = TutorialTextMetrics.HeightLevel(lines.Count, look);
                canvas.RotateDegrees(
                    (float)look.Angle,
                    (float)(originX + (widthLevel / 2)),
                    (float)(originY + (heightLevel / 2)));
            }

            SKFontMetrics metrics = font.Metrics;
            double firstBaseline = originY + TutorialFont.TopSpacingLevel - metrics.Ascent;
            double lineAdvance = TutorialFont.LineAdvanceLevel * look.Size * look.LineHeight;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                float lineWidth = font.MeasureText(line);
                float x = (float)(originX + ((widthLevel - lineWidth) / 2));
                float y = (float)(firstBaseline + (i * lineAdvance));
                canvas.DrawText(line, x, y, font, paint);
            }

            canvas.RestoreToCount(save);
        }
    }
}
