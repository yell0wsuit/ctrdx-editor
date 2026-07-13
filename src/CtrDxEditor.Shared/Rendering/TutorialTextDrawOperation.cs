using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Media;
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

        /// <summary>Line advance including the game's five-pixel line spacing.</summary>
        public const double LineAdvanceLevel = FontSizeLevel + (5.0 / 3.0);

        /// <summary>The game's 25-pixel top spacing converted to level units.</summary>
        public const double TopSpacingLevel = 25.0 / 3.0;

        private static SKTypeface? TypefaceCache { get; set; }

        private static float? EmSizeCache { get; set; }

        /// <summary>Returns the gooddog typeface, or Skia's default when the font asset is unavailable.</summary>
        /// <param name="sprites">Sprite cache whose platform content store supplies the font bytes.</param>
        /// <returns>The cached gooddog or fallback typeface.</returns>
        public static SKTypeface GetTypeface(SpriteCache sprites)
        {
            if (TypefaceCache is not null)
            {
                return TypefaceCache;
            }

            try
            {
                byte[] bytes = sprites.ReadContentBytes("fonts/gooddog_new-webfont.ttf");
                using SKData data = SKData.CreateCopy(bytes);
                TypefaceCache = SKTypeface.FromData(data);
            }
            catch (Exception)
            {
                TypefaceCache = null;
            }

            TypefaceCache ??= SKTypeface.Default;
            return TypefaceCache;
        }

        /// <summary>
        /// Builds an <see cref="SKFont"/> whose pixel height (descent − ascent) equals
        /// <see cref="FontSizeLevel"/> level units, matching the game's FontStashSharp pixel-height sizing.
        /// SKFont.Size is the em size, so we scale it by the font's (pixel height / em) ratio, probed once.
        /// </summary>
        /// <param name="sprites">Sprite cache supplying the typeface.</param>
        /// <returns>A font sized to the game's glyph pixel height.</returns>
        public static SKFont CreateFont(SpriteCache sprites)
        {
            SKTypeface typeface = GetTypeface(sprites);
            if (EmSizeCache is null)
            {
                using SKFont probe = new(typeface, 100f);
                SKFontMetrics metrics = probe.Metrics;
                float heightPer100 = metrics.Descent - metrics.Ascent;
                EmSizeCache = heightPer100 > 0
                    ? (float)(FontSizeLevel * 100f / heightPer100)
                    : (float)FontSizeLevel;
            }

            return new SKFont(typeface, EmSizeCache.Value);
        }
    }

    /// <summary>
    /// Draws tutorial text from the game's top-left wrap-box origin in the gooddog font, greedily wrapped
    /// to the authored width. Text is white on the dark blank canvas and black otherwise. A non-Skia
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
        bool dark)
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
            using SKFont font = TutorialFont.CreateFont(sprites);
            using SKPaint paint = new()
            {
                IsAntialias = true,
                Color = dark ? SKColors.White : SKColors.Black,
            };

            IReadOnlyList<string> lines = TutorialTextLayout.Wrap(text, widthLevel, s => font.MeasureText(s));
            if (lines.Count == 0)
            {
                return;
            }

            int save = canvas.Save();
            canvas.Translate((float)view.PanX, (float)view.PanY);
            canvas.Scale((float)view.Zoom);

            SKFontMetrics metrics = font.Metrics;
            double firstBaseline = originY + TutorialFont.TopSpacingLevel - metrics.Ascent;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                float lineWidth = font.MeasureText(line);
                float x = (float)(originX + ((widthLevel - lineWidth) / 2));
                float y = (float)(firstBaseline + (i * TutorialFont.LineAdvanceLevel));
                canvas.DrawText(line, x, y, font, paint);
            }

            canvas.RestoreToCount(save);
        }
    }
}
