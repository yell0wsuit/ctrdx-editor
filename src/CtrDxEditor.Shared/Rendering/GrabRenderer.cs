using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Grab-specific canvas visuals: the auto-hook sprite pick, the
    /// auto-catch radius rings, and the movable-rail assembly (rail bar + movable hook). All methods are
    /// pure drawing over a <see cref="ViewTransform"/>.
    /// </summary>
    internal static class GrabRenderer
    {
        /// <summary>
        /// The sprite element to render an object with. Auto-catch grabs use the auto-hook art (game
        /// HookAuto quads 4/5); every other object uses its element sprite directly.
        /// </summary>
        public static string SpriteKey(LevelObject obj)
        {
            return obj.Type == "grab" && GrabRadius.Of(obj) is not null ? "grab_auto" : obj.Type;
        }

        /// <summary>
        /// Level-space box around a whole movable rail: the axis span (start..end) grown by a cap overhang
        /// at each end and half the hook/bar width to each side, so the marquee clears the art.
        /// </summary>
        public static LevelBounds RailBounds(GrabRail.Geometry g)
        {
            const double cap = 28;  // end-cap overhang in level units
            const double half = 26; // half the movable hook / rail thickness
            double minX = Math.Min(g.Start.X, g.End.X) - (g.Vertical ? half : cap);
            double maxX = Math.Max(g.Start.X, g.End.X) + (g.Vertical ? half : cap);
            double minY = Math.Min(g.Start.Y, g.End.Y) - (g.Vertical ? cap : half);
            double maxY = Math.Max(g.Start.Y, g.End.Y) + (g.Vertical ? cap : half);
            return new LevelBounds(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Draws the auto-catch radius as an orange dashed ring (distinct from the blue/red selection box)
        /// around every auto-catch grab, so its reach stays visible without selecting it; it disappears
        /// only when auto-catch is turned off. The game draws this circle blue and solid in-play; here it
        /// is purely an editor guide, resized by dragging the selected grab's ring edge.
        /// </summary>
        public static void DrawRadiusRings(DrawingContext ctx, ViewTransform v, IReadOnlyList<LevelObject> objects)
        {
            Pen radiusPen = new(Brushes.Orange, 1.5) { DashStyle = new DashStyle([4, 3], 0) };
            foreach (LevelObject obj in objects)
            {
                if (obj.Type == "grab" && GrabRadius.Of(obj) is double rr)
                {
                    Vec2 c = v.LevelToScreen(new Vec2(obj.X, obj.Y));
                    double screenR = rr * v.Zoom;
                    ctx.DrawEllipse(null, radiusPen, new Point(c.X, c.Y), screenR, screenR);
                }
            }
        }

        /// <summary>
        /// Draws a movable grab as its rail (left cap + tiled center + right cap) with the movable hook at
        /// the rest point, matching the game's HookMovable art. Everything is laid out in a local frame
        /// rotated onto the rail axis (0 for horizontal, 90 for a vertical rail), so the same code draws
        /// both orientations; distances are level units scaled to screen pixels by the current zoom.
        /// When <paramref name="hookHighlighted"/> the hook uses the highlight art (game moverDragging).
        /// </summary>
        public static void DrawMovableGrab(
            DrawingContext ctx, ViewTransform v, SpriteCache sprites, GrabRail.Geometry g, bool hookHighlighted = false)
        {
            Vec2 hook = v.LevelToScreen(g.Hook);
            double z = v.Zoom;
            Matrix m = Matrix.CreateRotation(g.Vertical ? Math.PI / 2 : 0) * Matrix.CreateTranslation(hook.X, hook.Y);
            using (ctx.PushTransform(m))
            {
                if (sprites.GetSprite("grab_rail") is { Layers.Count: >= 3 } rail)
                {
                    double startX = -g.Offset * z;
                    double endX = (g.Length - g.Offset) * z;
                    DrawRail(ctx, rail.Layers[0], rail.Layers[1], rail.Layers[2], startX, endX, z);
                }
                if (sprites.GetSprite(hookHighlighted ? "grab_movable_highlight" : "grab_movable") is { Layers.Count: >= 1 } hookSprite)
                {
                    SpriteLayerDraw h = hookSprite.Layers[0];
                    double w = PieceSize(h, z, horizontal: true);
                    double ht = PieceSize(h, z, horizontal: false);
                    DrawFrame(ctx, h, new Rect(-w / 2, -ht / 2, w, ht));
                }
            }
        }

        // Consecutive rail pieces overlap by this many screen pixels. Abutting two images at a fractional
        // coordinate leaves a sub-pixel seam (each edge is anti-aliased against transparent), so every tile
        // bleeds a hair into its neighbour to cover it - the same trick the game uses on the middle rail.
        private const double Bleed = 1.0;

        // Draws the rail bar between local x = startX (near end) and endX (far end): the center tile is
        // repeated to fill the span (the last tile clipped to fit), then the two caps sit just outside each
        // end. Full tiles and the caps are stretched by Bleed so neighbours overlap and hide the seams.
        private static void DrawRail(
            DrawingContext ctx, SpriteLayerDraw left, SpriteLayerDraw center, SpriteLayerDraw right,
            double startX, double endX, double z)
        {
            double ch = PieceSize(center, z, horizontal: false);
            double cw = PieceSize(center, z, horizontal: true);
            IntRect cf = center.Frame.Frame;
            for (double x = startX; x < endX - 0.01; x += cw)
            {
                double remaining = endX - x;
                bool partial = remaining < cw;
                double drawW = partial ? remaining : cw + Bleed;      // full tiles bleed into the next
                double srcW = partial ? cf.W * (remaining / cw) : cf.W; // never sample past the frame
                DrawFrame(ctx, center, new Rect(x, -ch / 2, drawW, ch), new Rect(cf.X, cf.Y, srcW, cf.H));
            }

            // Caps sit outside each end and extend Bleed inward, over the center, to cover the junction seam.
            double lw = PieceSize(left, z, horizontal: true);
            double lh = PieceSize(left, z, horizontal: false);
            DrawFrame(ctx, left, new Rect(startX - lw, -lh / 2, lw + Bleed, lh));

            double rw = PieceSize(right, z, horizontal: true);
            double rh = PieceSize(right, z, horizontal: false);
            DrawFrame(ctx, right, new Rect(endX - Bleed, -rh / 2, rw + Bleed, rh));
        }

        // A rail piece's on-screen size along one axis: atlas pixels mapped to level units (÷ MapScale)
        // then to screen (× zoom), matching how every other sprite is scaled.
        private static double PieceSize(SpriteLayerDraw layer, double z, bool horizontal)
        {
            int px = horizontal ? layer.Frame.Frame.W : layer.Frame.Frame.H;
            return px / SpritePlacement.MapScale * z;
        }

        private static void DrawFrame(DrawingContext ctx, SpriteLayerDraw layer, Rect dest, Rect? source = null)
        {
            IntRect f = layer.Frame.Frame;
            ctx.DrawImage(layer.Bitmap, source ?? new Rect(f.X, f.Y, f.W, f.H), dest);
        }
    }
}
