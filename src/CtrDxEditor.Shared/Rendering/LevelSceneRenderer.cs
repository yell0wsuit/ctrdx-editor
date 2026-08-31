using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

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
        /// <summary>Opacity of the editor-only frozen steam plume; hardware remains fully opaque.</summary>
        public const double SteamPuffOpacity = 0.55;

        /// <summary>Level-space Y offset for the lantern's inner candy (game innerCandy.y = -4 px).</summary>
        private const double LanternInnerCandyOffsetY = -4.0 / SpritePlacement.MapScale;

        /// <summary>Opacity for the primary candy while a lantern holds it (it is transparent in-game,
        /// but the editor keeps it faintly visible so it stays selectable and editable).</summary>
        private const double CapturedPrimaryCandyOpacity = 0.3;

        /// <summary>Bamboo-tube bounding-box half-size in game points (BambooTube.BambooBaseBbSize).</summary>
        private const double BambooBbHalfSize = 75.0;

        /// <summary>Bamboo-tube candy-capture radius in game points (BambooTube.BambooCaptureRadius).</summary>
        private const double BambooCaptureRadius = 17.5;

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
            if (AntPath.IsAnts(obj.Type))
            {
                return AntRenderer.Bounds(obj);
            }

            if (HandObject.IsHand(obj.Type))
            {
                return HandGeometry.Bounds(obj);
            }

            // A movable grab's marquee / click target wraps the whole rail, not just the hook, so it can
            // be selected by clicking anywhere along the bar.
            if (GrabRenderer.DrawsMovableRail(obj) && GrabRail.Of(obj) is { } rail)
            {
                return GrabRenderer.RailBounds(rail);
            }

            // The vinyl disc scales with its size attribute, so its click target is a size-derived box
            // rather than the fixed-scale sprite bounds.
            if (VinylGeometry.IsVinyl(obj.Type))
            {
                double r = VinylGeometry.DiscRadius(obj);
                return new LevelBounds(obj.X - r, obj.Y - r, r * 2, r * 2);
            }

            if (ConveyorGeometry.Of(obj) is { } beltShape)
            {
                return ConveyorGeometry.DialSelectionBounds(beltShape);
            }

            if (TutorialObject.IsText(obj.Type))
            {
                return TutorialRenderer.TextBounds(sprites, obj);
            }

            if (TutorialObject.IsImage(obj.Type))
            {
                return TutorialRenderer.IconBounds(sprites, obj);
            }

            if (obj.Type == "steamTube")
            {
                double mapScale = SpritePlacement.MapScale;
                double bodyW = 143 / mapScale;
                double bodyH = SteamTubeGeometry.BodyQuadHeight / mapScale;
                double valveW = 108 / mapScale;
                double valveH = 107 / mapScale;
                double valveY = obj.Y + SteamTubeGeometry.ValveDrawOffset;
                double steamMinX = Math.Min(obj.X - (bodyW / 2), obj.X - (valveW / 2));
                double steamMinY = Math.Min(obj.Y, valveY - (valveH / 2));
                double steamMaxX = Math.Max(obj.X + (bodyW / 2), obj.X + (valveW / 2));
                double steamMaxY = Math.Max(obj.Y + bodyH, valveY + (valveH / 2));
                return new LevelBounds(
                    steamMinX,
                    steamMinY,
                    steamMaxX - steamMinX,
                    steamMaxY - steamMinY);
            }

            // Pass the active decoration so the box matches the drawn art (candy skins and Om Nom
            // platforms vary in trimmed size, which would otherwise mis-size the marquee / hit box).
            // RenderSpriteKey (not SpriteKey) so a fixed hook's box matches whichever random quad pair it drew.
            string selectionKey = SpikeObject.IsSpike(obj.Type)
                ? SpikeObject.SpriteKey(obj)
                : obj.Type == "sock"
                    ? SockObject.SpriteKey(obj, SpecialEvents.IsXmas)
                    : LanternObject.IsLantern(obj.Type)
                        ? LanternObject.SpriteKey(obj)
                        : GrabRenderer.RenderSpriteKey(obj);
            ObjectSprite? sprite = sprites.GetSprite(SelectionSpriteKey(selectionKey, nightLevel), candySkin, omNomSupport);
            if (sprite is null || sprite.Layers.Count == 0)
            {
                return new LevelBounds(obj.X - 8, obj.Y - 8, 16, 16);
            }

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            double offsetY = SockPlacementOffsetY(obj, sprite);
            foreach (SpriteLayerDraw layer in sprite.Layers)
            {
                LevelBounds d = SpritePlacement.Compute(layer.Frame, obj.X, obj.Y + offsetY, sprite.Scale).Dest;
                minX = Math.Min(minX, d.X);
                minY = Math.Min(minY, d.Y);
                maxX = Math.Max(maxX, d.X + d.W);
                maxY = Math.Max(maxY, d.Y + d.H);
            }

            double w = maxX - minX, h = maxY - minY;
            const double grow = 0.25;
            return new LevelBounds(minX - (w * grow / 2.0), minY - (h * grow / 2.0), w * (1 + grow), h * (1 + grow));
        }

        /// <summary>The bounds to cull an object against: its art box, moved to wherever the object actually draws.</summary>
        /// <remarks>
        /// A mover of a type that <see cref="DrawsAtPreviewPosition"/> accepts draws at its live-preview position
        /// along its path, which can be thousands of units from the authored one, so culling on
        /// <see cref="SelectionBounds"/> alone erases it for the whole stretch it spends on screen. Passing the
        /// same elapsed seconds the draw call receives keeps the two in step. Types that draw from authored
        /// geometry keep the authored box: an ant conveyor carries a path by default yet never moves its trail,
        /// so shifting its box would cull art that is plainly visible.
        /// </remarks>
        /// <param name="sprites">Sprite cache used to resolve the object's art.</param>
        /// <param name="obj">The object to bound.</param>
        /// <param name="candySkin">Active candy skin index, so the box matches the drawn candy art.</param>
        /// <param name="omNomSupport">Active Om Nom support index, so the box matches the drawn platform art.</param>
        /// <param name="nightLevel">Whether night sprite variants apply.</param>
        /// <param name="drawOffset">How far preview has moved the object, from <see cref="DrawOffset"/>.</param>
        /// <returns>The bounds in level units at the object's drawn position.</returns>
        public static LevelBounds CullBounds(
            SpriteCache sprites,
            LevelObject obj,
            int candySkin,
            int omNomSupport,
            bool nightLevel,
            Vec2 drawOffset)
        {
            LevelBounds bounds = SelectionBounds(sprites, obj, candySkin, omNomSupport, nightLevel);
            return Shift(bounds, drawOffset);
        }

        /// <summary>Moves a box by a preview offset, returning it untouched when the offset is zero.</summary>
        /// <param name="bounds">The box at the object's authored position.</param>
        /// <param name="offset">The offset from <see cref="DrawOffset"/>.</param>
        /// <returns>The box at the object's drawn position.</returns>
        private static LevelBounds Shift(LevelBounds bounds, Vec2 offset)
        {
            return offset.X == 0.0 && offset.Y == 0.0
                ? bounds
                : new LevelBounds(bounds.X + offset.X, bounds.Y + offset.Y, bounds.W, bounds.H);
        }

        /// <summary>
        /// Whether an object's level-space bounds fall inside the rendered viewport, and so must be drawn.
        /// </summary>
        /// <remarks>
        /// <paramref name="bounds"/> is the art box from <see cref="CullBounds"/>, but some visuals
        /// legitimately overhang it — a star's duration text, tutorial overhang, glow halos. The margin keeps
        /// those drawing correctly; without it they would pop at the viewport edge. Callers that draw
        /// unbounded visuals (a grab's rope reaches an arbitrary target elsewhere) must not cull at all.
        /// </remarks>
        /// <param name="bounds">The object's bounds in level units.</param>
        /// <param name="view">The active view transform.</param>
        /// <param name="renderSize">The render surface size in screen pixels.</param>
        /// <param name="margin">Extra screen-pixel slack around the viewport, absorbing art that overhangs its bounds.</param>
        /// <returns>True when the bounds intersect the margin-expanded viewport.</returns>
        public static bool IsWithinViewport(LevelBounds bounds, ViewTransform view, Size renderSize, double margin)
        {
            Vec2 topLeft = view.LevelToScreen(new Vec2(bounds.X, bounds.Y));
            Vec2 bottomRight = view.LevelToScreen(new Vec2(bounds.X + bounds.W, bounds.Y + bounds.H));

            return bottomRight.X >= -margin
                && topLeft.X <= renderSize.Width + margin
                && bottomRight.Y >= -margin
                && topLeft.Y <= renderSize.Height + margin;
        }

        /// <summary>The object's fixed draw layer in the game's z-order.</summary>
        /// <remarks>
        /// The game draws objects in a fixed z-order independent of level-list order (GameScene.Draw):
        /// gravity button, Om Nom + support, vinyl discs, bubbles, bungee ropes, stars, candy (axes
        /// included), then light-bulb bottles. The vinyl (rotatedCircle) is drawn early (right after Om Nom, before bubbles
        /// and grabs), so ropes, hooks, and candy sit on top of it. Same-layer objects keep their list order
        /// because OrderBy is stable. Unknown types sit with the grabs (mid-stack) as a neutral default.
        /// </remarks>
        /// <param name="obj">The object to classify.</param>
        /// <returns>The z-order layer index (lower draws first / further back).</returns>
        public static int GameDrawLayer(LevelObject obj)
        {
            return obj.Type switch
            {
                "gravitySwitch" => 0,
                "target" => 1,
                "rotatedCircle" => 2,
                // Conveyors draw right after the vinyl discs and before bubbles (GameScene.Draw), so they
                // share the vinyl tier: behind bubbles, pumps, spikes, bouncers, mice, grabs, and candy.
                "transporter" => 2,
                // Ant conveyors sit immediately after ordinary conveyors and remain behind candy.
                "ants" => 3,
                "bubble" => 3,
                "pump" => 4,
                "spike1" or "spike2" or "spike3" or "spike4" or "electro" => 5,
                "bouncer1" or "bouncer2" => 6,
                // Mice bodies draw via DrawMice() after the bouncers and just before the socks.
                "gap" => 7,
                "sock" => 7,
                // Bamboo tubes draw right after the bouncers and before the socks and steam tubes,
                // matching GameScene.Draw (bouncers → bamboo tubes → hands → socks → steam tubes).
                "pipe" => 7,
                "steamTube" => 8,
                "ghost" => 9,
                "grab" => 10,
                "star" => 11,
                "candy" or "candyL" or "candyR" => 12,
                "lantern" => 12,
                // The game keeps axes in the candies list, so they draw in the same whole-candy pass.
                "axe" => 12,
                "lightBulb" => 13,
                "tutorialText" => 14,
                "tutorial01" or "tutorial02" or "tutorial03" or "tutorial04" or "tutorial05" or "tutorial06"
                    or "tutorial07" or "tutorial08" or "tutorial09" or "tutorial10" or "tutorial11" => 15,
                _ => 10,
            };
        }

        /// <summary>Whether <see cref="DrawObject"/> honours the live-preview position for this object's type.</summary>
        /// <remarks>
        /// Most types draw their sprite at the preview position, so their bounds travel with them. The types
        /// listed here draw from authored geometry instead — an ant conveyor lays its trail along the whole
        /// authored path and only marches individual ants down it, a conveyor, hand, and tutorial ignore preview
        /// time entirely — so their bounds must stay put. This mirrors the early returns in
        /// <see cref="DrawObject"/>: whenever a branch there draws without using <c>drawOffset</c>, its type
        /// belongs in this list, or <see cref="DrawOffset"/> will report a move that never happens and the cull
        /// box will slide off the art. <c>ViewportCullingTests.DrawsAtPreviewPositionMatchesTheDrawBranches</c>
        /// locks the list so the pairing is not silently broken.
        /// </remarks>
        /// <param name="obj">The object to classify.</param>
        /// <returns>True when the drawn position follows live preview, and so the cull box must follow it too.</returns>
        public static bool DrawsAtPreviewPosition(LevelObject obj)
        {
            return !TutorialObject.IsText(obj.Type)
                && !TutorialObject.IsImage(obj.Type)
                && obj.Type != "transporter"
                && !AntPath.IsAnts(obj.Type)
                && !HandObject.IsHand(obj.Type);
        }

        /// <summary>
        /// Draws a non-grab object: its optional decorative back-layer variant, then every sprite layer,
        /// then any overlays. Grabs go through <see cref="DrawGrab"/> instead so their rope can slot between
        /// hook layers.
        /// </summary>
        /// <remarks>
        /// The drawn position arrives as <paramref name="drawOffset"/> rather than being derived here, so the
        /// caller's viewport cull and this draw read one answer and cannot disagree about where the object is.
        /// Every branch below that draws without applying it must have its type excluded by
        /// <see cref="DrawsAtPreviewPosition"/>, which is what keeps the two aligned for the rest.
        /// </remarks>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve the object's art.</param>
        /// <param name="obj">The object to draw.</param>
        /// <param name="candySkin">Active candy skin index.</param>
        /// <param name="omNomSupport">Active Om Nom support index.</param>
        /// <param name="nightLevel">Whether night sprite variants apply.</param>
        /// <param name="starDurationText">Brush for the timed-star duration label.</param>
        /// <param name="objects">All level objects, used to decide whether binding id labels are needed.</param>
        /// <param name="drawOffset">How far preview has moved the object, from <see cref="DrawOffset"/>.</param>
        /// <param name="animationPreviewSeconds">Elapsed live-preview seconds, or null for authored static rendering.</param>
        /// <param name="tutorialBounds">Screen bounds for tutorial custom draw operations.</param>
        /// <param name="tutorialDark">Whether tutorials use dark blank-canvas styling.</param>
        public static void DrawObject(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            LevelObject obj,
            int candySkin,
            int omNomSupport,
            bool nightLevel,
            IBrush starDurationText,
            IReadOnlyList<LevelObject> objects,
            Vec2 drawOffset,
            double? animationPreviewSeconds = null,
            Rect tutorialBounds = default,
            bool tutorialDark = false)
        {
            if (TutorialObject.IsText(obj.Type))
            {
                TutorialRenderer.DrawText(ctx, v, sprites, obj, tutorialBounds, tutorialDark);
                return;
            }

            if (TutorialObject.IsImage(obj.Type))
            {
                TutorialRenderer.DrawIcon(ctx, v, sprites, obj, tutorialBounds, tutorialDark);
                return;
            }

            double x = obj.X + drawOffset.X;
            double y = obj.Y + drawOffset.Y;
            double? spinRotation = SpinPreviewRotation(obj, animationPreviewSeconds);
            if (obj.Type == "steamTube")
            {
                DrawSteamTubeBack(ctx, v, sprites, x, y, ObjectRotation.StoredAngle(obj, RotationTable.For("steamTube")!));
                return;
            }
            if (obj.Type == "star" && StarTimeout(obj) is double timeout && timeout > 0)
            {
                if (sprites.GetSprite("star_timed") is { } timed)
                {
                    DrawSprite(ctx, v, timed, x, y, spinRotation);
                }
                if (sprites.GetSprite(CanvasSpriteKey("star", nightLevel), candySkin, omNomSupport) is { } star)
                {
                    DrawSprite(ctx, v, star, x, y, spinRotation);
                    DrawDurationLabel(ctx, v, star, x, y, timeout, starDurationText);
                }
                DrawOverlays(ctx, v, sprites, obj, x, y);
                DrawBindingIdLabel(ctx, v, obj, objects, x, y);
                return;
            }

            if (obj.Type == LanternObject.Element)
            {
                if (sprites.GetSprite(LanternObject.SpriteKey(obj), candySkin, omNomSupport) is { } lanternBody)
                {
                    DrawSprite(ctx, v, lanternBody, x, y, spinRotation);
                }
                if (LanternObject.IsCaptured(obj) && sprites.GetLanternInnerCandy(candySkin) is { } innerCandy)
                {
                    DrawLayer(ctx, v, innerCandy, x, y, 1.0, spinRotation, LanternInnerCandyOffsetY);
                }
                DrawOverlays(ctx, v, sprites, obj, x, y);
                DrawBindingIdLabel(ctx, v, obj, objects, x, y);
                return;
            }

            if (obj.Type == "transporter")
            {
                ConveyorRenderer.Draw(ctx, v, sprites, obj);
                DrawBindingIdLabel(ctx, v, obj, objects, x, y);
                return;
            }

            if (AntPath.IsAnts(obj.Type))
            {
                AntRenderer.Draw(ctx, v, sprites, obj, animationPreviewSeconds);
                return;
            }

            if (HandObject.IsHand(obj.Type))
            {
                HandRenderer.Draw(ctx, v, sprites, obj);
                return;
            }

            if (obj.Type == "gap")
            {
                if (sprites.GetSprite(CanvasSpriteKey("gap", nightLevel), candySkin, omNomSupport) is { } mouseSprite
                    && mouseSprite.Layers.Count > 0)
                {
                    // The hole (layer 0) stays upright, matching the game's DrawHole; the body and eyes
                    // (layers 1+) rotate by the authored angle, as Mouse.Update rotates the body/eyes
                    // container but not the hole.
                    DrawLayer(ctx, v, mouseSprite.Layers[0], x, y, mouseSprite.Scale, null);
                    double deg = ObjectRotation.DisplayDegrees(obj, RotationTable.For("gap")!) + (spinRotation ?? 0.0);
                    for (int i = 1; i < mouseSprite.Layers.Count; i++)
                    {
                        DrawLayer(ctx, v, mouseSprite.Layers[i], x, y, mouseSprite.Scale, deg);
                    }
                }
                DrawOverlays(ctx, v, sprites, obj, x, y);
                DrawBindingIdLabel(ctx, v, obj, objects, x, y);
                return;
            }

            if (obj.Type == "pipe")
            {
                if (sprites.GetSprite(CanvasSpriteKey("pipe", nightLevel), candySkin, omNomSupport) is { } pipeSprite
                    && pipeSprite.Layers.Count > 0)
                {
                    // The core (layer 0) stays upright as the pivot, matching BambooTube's unrotated core
                    // quad; the back and front shells (layers 1+) rotate by the authored angle, as
                    // UpdateBambooRotation spins the shell sprites but leaves the core still.
                    double deg = ObjectRotation.DisplayDegrees(obj, RotationTable.For("pipe")!) + (spinRotation ?? 0.0);
                    DrawLayer(ctx, v, pipeSprite.Layers[0], x, y, pipeSprite.Scale, null);
                    for (int i = 1; i < pipeSprite.Layers.Count; i++)
                    {
                        DrawLayer(ctx, v, pipeSprite.Layers[i], x, y, pipeSprite.Scale, deg);
                    }
                }
                DrawOverlays(ctx, v, sprites, obj, x, y);
                DrawBindingIdLabel(ctx, v, obj, objects, x, y);
                return;
            }

            // Rocket launcher base: when isRotatable, LoadRocket adds an upright marker (quad 0) behind
            // the rocket. Draw it unrotated at the object anchor; the body then draws rotated by the
            // generic RotationTable block below.
            if (obj.Type == "rocket" && obj.GetAttr("isRotatable") == "true"
                && sprites.GetSprite(CanvasSpriteKey("rocket_launcher", nightLevel), candySkin, omNomSupport) is { } launcher
                && launcher.Layers.Count > 0)
            {
                DrawLayer(ctx, v, launcher.Layers[0], x, y, launcher.Scale, null);
            }

            if (RotationTable.For(obj.Type) is { } rotSpec)
            {
                string rotKey = PreviewSpriteKey(obj, animationPreviewSeconds);
                if (sprites.GetSprite(CanvasSpriteKey(rotKey, nightLevel), candySkin, omNomSupport) is { } rotSprite)
                {
                    double deg = ObjectRotation.DisplayDegrees(obj, rotSpec) + (spinRotation ?? 0.0);
                    double offsetY = SockPlacementOffsetY(obj, rotSprite);
                    foreach (SpriteLayerDraw layer in rotSprite.Layers)
                    {
                        DrawLayer(ctx, v, layer, x, y, rotSprite.Scale, deg, offsetY);
                    }
                    // Timed rocket: like the timed star, show the burn-time countdown at the rocket's top.
                    // time = -1 fires until impact (no label); a positive value burns for that many seconds.
                    if (obj.Type == "rocket" && RocketTime(obj) is double burnTime && burnTime > 0)
                    {
                        DrawDurationLabel(ctx, v, rotSprite, x, y + offsetY, burnTime, starDurationText);
                    }
                    DrawOverlays(ctx, v, sprites, obj, x, y);
                    DrawBindingIdLabel(ctx, v, obj, objects, x, y + offsetY);
                    return;
                }
                DrawOverlays(ctx, v, sprites, obj, x, y);
                DrawBindingIdLabel(ctx, v, obj, objects, x, y);
                return;
            }

            string spriteKey = PreviewSpriteKey(obj, animationPreviewSeconds);
            ObjectSprite? sprite = sprites.GetSprite(CanvasSpriteKey(spriteKey, nightLevel), candySkin, omNomSupport);
            bool candyCaptured = obj.Type == "candy"
                && LanternObject.IsPrimaryCandy(obj, objects)
                && LanternObject.AnyCaptured(objects);
            if (sprite is not null)
            {
                if (candyCaptured)
                {
                    using (ctx.PushOpacity(CapturedPrimaryCandyOpacity))
                    {
                        DrawGenericSprite(ctx, v, obj, sprite, x, y, spinRotation);
                    }
                }
                else
                {
                    DrawGenericSprite(ctx, v, obj, sprite, x, y, spinRotation);
                }
            }
            DrawOverlays(ctx, v, sprites, obj, x, y);
            DrawBindingIdLabel(ctx, v, obj, objects, x, y);
        }

        /// <summary>Draws a generic sprite's selected back-layer variant followed by its main layers.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="obj">Object used to select a stable decorative variant.</param>
        /// <param name="sprite">Resolved sprite to draw.</param>
        /// <param name="x">Level-space X coordinate.</param>
        /// <param name="y">Level-space Y coordinate.</param>
        /// <param name="spinRotation">Optional live-preview rotation in degrees.</param>
        private static void DrawGenericSprite(
            DrawingContext ctx,
            ViewTransform v,
            LevelObject obj,
            ObjectSprite sprite,
            double x,
            double y,
            double? spinRotation)
        {
            if (sprite.Variants.Count > 0)
            {
                DrawLayer(ctx, v, sprite.Variants[SpriteVariantPicker.Pick(obj.Element, sprite.Variants.Count)], x, y, sprite.Scale, spinRotation);
            }
            DrawSprite(ctx, v, sprite, x, y, spinRotation);
        }

        /// <summary>
        /// Draws a vinyl (rotatedCircle) using the distinct body, sticker, center, and controller scales from
        /// the game's <c>RotatedCircle.SetSize</c>: the body + center (unrotated, radially symmetric), the highlight sheen and label as two
        /// mirrored halves (the label spins with <c>handleAngle</c>, the sheen is a fixed top light), then
        /// the handle sprite at each handle position (one when oneHandle is set), dome pointing outward.
        /// Mirrored halves and handle rotation use a <see cref="Matrix"/> pushed onto the context, the same
        /// approach as the grab rail; the disc body/center draw unrotated like the game's RotatedCircle.Draw.
        /// </summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve the disc, highlight, sticker, and handle art.</param>
        /// <param name="obj">The vinyl object to draw.</param>
        /// <param name="activeHandle">The handle being dragged or hovered, which shows the active-controller glow and disc ring; None for no active handle.</param>
        /// <param name="includeHandles">Whether to draw the controller handles; false for the palette thumbnail, which shows the bare disc.</param>
        public static void DrawVinyl(
            DrawingContext ctx, ViewTransform v, SpriteCache sprites, LevelObject obj,
            VinylGeometry.Handle activeHandle = VinylGeometry.Handle.None,
            bool includeHandles = true)
        {
            Vec2 c = v.LevelToScreen(new Vec2(obj.X, obj.Y));
            double atlasToScreen = v.Zoom / SpritePlacement.MapScale;
            double baseScale = VinylGeometry.LayerScale(obj) * atlasToScreen;
            double stickerScale = VinylGeometry.StickerScale(obj) * atlasToScreen;
            double centerScale = VinylGeometry.CenterScale(obj) * atlasToScreen;
            double controllerScale = VinylGeometry.ControllerScale(obj) * atlasToScreen;

            // Active-handle state (game RotatedCircle.Draw): a white anti-aliased ring hugging the disc edge,
            // drawn behind the body so only its outer rim shows. Radius = sizeInPixels + ACTIVE_CIRCLE_WIDTH,
            // stroke = ACTIVE_CIRCLE_WIDTH + 3, both scaled by the controller scale (RTPD is identity here).
            if (activeHandle != VinylGeometry.Handle.None)
            {
                double ctrl = VinylGeometry.ControllerScale(obj);
                double sizeInPixels = VinylGeometry.HighlightFrameWidth * VinylGeometry.LayerScale(obj);
                double ringRadius = (sizeInPixels + (9.0 * ctrl)) * atlasToScreen;
                double ringStroke = 12.0 * ctrl * atlasToScreen;
                ctx.DrawEllipse(null, new Pen(Brushes.White, ringStroke), new Point(c.X, c.Y), ringRadius, ringRadius);
            }

            // Disc body (quad 0): centered, unrotated.
            if (VinylLayer(sprites, "rotatedCircle", 0) is { } body)
            {
                VinylDrawCentered(ctx, body, c, baseScale);
            }

            // Highlight sheen (quad 1): a fixed top light, two halves mirrored across the centerline.
            if (VinylLayer(sprites, "vinyl_highlight", 0) is { } highlight)
            {
                VinylDrawHighlightPair(ctx, highlight, c, baseScale);
            }

            // Label sticker (quad 2): two mirrored halves that spin with the disc by handleAngle.
            if (VinylLayer(sprites, "vinyl_sticker", 0) is { } sticker)
            {
                double a = VinylGeometry.HandleAngleDegrees(obj) * Math.PI / 180.0;
                Matrix rot = Matrix.CreateTranslation(-c.X, -c.Y)
                    * Matrix.CreateRotation(a)
                    * Matrix.CreateTranslation(c.X, c.Y);
                using (ctx.PushTransform(rot))
                {
                    VinylDrawStickerPair(ctx, sticker, c, stickerScale, v.Zoom / SpritePlacement.MapScale);
                }
            }

            // Handles (quad 5) at the handleAngle direction, dome pointing outward. The active handle also
            // shows the larger controller glow (quad 4) behind it, matching vinilActiveController.
            if (includeHandles && VinylLayer(sprites, "vinyl_handle", 0) is { } handle)
            {
                SpriteLayerDraw? glow = activeHandle != VinylGeometry.Handle.None
                    ? VinylLayer(sprites, "vinyl_active_controller", 0)
                    : null;
                double baseAngle = VinylGeometry.HandleAngleDegrees(obj);
                DrawOneHandle(VinylGeometry.Handle.Right, baseAngle - 90.0);
                if (!VinylGeometry.OneHandle(obj))
                {
                    DrawOneHandle(VinylGeometry.Handle.Left, baseAngle + 90.0);
                }

                void DrawOneHandle(VinylGeometry.Handle which, double rotationDegrees)
                {
                    Vec2 pos = VinylGeometry.VisualHandlePosition(obj, which);
                    if (activeHandle == which && glow is { } activeGlow)
                    {
                        VinylDrawHandle(ctx, v, activeGlow, pos, controllerScale, rotationDegrees);
                    }
                    VinylDrawHandle(ctx, v, handle, pos, controllerScale, rotationDegrees);
                }
            }

            // Center spindle (quad 3): drawn last so it sits over the label, matching RotatedCircle.Draw.
            if (VinylLayer(sprites, "rotatedCircle", 1) is { } center)
            {
                VinylDrawCentered(ctx, center, c, centerScale);
            }
        }

        /// <summary>A default-size vinyl object at (<paramref name="x"/>, <paramref name="y"/>), for previews and thumbnails.</summary>
        private static LevelObject DefaultVinyl(int x, int y)
        {
            return new LevelObject(new XElement(
                VinylGeometry.Element,
                new XAttribute("x", x.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("y", y.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("size", VinylGeometry.DefaultSize.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("handleAngle", "0"),
                new XAttribute("oneHandle", "false")));
        }

        /// <summary>Draws the palette drag preview for a vinyl: the real composited disc at its default size.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve the vinyl art.</param>
        /// <param name="level">Snapped drop position in level coordinates.</param>
        public static void DrawVinylPreview(DrawingContext ctx, ViewTransform v, SpriteCache sprites, Vec2 level)
        {
            DrawVinyl(ctx, v, sprites, DefaultVinyl((int)Math.Round(level.X), (int)Math.Round(level.Y)));
        }

        /// <summary>
        /// Renders a palette thumbnail of the composited vinyl disc (body, highlight, label, center — no
        /// handles) at its default size into a <paramref name="px"/>×<paramref name="px"/> bitmap.
        /// </summary>
        /// <param name="sprites">Sprite cache used to resolve the vinyl art.</param>
        /// <param name="px">Square bitmap side in pixels.</param>
        /// <returns>The rendered thumbnail, or null when the sprite scale collapses.</returns>
        public static RenderTargetBitmap? RenderVinylThumbnail(SpriteCache sprites, int px)
        {
            LevelObject obj = DefaultVinyl(0, 0);
            double radius = VinylGeometry.BodyRadius(obj);
            if (radius <= 0 || px <= 0)
            {
                return null;
            }

            const double margin = 1.5;
            double zoom = ((px / 2.0) - margin) / radius;
            ViewTransform v = new(zoom, px / 2.0, px / 2.0);
            RenderTargetBitmap rtb = new(new PixelSize(px, px), new Vector(96, 96));
            using (DrawingContext ctx = rtb.CreateDrawingContext())
            {
                DrawVinyl(ctx, v, sprites, obj, includeHandles: false);
            }
            return rtb;
        }

        /// <summary>
        /// Renders the tutorial-text palette thumbnail: the word "Text" in a neutral gray that reads on
        /// both the light and dark palette background (tutorial text has no atlas sprite of its own).
        /// </summary>
        /// <param name="px">Square bitmap side in pixels.</param>
        /// <returns>The rendered thumbnail, or null when <paramref name="px"/> is not positive.</returns>
        public static RenderTargetBitmap? RenderTutorialTextThumbnail(int px)
        {
            if (px <= 0)
            {
                return null;
            }

            FormattedText formatted = new(
                "Text",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.DefaultFontFamilyName, FontStyle.Normal, FontWeight.SemiBold),
                px * 0.34,
                new SolidColorBrush(Color.FromRgb(0x9A, 0x9A, 0x9A)));

            RenderTargetBitmap rtb = new(new PixelSize(px, px), new Vector(96, 96));
            using (DrawingContext ctx = rtb.CreateDrawingContext())
            {
                ctx.DrawText(formatted, new Point((px - formatted.Width) / 2.0, (px - formatted.Height) / 2.0));
            }
            return rtb;
        }

        /// <summary>Resolves a single atlas layer of a vinyl sprite key, or null when unavailable.</summary>
        private static SpriteLayerDraw? VinylLayer(SpriteCache sprites, string key, int index)
        {
            return sprites.GetSprite(key) is { } sprite && sprite.Layers.Count > index ? sprite.Layers[index] : null;
        }

        /// <summary>Draws a vinyl layer centered on the disc at screen scale <paramref name="s"/> (atlas px → screen px).</summary>
        private static void VinylDrawCentered(DrawingContext ctx, SpriteLayerDraw layer, Vec2 c, double s)
        {
            IntRect f = layer.Frame.Frame;
            double w = f.W * s, h = f.H * s;
            ctx.DrawImage(layer.Bitmap, new Rect(f.X, f.Y, f.W, f.H), new Rect(c.X - (w / 2), c.Y - (h / 2), w, h));
        }

        /// <summary>
        /// Draws the highlight halves with the game's bottom-center seam (anchors 12 and 9).
        /// </summary>
        private static void VinylDrawHighlightPair(DrawingContext ctx, SpriteLayerDraw layer, Vec2 c, double s)
        {
            IntRect f = layer.Frame.Frame;
            double w = f.W * s, h = f.H * s;
            Rect src = new(f.X, f.Y, f.W, f.H);
            Rect left = new(c.X - w, c.Y, w, h);
            ctx.DrawImage(layer.Bitmap, src, left);
            Matrix flip = Matrix.CreateTranslation(-c.X, 0)
                * Matrix.CreateScale(-1, 1)
                * Matrix.CreateTranslation(c.X, 0);
            using (ctx.PushTransform(flip))
            {
                ctx.DrawImage(layer.Bitmap, src, left);
            }
        }

        /// <summary>Draws the label halves around their game-authored ±1 px center pivots.</summary>
        private static void VinylDrawStickerPair(DrawingContext ctx, SpriteLayerDraw layer, Vec2 c, double s, double oneGamePixel)
        {
            IntRect f = layer.Frame.Frame;
            double w = f.W * s, h = f.H * s;
            Rect src = new(f.X, f.Y, f.W, f.H);

            double leftPivot = c.X + oneGamePixel;
            ctx.DrawImage(layer.Bitmap, src, new Rect(leftPivot - w, c.Y - (h / 2), w, h));

            double rightPivot = c.X - oneGamePixel;
            Rect rightSourceDest = new(rightPivot - w, c.Y - (h / 2), w, h);
            Matrix flip = Matrix.CreateTranslation(-rightPivot, 0)
                * Matrix.CreateScale(-1, 1)
                * Matrix.CreateTranslation(rightPivot, 0);
            using (ctx.PushTransform(flip))
            {
                ctx.DrawImage(layer.Bitmap, src, rightSourceDest);
            }
        }

        /// <summary>Draws the handle sprite centered on a handle position, rotated to point outward.</summary>
        private static void VinylDrawHandle(DrawingContext ctx, ViewTransform v, SpriteLayerDraw layer, Vec2 levelPos, double s, double rotationDegrees)
        {
            IntRect f = layer.Frame.Frame;
            Vec2 hp = v.LevelToScreen(levelPos);
            double w = f.W * s, h = f.H * s;
            Matrix rot = Matrix.CreateTranslation(-hp.X, -hp.Y)
                * Matrix.CreateRotation(rotationDegrees * Math.PI / 180.0)
                * Matrix.CreateTranslation(hp.X, hp.Y);
            using (ctx.PushTransform(rot))
            {
                ctx.DrawImage(layer.Bitmap, new Rect(f.X, f.Y, f.W, f.H), new Rect(hp.X - (w / 2), hp.Y - (h / 2), w, h));
            }
        }

        /// <summary>Returns the hidden binding id to show on a candy or bulb, or null when no label is needed.</summary>
        /// <param name="obj">The object being drawn.</param>
        /// <param name="objects">All level objects.</param>
        /// <returns>The id label for multi-object candy/bulb groups, otherwise null.</returns>
        internal static string? BindingIdLabel(LevelObject obj, IReadOnlyList<LevelObject> objects)
        {
            return obj.Type switch
            {
                "candy" => LabelForGroup(obj, objects, "candy", "candyNumber"),
                "lightBulb" or "lightbulb" => LabelForGroup(obj, objects, obj.Type, "bulbNumber"),
                "sock" => SockObject.GroupLabel(obj, objects),
                // The mouse's activation index (auto-numbered, hidden from the field panel).
                "gap" => obj.GetAttr("index"),
                _ => null,
            };
        }

        internal static string PreviewSpriteKey(LevelObject obj, double? animationPreviewSeconds)
        {
            return obj.Type switch
            {
                "electro" => ElectroAnimation.SpriteKey(obj, animationPreviewSeconds),
                "sock" => SockObject.SpriteKey(obj, SpecialEvents.IsXmas),
                _ when SpikeObject.IsSpike(obj.Type) => SpikeObject.SpriteKey(obj),
                _ => GrabRenderer.SpriteKey(obj),
            };
        }

        /// <summary>How far live preview has carried an object from where it is authored, or zero when it has not.</summary>
        /// <remarks>
        /// The single answer to "has this object moved, and by how much" — the draw, the viewport cull, the
        /// selection outline, and hit testing all resolve through it, so none of them can place the object
        /// somewhere the others do not. That matters most for the cull, which decides whether the object is
        /// drawn at all: an answer derived twice is an answer that can drift, and the two derivations
        /// disagreeing is what made movers vanish mid-playback.
        /// <para>
        /// An offset rather than a position so the answer costs nothing when there is nothing to say: the
        /// early-out returns before reading a single attribute. Types that <see cref="DrawsAtPreviewPosition"/>
        /// rejects draw from authored geometry and never move; a pathless object cannot move either, and most
        /// of a level is pathless. During preview this runs for every object every frame, and
        /// <see cref="LevelObject"/> re-reads the XML on each attribute access, so the order matters.
        /// </para>
        /// </remarks>
        /// <param name="obj">The object to locate.</param>
        /// <param name="animationPreviewSeconds">Elapsed live-preview seconds, or null when drawn static.</param>
        /// <returns>The level-unit offset from the authored position, or zero when the object draws where authored.</returns>
        public static Vec2 DrawOffset(LevelObject obj, double? animationPreviewSeconds)
        {
            if (animationPreviewSeconds is not double seconds
                || !DrawsAtPreviewPosition(obj)
                || string.IsNullOrWhiteSpace(obj.GetAttr("path")))
            {
                return default;
            }

            Vec2 moved = ObjectSpin.PreviewPosition(obj, seconds);
            return new Vec2(moved.X - obj.X, moved.Y - obj.Y);
        }

        /// <summary>Where an object draws this frame: its authored point shifted by <see cref="DrawOffset"/>.</summary>
        /// <param name="obj">The object to locate.</param>
        /// <param name="animationPreviewSeconds">Elapsed live-preview seconds, or null when drawn static.</param>
        /// <returns>The position in level units the object's art is drawn around.</returns>
        public static Vec2 DrawPosition(LevelObject obj, double? animationPreviewSeconds)
        {
            Vec2 offset = DrawOffset(obj, animationPreviewSeconds);
            return new Vec2(obj.X + offset.X, obj.Y + offset.Y);
        }

        private static double? SpinPreviewRotation(LevelObject obj, double? spinPreviewSeconds)
        {
            if (spinPreviewSeconds is not double seconds || !SpinTable.IsSpinnable(obj.Type) || !ObjectSpin.IsRotatingInPlace(obj))
            {
                return null;
            }

            double degrees = ObjectSpin.PreviewDegrees(obj, seconds);
            return degrees == 0 ? null : degrees;
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

        /// <summary>Reads a rocket's <c>time</c> attribute in seconds (burn time; -1 fires until impact).</summary>
        /// <param name="obj">The rocket object.</param>
        /// <returns>The burn time in seconds, or 0 when the attribute is absent or unparseable.</returns>
        private static double RocketTime(LevelObject obj)
        {
            return double.TryParse(obj.GetAttr("time"), NumberStyles.Float, CultureInfo.InvariantCulture, out double time)
                ? time
                : 0;
        }

        /// <summary>Resolves the sprite key for an object, applying night-level variants.</summary>
        /// <param name="obj">The object whose base sprite key is resolved first.</param>
        /// <param name="nightLevel">Whether night sprite variants apply.</param>
        /// <returns>The sprite key to draw.</returns>
        public static string CanvasSpriteKey(LevelObject obj, bool nightLevel)
        {
            return CanvasSpriteKey(obj, nightLevel, SpecialEvents.IsXmas);
        }

        /// <summary>Resolves an object's sprite key with explicit level and seasonal state.</summary>
        /// <param name="obj">Object whose state selects the base sprite.</param>
        /// <param name="nightLevel">Whether night sprite variants apply.</param>
        /// <param name="isXmas">Whether DX's Christmas event is active.</param>
        /// <returns>The fully resolved sprite key.</returns>
        public static string CanvasSpriteKey(LevelObject obj, bool nightLevel, bool isXmas)
        {
            string key = obj.Type switch
            {
                "sock" => SockObject.SpriteKey(obj, isXmas),
                _ when SpikeObject.IsSpike(obj.Type) => SpikeObject.SpriteKey(obj),
                _ => GrabRenderer.SpriteKey(obj),
            };
            return CanvasSpriteKey(key, nightLevel);
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

        /// <summary>Screen-space selection outline corners, rotated around the object anchor when the object rotates.</summary>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="obj">The selected object.</param>
        /// <param name="bounds">The unrotated level-space selection bounds.</param>
        /// <returns>Four screen-space corners ordered clockwise from top-left.</returns>
        public static Point[] SelectionOutlinePoints(ViewTransform v, LevelObject obj, LevelBounds bounds)
        {
            return SelectionOutlinePointsWithPreview(v, obj, bounds, previewRotationDegrees: 0.0);
        }

        /// <summary>Screen-space selection outline corners with optional live-preview spin added.</summary>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="obj">The selected object.</param>
        /// <param name="bounds">The unrotated level-space selection bounds.</param>
        /// <param name="previewRotationDegrees">Live-preview spin degrees to add to the authored rotation.</param>
        /// <param name="animationPreviewSeconds">Elapsed live-preview seconds used to translate orbiting objects.</param>
        /// <returns>Four screen-space corners ordered clockwise from top-left.</returns>
        public static Point[] SelectionOutlinePointsWithPreview(
            ViewTransform v,
            LevelObject obj,
            LevelBounds bounds,
            double previewRotationDegrees,
            double? animationPreviewSeconds = null)
        {
            bounds = PreviewSelectionBounds(obj, bounds, animationPreviewSeconds);
            Point[] points =
            [
                ScreenPoint(v, bounds.X, bounds.Y),
                ScreenPoint(v, bounds.X + bounds.W, bounds.Y),
                ScreenPoint(v, bounds.X + bounds.W, bounds.Y + bounds.H),
                ScreenPoint(v, bounds.X, bounds.Y + bounds.H),
            ];

            RotationSpec? rotSpec = RotationTable.For(obj.Type);
            double degrees = previewRotationDegrees + AuthoredSelectionDegrees(obj, rotSpec);
            if (degrees == 0)
            {
                return points;
            }

            Vec2 rotationCenter = SelectionRotationCenter(obj, rotSpec, animationPreviewSeconds);
            Point center = ScreenPoint(v, rotationCenter.X, rotationCenter.Y);
            double radians = degrees * Math.PI / 180.0;
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
            for (int i = 0; i < points.Length; i++)
            {
                double dx = points[i].X - center.X;
                double dy = points[i].Y - center.Y;
                points[i] = new Point(
                    center.X + (dx * cos) - (dy * sin),
                    center.Y + (dx * sin) + (dy * cos));
            }

            return points;
        }

        private static LevelBounds PreviewSelectionBounds(LevelObject obj, LevelBounds bounds, double? animationPreviewSeconds)
        {
            return Shift(bounds, DrawOffset(obj, animationPreviewSeconds));
        }

        /// <summary>Whether a level-space point is inside the selected object's drawn selection outline.</summary>
        /// <param name="obj">The object whose selection outline is being hit-tested.</param>
        /// <param name="bounds">The unrotated level-space selection bounds.</param>
        /// <param name="point">Level-space point to test.</param>
        /// <param name="previewRotationDegrees">Live-preview spin degrees to add to the authored rotation.</param>
        /// <param name="animationPreviewSeconds">Elapsed live-preview seconds used to translate orbiting objects.</param>
        /// <returns>True when <paramref name="point"/> lies inside the same rotated box drawn by <see cref="SelectionOutlinePointsWithPreview"/>.</returns>
        public static bool SelectionContains(
            LevelObject obj,
            LevelBounds bounds,
            Vec2 point,
            double previewRotationDegrees = 0.0,
            double? animationPreviewSeconds = null)
        {
            if (AntPath.IsAnts(obj.Type))
            {
                return AntPathContains(obj, point, tolerance: 16);
            }

            bounds = PreviewSelectionBounds(obj, bounds, animationPreviewSeconds);
            RotationSpec? rotSpec = RotationTable.For(obj.Type);
            double degrees = previewRotationDegrees + AuthoredSelectionDegrees(obj, rotSpec);
            if (degrees == 0)
            {
                return bounds.Contains(point);
            }

            Vec2 center = SelectionRotationCenter(obj, rotSpec, animationPreviewSeconds);
            double radians = -degrees * Math.PI / 180.0;
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
            double dx = point.X - center.X;
            double dy = point.Y - center.Y;
            Vec2 unrotated = new(
                center.X + (dx * cos) - (dy * sin),
                center.Y + (dx * sin) + (dy * cos));
            return bounds.Contains(unrotated);
        }

        /// <summary>
        /// The authored on-screen rotation of the selection box in degrees. A hand carries one angle per
        /// segment rather than a single <see cref="RotationTable"/> entry, so its box is oriented by the base
        /// segment's angle (about the base, which the null-spec rotation center already resolves to). Every
        /// other object uses its rotation spec, or 0 when it has none.
        /// </summary>
        private static double AuthoredSelectionDegrees(LevelObject obj, RotationSpec? spec)
        {
            return HandObject.IsHand(obj.Type)
                ? HandGeometry.BaseAngle(obj)
                : spec is not null ? ObjectRotation.DisplayDegrees(obj, spec) : 0.0;
        }

        private static Vec2 SelectionRotationCenter(
            LevelObject obj,
            RotationSpec? spec,
            double? animationPreviewSeconds)
        {
            Vec2 authoredPosition = new(obj.X, obj.Y);
            Vec2 previewPosition = DrawPosition(obj, animationPreviewSeconds);
            Vec2 center = spec is null ? authoredPosition : ObjectRotation.Center(obj, spec);
            return center + (previewPosition - authoredPosition);
        }

        private static Point ScreenPoint(ViewTransform v, double x, double y)
        {
            Vec2 point = v.LevelToScreen(new Vec2(x, y));
            return new Point(point.X, point.Y);
        }

        private static string? LabelForGroup(
            LevelObject obj,
            IReadOnlyList<LevelObject> objects,
            string element,
            string attribute)
        {
            List<LevelObject> group = [.. objects.Where(o => o.Type == element)];
            if (group.Count <= 1)
            {
                return null;
            }

            if (obj.GetAttr(attribute) is { Length: > 0 } key)
            {
                return key;
            }

            int index = group.IndexOf(obj);
            return index >= 0 ? index.ToString(CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// The magic hat's downward sprite offset (see <see cref="SockSprite"/>), or 0 for any other object.
        /// The hat sprite is drawn offset from its collision anchor but still rotates about it, so the offset
        /// applies to placement (and marquee / label) but never to the rotation center.
        /// </summary>
        private static double SockPlacementOffsetY(LevelObject obj, ObjectSprite sprite)
        {
            return SockPlacementOffsetY(obj.Type, sprite);
        }

        private static double SockPlacementOffsetY(string element, ObjectSprite sprite)
        {
            return element == "sock" && sprite.Layers.Count > 0
                ? SockSprite.DrawOffsetY(sprite.Layers[0].Frame.SourceSize.H, sprite.Scale)
                : 0.0;
        }

        private static void DrawBindingIdLabel(
            DrawingContext ctx,
            ViewTransform v,
            LevelObject obj,
            IReadOnlyList<LevelObject> objects,
            double x,
            double y)
        {
            string? label = BindingIdLabel(obj, objects);
            if (label is null)
            {
                return;
            }

            FormattedText shadow = CreateBindingIdText(label, v.Zoom, Brushes.Black);
            FormattedText text = CreateBindingIdText(label, v.Zoom, Brushes.White);
            Vec2 center = v.LevelToScreen(new Vec2(x, y));
            Point origin = new(
                center.X - (text.Width / 2.0),
                center.Y - (text.Height / 2.0));
            double shadowOffset = Math.Max(1.0, 1.5 * v.Zoom);
            ctx.DrawText(shadow, new Point(origin.X + shadowOffset, origin.Y + shadowOffset));
            ctx.DrawText(text, origin);
        }

        private static FormattedText CreateBindingIdText(string text, double zoom, IBrush foreground)
        {
            return new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.DefaultFontFamilyName, FontStyle.Normal, FontWeight.Bold),
                Math.Max(10.0, 16.0 * zoom),
                foreground);
        }

        /// <summary>Draws a countdown label just inside the top of a timed object's sprite (star or rocket).</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprite">The object sprite, used to find its visible top edge.</param>
        /// <param name="x">Object anchor X in level units.</param>
        /// <param name="y">Object anchor Y in level units.</param>
        /// <param name="seconds">The countdown value in seconds.</param>
        /// <param name="foreground">Brush for the label text.</param>
        private static void DrawDurationLabel(
            DrawingContext ctx,
            ViewTransform v,
            ObjectSprite sprite,
            double x,
            double y,
            double seconds,
            IBrush foreground)
        {
            FormattedText formatted = CreateStarDurationText(FormatStarDuration(seconds), v.Zoom, foreground);

            double top = StarTop(sprite, x, y);
            Vec2 anchor = v.LevelToScreen(new Vec2(x, top));
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
        /// <param name="x">Star anchor X in level units.</param>
        /// <param name="y">Star anchor Y in level units.</param>
        /// <returns>The topmost drawn Y, or the object's Y when the sprite has no layers.</returns>
        private static double StarTop(ObjectSprite star, double x, double y)
        {
            double top = double.MaxValue;
            foreach (SpriteLayerDraw layer in star.Layers)
            {
                LevelBounds bounds = SpritePlacement.Compute(layer.Frame, x, y, star.Scale).Dest;
                top = Math.Min(top, bounds.Y);
            }
            return top == double.MaxValue ? y : top;
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
        /// <param name="animationPreviewSeconds">Elapsed mover-preview time, or null for authored position.</param>
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
            bool hookHighlighted,
            double? animationPreviewSeconds = null)
        {
            Vec2 previewPosition = DrawPosition(obj, animationPreviewSeconds);
            // The hook art and Christmas lights are DrawImage calls that PushOpacity fades; the rope is a
            // Skia custom draw op that PushOpacity does not reach, so its alpha is passed through explicitly.
            double opacity = IsInvisible(obj) ? InvisibleGrabOpacity : 1.0;
            if (opacity < 1.0)
            {
                using (ctx.PushOpacity(opacity))
                {
                    DrawGrabContent(ctx, v, sprites, obj, objects, twoParts, rope, ropeSeed, opBounds, opacity, hookHighlighted, previewPosition, animationPreviewSeconds);
                }
            }
            else
            {
                DrawGrabContent(ctx, v, sprites, obj, objects, twoParts, rope, ropeSeed, opBounds, opacity, hookHighlighted, previewPosition, animationPreviewSeconds);
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
        /// <param name="previewPosition">Preview-aware hook anchor.</param>
        /// <param name="animationPreviewSeconds">Elapsed mover-preview time, or null for static art.</param>
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
            bool hookHighlighted,
            Vec2 previewPosition,
            double? animationPreviewSeconds)
        {
            bool drawRope = GrabBeeRenderer.ShouldDrawRope(obj, animationPreviewSeconds);
            if (GrabRenderer.DrawsMovableRail(obj) && GrabRail.Of(obj) is { } rail)
            {
                // Highlight the hook while it's hovered or being slid, matching the game's mover art.
                bool active = hookHighlighted;
                GrabRenderer.DrawMovableRail(ctx, v, sprites, rail);
                if (drawRope && rope is not null)
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
                    DrawLayer(ctx, v, sprite.Variants[SpriteVariantPicker.Pick(obj.Element, sprite.Variants.Count)], previewPosition.X, previewPosition.Y, sprite.Scale);
                }
                int back = Math.Min(GrabRenderer.BackLayerCount(obj), sprite.Layers.Count);
                DrawGrabLayers(ctx, v, sprite, obj, objects, twoParts, 0, back, previewPosition);
                if (drawRope && rope is not null)
                {
                    RopeRenderer.DrawRope(ctx, v, sprites, rope, ropeSeed, opBounds, ropeOpacity);
                }
                DrawGrabLayers(ctx, v, sprite, obj, objects, twoParts, back, sprite.Layers.Count, previewPosition);
            }
            else if (drawRope && rope is not null)
            {
                RopeRenderer.DrawRope(ctx, v, sprites, rope, ropeSeed, opBounds, ropeOpacity);
            }
            DrawOverlays(ctx, v, sprites, obj, previewPosition.X, previewPosition.Y);
            DrawBee(ctx, v, sprites, obj, previewPosition, animationPreviewSeconds);
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
        /// <param name="position">Preview-aware layer anchor.</param>
        private static void DrawGrabLayers(
            DrawingContext ctx,
            ViewTransform v,
            ObjectSprite sprite,
            LevelObject obj,
            IReadOnlyList<LevelObject> objects,
            bool twoParts,
            int from,
            int to,
            Vec2 position)
        {
            double? gunAim = sprite.Layers.Count >= 3
                ? GrabRenderer.GunAimRotationDegrees(obj, objects, twoParts)
                : null;
            for (int i = from; i < to; i++)
            {
                double? rotation = gunAim is double deg && i == 1 ? deg : null;
                DrawLayer(ctx, v, sprite.Layers[i], position.X, position.Y, sprite.Scale, rotation);
            }
        }

        /// <summary>Draws the bee body and current wing frame for an actively moving grab.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve bee art.</param>
        /// <param name="obj">Grab whose movement state controls bee visibility.</param>
        /// <param name="position">Preview-aware bee anchor in level coordinates.</param>
        /// <param name="seconds">Elapsed animation-preview time, or null for static wings.</param>
        private static void DrawBee(
            DrawingContext ctx, ViewTransform v, SpriteCache sprites, LevelObject obj, Vec2 position, double? seconds)
        {
            if (!GrabBeeRenderer.HasBee(obj))
            {
                return;
            }
            Vec2 beePosition = GrabBeeRenderer.BeeAnchor(position);
            if (sprites.GetSprite("grab_bee_body") is { } body)
            {
                DrawSprite(ctx, v, body, beePosition.X, beePosition.Y);
            }
            if (sprites.GetSprite(GrabBeeRenderer.WingSpriteKey(seconds)) is { } wings)
            {
                DrawSprite(ctx, v, wings, beePosition.X, beePosition.Y);
            }
        }

        /// <summary>Draws one grab's deterministic pollen particles in the global pre-object pollen pass.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprites">Sprite cache used to resolve pollen art.</param>
        /// <param name="obj">Grab whose movement path supplies pollen positions.</param>
        /// <param name="seconds">Elapsed animation-preview time, or null for static pollen.</param>
        /// <param name="startIndex">First shared particle index, used for deterministic variation across grabs.</param>
        /// <returns>The next shared particle index after this grab's pollen.</returns>
        public static int DrawGrabPollen(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            LevelObject obj,
            double? seconds,
            int startIndex = 0)
        {
            if (sprites.GetSprite("grab_pollen") is not { } pollen)
            {
                return startIndex;
            }
            int index = startIndex;
            foreach (Vec2 point in GrabBeeRenderer.PollenPoints(obj))
            {
                GrabBeeRenderer.PollenVisual visual = GrabBeeRenderer.PollenVisualAt(index, seconds);
                using (ctx.PushOpacity(visual.Alpha))
                {
                    foreach (SpriteLayerDraw layer in pollen.Layers)
                    {
                        DrawPollenLayer(ctx, v, layer, point, visual);
                    }
                }
                index++;
            }
            return index;
        }

        /// <summary>Draws one pollen layer with the game's 1.5x base quad and independent axis scales.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="layer">Resolved pollen atlas layer.</param>
        /// <param name="position">Particle center in level coordinates.</param>
        /// <param name="visual">Current deterministic scale and alpha state.</param>
        private static void DrawPollenLayer(
            DrawingContext ctx,
            ViewTransform v,
            SpriteLayerDraw layer,
            Vec2 position,
            GrabBeeRenderer.PollenVisual visual)
        {
            SpriteLayout layout = SpritePlacement.Compute(
                layer.Frame,
                position.X,
                position.Y,
                GrabBeeRenderer.PollenQuadScale);
            double width = layout.Dest.W * visual.ScaleX;
            double height = layout.Dest.H * visual.ScaleY;
            Vec2 topLeft = v.LevelToScreen(new Vec2(position.X - (width / 2), position.Y - (height / 2)));
            Vec2 bottomRight = v.LevelToScreen(new Vec2(position.X + (width / 2), position.Y + (height / 2)));
            Rect source = new(layout.Source.X, layout.Source.Y, layout.Source.W, layout.Source.H);
            Rect destination = new(
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y);
            ctx.DrawImage(layer.Bitmap, source, destination);
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
        /// <param name="rotationDegrees">Rotation about the anchor in degrees, or null for no rotation.</param>
        public static void DrawSprite(DrawingContext ctx, ViewTransform v, ObjectSprite sprite, double x, double y, double? rotationDegrees = null)
        {
            foreach (SpriteLayerDraw layer in sprite.Layers)
            {
                DrawLayer(ctx, v, layer, x, y, sprite.Scale, rotationDegrees);
            }
        }

        /// <summary>Draws a palette preview sprite, applying any object display-offset rotation.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="sprite">The sprite to draw.</param>
        /// <param name="element">The palette element key whose preview is being drawn.</param>
        /// <param name="x">Anchor X in level units.</param>
        /// <param name="y">Anchor Y in level units.</param>
        public static void DrawSpritePreview(DrawingContext ctx, ViewTransform v, ObjectSprite sprite, string element, double x, double y)
        {
            double rotation = PreviewRotationDegrees(element);
            double? rotationOrNone = rotation == 0 ? null : rotation;
            double offsetY = SockPlacementOffsetY(element, sprite);
            foreach (SpriteLayerDraw layer in sprite.Layers)
            {
                DrawLayer(ctx, v, layer, x, y, sprite.Scale, rotationOrNone, offsetY);
            }
        }

        /// <summary>
        /// Computes the two bamboo-tube hole centres in level space. The holes sit half a bounding box
        /// from the centre along perpendicular axes, then rotate by (angle − 90°) about the origin,
        /// matching BambooTube.UpdateBambooRotation. Returned as [entry-axis, exit-axis].
        /// </summary>
        /// <param name="origin">The tube's level-space centre.</param>
        /// <param name="angleDegrees">The tube's on-screen rotation in degrees.</param>
        /// <returns>The two hole centres in level space.</returns>
        public static Vec2[] ComputeBambooHoles(Vec2 origin, double angleDegrees)
        {
            double offset = BambooBbHalfSize * 0.5 / SpritePlacement.MapScale;
            double radians = (angleDegrees - 90.0) * Math.PI / 180.0;
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
            Vec2 Rotate(double localX, double localY)
            {
                return new Vec2(
                    origin.X + (localX * cos) - (localY * sin),
                    origin.Y + (localX * sin) + (localY * cos));
            }

            return [Rotate(offset, 0), Rotate(0, offset)];
        }

        /// <summary>
        /// Draws the bamboo tube's two candy-capture holes as collision circles for the hitbox overlay.
        /// The game catches candy when its physics point comes within <see cref="BambooCaptureRadius"/>
        /// (world units) of either hole, so the circles mark exactly where candy makes contact.
        /// </summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="obj">The bamboo-tube object whose holes are drawn.</param>
        /// <param name="pen">Pen for the collision-hole outline (the hitbox pen).</param>
        /// <param name="previewSpinDegrees">Extra live-preview rotation in degrees, or 0 when static.</param>
        public static void DrawBambooHitbox(
            DrawingContext ctx, ViewTransform v, LevelObject obj, Pen pen, double previewSpinDegrees = 0.0)
        {
            double angle = ObjectRotation.DisplayDegrees(obj, RotationTable.For("pipe")!) + previewSpinDegrees;
            double screenRadius = BambooCaptureRadius / SpritePlacement.MapScale * v.Zoom;
            foreach (Vec2 hole in ComputeBambooHoles(new Vec2(obj.X, obj.Y), angle))
            {
                Vec2 s = v.LevelToScreen(hole);
                ctx.DrawEllipse(null, pen, new Point(s.X, s.Y), screenRadius, screenRadius);
            }
        }

        /// <summary>Computes the tube-art and valve centers after parent rotation.</summary>
        public static Vec2[] ComputeSteamTubePartCenters(Vec2 origin, double rotationDegrees)
        {
            double radians = rotationDegrees * Math.PI / 180.0;
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
            Vec2 RotateOffset(double localY)
            {
                return new Vec2(origin.X - (sin * localY), origin.Y + (cos * localY));
            }

            return
            [
                RotateOffset(SteamTubeGeometry.BodyDrawCenterOffset()),
                RotateOffset(SteamTubeGeometry.ValveDrawOffset),
            ];
        }

        /// <summary>Rotates a frozen puff's local timeline position around the SteamTube origin.</summary>
        public static Vec2 ComputeSteamPuffCenter(Vec2 origin, double rotationDegrees, SteamPuffSpec puff)
        {
            double radians = rotationDegrees * Math.PI / 180.0;
            double sin = Math.Sin(radians);
            double cos = Math.Cos(radians);
            return new Vec2(
                origin.X + (puff.LocalX * cos) - (puff.LocalY * sin),
                origin.Y + (puff.LocalX * sin) + (puff.LocalY * cos));
        }

        /// <summary>Draws the tube and the game's seven back-layer maximum-state puffs.</summary>
        public static void DrawSteamTubeBack(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            double x,
            double y,
            double rotationDegrees)
        {
            if (sprites.GetSprite("steamTube") is not { Layers.Count: >= 2 } pipe)
            {
                return;
            }

            Vec2[] centers = ComputeSteamTubePartCenters(new Vec2(x, y), rotationDegrees);
            DrawTrimmedLayer(ctx, v, pipe.Layers[0], centers[0].X, centers[0].Y, pipe.Scale, rotationDegrees);
            DrawTrimmedLayer(ctx, v, pipe.Layers[1], centers[1].X, centers[1].Y, pipe.Scale, rotationDegrees);
            DrawSteamPuffs(ctx, v, sprites, new Vec2(x, y), rotationDegrees, front: false);
        }

        /// <summary>Draws the game's thirteen front-layer maximum-state puffs.</summary>
        public static void DrawSteamTubeFront(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            double x,
            double y,
            double rotationDegrees)
        {
            DrawSteamPuffs(ctx, v, sprites, new Vec2(x, y), rotationDegrees, front: true);
        }

        /// <summary>Draws both passes together for the editor's topmost translucent drag preview.</summary>
        public static void DrawSteamTube(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            double x,
            double y,
            double rotationDegrees)
        {
            DrawSteamTubeBack(ctx, v, sprites, x, y, rotationDegrees);
            DrawSteamTubeFront(ctx, v, sprites, x, y, rotationDegrees);
        }

        private static void DrawSteamPuffs(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            Vec2 origin,
            double rotationDegrees,
            bool front)
        {
            if (sprites.GetSprite("steamTube_puffs") is not { Layers.Count: 33 } atlas)
            {
                return;
            }

            using (ctx.PushOpacity(SteamPuffOpacity))
            {
                foreach (SteamPuffSpec puff in SteamTubeGeometry.MaximumPlume())
                {
                    if (puff.Front != front)
                    {
                        continue;
                    }
                    Vec2 center = ComputeSteamPuffCenter(origin, rotationDegrees, puff);
                    DrawLayer(
                        ctx,
                        v,
                        atlas.Layers[puff.Quad - 2],
                        center.X,
                        center.Y,
                        atlas.Scale * puff.Scale,
                        rotationDegrees);
                }
            }
        }

        /// <summary>The rotation used by palette previews and drag ghosts for raw sprite art.</summary>
        private static double PreviewRotationDegrees(string element)
        {
            return RotationTable.For(element)?.DisplayOffset ?? 0;
        }

        /// <summary>Draws a single sprite layer for specialized renderers.</summary>
        public static void DrawSpriteLayerPublic(
            DrawingContext ctx,
            ViewTransform v,
            SpriteLayerDraw layer,
            double x,
            double y,
            double scale,
            double? rotationDegrees)
        {
            DrawLayer(ctx, v, layer, x, y, scale, rotationDegrees);
        }

        /// <summary>Draws a single sprite layer, optionally rotated about the object's anchor.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="layer">The sprite layer to draw.</param>
        /// <param name="x">Anchor X in level units.</param>
        /// <param name="y">Anchor Y in level units.</param>
        /// <param name="scale">Sprite scale factor.</param>
        /// <param name="rotationDegrees">Rotation about the anchor in degrees, or null for no rotation.</param>
        /// <param name="placementOffsetY">
        /// Level-space vertical shift of the drawn sprite that does NOT move the rotation anchor. Used by the
        /// magic hat, whose sprite is offset from its (collision) anchor but still rotates about it. See
        /// <see cref="SockSprite"/>.
        /// </param>
        private static void DrawLayer(
            DrawingContext ctx,
            ViewTransform v,
            SpriteLayerDraw layer,
            double x,
            double y,
            double scale,
            double? rotationDegrees = null,
            double placementOffsetY = 0.0)
        {
            SpriteLayout layout = SpritePlacement.Compute(layer.Frame, x, y + placementOffsetY, scale);
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

        /// <summary>Draws a TexturePacker quad by its trimmed dimensions, matching Image without restoreCutTransparency.</summary>
        private static void DrawTrimmedLayer(
            DrawingContext ctx,
            ViewTransform v,
            SpriteLayerDraw layer,
            double centerX,
            double centerY,
            double scale,
            double rotationDegrees)
        {
            double w = layer.Frame.Frame.W * scale / SpritePlacement.MapScale;
            double h = layer.Frame.Frame.H * scale / SpritePlacement.MapScale;
            Vec2 tl = v.LevelToScreen(new Vec2(centerX - (w / 2), centerY - (h / 2)));
            Vec2 br = v.LevelToScreen(new Vec2(centerX + (w / 2), centerY + (h / 2)));
            Rect source = new(layer.Frame.Frame.X, layer.Frame.Frame.Y, layer.Frame.Frame.W, layer.Frame.Frame.H);
            Rect dest = new(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
            if (rotationDegrees == 0)
            {
                ctx.DrawImage(layer.Bitmap, source, dest);
                return;
            }

            Vec2 center = v.LevelToScreen(new Vec2(centerX, centerY));
            Matrix transform = Matrix.CreateTranslation(-center.X, -center.Y)
                * Matrix.CreateRotation(rotationDegrees * Math.PI / 180.0)
                * Matrix.CreateTranslation(center.X, center.Y);
            using (ctx.PushTransform(transform))
            {
                ctx.DrawImage(layer.Bitmap, source, dest);
            }
        }

        /// <summary>Draws an object's hitbox outline for the active physics model, if it has one.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="obj">The object whose hitbox is drawn.</param>
        /// <param name="scale">Sprite scale factor, used to size the hitbox.</param>
        /// <param name="model">Which device hitbox model (desktop or phone) to compute.</param>
        /// <param name="pen">Pen for the hitbox outline.</param>
        /// <param name="previewRotationDegrees">Live-preview spin degrees to add to the authored rotation.</param>
        /// <param name="animationPreviewSeconds">Elapsed live-preview seconds used to translate orbiting objects.</param>
        public static void DrawHitbox(
            DrawingContext ctx,
            ViewTransform v,
            LevelObject obj,
            double scale,
            HitboxModel model,
            Pen pen,
            double previewRotationDegrees = 0.0,
            double? animationPreviewSeconds = null)
        {
            if (PreviewHitboxBounds(obj, scale, model, animationPreviewSeconds) is not { } b)
            {
                return;
            }
            Vec2 tl = v.LevelToScreen(new Vec2(b.X, b.Y));
            Vec2 br = v.LevelToScreen(new Vec2(b.X + b.W, b.Y + b.H));
            Rect box = new(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);

            // A rotatable object's box turns with its sprite about the same anchor (see DrawLayer). The view
            // transform is translation + uniform scale, so rotating the projected box about the projected
            // anchor equals rotating it in level space. Square boxes only diverge visibly off the axes.
            double deg = previewRotationDegrees
                + (RotationTable.For(obj.Type) is { } rotSpec ? ObjectRotation.DisplayDegrees(obj, rotSpec) : 0.0);
            if (deg != 0)
            {
                Vec2 previewPosition = DrawPosition(obj, animationPreviewSeconds);
                Vec2 center = v.LevelToScreen(previewPosition);
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

        /// <summary>
        /// Draws the candy's physics-center point — the marker hazards (spike/bouncer/bubble) actually test —
        /// as a small screen-space crosshair, sized in pixels so it stays readable at any zoom.
        /// </summary>
        public static void DrawCandyCrosshair(DrawingContext ctx, ViewTransform v, LevelObject obj, Pen pen)
        {
            Point[] points = ComputeCandyCrosshairPoints(v, obj, armScreen: 6.0);
            ctx.DrawLine(pen, points[0], points[1]);
            ctx.DrawLine(pen, points[2], points[3]);
        }

        /// <summary>Computes the candy crosshair's horizontal and vertical arm endpoints in screen space.</summary>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="obj">The candy object; the crosshair marks its physics center.</param>
        /// <param name="armScreen">Half-length of each arm in screen pixels.</param>
        /// <returns>Horizontal endpoints [left, right] followed by vertical endpoints [top, bottom].</returns>
        public static Point[] ComputeCandyCrosshairPoints(ViewTransform v, LevelObject obj, double armScreen)
        {
            Vec2 c = v.LevelToScreen(new Vec2(obj.X, obj.Y));
            return
            [
                new Point(c.X - armScreen, c.Y),
                new Point(c.X + armScreen, c.Y),
                new Point(c.X, c.Y - armScreen),
                new Point(c.X, c.Y + armScreen),
            ];
        }

        /// <summary>Computes hitbox bounds translated to the live orbit-preview position.</summary>
        public static LevelBounds? PreviewHitboxBounds(
            LevelObject obj,
            double scale,
            HitboxModel model,
            double? animationPreviewSeconds)
        {
            return HitboxTable.Compute(obj, scale, model) is not { } bounds
                ? null
                : Shift(bounds, DrawOffset(obj, animationPreviewSeconds));
        }

        /// <summary>Draws the circular path used by active <c>RC</c>/<c>RW</c> orbit movement.</summary>
        public static void DrawOrbitPath(DrawingContext ctx, ViewTransform v, LevelObject obj, Pen pathPen, Pen arrowPen)
        {
            Point[] points = ComputeOrbitPathPoints(v, obj);
            if (points.Length < 2)
            {
                return;
            }

            for (int i = 0; i < points.Length; i++)
            {
                ctx.DrawLine(pathPen, points[i], points[(i + 1) % points.Length]);
            }

            Point[] arrow = ComputeOrbitArrowPoints(v, obj);
            if (arrow.Length == 4)
            {
                ctx.DrawLine(arrowPen, arrow[0], arrow[1]);
                ctx.DrawLine(arrowPen, arrow[1], arrow[2]);
                ctx.DrawLine(arrowPen, arrow[1], arrow[3]);
            }
        }

        /// <summary>Draws the movement path used by active DX mover data.</summary>
        /// <param name="ctx">The drawing context to render into.</param>
        /// <param name="v">The view transform mapping level coordinates to screen pixels.</param>
        /// <param name="obj">The mover whose path is drawn.</param>
        /// <param name="pathPen">The pen for the path line.</param>
        /// <param name="arrowPen">The pen for the direction chevrons.</param>
        /// <param name="viewport">The canvas size in pixels, used to clip path segments to what is visible.</param>
        public static void DrawMovementPath(
            DrawingContext ctx,
            ViewTransform v,
            LevelObject obj,
            Pen pathPen,
            Pen arrowPen,
            IntSize viewport)
        {
            if (AntPath.IsAnts(obj.Type))
            {
                DrawAntMovementPath(ctx, v, obj, pathPen, arrowPen, viewport);
                return;
            }

            if (ObjectSpin.IsOrbital(obj))
            {
                DrawOrbitPath(ctx, v, obj, pathPen, arrowPen);
                return;
            }

            Point[] points = ComputeMovementPathPoints(v, obj);
            if (points.Length < 2)
            {
                return;
            }

            for (int i = 0; i < points.Length; i++)
            {
                DrawClippedPathLine(ctx, pathPen, points[i], points[(i + 1) % points.Length], viewport);
            }

            // A loop reads one way all the way round, so arrow every segment. A back-and-forth (retrace) path is
            // stored as an out-and-back palindrome whose return segments overlap the outbound ones — arrowing all
            // of them would draw contradictory '><' chevrons, so only arrow the outbound half (first length/2).
            if (MoverPath.IsRetrace(obj.GetAttr("path")))
            {
                int outboundSegments = points.Length / 2;
                for (int i = 0; i < outboundSegments; i++)
                {
                    DrawSegmentArrow(ctx, arrowPen, points[i], points[i + 1]);
                }
            }
            else
            {
                for (int i = 0; i < points.Length; i++)
                {
                    DrawSegmentArrow(ctx, arrowPen, points[i], points[(i + 1) % points.Length]);
                }
            }
        }

        /// <summary>
        /// Draws one path segment, trimmed to the visible canvas. Movement paths use sentinel offsets that run
        /// far off the map, and the dotted path pen tessellates a segment's whole screen length into dashes
        /// before rasterization can discard the off-screen ones — so a level full of long paths pays for
        /// hundreds of thousands of invisible dashes on every frame. Clipping first keeps that cost
        /// proportional to what is actually on screen.
        /// </summary>
        private static void DrawClippedPathLine(DrawingContext ctx, Pen pathPen, Point a, Point b, IntSize viewport)
        {
            Vec2 start = new(a.X, a.Y);
            Vec2 end = new(b.X, b.Y);
            if (!SegmentClip.ToViewport(ref start, ref end, viewport))
            {
                return;
            }

            ctx.DrawLine(pathPen, new Point(start.X, start.Y), new Point(end.X, end.Y));
        }

        /// <summary>Draws a small chevron at the midpoint of <paramref name="a"/>→<paramref name="b"/> pointing toward b.</summary>
        private static void DrawSegmentArrow(DrawingContext ctx, Pen arrowPen, Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double length = Math.Sqrt((dx * dx) + (dy * dy));
            if (length < 12.0)
            {
                return;
            }

            double ux = dx / length;
            double uy = dy / length;
            Point mid = new((a.X + b.X) / 2.0, (a.Y + b.Y) / 2.0);

            const double barb = 6.0;
            const double cos = 0.866; // 30° barb spread
            const double sin = 0.5;
            double rx = -ux;
            double ry = -uy;
            Point left = new(mid.X + (barb * ((rx * cos) - (ry * sin))), mid.Y + (barb * ((rx * sin) + (ry * cos))));
            Point right = new(mid.X + (barb * ((rx * cos) + (ry * sin))), mid.Y + (barb * ((-rx * sin) + (ry * cos))));
            ctx.DrawLine(arrowPen, mid, left);
            ctx.DrawLine(arrowPen, mid, right);
        }

        /// <summary>Computes screen-space points for the active DX movement path.</summary>
        public static Point[] ComputeMovementPathPoints(ViewTransform v, LevelObject obj)
        {
            if (AntPath.IsAnts(obj.Type))
            {
                Vec2[] antPoints = AntPath.Points(obj);
                Point[] antScreenPoints = new Point[antPoints.Length];
                for (int i = 0; i < antPoints.Length; i++)
                {
                    Vec2 screen = v.LevelToScreen(antPoints[i]);
                    antScreenPoints[i] = new Point(screen.X, screen.Y);
                }
                return antScreenPoints;
            }

            if (!MoverPath.HasActiveMovement(obj))
            {
                return [];
            }

            if (ObjectSpin.IsOrbital(obj))
            {
                return ComputeOrbitPathPoints(v, obj);
            }

            Vec2[] points = MoverPath.Points(new Vec2(obj.X, obj.Y), obj.GetAttr("path"));
            Point[] screenPoints = new Point[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Vec2 screen = v.LevelToScreen(points[i]);
                screenPoints[i] = new Point(screen.X, screen.Y);
            }

            return screenPoints;
        }

        private static void DrawAntMovementPath(
            DrawingContext ctx,
            ViewTransform view,
            LevelObject ants,
            Pen pathPen,
            Pen arrowPen,
            IntSize viewport)
        {
            if (EditablePath.For(ants) is not { } path || path.Points is not { Length: >= 2 } points)
            {
                return;
            }

            for (int i = 0; i < path.SegmentCount; i++)
            {
                Vec2 a = view.LevelToScreen(points[i]);
                Vec2 b = view.LevelToScreen(points[(i + 1) % points.Length]);
                Point start = new(a.X, a.Y);
                Point end = new(b.X, b.Y);
                DrawClippedPathLine(ctx, pathPen, start, end, viewport);
                DrawSegmentArrow(ctx, arrowPen, start, end);
            }
        }

        private static bool AntPathContains(LevelObject ants, Vec2 point, double tolerance)
        {
            if (EditablePath.For(ants) is not { } path || path.Points is not { Length: >= 2 } points)
            {
                return AntRenderer.Bounds(ants).Contains(point);
            }

            double toleranceSquared = tolerance * tolerance;
            for (int i = 0; i < path.SegmentCount; i++)
            {
                Vec2 a = points[i];
                Vec2 b = points[(i + 1) % points.Length];
                double dx = b.X - a.X;
                double dy = b.Y - a.Y;
                double lengthSquared = (dx * dx) + (dy * dy);
                double t = lengthSquared <= 0
                    ? 0
                    : Math.Clamp((((point.X - a.X) * dx) + ((point.Y - a.Y) * dy)) / lengthSquared, 0, 1);
                double nearestX = a.X + (dx * t);
                double nearestY = a.Y + (dy * t);
                double pointDx = point.X - nearestX;
                double pointDy = point.Y - nearestY;
                if ((pointDx * pointDx) + (pointDy * pointDy) <= toleranceSquared)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Computes screen-space points for the circular orbit path centered on authored object coordinates.</summary>
        public static Point[] ComputeOrbitPathPoints(ViewTransform v, LevelObject obj)
        {
            if (!ObjectSpin.IsOrbital(obj))
            {
                return [];
            }

            int radius = ObjectSpin.OrbitRadius(obj);
            int segments = Math.Max(24, radius);
            Point[] points = new Point[segments];
            Vec2 center = new(obj.X, obj.Y);
            for (int i = 0; i < points.Length; i++)
            {
                double angle = Math.Tau * i / points.Length;
                Vec2 level = new(
                    center.X + (Math.Cos(angle) * radius),
                    center.Y + (Math.Sin(angle) * radius));
                Vec2 screen = v.LevelToScreen(level);
                points[i] = new Point(screen.X, screen.Y);
            }

            return points;
        }

        /// <summary>Computes a small screen-space tangent arrow for the RC/RW orbit path direction.</summary>
        public static Point[] ComputeOrbitArrowPoints(ViewTransform v, LevelObject obj)
        {
            if (!ObjectSpin.IsOrbital(obj))
            {
                return [];
            }

            int radius = ObjectSpin.OrbitRadius(obj);
            if (radius <= 0)
            {
                return [];
            }

            Vec2 center = v.LevelToScreen(new Vec2(obj.X, obj.Y));
            double radiusScreen = radius * v.Zoom;
            double arrowLength = Math.Min(Math.Max(radiusScreen * 0.35, 8.0), 18.0);
            double direction = ObjectSpin.OrbitClockwise(obj) ? 0.0 : Math.PI;
            Point tail = new(
                center.X + (Math.Cos(-Math.PI / 2.0) * radiusScreen),
                center.Y + (Math.Sin(-Math.PI / 2.0) * radiusScreen));
            Point tip = new(
                tail.X + (Math.Cos(direction) * arrowLength),
                tail.Y + (Math.Sin(direction) * arrowLength));
            double barbLength = Math.Min(arrowLength * 0.6, 8.0);
            double spread = Math.PI / 6.0;
            Point barb1 = new(
                tip.X + (Math.Cos(direction + Math.PI - spread) * barbLength),
                tip.Y + (Math.Sin(direction + Math.PI - spread) * barbLength));
            Point barb2 = new(
                tip.X + (Math.Cos(direction + Math.PI + spread) * barbLength),
                tip.Y + (Math.Sin(direction + Math.PI + spread) * barbLength));

            return [tail, tip, barb1, barb2];
        }

        /// <summary>
        /// Draws an abstract force-field arrow: a shaft from <paramref name="centerLevel"/> along
        /// <paramref name="directionRadians"/> for <paramref name="lengthLevel"/> level units, capped with a
        /// V arrowhead. Object-agnostic on purpose so any directional emitter — the pump now, steam later —
        /// reuses it by passing its own push direction. The direction is a plain screen angle: the view
        /// transform is translation + uniform scale (no rotation/flip), so a level-space direction is the
        /// same on screen.
        /// </summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="centerLevel">Arrow tail (the emitter center) in level units.</param>
        /// <param name="directionRadians">Push direction, clockwise-positive in the shared Y-down space.</param>
        /// <param name="lengthLevel">Shaft length in level units.</param>
        /// <param name="pen">Pen for the shaft and head.</param>
        /// <param name="levelMarks">Optional distances from the tail where transverse ticks are drawn.</param>
        public static void DrawForceArrow(
            DrawingContext ctx,
            ViewTransform v,
            Vec2 centerLevel,
            double directionRadians,
            double lengthLevel,
            Pen pen,
            IReadOnlyList<double>? levelMarks = null)
        {
            Vec2 tipLevel = new(
                centerLevel.X + (Math.Cos(directionRadians) * lengthLevel),
                centerLevel.Y + (Math.Sin(directionRadians) * lengthLevel));
            Vec2 tail = v.LevelToScreen(centerLevel);
            Vec2 tip = v.LevelToScreen(tipLevel);
            ctx.DrawLine(pen, new Point(tail.X, tail.Y), new Point(tip.X, tip.Y));

            Point[] markPoints = ComputeForceLevelMarkPoints(
                v, centerLevel, directionRadians, levelMarks ?? [], tickWidthScreen: 10.0);
            for (int i = 0; i < markPoints.Length; i += 2)
            {
                ctx.DrawLine(pen, markPoints[i], markPoints[i + 1]);
            }

            // V arrowhead swept back from the tip, sized to the on-screen shaft so it tracks zoom.
            double shaft = GrabRadius.Distance(tail, tip);
            if (shaft <= 0)
            {
                return;
            }
            double screenDir = Math.Atan2(tip.Y - tail.Y, tip.X - tail.X);
            double head = Math.Min(shaft, 10.0);
            double spread = Math.PI / 6;
            foreach (double barb in new[] { screenDir + Math.PI - spread, screenDir + Math.PI + spread })
            {
                ctx.DrawLine(pen, new Point(tip.X, tip.Y),
                    new Point(tip.X + (Math.Cos(barb) * head), tip.Y + (Math.Sin(barb) * head)));
            }
        }

        /// <summary>Computes transverse screen-space tick endpoints along a directional force shaft.</summary>
        public static Point[] ComputeForceLevelMarkPoints(
            ViewTransform v,
            Vec2 centerLevel,
            double directionRadians,
            IReadOnlyList<double> levelMarks,
            double tickWidthScreen)
        {
            Point[] points = new Point[levelMarks.Count * 2];
            double half = tickWidthScreen / 2.0;
            double perpX = -Math.Sin(directionRadians);
            double perpY = Math.Cos(directionRadians);
            for (int i = 0; i < levelMarks.Count; i++)
            {
                Vec2 center = v.LevelToScreen(new Vec2(
                    centerLevel.X + (Math.Cos(directionRadians) * levelMarks[i]),
                    centerLevel.Y + (Math.Sin(directionRadians) * levelMarks[i])));
                points[i * 2] = new Point(center.X - (perpX * half), center.Y - (perpY * half));
                points[(i * 2) + 1] = new Point(center.X + (perpX * half), center.Y + (perpY * half));
            }
            return points;
        }

        /// <summary>Draws a curved arrow around an object with active rotateSpeed-backed spin.</summary>
        /// <param name="ctx">Destination drawing context.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="obj">Object that may carry <c>rotateSpeed</c>.</param>
        /// <param name="pen">Pen used for the arc and arrowhead.</param>
        public static void DrawSpinArrow(DrawingContext ctx, ViewTransform v, LevelObject obj, Pen pen)
        {
            if (!SpinTable.IsSpinnable(obj.Type) || !ObjectSpin.IsSpinning(obj))
            {
                return;
            }

            Point[] points = ComputeSpinArrowPoints(v, obj, radiusScreen: 18.0);
            for (int i = 1; i < points.Length - 2; i++)
            {
                ctx.DrawLine(pen, points[i - 1], points[i]);
            }

            Point tip = points[^3];
            ctx.DrawLine(pen, tip, points[^2]);
            ctx.DrawLine(pen, tip, points[^1]);
        }

        /// <summary>Computes screen-space points for the spin arrow arc plus the two arrowhead barbs.</summary>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="obj">Object with a non-zero <c>rotateSpeed</c>.</param>
        /// <param name="radiusScreen">Arrow radius in screen pixels.</param>
        /// <returns>Arc points followed by the two arrowhead barb endpoints.</returns>
        public static Point[] ComputeSpinArrowPoints(ViewTransform v, LevelObject obj, double radiusScreen)
        {
            Vec2 centerLevel = new(obj.X, obj.Y);
            Vec2 screen = v.LevelToScreen(centerLevel);
            Point center = new(screen.X, screen.Y);
            bool clockwise = ObjectSpin.SpinClockwise(obj);
            const int arcSegments = 18;
            double start = -Math.PI * 0.75;
            double sweep = Math.PI * 1.5 * (clockwise ? 1 : -1);
            Point[] points = new Point[arcSegments + 3];
            for (int i = 0; i <= arcSegments; i++)
            {
                double t = i / (double)arcSegments;
                double angle = start + (sweep * t);
                points[i] = new Point(
                    center.X + (Math.Cos(angle) * radiusScreen),
                    center.Y + (Math.Sin(angle) * radiusScreen));
            }

            Point prev = points[arcSegments - 1];
            Point tip = points[arcSegments];
            double direction = Math.Atan2(tip.Y - prev.Y, tip.X - prev.X);
            double head = Math.Min(radiusScreen * 0.45, 9.0);
            double spread = Math.PI / 6;
            points[^2] = new Point(
                tip.X + (Math.Cos(direction + Math.PI - spread) * head),
                tip.Y + (Math.Sin(direction + Math.PI - spread) * head));
            points[^1] = new Point(
                tip.X + (Math.Cos(direction + Math.PI + spread) * head),
                tip.Y + (Math.Sin(direction + Math.PI + spread) * head));
            return points;
        }
    }
}
