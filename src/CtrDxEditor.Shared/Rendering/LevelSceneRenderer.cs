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
        /// <summary>Maps a level-space rectangle to its axis-aligned screen rectangle.</summary>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="x">Left edge of the rectangle in level units.</param>
        /// <param name="y">Top edge of the rectangle in level units.</param>
        /// <param name="w">Width of the rectangle in level units.</param>
        /// <param name="h">Height of the rectangle in level units.</param>
        /// <returns>The rectangle in screen pixels.</returns>
        public static Rect LevelRectToScreen(ViewTransform v, double x, double y, double w, double h)
        {
            Vec2 tl = v.LevelToScreen(new Vec2(x, y));
            Vec2 br = v.LevelToScreen(new Vec2(x + w, y + h));
            return new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
        }

        /// <summary>Computes the selection marquee / click box for an object.</summary>
        /// <remarks>
        /// The box is the trimmed (visible) sprite bounds — the union of every layer's drawn region — grown
        /// 25% so the dashed box sits a little outside the art rather than hugging the untrimmed sourceSize
        /// box (which is much larger than what the player sees). A movable grab's box wraps the whole rail
        /// so it can be selected by clicking anywhere along the bar.
        /// </remarks>
        /// <param name="sprites">Sprite cache used to resolve the object's art.</param>
        /// <param name="obj">The object to bound.</param>
        /// <param name="candySkin">Active candy skin index, so the box matches the drawn candy art.</param>
        /// <param name="omNomSupport">Active Om Nom support index, so the box matches the drawn platform art.</param>
        /// <param name="nightLevel">Whether night sprite variants apply.</param>
        /// <returns>The selection bounds in level units.</returns>
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

        /// <summary>The object's fixed draw layer in the game's z-order.</summary>
        /// <remarks>
        /// The game draws objects in a fixed z-order independent of level-list order (GameScene.Draw):
        /// gravity button, Om Nom + support, bubbles, bungee ropes, stars, candy, then light-bulb bottles.
        /// Same-layer objects keep their list order because OrderBy is stable. Unknown types sit with the
        /// grabs (mid-stack) as a neutral default.
        /// </remarks>
        /// <param name="obj">The object to classify.</param>
        /// <returns>The z-order layer index (lower draws first / further back).</returns>
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

        /// <summary>
        /// Draws a non-grab object: its optional decorative back-layer variant, then every sprite layer,
        /// then any overlays. Grabs go through <see cref="DrawGrab"/> instead so their rope can slot between
        /// hook layers.
        /// </summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve the object's art.</param>
        /// <param name="obj">The object to draw.</param>
        /// <param name="candySkin">Active candy skin index.</param>
        /// <param name="omNomSupport">Active Om Nom support index.</param>
        /// <param name="nightLevel">Whether night sprite variants apply.</param>
        /// <param name="starDurationText">Brush for the timed-star duration label.</param>
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

        /// <summary>Reads a star's <c>timeout</c> attribute in seconds.</summary>
        /// <param name="obj">The star object.</param>
        /// <returns>The timeout in seconds, or 0 when the attribute is absent or unparseable.</returns>
        private static double StarTimeout(LevelObject obj)
        {
            return double.TryParse(obj.GetAttr("timeout"), NumberStyles.Float, CultureInfo.InvariantCulture, out double timeout)
                ? timeout
                : 0;
        }

        /// <summary>Resolves the sprite key for an object, applying night-level variants.</summary>
        /// <param name="obj">The object whose base sprite key is resolved first.</param>
        /// <param name="nightLevel">Whether night sprite variants apply.</param>
        /// <returns>The sprite key to draw.</returns>
        public static string CanvasSpriteKey(LevelObject obj, bool nightLevel)
        {
            return CanvasSpriteKey(GrabRenderer.SpriteKey(obj), nightLevel);
        }

        /// <summary>Applies night-level variants to a sprite element key (e.g. sleeping Om Nom target).</summary>
        /// <param name="element">The base sprite element key.</param>
        /// <param name="nightLevel">Whether night sprite variants apply.</param>
        /// <returns>The night variant key when applicable, otherwise <paramref name="element"/> unchanged.</returns>
        public static string CanvasSpriteKey(string element, bool nightLevel)
        {
            return nightLevel ? element switch
            {
                "target" => "target_sleeping",
                _ => element,
            } : element;
        }

        /// <summary>
        /// The sprite key used to size a selection box. Stars keep their normal (day) marquee even on night
        /// levels; everything else follows <see cref="CanvasSpriteKey(string, bool)"/>.
        /// </summary>
        /// <param name="element">The base sprite element key.</param>
        /// <param name="nightLevel">Whether night sprite variants apply.</param>
        /// <returns>The sprite key to measure for the selection box.</returns>
        public static string SelectionSpriteKey(string element, bool nightLevel)
        {
            return element == "star" ? "star" : CanvasSpriteKey(element, nightLevel);
        }

        /// <summary>Draws the countdown label above a timed star.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="star">The star sprite, used to find its visible top edge.</param>
        /// <param name="obj">The star object.</param>
        /// <param name="timeout">The star's timeout in seconds.</param>
        /// <param name="foreground">Brush for the label text.</param>
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

        /// <summary>Builds the formatted duration text, scaling the font with the current zoom.</summary>
        /// <param name="text">The label string to render.</param>
        /// <param name="zoom">Current view zoom, used to scale the font size.</param>
        /// <param name="foreground">Brush for the label text.</param>
        /// <returns>The formatted text ready to draw.</returns>
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

        /// <summary>Formats a star timeout as a trimmed seconds string (e.g. "4.5s").</summary>
        /// <param name="timeout">The timeout in seconds.</param>
        /// <returns>The formatted label including the seconds unit.</returns>
        private static string FormatStarDuration(double timeout)
        {
            return timeout.ToString("0.###", CultureInfo.InvariantCulture) + "s";
        }

        /// <summary>Positions the duration label centered horizontally and just inside the star top.</summary>
        /// <param name="starTopCenter">Screen point at the star's top-center.</param>
        /// <param name="textSize">Measured size of the label.</param>
        /// <param name="zoom">Current view zoom, used for the vertical inset.</param>
        /// <returns>The top-left screen origin at which to draw the label.</returns>
        private static Point ComputeStarDurationOrigin(Point starTopCenter, Size textSize, double zoom)
        {
            return new Point(starTopCenter.X - (textSize.Width / 2.0), starTopCenter.Y + (2.0 * zoom));
        }

        /// <summary>Finds the visible top edge of a star's art in level units.</summary>
        /// <param name="star">The star sprite.</param>
        /// <param name="obj">The star object providing the anchor position.</param>
        /// <returns>The topmost drawn Y, or the object's Y when the sprite has no layers.</returns>
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

        /// <summary>
        /// Opacity for a grab the game hides outright (<c>invisible="true"</c>). The editor keeps it visible
        /// at this opacity so it stays selectable and editable rather than vanishing.
        /// </summary>
        private const double InvisibleGrabOpacity = 0.3;

        /// <summary>Draws a grab with its rope threaded between the hook's back and front art.</summary>
        /// <remarks>
        /// Matches the game's Grab.DrawBack (back art) then Grab.Draw (rope, then front art) order. An
        /// invisible grab (hidden entirely in-game) is drawn pale so it can still be selected. A movable
        /// grab splits into its rail bar (back) and movable hook (front); every other grab splits its sprite
        /// layers by <c>GrabRenderer.BackLayerCount</c>.
        /// </remarks>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve the grab's art.</param>
        /// <param name="obj">The grab object.</param>
        /// <param name="objects">All level objects, used to resolve gun-aim targets.</param>
        /// <param name="twoParts">Whether the level uses two-part rope physics.</param>
        /// <param name="rope">The grab's rope visual, or null when it has nothing to hang from.</param>
        /// <param name="ropeSeed">Per-rope seed for deterministic rope decoration.</param>
        /// <param name="opBounds">Screen bounds passed to the rope's custom draw op.</param>
        /// <param name="hookHighlighted">Whether to light the movable hook (hovered or being slid).</param>
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

        /// <summary>Whether a grab is flagged <c>invisible="true"</c> (hidden in-game).</summary>
        /// <param name="obj">The grab object.</param>
        /// <returns>True when the grab is invisible.</returns>
        private static bool IsInvisible(LevelObject obj)
        {
            return bool.TryParse(obj.GetAttr("invisible"), out bool b) && b;
        }

        /// <summary>
        /// Draws the grab's art and rope in the correct back-to-front order, splitting a movable grab into
        /// rail bar then rope then hook, and every other grab into back layers then rope then front layers.
        /// </summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve the grab's art.</param>
        /// <param name="obj">The grab object.</param>
        /// <param name="objects">All level objects, used to resolve gun-aim targets.</param>
        /// <param name="twoParts">Whether the level uses two-part rope physics.</param>
        /// <param name="rope">The grab's rope visual, or null when it has nothing to hang from.</param>
        /// <param name="ropeSeed">Per-rope seed for deterministic rope decoration.</param>
        /// <param name="opBounds">Screen bounds passed to the rope's custom draw op.</param>
        /// <param name="ropeOpacity">Rope alpha, passed explicitly since <c>PushOpacity</c> doesn't reach the custom op.</param>
        /// <param name="hookHighlighted">Whether to light the movable hook (hovered or being slid).</param>
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

        /// <summary>
        /// Draws the sprite layers in the half-open range [<paramref name="from"/>, <paramref name="to"/>),
        /// applying the gun's aim rotation to the arrow layer (index 1) when this grab previews a gun aim.
        /// </summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprite">The grab's resolved sprite.</param>
        /// <param name="obj">The grab object.</param>
        /// <param name="objects">All level objects, used to resolve the gun-aim target.</param>
        /// <param name="twoParts">Whether the level uses two-part rope physics.</param>
        /// <param name="from">First layer index to draw (inclusive).</param>
        /// <param name="to">Last layer index to draw (exclusive).</param>
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

        /// <summary>Draws every overlay sprite an object contributes (e.g. spider, Christmas lights).</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve the overlay art.</param>
        /// <param name="obj">The object whose overlays are drawn.</param>
        /// <param name="x">Overlay anchor X in level units.</param>
        /// <param name="y">Overlay anchor Y in level units.</param>
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

        /// <summary>Draws every layer of a sprite at a level position.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprite">The sprite to draw.</param>
        /// <param name="x">Anchor X in level units.</param>
        /// <param name="y">Anchor Y in level units.</param>
        public static void DrawSprite(DrawingContext ctx, ViewTransform v, ObjectSprite sprite, double x, double y)
        {
            foreach (SpriteLayerDraw layer in sprite.Layers)
            {
                DrawLayer(ctx, v, layer, x, y, sprite.Scale);
            }
        }

        /// <summary>Draws a single sprite layer, optionally rotated about the object's anchor.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="layer">The sprite layer to draw.</param>
        /// <param name="x">Anchor X in level units.</param>
        /// <param name="y">Anchor Y in level units.</param>
        /// <param name="scale">Sprite scale factor.</param>
        /// <param name="rotationDegrees">Rotation about the anchor in degrees, or null for no rotation.</param>
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

        /// <summary>Draws an object's hitbox rectangle for the given device model, if it has one.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="obj">The object whose hitbox is drawn.</param>
        /// <param name="scale">Sprite scale factor, used to size the hitbox.</param>
        /// <param name="model">Which device hitbox model (desktop or phone) to compute.</param>
        /// <param name="pen">Pen for the hitbox outline.</param>
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
            Rect box = new(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);

            // A rotatable object's box turns with its sprite about the same anchor (see DrawLayer). The view
            // transform is translation + uniform scale, so rotating the projected box about the projected
            // anchor equals rotating it in level space. Square boxes only diverge visibly off the axes.
            if (RotationTable.For(obj.Type) is { } rotSpec && ObjectRotation.DisplayDegrees(obj, rotSpec) is var deg && deg != 0)
            {
                Vec2 center = v.LevelToScreen(new Vec2(obj.X, obj.Y));
                Matrix m = Matrix.CreateTranslation(-center.X, -center.Y)
                    * Matrix.CreateRotation(deg * Math.PI / 180.0)
                    * Matrix.CreateTranslation(center.X, center.Y);
                using (ctx.PushTransform(m))
                {
                    ctx.DrawRectangle(null, pen, box);
                }
                return;
            }
            ctx.DrawRectangle(null, pen, box);
        }
    }
}
