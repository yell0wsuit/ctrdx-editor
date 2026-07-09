using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Scene render passes: interactive <see cref="Render"/> chrome and the clean screenshot export.</summary>
    public sealed partial class LevelCanvas
    {
        /// <summary>
        /// Game-accurate grab auto-catch ring for screenshots: a dashed blue circle matching the game's
        /// <c>Grab.DrawGrabCircle</c> (RGBA 0.2/0.5/0.9, drawn as alternating segments). The on-canvas ring keeps
        /// the themed orange editor guide; this fixed color is only baked into the exported image.
        /// </summary>
        private static readonly Pen ScreenshotGrabRadiusPen =
            new(new SolidColorBrush(Color.FromArgb(255, 51, 128, 230)), 3.0)
            {
                DashStyle = new DashStyle([4, 3], 0),
            };

        /// <summary>The pixel size and view transform for a clean full-level screenshot.</summary>
        /// <param name="Size">Output bitmap size in pixels (level units x MapScale).</param>
        /// <param name="View">Transform placing the frame's top-left at pixel (0, 0).</param>
        public readonly record struct ScreenshotFrame(PixelSize Size, ViewTransform View);

        /// <summary>
        /// Computes the screenshot frame for a level. The frame width is the wider of the playfield and the
        /// background column (<paramref name="bgWidth"/>, 0 when no background), centered on the playfield;
        /// the height is the level height. Everything renders at <see cref="SpritePlacement.MapScale"/> so the
        /// output matches the game's native art resolution.
        /// </summary>
        /// <param name="levelWidth">Playfield width in level units.</param>
        /// <param name="levelHeight">Playfield height in level units.</param>
        /// <param name="bgWidth">Background column width in level units, or 0 when there is no background.</param>
        /// <returns>The output pixel size and the view transform that frames the level.</returns>
        public static ScreenshotFrame ComputeScreenshotFrame(int levelWidth, int levelHeight, double bgWidth)
        {
            double scale = SpritePlacement.MapScale;
            double frameWidth = Math.Max(levelWidth, bgWidth);
            double frameLeft = (levelWidth - frameWidth) / 2.0;
            PixelSize size = new(
                Math.Max(1, (int)Math.Round(frameWidth * scale)),
                Math.Max(1, (int)Math.Round(levelHeight * scale)));
            ViewTransform view = new(scale, -frameLeft * scale, 0.0);
            return new ScreenshotFrame(size, view);
        }

        /// <inheritdoc />
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.FillRectangle(_palette.Background, new Rect(Bounds.Size));

            LevelDocument? doc = Document;
            SpriteCache? sprites = Sprites;
            if (doc is null || sprites is null)
            {
                return;
            }

            ViewTransform v = View;
            DrawLevelContent(context, v, Bounds.Size, doc, sprites, drawGrid: true, grabRadiusPen: null, useAnimationPreview: true);

            IReadOnlyList<LevelObject> objects = doc.Objects;

            GrabRenderer.DrawRadiusRings(context, v, objects, _palette.GrabRadius, _palette.BulbRadius);

            foreach (LevelObject obj in objects)
            {
                if (ShowMovementPaths)
                {
                    LevelSceneRenderer.DrawOrbitPath(context, v, obj, _palette.OrbitPath, _palette.OrbitPathArrow);
                }
                if (!IsAnimationPreviewing(obj))
                {
                    LevelSceneRenderer.DrawSpinArrow(context, v, obj, _palette.SpinArrow);
                }
            }

            if (ShowHitboxes || ShowMobileHitboxes)
            {
                foreach (LevelObject obj in objects)
                {
                    if (sprites.GetSprite(LevelSceneRenderer.CanvasSpriteKey(obj, doc.NightLevel), ActiveCandySkin, ActiveOmNomSupport) is not { } sprite)
                    {
                        continue;
                    }
                    if (ShowHitboxes)
                    {
                        LevelSceneRenderer.DrawHitbox(context, v, obj, sprite.Scale, HitboxModel.Desktop, _palette.HitboxDesktop, PreviewSpinDegrees(obj), PreviewAnimationSeconds(obj));
                    }
                    if (ShowMobileHitboxes)
                    {
                        LevelSceneRenderer.DrawHitbox(context, v, obj, sprite.Scale, HitboxModel.Phone, _palette.HitboxPhone, PreviewSpinDegrees(obj), PreviewAnimationSeconds(obj));
                    }
                }
            }

            // A directional emitter shows which way and how far it pushes, toggled on its own: it is a force
            // region, not a collision box, and its reach is model-independent (so no desktop/mobile split).
            // Push direction is the display angle plus the field offset; reach is the game-unit flow length in
            // level units (scale / mapScale, as hitboxes).
            if (ShowForceFields)
            {
                foreach (LevelObject obj in objects)
                {
                    if (ForceFieldTable.For(obj.Type) is not { } field || RotationTable.For(obj.Type) is not { } forceSpec)
                    {
                        continue;
                    }
                    if (sprites.GetSprite(LevelSceneRenderer.CanvasSpriteKey(obj, doc.NightLevel), ActiveCandySkin, ActiveOmNomSupport) is not { } sprite)
                    {
                        continue;
                    }
                    double dir = (ObjectRotation.DisplayDegrees(obj, forceSpec) + field.DirectionOffset) * Math.PI / 180.0;
                    double reach = field.Reach * sprite.Scale / SpritePlacement.MapScale;
                    LevelSceneRenderer.DrawForceArrow(context, v, new Vec2(obj.X, obj.Y), dir, reach, _palette.ForceArrow);
                }
            }

            LevelObject? selected = SelectedObject;
            if (selected is not null)
            {
                LevelBounds sb = LevelSceneRenderer.SelectionBounds(sprites, selected, ActiveCandySkin, ActiveOmNomSupport, doc.NightLevel);
                // Both boxes are dashed; a locked object is red, an unlocked one blue.
                Pen pen = Equals(LockedObject, selected) ? _palette.ObjectLocked : _palette.ObjectSelected;
                Point[] points = LevelSceneRenderer.SelectionOutlinePointsWithPreview(v, selected, sb, PreviewSpinDegrees(selected), PreviewAnimationSeconds(selected));
                for (int i = 0; i < points.Length; i++)
                {
                    context.DrawLine(pen, points[i], points[(i + 1) % points.Length]);
                }
            }

            if (selected is not null && RotationTable.For(selected.Type) is { } rotSpec)
            {
                RotationDialRenderer.Draw(context, v, selected, rotSpec, _rotating || _dialKnobHovered);
            }

            // Translucent ghost of the object being dragged from the palette, at its snapped drop spot.
            if (_ghostActive && _ghostElement is { } ghostElement
                && sprites.GetSprite(LevelSceneRenderer.CanvasSpriteKey(ghostElement, doc.NightLevel), ActiveCandySkin, ActiveOmNomSupport) is { } ghostSprite)
            {
                using (context.PushOpacity(0.7))
                {
                    LevelSceneRenderer.DrawSpritePreview(context, v, ghostSprite, ghostElement, _ghostLevel.X, _ghostLevel.Y);
                }
            }
        }

        /// <summary>
        /// Draws the level itself — background decoration, optional border and grid, light-bulb glow, and all
        /// objects/grabs in the game's z-order — into the given surface. Interactive chrome (selection, hitboxes,
        /// ghost) is not drawn here; <see cref="Render"/> layers that on top.
        /// </summary>
        /// <param name="context">Drawing surface to render into.</param>
        /// <param name="v">View transform mapping level coordinates to screen coordinates.</param>
        /// <param name="renderSize">Size of the target surface in screen pixels.</param>
        /// <param name="doc">The level document supplying dimensions and objects.</param>
        /// <param name="sprites">Sprite cache used to resolve object and background art.</param>
        /// <param name="drawGrid">When true, draws the editor-only level border and grid; a clean screenshot omits them.</param>
        /// <param name="grabRadiusPen">
        /// When set, bakes the grab auto-catch rings into the image with this pen (the screenshot's game-blue ring);
        /// <see cref="Render"/> passes null and draws its own themed rings in the chrome pass instead.
        /// </param>
        /// <param name="useAnimationPreview">When true, applies live-preview elapsed spin to eligible objects.</param>
        private void DrawLevelContent(
            DrawingContext context,
            ViewTransform v,
            Size renderSize,
            LevelDocument doc,
            SpriteCache sprites,
            bool drawGrid,
            Pen? grabRadiusPen,
            bool useAnimationPreview)
        {
            Vec2 tl = v.LevelToScreen(new Vec2(0, 0));
            Vec2 br = v.LevelToScreen(new Vec2(doc.Width, doc.Height));

            Bitmap? bg = sprites.GetBackground(ActiveBackground);
            if (bg is not null && bg.Size is { Width: > 0, Height: > 0 } bgSize)
            {
                Bitmap? p2 = sprites.GetBackgroundP2(ActiveBackground);
                double p2Aspect = p2 is { Size: { Width: > 0 } p2s } ? p2s.Height / p2s.Width : 0.0;
                BackgroundLayout layout = BackgroundPlacement.Compute(
                    doc.Width, doc.Height, bgSize.Height / bgSize.Width,
                    p2Aspect, SpriteCache.GetBackgroundP2Y(ActiveBackground),
                    SpriteCache.GetEarthBgPosition(ActiveBackground));

                using (context.PushClip(new Rect(0, tl.Y, renderSize.Width, br.Y - tl.Y)))
                {
                    if (layout.TileHeight > 0.5)
                    {
                        Rect bgSrc = new(bgSize);
                        for (double ty = 0; ty < doc.Height; ty += layout.TileHeight)
                        {
                            context.DrawImage(bg, bgSrc, LevelSceneRenderer.LevelRectToScreen(v, layout.Left, ty, layout.Width, layout.TileHeight));
                        }
                    }

                    if (layout.P2 is { } p2b && p2 is not null)
                    {
                        context.DrawImage(p2, new Rect(p2.Size), LevelSceneRenderer.LevelRectToScreen(v, p2b.X, p2b.Y, p2b.W, p2b.H));
                    }

                    if (layout.EarthCenters.Count > 0 && sprites.GetEarthArt() is { } earthArt)
                    {
                        IntRect ef = earthArt.Frame.Frame;
                        double ew = ef.W / SpritePlacement.MapScale;
                        double eh = ef.H / SpritePlacement.MapScale;
                        Rect earthSrc = new(ef.X, ef.Y, ef.W, ef.H);
                        foreach (Vec2 ec in layout.EarthCenters)
                        {
                            context.DrawImage(
                                earthArt.Bitmap,
                                earthSrc,
                                LevelSceneRenderer.LevelRectToScreen(v, ec.X - (ew / 2.0), ec.Y - (eh / 2.0), ew, eh));
                        }
                    }
                }
            }

            if (drawGrid)
            {
                context.DrawRectangle(null, _palette.LevelBorder,
                    new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y));

                int grid = doc.GridSize > 0 ? doc.GridSize : 32;
                for (int gx = 0; gx <= doc.Width; gx += grid)
                {
                    Vec2 a = v.LevelToScreen(new Vec2(gx, 0));
                    Vec2 b = v.LevelToScreen(new Vec2(gx, doc.Height));
                    context.DrawLine(_palette.Grid, new Point(a.X, a.Y), new Point(b.X, b.Y));
                }
                for (int gy = 0; gy <= doc.Height; gy += grid)
                {
                    Vec2 a = v.LevelToScreen(new Vec2(0, gy));
                    Vec2 b = v.LevelToScreen(new Vec2(doc.Width, gy));
                    context.DrawLine(_palette.Grid, new Point(a.X, a.Y), new Point(b.X, b.Y));
                }
            }

            IReadOnlyList<LevelObject> objects = doc.Objects;
            Rect opBounds = new(renderSize);

            // Light-bulb lit-glow halos: an additive Skia pass under the bottles (game's DrawLight order).
            List<(Vec2 Center, double Radius)> glowBulbs = [];
            foreach (LevelObject o in objects)
            {
                if (o.Type == "lightBulb" && RadiusRing.Of(o) is { } ring)
                {
                    glowBulbs.Add((new Vec2(o.X, o.Y), ring.Radius));
                }
            }
            if (glowBulbs.Count > 0 && sprites.GetSprite("lightBulb_glow") is { Layers.Count: >= 1 } glow)
            {
                SpriteLayerDraw glowLayer = glow.Layers[0];
                context.Custom(new GlowDrawOperation(opBounds, v, glowLayer.Bitmap, glowLayer.Frame.Frame, glowBulbs));
            }

            // Draw in the game's fixed z-order (GameScene.Draw), a stable sort so same-layer objects keep list order.
            int ropeSeed = 0;
            foreach (LevelObject obj in objects.OrderBy(LevelSceneRenderer.GameDrawLayer))
            {
                if (obj.Type == "grab")
                {
                    RopeVisual? rope = RopeRenderer.BuildRope(obj, objects, doc.TwoParts, ActiveRopeSkin);
                    // The movable hook lights up while the selected grab's hook is hovered or being slid.
                    bool hookHighlighted =
                        (_railDrag == GrabRail.Handle.SlideHook || _hookHovered) && Equals(obj, SelectedObject);
                    LevelSceneRenderer.DrawGrab(context, v, sprites, obj, objects, doc.TwoParts, rope, ropeSeed, opBounds, hookHighlighted);
                    if (rope is not null)
                    {
                        ropeSeed++;
                    }
                }
                else
                {
                    LevelSceneRenderer.DrawObject(context, v, sprites, obj, ActiveCandySkin, ActiveOmNomSupport, doc.NightLevel,
                        ActiveBackground > 0 ? Brushes.Black : _palette.StarDurationText,
                        objects,
                        useAnimationPreview && IsAnimationPreviewing(obj) ? AnimationPreviewElapsedSeconds : null);
                }
            }

            if (grabRadiusPen is not null)
            {
                GrabRenderer.DrawGrabRadiusRings(context, v, objects, grabRadiusPen);
            }
        }

        /// <summary>
        /// Renders the whole level clean (no grid, border, selection, hitboxes, or ghost) onto an opaque
        /// black backdrop at the game's native scale, for saving as a screenshot. Returns null when there is
        /// no document or sprite cache.
        /// </summary>
        public RenderTargetBitmap? RenderLevelToBitmap()
        {
            if (Document is not { } doc || Sprites is not { } sprites)
            {
                return null;
            }

            double bgWidth = ActiveBackground > 0 ? BackgroundPlacement.LevelScreenWidth : 0.0;
            ScreenshotFrame frame = ComputeScreenshotFrame(doc.Width, doc.Height, bgWidth);

            RenderTargetBitmap rtb = new(frame.Size, new Vector(96, 96));
            Size renderSize = new(frame.Size.Width, frame.Size.Height);
            using (DrawingContext ctx = rtb.CreateDrawingContext())
            {
                ctx.FillRectangle(Brushes.Black, new Rect(renderSize));
                DrawLevelContent(ctx, frame.View, renderSize, doc, sprites, drawGrid: false, grabRadiusPen: ScreenshotGrabRadiusPen, useAnimationPreview: false);
            }
            return rtb;
        }

        private bool IsAnimationPreviewing(LevelObject obj)
        {
            return AnimationPreviewMode == CtrDxEditor.ViewModels.AnimationPreviewMode.All
                || (AnimationPreviewMode == CtrDxEditor.ViewModels.AnimationPreviewMode.Focused && Equals(AnimationPreviewObject, obj));
        }

        private double PreviewSpinDegrees(LevelObject obj)
        {
            return IsAnimationPreviewing(obj) ? ObjectSpin.PreviewDegrees(obj, AnimationPreviewElapsedSeconds) : 0.0;
        }

        private double? PreviewAnimationSeconds(LevelObject obj)
        {
            return IsAnimationPreviewing(obj) ? AnimationPreviewElapsedSeconds : null;
        }
    }
}
