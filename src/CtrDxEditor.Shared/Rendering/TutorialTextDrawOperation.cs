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
        /// <summary>Game SmallFont size 72 game pixels divided by map scale 3.</summary>
        public const double FontSizeLevel = 24.0;

        /// <summary>Line advance including the game's five-pixel line spacing.</summary>
        public const double LineAdvanceLevel = FontSizeLevel + (5.0 / 3.0);

        /// <summary>The game's 25-pixel top spacing converted to level units.</summary>
        public const double TopSpacingLevel = 25.0 / 3.0;

        private static SKTypeface? TypefaceCache { get; set; }

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
            using SKFont font = new(TutorialFont.GetTypeface(sprites), (float)TutorialFont.FontSizeLevel);
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
