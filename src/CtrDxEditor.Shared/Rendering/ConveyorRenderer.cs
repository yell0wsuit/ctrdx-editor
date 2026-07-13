using System;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Draws a conveyor belt by reproducing the complete static scene graph built by the game's
    /// <c>ConveyorBelt.BuildVisuals</c>: background, end caps, rails, corners, tiled plate, arrows, and
    /// end highlights. The game authors the composition in a width-by-length root rotated 90 degrees;
    /// that coordinate system is retained here so its anchors and mirrors remain exact.
    /// </summary>
    internal static class ConveyorRenderer
    {
        private const int QuadPlate = 4;
        private const int QuadPlateArrow = 5;

        /// <summary>Draws the belt for <paramref name="belt"/> using the transporter_belt pieces.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve the belt pieces.</param>
        /// <param name="belt">The conveyor object to draw.</param>
        public static void Draw(DrawingContext ctx, ViewTransform v, SpriteCache sprites, LevelObject belt)
        {
            if (ConveyorGeometry.Of(belt) is not { } s)
            {
                return;
            }
            if (sprites.GetSprite("transporter_belt", 0, 0) is not { Layers.Count: >= 7 } pieces)
            {
                return;
            }

            double z = v.Zoom;
            Vec2 anchor = v.LevelToScreen(s.Anchor);
            double a = s.AngleDeg * Math.PI / 180.0;
            ConveyorVisualLayout layout = ConveyorVisualLayout.Build(s.Length, s.Width, ConveyorObject.ArrowSign(belt));
            double pivotX = layout.ParentRotationPivotX * z;
            // ConveyorBelt rotates around its integer parent half-width plus rotationCenterX.
            Matrix m = Matrix.CreateTranslation(-pivotX, 0)
                * Matrix.CreateRotation(-a)
                * Matrix.CreateTranslation(anchor.X + pivotX, anchor.Y);
            using (ctx.PushTransform(m))
            {
                // visualRoot.rotation = 90 around integer half-width/height pivots.
                Matrix root = Matrix.CreateRotation(Math.PI / 2)
                    * Matrix.CreateTranslation(layout.RootTranslationX * z, layout.RootTranslationY * z);
                using (ctx.PushTransform(root))
                {
                    ConveyorVisualPiece? arrow = FindArrow(layout);
                    foreach (ConveyorVisualPiece piece in layout.Pieces)
                    {
                        if (piece.Kind == ConveyorVisualPieceKind.Arrow)
                        {
                            continue;
                        }
                        else if (piece.Kind == ConveyorVisualPieceKind.PlateSurface)
                        {
                            DrawPlateSurface(ctx, pieces, piece, z, arrow);
                        }
                        else
                        {
                            DrawPiece(ctx, pieces.Layers[piece.Quad], piece, z);
                        }
                    }
                }
            }
        }

        /// <summary>Draws the far-end knob and the two width knobs for a selected belt.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="belt">The selected conveyor object.</param>
        /// <param name="handlePen">Pen used to stroke the handle knobs.</param>
        public static void DrawHandles(DrawingContext ctx, ViewTransform v, LevelObject belt, Pen handlePen)
        {
            if (ConveyorGeometry.Of(belt) is not { } s)
            {
                return;
            }

            Vec2 far = v.LevelToScreen(s.Far);
            ctx.DrawEllipse(Brushes.White, handlePen, new Point(far.X, far.Y), 5, 5);

            double a = s.AngleDeg * Math.PI / 180.0;
            double px = Math.Sin(a);
            double py = Math.Cos(a);
            double hw = s.Width / 2.0;
            Vec2 mid = new(
                (s.Anchor.X + s.Far.X) / 2.0,
                (s.Anchor.Y + s.Far.Y) / 2.0);
            foreach (int sign in new[] { 1, -1 })
            {
                Vec2 side = v.LevelToScreen(new Vec2(mid.X + (px * hw * sign), mid.Y + (py * hw * sign)));
                ctx.DrawEllipse(Brushes.White, handlePen, new Point(side.X, side.Y), 4, 4);
            }
        }

        private static ConveyorVisualPiece? FindArrow(ConveyorVisualLayout layout)
        {
            foreach (ConveyorVisualPiece piece in layout.Pieces)
            {
                if (piece.Kind == ConveyorVisualPieceKind.Arrow)
                {
                    return piece;
                }
            }
            return null;
        }

        private static void DrawPiece(DrawingContext ctx, SpriteLayerDraw layer, ConveyorVisualPiece piece, double z)
        {
            IntRect source = layer.Frame.Frame;
            if (source.W <= 0 || source.H <= 0 || piece.Bounds.W <= 0 || piece.Bounds.H <= 0)
            {
                return;
            }

            Rect dest = ScreenRect(piece.Bounds, z);
            if (!piece.FlipX && !piece.FlipY)
            {
                ctx.DrawImage(layer.Bitmap, SourceRect(source), dest);
                return;
            }

            double sx = piece.FlipX ? -1 : 1;
            double sy = piece.FlipY ? -1 : 1;
            Matrix mirror = Matrix.CreateScale(sx, sy)
                * Matrix.CreateTranslation(dest.Center.X * (1 - sx), dest.Center.Y * (1 - sy));
            using (ctx.PushTransform(mirror))
            {
                ctx.DrawImage(layer.Bitmap, SourceRect(source), dest);
            }
        }

        private static void DrawPlateSurface(
            DrawingContext ctx,
            ObjectSprite pieces,
            ConveyorVisualPiece plate,
            double z,
            ConveyorVisualPiece? arrow)
        {
            SpriteLayerDraw tile = pieces.Layers[QuadPlate];
            IntRect source = tile.Frame.Frame;
            double tileHeight = source.H / SpritePlacement.MapScale;
            if (source.W <= 0 || source.H <= 0 || tileHeight <= 0)
            {
                return;
            }

            for (double y = plate.Bounds.Y; y < plate.Bounds.Y + plate.Bounds.H - 0.0001; y += tileHeight)
            {
                double visible = Math.Min(tileHeight, plate.Bounds.Y + plate.Bounds.H - y);
                double scaleY = visible / tileHeight;
                double drawY = visible < tileHeight
                    ? plate.Bounds.Y + plate.Bounds.H - visible - (0.5 * (tileHeight - visible))
                    : y;
                // Image scaling pivots on height >> 1 (31 atlas px), not the geometric 31.5px center.
                double destY = drawY + (31.0 / SpritePlacement.MapScale * (1 - scaleY));
                Rect dest = new(plate.Bounds.X * z, destY * z, plate.Bounds.W * z, visible * z);
                ctx.DrawImage(tile.Bitmap, SourceRect(source), dest);
                if (arrow is { } arrowPiece)
                {
                    DrawArrowInTile(
                        ctx, pieces.Layers[QuadPlateArrow], plate, drawY, visible, z, arrowPiece.Direction);
                }
            }
        }

        private static void DrawArrowInTile(
            DrawingContext ctx,
            SpriteLayerDraw arrow,
            ConveyorVisualPiece plate,
            double tileDrawY,
            double visibleHeight,
            double z,
            int direction)
        {
            IntRect source = arrow.Frame.Frame;
            if (source.W <= 0 || source.H <= 0)
            {
                return;
            }

            // plateArrow is center-anchored inside the odd-sized 235x63 plateSection. Both elements use
            // integer half pivots (117,31 and 33,17 respectively) before plateSection's scale is inherited.
            double pivotX = plate.Bounds.X + (117.0 / 235 * plate.Bounds.W);
            double pivotY = tileDrawY + (31.0 / SpritePlacement.MapScale);
            double w = 66.0 / 235 * plate.Bounds.W;
            double h = 35.0 / 63 * visibleHeight;
            double left = pivotX - (33.0 / 235 * plate.Bounds.W);
            double top = pivotY - (17.0 / 63 * visibleHeight);
            Rect dest = new(left * z, top * z, w * z, h * z);
            if (direction >= 0)
            {
                ctx.DrawImage(arrow.Bitmap, SourceRect(source), dest);
                return;
            }

            double rotationPivotX = pivotX * z;
            double rotationPivotY = pivotY * z;
            Matrix rotate = Matrix.CreateTranslation(-rotationPivotX, -rotationPivotY)
                * Matrix.CreateRotation(Math.PI)
                * Matrix.CreateTranslation(rotationPivotX, rotationPivotY);
            using (ctx.PushTransform(rotate))
            {
                ctx.DrawImage(arrow.Bitmap, SourceRect(source), dest);
            }
        }

        private static Rect SourceRect(IntRect source)
        {
            return new Rect(source.X, source.Y, source.W, source.H);
        }

        private static Rect ScreenRect(LevelBounds bounds, double z)
        {
            return new Rect(bounds.X * z, bounds.Y * z, bounds.W * z, bounds.H * z);
        }
    }
}
