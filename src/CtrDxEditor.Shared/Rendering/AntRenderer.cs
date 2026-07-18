using System;
using System.Globalization;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Atlas;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Draws deterministic static and elapsed-time ant-conveyor layouts from the ant atlas.</summary>
    internal static class AntRenderer
    {
        private const int HoleQuad = 6;

        /// <summary>Builds the pure visual layout for a level object and optional preview time.</summary>
        public static AntVisualLayout BuildLayout(LevelObject ants, double? elapsedSeconds)
        {
            double moveSpeed = double.TryParse(
                ants.GetAttr("moveSpeed"),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsed)
                ? parsed
                : double.Parse(AntPath.DefaultMoveSpeed, CultureInfo.InvariantCulture);
            return AntVisualLayout.Build(
                new Vec2(ants.X, ants.Y),
                ants.GetAttr("path"),
                moveSpeed,
                elapsedSeconds);
        }

        /// <summary>Draws holes first and then ant sprites in path order.</summary>
        public static void Draw(
            DrawingContext ctx,
            ViewTransform view,
            SpriteCache sprites,
            LevelObject ants,
            double? elapsedSeconds)
        {
            if (!AntPath.IsAnts(ants.Type)
                || sprites.GetSprite("ant_parts", 0, 0) is not { Layers.Count: >= 7 } parts)
            {
                return;
            }

            AntVisualLayout layout = BuildLayout(ants, elapsedSeconds);
            foreach (AntHoleVisual hole in layout.Holes)
            {
                DrawVisual(
                    ctx,
                    view,
                    parts.Layers[HoleQuad],
                    hole.Position,
                    hole.HeadingDeg,
                    scale: 1,
                    opacity: 1,
                    flipX: false);
            }

            foreach (AntVisual ant in layout.Ants)
            {
                DrawVisual(
                    ctx,
                    view,
                    parts.Layers[ant.Frame],
                    ant.Position,
                    ant.HeadingDeg,
                    ant.Scale,
                    ant.Opacity,
                    flipX: true);
            }
        }

        /// <summary>Returns complete path bounds including ant artwork padding.</summary>
        public static LevelBounds Bounds(LevelObject ants)
        {
            return BuildLayout(ants, elapsedSeconds: null).Bounds;
        }

        /// <summary>Places a quad using the game's trimmed dimensions and integer center anchor.</summary>
        public static SpriteLayout ComputeTrimmedPlacement(AtlasFrame frame, Vec2 position, double scale)
        {
            double normalizedScale = scale / SpritePlacement.MapScale;
            LevelBounds dest = new(
                position.X - (frame.Frame.W / 2 * normalizedScale),
                position.Y - (frame.Frame.H / 2 * normalizedScale),
                frame.Frame.W * normalizedScale,
                frame.Frame.H * normalizedScale);
            return new SpriteLayout(frame.Frame, dest, dest);
        }

        private static void DrawVisual(
            DrawingContext ctx,
            ViewTransform view,
            SpriteLayerDraw layer,
            Vec2 position,
            double headingDeg,
            double scale,
            double opacity,
            bool flipX)
        {
            if (opacity <= 0 || scale <= 0)
            {
                return;
            }

            SpriteLayout layout = ComputeTrimmedPlacement(layer.Frame, position, scale);
            Rect source = new(layout.Source.X, layout.Source.Y, layout.Source.W, layout.Source.H);
            Vec2 topLeft = view.LevelToScreen(new Vec2(layout.Dest.X, layout.Dest.Y));
            Vec2 bottomRight = view.LevelToScreen(new Vec2(
                layout.Dest.X + layout.Dest.W,
                layout.Dest.Y + layout.Dest.H));
            Rect destination = new(
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y);
            Vec2 center = view.LevelToScreen(position);
            Matrix transform = Matrix.CreateTranslation(-center.X, -center.Y)
                * Matrix.CreateScale(flipX ? -1 : 1, 1)
                * Matrix.CreateRotation(headingDeg * Math.PI / 180)
                * Matrix.CreateTranslation(center.X, center.Y);
            using (ctx.PushOpacity(Math.Clamp(opacity, 0, 1)))
            using (ctx.PushTransform(transform))
            {
                ctx.DrawImage(layer.Bitmap, source, destination);
            }
        }
    }
}
