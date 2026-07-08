using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Stateless drawing primitives for the level scene: objects, grabs, sprite layers, hitboxes, and
    /// star-duration labels. Everything here takes its context through parameters so it can be shared by
    /// the interactive <see cref="LevelCanvas"/> render pass and the clean screenshot pass alike.
    /// </summary>
    internal static class LevelSceneRenderer
    {
        /// <summary>Maps a level-space rectangle (x, y, w, h) to its axis-aligned screen rectangle.</summary>
        public static Rect LevelRectToScreen(ViewTransform v, double x, double y, double w, double h)
        {
            Vec2 tl = v.LevelToScreen(new Vec2(x, y));
            Vec2 br = v.LevelToScreen(new Vec2(x + w, y + h));
            return new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
        }

        // Selection marquee: the trimmed (visible) sprite bounds — the union of every layer's drawn
        // region — grown 25% so the dashed box sits a little outside the art rather than hugging the
        // untrimmed sourceSize box (which is much larger than what the player sees).
        public static LevelBounds SelectionBounds(SpriteCache sprites, LevelObject obj, int candySkin, int omNomSupport, bool nightLevel)
        {
            // A movable grab's marquee / click target wraps the whole rail, not just the hook, so it can
            // be selected by clicking anywhere along the bar.
            if (GrabRenderer.DrawsMovableRail(obj) && GrabRail.Of(obj) is { } rail)
            {
                return GrabRenderer.RailBounds(rail);
            }

            // Pass the active decoration so the box matches the drawn art (candy skins and Om Nom
            // platforms vary in trimmed size, which would otherwise mis-size the marquee / hit box).
            // RenderSpriteKey (not SpriteKey) so a fixed hook's box matches whichever random quad pair it drew.
            ObjectSprite? sprite = sprites.GetSprite(SelectionSpriteKey(GrabRenderer.RenderSpriteKey(obj), nightLevel), candySkin, omNomSupport);
            if (sprite is null || sprite.Layers.Count == 0)
            {
                return new LevelBounds(obj.X - 8, obj.Y - 8, 16, 16);
            }

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (SpriteLayerDraw layer in sprite.Layers)
            {
                LevelBounds d = SpritePlacement.Compute(layer.Frame, obj.X, obj.Y, sprite.Scale).Dest;
                minX = Math.Min(minX, d.X);
                minY = Math.Min(minY, d.Y);
                maxX = Math.Max(maxX, d.X + d.W);
                maxY = Math.Max(maxY, d.Y + d.H);
            }

            double w = maxX - minX, h = maxY - minY;
            const double grow = 0.25;
            return new LevelBounds(minX - (w * grow / 2.0), minY - (h * grow / 2.0), w * (1 + grow), h * (1 + grow));
        }

        // The game draws objects in a fixed z-order independent of level-list order (GameScene.Draw):
        // gravity button, Om Nom + support, bubbles, bungee ropes, stars, candy, then light-bulb bottles.
        // Same-layer objects keep their list order because OrderBy is stable. Unknown types sit with the
        // grabs (mid-stack) as a neutral default.
        public static int GameDrawLayer(LevelObject obj)
        {
            return obj.Type switch
            {
                "gravitySwitch" => 0,
                "target" => 1,
                "bubble" => 2,
                "grab" => 3,
                "star" => 4,
                "candy" or "candyL" or "candyR" => 5,
                "lightBulb" => 6,
                _ => 3,
            };
        }

        // Draws a non-grab object: its optional decorative back-layer variant, then every sprite layer,
        // then any overlays. Grabs go through DrawGrab instead so their rope can slot between hook layers.
        public static void DrawObject(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            LevelObject obj,
            int candySkin,
            int omNomSupport,
            bool nightLevel,
            IBrush starDurationText)
        {
            if (obj.Type == "star" && StarTimeout(obj) is double timeout && timeout > 0)
            {
                if (sprites.GetSprite("star_timed") is { } timed)
                {
                    DrawSprite(ctx, v, timed, obj.X, obj.Y);
                }
                if (sprites.GetSprite(CanvasSpriteKey("star", nightLevel), candySkin, omNomSupport) is { } star)
                {
                    DrawSprite(ctx, v, star, obj.X, obj.Y);
                    DrawStarDuration(ctx, v, star, obj, timeout, starDurationText);
                }
                DrawOverlays(ctx, v, sprites, obj, obj.X, obj.Y);
                return;
            }

            if (RotationTable.For(obj.Type) is { } rotSpec)
            {
                if (sprites.GetSprite(CanvasSpriteKey(obj.Type, nightLevel), candySkin, omNomSupport) is { } rotSprite)
                {
                    double deg = ObjectRotation.DisplayDegrees(obj, rotSpec);
                    foreach (SpriteLayerDraw layer in rotSprite.Layers)
                    {
                        DrawLayer(ctx, v, layer, obj.X, obj.Y, rotSprite.Scale, deg);
                    }
                }
                DrawOverlays(ctx, v, sprites, obj, obj.X, obj.Y);
                return;
            }

            ObjectSprite? sprite = sprites.GetSprite(CanvasSpriteKey(GrabRenderer.SpriteKey(obj), nightLevel), candySkin, omNomSupport);
            if (sprite is not null)
            {
                if (sprite.Variants.Count > 0)
                {
                    DrawLayer(ctx, v, sprite.Variants[SpriteVariantPicker.Pick(obj.Element, sprite.Variants.Count)], obj.X, obj.Y, sprite.Scale);
                }
                DrawSprite(ctx, v, sprite, obj.X, obj.Y);
            }
            DrawOverlays(ctx, v, sprites, obj, obj.X, obj.Y);
        }

        private static double StarTimeout(LevelObject obj)
        {
            return double.TryParse(obj.GetAttr("timeout"), NumberStyles.Float, CultureInfo.InvariantCulture, out double timeout)
                ? timeout
                : 0;
        }

        public static string CanvasSpriteKey(LevelObject obj, bool nightLevel)
        {
            return CanvasSpriteKey(GrabRenderer.SpriteKey(obj), nightLevel);
        }

        public static string CanvasSpriteKey(string element, bool nightLevel)
        {
            return nightLevel ? element switch
            {
                "target" => "target_sleeping",
                _ => element,
            } : element;
        }

        public static string SelectionSpriteKey(string element, bool nightLevel)
        {
            return element == "star" ? "star" : CanvasSpriteKey(element, nightLevel);
        }

        private static void DrawStarDuration(
            DrawingContext ctx,
            ViewTransform v,
            ObjectSprite star,
            LevelObject obj,
            double timeout,
            IBrush foreground)
        {
            FormattedText formatted = CreateStarDurationText(FormatStarDuration(timeout), v.Zoom, foreground);

            double top = StarTop(star, obj);
            Vec2 anchor = v.LevelToScreen(new Vec2(obj.X, top));
            Point origin = ComputeStarDurationOrigin(
                new Point(anchor.X, anchor.Y),
                new Size(formatted.Width, formatted.Height),
                v.Zoom);
            ctx.DrawText(formatted, origin);
        }

        private static FormattedText CreateStarDurationText(string text, double zoom, IBrush foreground)
        {
            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.DefaultFontFamilyName, FontStyle.Normal, FontWeight.Bold),
                Math.Max(10.0, 18.0 * zoom),
                foreground);
        }

        private static string FormatStarDuration(double timeout)
        {
            return timeout.ToString("0.###", CultureInfo.InvariantCulture) + "s";
        }

        private static Point ComputeStarDurationOrigin(Point starTopCenter, Size textSize, double zoom)
        {
            return new Point(starTopCenter.X - (textSize.Width / 2.0), starTopCenter.Y + (2.0 * zoom));
        }

        private static double StarTop(ObjectSprite star, LevelObject obj)
        {
            double top = double.MaxValue;
            foreach (SpriteLayerDraw layer in star.Layers)
            {
                LevelBounds bounds = SpritePlacement.Compute(layer.Frame, obj.X, obj.Y, star.Scale).Dest;
                top = Math.Min(top, bounds.Y);
            }
            return top == double.MaxValue ? obj.Y : top;
        }

        // A pale grab is one the game hides outright (invisible="true"); the editor keeps it visible at
        // this opacity so it stays selectable and editable rather than vanishing.
        private const double InvisibleGrabOpacity = 0.3;

        // Draws a grab with its rope threaded between the hook's back and front art, matching the game's
        // Grab.DrawBack (back art) then Grab.Draw (rope, then front art) order. An invisible grab (hidden
        // entirely in-game) is drawn pale so it can still be selected. A movable grab splits into its rail
        // bar (back) and movable hook (front); every other grab splits its sprite layers by
        // GrabRenderer.BackLayerCount. rope is null when the grab has nothing to hang from. hookHighlighted
        // lights the movable hook while the caller reports it hovered or being slid.
        public static void DrawGrab(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            LevelObject obj,
            IReadOnlyList<LevelObject> objects,
            bool twoParts,
            RopeVisual? rope,
            int ropeSeed,
            Rect opBounds,
            bool hookHighlighted)
        {
            // The hook art and Christmas lights are DrawImage calls that PushOpacity fades; the rope is a
            // Skia custom draw op that PushOpacity does not reach, so its alpha is passed through explicitly.
            double opacity = IsInvisible(obj) ? InvisibleGrabOpacity : 1.0;
            if (opacity < 1.0)
            {
                using (ctx.PushOpacity(opacity))
                {
                    DrawGrabContent(ctx, v, sprites, obj, objects, twoParts, rope, ropeSeed, opBounds, opacity, hookHighlighted);
                }
            }
            else
            {
                DrawGrabContent(ctx, v, sprites, obj, objects, twoParts, rope, ropeSeed, opBounds, opacity, hookHighlighted);
            }
        }

        private static bool IsInvisible(LevelObject obj)
        {
            return bool.TryParse(obj.GetAttr("invisible"), out bool b) && b;
        }

        private static void DrawGrabContent(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            LevelObject obj,
            IReadOnlyList<LevelObject> objects,
            bool twoParts,
            RopeVisual? rope,
            int ropeSeed,
            Rect opBounds,
            double ropeOpacity,
            bool hookHighlighted)
        {
            if (GrabRenderer.DrawsMovableRail(obj) && GrabRail.Of(obj) is { } rail)
            {
                // Highlight the hook while it's hovered or being slid, matching the game's mover art.
                bool active = hookHighlighted;
                GrabRenderer.DrawMovableRail(ctx, v, sprites, rail);
                if (rope is not null)
                {
                    RopeRenderer.DrawRope(ctx, v, sprites, rope, ropeSeed, opBounds, ropeOpacity);
                }
                GrabRenderer.DrawMovableHook(ctx, v, sprites, rail, active);
                Vec2 anchor = GrabRenderer.SpiderOverlayAnchor(obj);
                DrawOverlays(ctx, v, sprites, obj, anchor.X, anchor.Y);
                return;
            }

            ObjectSprite? sprite = sprites.GetSprite(GrabRenderer.RenderSpriteKey(obj));
            if (sprite is not null)
            {
                if (sprite.Variants.Count > 0)
                {
                    DrawLayer(ctx, v, sprite.Variants[SpriteVariantPicker.Pick(obj.Element, sprite.Variants.Count)], obj.X, obj.Y, sprite.Scale);
                }
                int back = Math.Min(GrabRenderer.BackLayerCount(obj), sprite.Layers.Count);
                DrawGrabLayers(ctx, v, sprite, obj, objects, twoParts, 0, back);
                if (rope is not null)
                {
                    RopeRenderer.DrawRope(ctx, v, sprites, rope, ropeSeed, opBounds, ropeOpacity);
                }
                DrawGrabLayers(ctx, v, sprite, obj, objects, twoParts, back, sprite.Layers.Count);
            }
            else if (rope is not null)
            {
                RopeRenderer.DrawRope(ctx, v, sprites, rope, ropeSeed, opBounds, ropeOpacity);
            }
            DrawOverlays(ctx, v, sprites, obj, obj.X, obj.Y);
        }

        // Draws the sprite layers in [from, to), applying the gun's aim rotation to the arrow layer
        // (index 1) when this grab previews a gun aim - the same rotation the old single-pass path used.
        private static void DrawGrabLayers(
            DrawingContext ctx,
            ViewTransform v,
            ObjectSprite sprite,
            LevelObject obj,
            IReadOnlyList<LevelObject> objects,
            bool twoParts,
            int from,
            int to)
        {
            double? gunAim = sprite.Layers.Count >= 3
                ? GrabRenderer.GunAimRotationDegrees(obj, objects, twoParts)
                : null;
            for (int i = from; i < to; i++)
            {
                double? rotation = gunAim is double deg && i == 1 ? deg : null;
                DrawLayer(ctx, v, sprite.Layers[i], obj.X, obj.Y, sprite.Scale, rotation);
            }
        }

        private static void DrawOverlays(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            LevelObject obj,
            double x,
            double y)
        {
            foreach (string overlayKey in GrabRenderer.OverlaySpriteKeys(obj))
            {
                if (sprites.GetSprite(overlayKey) is { } overlay)
                {
                    DrawSprite(ctx, v, overlay, x, y);
                }
            }
        }

        public static void DrawSprite(DrawingContext ctx, ViewTransform v, ObjectSprite sprite, double x, double y)
        {
            foreach (SpriteLayerDraw layer in sprite.Layers)
            {
                DrawLayer(ctx, v, layer, x, y, sprite.Scale);
            }
        }

        private static void DrawLayer(
            DrawingContext ctx,
            ViewTransform v,
            SpriteLayerDraw layer,
            double x,
            double y,
            double scale,
            double? rotationDegrees = null)
        {
            SpriteLayout layout = SpritePlacement.Compute(layer.Frame, x, y, scale);
            Rect source = new(layout.Source.X, layout.Source.Y, layout.Source.W, layout.Source.H);
            Vec2 dtl = v.LevelToScreen(new Vec2(layout.Dest.X, layout.Dest.Y));
            Vec2 dbr = v.LevelToScreen(new Vec2(layout.Dest.X + layout.Dest.W, layout.Dest.Y + layout.Dest.H));
            Rect dest = new(dtl.X, dtl.Y, dbr.X - dtl.X, dbr.Y - dtl.Y);
            if (rotationDegrees is double degrees)
            {
                Vec2 center = v.LevelToScreen(new Vec2(x, y));
                Matrix m = Matrix.CreateTranslation(-center.X, -center.Y)
                    * Matrix.CreateRotation(degrees * Math.PI / 180.0)
                    * Matrix.CreateTranslation(center.X, center.Y);
                using (ctx.PushTransform(m))
                {
                    ctx.DrawImage(layer.Bitmap, source, dest);
                }
            }
            else
            {
                ctx.DrawImage(layer.Bitmap, source, dest);
            }
        }

        public static void DrawHitbox(
            DrawingContext ctx,
            ViewTransform v,
            LevelObject obj,
            double scale,
            HitboxModel model,
            Pen pen)
        {
            if (HitboxTable.Compute(obj.Type, obj.X, obj.Y, scale, model) is not { } b)
            {
                return;
            }
            Vec2 tl = v.LevelToScreen(new Vec2(b.X, b.Y));
            Vec2 br = v.LevelToScreen(new Vec2(b.X + b.W, b.Y + b.H));
            ctx.DrawRectangle(null, pen, new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y));
        }
    }
}
