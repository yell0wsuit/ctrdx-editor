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
    /// Draws a conveyor belt on the canvas, assembling the game's obj_conveyor pieces along the belt axis.
    /// The belt is anchored at (x,y) and rotated by its angle; local +X runs from the anchor to the far end
    /// (length), local Y is centred thickness (width). Modeled on <see cref="GrabRenderer"/>'s rail drawing.
    /// Quad order in the "transporter_belt" sprite: 0 end, 1 end-side, 2 middle, 3 middle-side, 4 plate,
    /// 5 plate-arrow, 6 highlight (see ConveyorBelt.cs).
    /// </summary>
    internal static class ConveyorRenderer
    {
        // Consecutive tiles overlap by this many screen px to hide sub-pixel seams (same trick as the grab rail).
        private const double Bleed = 1.0;

        // Quad indices into the transporter_belt sprite layers.
        private const int QuadMiddle = 2;
        private const int QuadPlate = 4;
        private const int QuadPlateArrow = 5;
        private const int QuadHighlight = 6;

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
            // Screen space is y-down; the belt axis is (cos a, -sin a) in level space, i.e. rotate by -a on screen.
            Matrix m = Matrix.CreateRotation(-a) * Matrix.CreateTranslation(anchor.X, anchor.Y);
            using (ctx.PushTransform(m))
            {
                double lengthPx = s.Length * z;
                double widthPx = s.Width * z;

                // Middle background stretched to the full belt (tiled along +X, centred on Y).
                DrawTiledAlong(ctx, pieces.Layers[QuadMiddle], 0, lengthPx, widthPx, z);

                // Moving plate + optional directional arrow overlay (auto belts only).
                DrawTiledAlong(ctx, pieces.Layers[QuadPlate], 0, lengthPx, widthPx * 0.8, z);
                int arrow = ConveyorObject.ArrowSign(belt);
                if (arrow != 0)
                {
                    DrawArrow(ctx, pieces.Layers[QuadPlateArrow], lengthPx, widthPx, z, arrow);
                }

                // Highlight overlay along the top edge.
                DrawTiledAlong(ctx, pieces.Layers[QuadHighlight], 0, lengthPx, widthPx * 0.8, z);
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

        // Repeats a tile from local x=start to x=end along +X, centred on local Y, height = heightPx.
        private static void DrawTiledAlong(
            DrawingContext ctx, SpriteLayerDraw tile, double start, double end, double heightPx, double z)
        {
            IntRect f = tile.Frame.Frame;
            if (f.W <= 0 || f.H <= 0 || end <= start)
            {
                return;
            }

            double tileW = f.W / SpritePlacement.MapScale * z; // matches other sprites' px->screen mapping
            if (tileW <= 0)
            {
                return;
            }

            for (double x = start; x < end - 0.01; x += tileW)
            {
                double remaining = end - x;
                bool partial = remaining < tileW;
                double drawW = partial ? remaining : tileW + Bleed;
                double srcW = partial ? f.W * (remaining / tileW) : f.W;
                ctx.DrawImage(
                    tile.Bitmap,
                    new Rect(f.X, f.Y, srcW, f.H),
                    new Rect(x, -heightPx / 2, drawW, heightPx));
            }
        }

        // Draws the directional arrow near the belt centre, flipped 180 deg for arrow==-1 (game plateArrow).
        private static void DrawArrow(
            DrawingContext ctx, SpriteLayerDraw arrowTile, double lengthPx, double widthPx, double z, int arrow)
        {
            _ = widthPx;
            IntRect f = arrowTile.Frame.Frame;
            if (f.W <= 0 || f.H <= 0)
            {
                return;
            }

            double w = f.W / SpritePlacement.MapScale * z;
            double h = f.H / SpritePlacement.MapScale * z;
            double cx = lengthPx / 2.0;
            Matrix m = arrow < 0
                ? Matrix.CreateRotation(Math.PI) * Matrix.CreateTranslation(cx, 0)
                : Matrix.CreateTranslation(cx, 0);
            using (ctx.PushTransform(m))
            {
                ctx.DrawImage(arrowTile.Bitmap, new Rect(f.X, f.Y, f.W, f.H), new Rect(-w / 2, -h / 2, w, h));
            }
        }
    }
}
