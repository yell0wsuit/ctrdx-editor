using System.Globalization;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Draws tutorial icons and text and computes their selection bounds.</summary>
    internal static class TutorialRenderer
    {
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
            if (TutorialObject.ShouldInvert(quad, dark))
            {
                SpriteLayout layout = SpritePlacement.Compute(layer.Frame, obj.X, obj.Y, sprite.Scale);
                Rect destination = new(layout.Dest.X, layout.Dest.Y, layout.Dest.W, layout.Dest.H);
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
                LevelSceneRenderer.DrawSpriteLayerPublic(
                    ctx,
                    view,
                    layer,
                    obj.X,
                    obj.Y,
                    sprite.Scale,
                    angle == 0 ? null : angle);
            }
        }

        /// <summary>Draws centered tutorial text, white on the dark canvas and black otherwise.</summary>
        public static void DrawText(
            DrawingContext ctx,
            ViewTransform view,
            LevelObject obj,
            Rect operationBounds,
            bool dark)
        {
            string text = obj.GetAttr("text") ?? string.Empty;
            double width = ParseDouble(obj.GetAttr("width"), TutorialObject.DefaultTextWidth);
            ctx.Custom(new TutorialTextDrawOperation(
                operationBounds,
                view,
                text,
                obj.X,
                obj.Y,
                width,
                dark));
        }

        /// <summary>Returns the trimmed sprite bounds for a tutorial icon.</summary>
        public static LevelBounds IconBounds(SpriteCache sprites, LevelObject obj)
        {
            return sprites.GetSprite(obj.Type) is { Layers.Count: > 0 } sprite
                ? SpritePlacement.Compute(sprite.Layers[0].Frame, obj.X, obj.Y, sprite.Scale).Dest
                : new LevelBounds(obj.X - 8, obj.Y - 8, 16, 16);
        }

        /// <summary>Returns a one-line, authored-width selection box centered on tutorial text.</summary>
        public static LevelBounds TextBounds(LevelObject obj)
        {
            double width = ParseDouble(obj.GetAttr("width"), TutorialObject.DefaultTextWidth);
            double height = TutorialFont.LineAdvanceLevel;
            return new LevelBounds(obj.X - (width / 2), obj.Y - (height / 2), width, height);
        }

        private static double ParseDouble(string? value, double fallback)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                ? parsed
                : fallback;
        }
    }
}
