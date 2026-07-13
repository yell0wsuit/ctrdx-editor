using System.Globalization;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using SkiaSharp;

namespace CtrDxEditor.Rendering
{
    /// <summary>Draws tutorial icons and text and computes their selection bounds.</summary>
    internal static class TutorialRenderer
    {
        private const double MinimumIconSelectionSize = 16.0;

        /// <summary>Draws a tutorial icon, inverted on the dark canvas unless it is a color quad.</summary>
        public static void DrawIcon(
            DrawingContext ctx,
            ViewTransform view,
            SpriteCache sprites,
            LevelObject obj,
            Rect operationBounds,
            bool dark)
        {
            if (sprites.GetSprite(obj.Type) is not { Layers.Count: > 0 } sprite)
            {
                return;
            }

            SpriteLayerDraw layer = sprite.Layers[0];
            double angle = RotationTable.For(obj.Type) is { } spec
                ? ObjectRotation.DisplayDegrees(obj, spec)
                : 0.0;

            int quad = TutorialObject.Icon(obj);
            LevelBounds artBounds = IconArtBounds(layer, obj.X, obj.Y, sprite.Scale);
            if (TutorialObject.ShouldInvert(quad, dark))
            {
                Rect destination = new(artBounds.X, artBounds.Y, artBounds.W, artBounds.H);
                ctx.Custom(new TutorialInvertDrawOperation(
                    operationBounds,
                    view,
                    layer.Bitmap,
                    layer.Frame.Frame,
                    destination,
                    angle));
            }
            else
            {
                DrawIconLayer(ctx, view, layer, artBounds, angle);
            }
        }

        /// <summary>Draws game-aligned tutorial text, white on the dark canvas and black otherwise.</summary>
        public static void DrawText(
            DrawingContext ctx,
            ViewTransform view,
            SpriteCache sprites,
            LevelObject obj,
            Rect operationBounds,
            bool dark)
        {
            string text = obj.GetAttr("text") ?? string.Empty;
            double width = ParseDouble(obj.GetAttr("width"), TutorialObject.DefaultTextWidth);
            ctx.Custom(new TutorialTextDrawOperation(
                operationBounds,
                view,
                sprites,
                text,
                obj.X,
                obj.Y,
                width,
                dark));
        }

        /// <summary>
        /// Returns the visible tutorial icon bounds positioned from its <c>spriteSourceSize</c>. Tutorial
        /// quads share a very large untrimmed <c>sourceSize</c> canvas, so using the untrimmed hit region
        /// would create a selection box hundreds of level units larger than the actual icon.
        /// </summary>
        public static LevelBounds IconBounds(SpriteCache sprites, LevelObject obj)
        {
            if (sprites.GetSprite(obj.Type) is not { Layers.Count: > 0 } sprite)
            {
                return new LevelBounds(obj.X - 8, obj.Y - 8, 16, 16);
            }

            LevelBounds art = IconArtBounds(sprite.Layers[0], obj.X, obj.Y, sprite.Scale);
            double width = System.Math.Max(art.W, MinimumIconSelectionSize);
            double height = System.Math.Max(art.H, MinimumIconSelectionSize);
            return new LevelBounds(obj.X - (width / 2), obj.Y - (height / 2), width, height);
        }

        /// <summary>Returns the game's top-left-anchored, authored-width tutorial text box.</summary>
        public static LevelBounds TextBounds(SpriteCache sprites, LevelObject obj)
        {
            double width = ParseDouble(obj.GetAttr("width"), TutorialObject.DefaultTextWidth);
            string text = obj.GetAttr("text") ?? string.Empty;
            using SKFont font = TutorialFont.CreateFont(sprites);
            int lineCount = TutorialTextLayout.Wrap(text, width, value => font.MeasureText(value)).Count;
            double height = lineCount > 0
                ? ((lineCount - 1) * TutorialFont.LineAdvanceLevel)
                    + TutorialFont.FontSizeLevel
                    + TutorialFont.TopSpacingLevel
                : TutorialFont.FontSizeLevel + TutorialFont.TopSpacingLevel;
            return new LevelBounds(obj.X, obj.Y, width, height);
        }

        /// <summary>Measures the widest line of <paramref name="text"/> in level units with the gooddog font.</summary>
        public static double MeasureTextWidth(SpriteCache sprites, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            using SKFont font = TutorialFont.CreateFont(sprites);
            double max = 0;
            foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
            {
                max = System.Math.Max(max, font.MeasureText(line));
            }

            return max;
        }

        /// <summary>
        /// When the tutorial text is in auto-width mode, syncs its <c>width</c> attribute to the measured
        /// text so the box fits exactly (no wrapping) and the game renders the same single lines. No-op for
        /// a fixed (manual) width.
        /// </summary>
        public static void ApplyAutoWidth(SpriteCache sprites, LevelObject obj)
        {
            if (!TutorialObject.IsAutoWidth(obj))
            {
                return;
            }

            int width = System.Math.Max(1, (int)System.Math.Ceiling(MeasureTextWidth(sprites, obj.GetAttr("text") ?? string.Empty)));
            obj.SetAttr("width", width.ToString(CultureInfo.InvariantCulture));
        }

        private static LevelBounds IconArtBounds(SpriteLayerDraw layer, double x, double y, double scale)
        {
            double normalizedScale = scale / SpritePlacement.MapScale;
            double width = layer.Frame.SpriteSource.W * normalizedScale;
            double height = layer.Frame.SpriteSource.H * normalizedScale;
            return new LevelBounds(x - (width / 2), y - (height / 2), width, height);
        }

        private static void DrawIconLayer(
            DrawingContext ctx,
            ViewTransform view,
            SpriteLayerDraw layer,
            LevelBounds artBounds,
            double angle)
        {
            Rect source = new(
                layer.Frame.Frame.X,
                layer.Frame.Frame.Y,
                layer.Frame.Frame.W,
                layer.Frame.Frame.H);
            Vec2 topLeft = view.LevelToScreen(new Vec2(artBounds.X, artBounds.Y));
            Vec2 bottomRight = view.LevelToScreen(new Vec2(
                artBounds.X + artBounds.W,
                artBounds.Y + artBounds.H));
            Rect destination = new(
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y);

            if (angle == 0)
            {
                ctx.DrawImage(layer.Bitmap, source, destination);
                return;
            }

            Vec2 center = view.LevelToScreen(new Vec2(
                artBounds.X + (artBounds.W / 2),
                artBounds.Y + (artBounds.H / 2)));
            Matrix transform = Matrix.CreateTranslation(-center.X, -center.Y)
                * Matrix.CreateRotation(angle * System.Math.PI / 180.0)
                * Matrix.CreateTranslation(center.X, center.Y);
            using (ctx.PushTransform(transform))
            {
                ctx.DrawImage(layer.Bitmap, source, destination);
            }
        }

        private static double ParseDouble(string? value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                ? parsed
                : fallback;
        }
    }
}
