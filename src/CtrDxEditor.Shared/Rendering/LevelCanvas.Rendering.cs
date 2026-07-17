using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;
using CtrDxEditor.Localization;

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
            _ghostIconHits.Clear();
            if (SelectedObject is { Type: "ghost" } selectedGhost
                && _ghostPreview.Active is { } activeMorph
                && !GhostStates.Enabled(selectedGhost).Contains(activeMorph))
            {
                _ghostPreview.Clear();
            }
            DrawLevelContent(context, v, Bounds.Size, doc, sprites, drawGrid: true, grabRadiusPen: null, useAnimationPreview: true);

            IReadOnlyList<LevelObject> objects = doc.Objects;

            GrabRenderer.DrawRadiusRings(context, v, objects, _palette.GrabRadius, _palette.BulbRadius, PreviewAnimationSeconds);

            foreach (LevelObject obj in objects)
            {
                if (ShowMovementPaths)
                {
                    LevelSceneRenderer.DrawMovementPath(context, v, obj, _palette.OrbitPath, _palette.OrbitPathArrow);
                }
                if (!IsAnimationPreviewing(obj))
                {
                    LevelSceneRenderer.DrawSpinArrow(context, v, obj, _palette.SpinArrow);
                }
            }

            if (ShowHitboxes)
            {
                HitboxModel hitboxModel = HitboxTable.ModelFor(doc.UseMobilePhysics);
                System.Collections.Generic.HashSet<LevelObject> hazardCandies = [.. HazardOverlap.CandiesInHazards(doc)];
                foreach (LevelObject obj in objects)
                {
                    if (sprites.GetSprite(LevelSceneRenderer.CanvasSpriteKey(obj, doc.NightLevel), ActiveCandySkin, ActiveOmNomSupport) is not { } sprite)
                    {
                        continue;
                    }
                    LevelSceneRenderer.DrawHitbox(
                        context,
                        v,
                        obj,
                        sprite.Scale,
                        hitboxModel,
                        _palette.HitboxDesktop,
                        PreviewSpinDegrees(obj),
                        PreviewAnimationSeconds(obj));
                    if (obj.Type is "candy" or "candyL" or "candyR")
                    {
                        Pen crosshairPen = hazardCandies.Contains(obj)
                            ? _palette.CandyCrosshairAlert
                            : _palette.CandyCrosshair;
                        LevelSceneRenderer.DrawCandyCrosshair(context, v, obj, crosshairPen);
                    }
                }

                // A selected ghost previewing a bubble/bouncer morph shows that transformed object's
                // hitbox. The ghost has no HitboxTable row, so render the morph's hitbox through a
                // throwaway proxy of the morph's element type at the ghost's position (never persisted).
                // DrawHitbox reads rotation from RotationTable keyed on the proxy's type, so copying the
                // ghost's angle onto a bouncer1 proxy rotates its hitbox exactly like a real bouncer.
                if (SelectedObject is { Type: "ghost" } hbGhost
                    && _ghostPreview.MorphHitboxElement is { } hbElement)
                {
                    XElement proxyEl = new(hbElement);
                    proxyEl.SetAttributeValue("x", hbGhost.X.ToString(CultureInfo.InvariantCulture));
                    proxyEl.SetAttributeValue("y", hbGhost.Y.ToString(CultureInfo.InvariantCulture));
                    if (hbElement == "bouncer1")
                    {
                        proxyEl.SetAttributeValue("angle", hbGhost.GetAttr("angle") ?? "0");
                    }

                    LevelObject proxy = new(proxyEl);
                    if (sprites.GetSprite(LevelSceneRenderer.CanvasSpriteKey(proxy, doc.NightLevel), ActiveCandySkin, ActiveOmNomSupport) is { } proxySprite)
                    {
                        LevelSceneRenderer.DrawHitbox(
                            context,
                            v,
                            proxy,
                            proxySprite.Scale,
                            hitboxModel,
                            _palette.HitboxDesktop);
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
                    double reach = field.LevelReach(sprite.Scale);
                    LevelSceneRenderer.DrawForceArrow(
                        context,
                        v,
                        new Vec2(obj.X, obj.Y),
                        dir,
                        reach,
                        _palette.ForceArrow,
                        field.LevelMarkDistances(sprite.Scale));
                }
            }

            // Selection chrome (outline, edit handles, rotation dial) is hidden while the selected object is a
            // moving preview target — it isn't pickable, and drawing chrome at its stale home position would mislead.
            LevelObject? selected = SelectedObject is { } s && !IsAnimatingInPreview(s) ? s : null;
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
                DrawTutorialTextResizeHandle(context, v, sprites, selected);
                DrawPolylinePointHandles(context, v, selected);

                if (selected.Type == "transporter")
                {
                    ConveyorRenderer.DrawHandles(context, v, selected, _palette.OrbitPathArrow);
                }

                if (selected.Type == "ghost")
                {
                    DrawGhostBadge(context, v, selected, points);
                    if (_ghostPreview.ShowsRadiusRing(selected)
                        && GrabRadius.Of(selected) is double ghostRadius)
                    {
                        Vec2 center = v.LevelToScreen(new Vec2(selected.X, selected.Y));
                        double screenRadius = ghostRadius * v.Zoom;
                        context.DrawEllipse(
                            null,
                            _palette.GrabRadius,
                            new Point(center.X, center.Y),
                            screenRadius,
                            screenRadius);
                    }
                }
            }

            // The water surface doubles as its own drag handle; it only exists when the level has water,
            // so a water-free level shows nothing to grab and the settings dialog is the way in.
            if (WaterGeometry.Band(doc.Width, doc.Height, doc.Water) is { } handleBand
                && (_waterHandleHovered || _waterDrag))
            {
                Vec2 left = v.LevelToScreen(new Vec2(handleBand.X, handleBand.Y));
                Vec2 right = v.LevelToScreen(new Vec2(handleBand.X + handleBand.W, handleBand.Y));
                context.DrawLine(_palette.OrbitPathArrow, new Point(left.X, left.Y), new Point(right.X, right.Y));
            }

            if (selected is not null && EditableRotationSpec(selected) is { } rotSpec)
            {
                RotationDialRenderer.Draw(context, v, selected, rotSpec, _rotating || _dialKnobHovered);
            }

            // Translucent preview of the object being dragged from the palette, at its snapped drop spot.
            if (_dragPreviewActive && _dragPreviewElement is { } dragPreviewElement)
            {
                // The vinyl scales with its size, so preview the real composited disc at its default size
                // rather than the fixed-scale sprite, which would render an oversized disc.
                if (dragPreviewElement == "rotatedCircle")
                {
                    using (context.PushOpacity(0.7))
                    {
                        LevelSceneRenderer.DrawVinylPreview(context, v, sprites, _dragPreviewLevel);
                    }
                }
                else if (dragPreviewElement == ConveyorObject.Element)
                {
                    using (context.PushOpacity(0.7))
                    {
                        ConveyorRenderer.Draw(
                            context,
                            v,
                            sprites,
                            ConveyorObject.CreatePreset(_dragPreviewLevel.X, _dragPreviewLevel.Y));
                    }
                }
                else if (dragPreviewElement == TutorialObject.TextElement)
                {
                    // Tutorial text has no sprite; preview the placeholder text at its auto-fit width.
                    using (context.PushOpacity(0.7))
                    {
                        bool previewDark = ActiveBackground == 0 && ActualThemeVariant == ThemeVariant.Dark;
                        LevelObject previewText = new(new XElement(
                            TutorialObject.TextElement,
                            new XAttribute("x", ((int)Math.Round(_dragPreviewLevel.X)).ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("y", ((int)Math.Round(_dragPreviewLevel.Y)).ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("text", TutorialObject.DefaultText)));
                        TutorialObject.SetAutoWidth(previewText, true);
                        TutorialRenderer.ApplyAutoWidth(sprites, previewText);
                        TutorialRenderer.DrawText(context, v, sprites, previewText, new Rect(Bounds.Size), previewDark);
                    }
                }
                else if (TutorialObject.IsImage(dragPreviewElement))
                {
                    // Tutorial line art needs the same dark-theme inversion as a placed icon.
                    using (context.PushOpacity(0.7))
                    {
                        bool previewDark = ActiveBackground == 0 && ActualThemeVariant == ThemeVariant.Dark;
                        LevelObject previewIcon = new(new XElement(
                            dragPreviewElement,
                            new XAttribute("x", ((int)Math.Round(_dragPreviewLevel.X)).ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("y", ((int)Math.Round(_dragPreviewLevel.Y)).ToString(CultureInfo.InvariantCulture)),
                            new XAttribute("angle", "0")));
                        TutorialRenderer.DrawIcon(
                            context,
                            v,
                            sprites,
                            previewIcon,
                            new Rect(Bounds.Size),
                            previewDark);
                    }
                }
                else if (sprites.GetSprite(LevelSceneRenderer.CanvasSpriteKey(
                    dragPreviewElement == "sock" && SpecialEvents.IsXmas ? "sock_xmas" : dragPreviewElement,
                    doc.NightLevel), ActiveCandySkin, ActiveOmNomSupport) is { } dragPreviewSprite)
                {
                    using (context.PushOpacity(0.7))
                    {
                        if (dragPreviewElement == "steamTube")
                        {
                            LevelSceneRenderer.DrawSteamTube(
                                context, v, sprites, _dragPreviewLevel.X, _dragPreviewLevel.Y, 0);
                        }
                        else
                        {
                            LevelSceneRenderer.DrawSpritePreview(
                                context,
                                v,
                                dragPreviewSprite,
                                dragPreviewElement,
                                _dragPreviewLevel.X,
                                _dragPreviewLevel.Y);
                        }
                    }
                }
            }

            if (_polylineAtLimitHint && SelectedObject is { } limitObj)
            {
                DrawPolylineLimitHint(context, v, limitObj);
            }
        }

        /// <summary>Draws the selected tutorial text's wrap-width handle.</summary>
        private void DrawTutorialTextResizeHandle(
            DrawingContext context,
            ViewTransform view,
            SpriteCache sprites,
            LevelObject selected)
        {
            if (!TutorialObject.IsText(selected.Type))
            {
                return;
            }

            LevelBounds bounds = TutorialRenderer.TextBounds(sprites, selected);
            Vec2 screen = view.LevelToScreen(TutorialTextResize.HandlePosition(bounds));
            context.DrawEllipse(
                Brushes.White,
                _palette.OrbitPathArrow,
                new Point(screen.X, screen.Y),
                5,
                5);
        }

        /// <summary>Draws the selected ghost's enabled-state selector badge and records its hit targets.</summary>
        private void DrawGhostBadge(DrawingContext context, ViewTransform v, LevelObject ghost, Point[] outline)
        {
            IReadOnlyList<GhostMorph> states = GhostStates.Enabled(ghost);
            if (states.Count == 0)
            {
                return;
            }

            const double height = 26;
            const double cellPadding = 14;
            const double separatorGap = 10;
            Typeface typeface = new(FontFamily.DefaultFontFamilyName, FontStyle.Normal, FontWeight.SemiBold);

            // Measure each state's localized label (reusing the object names) so the badge fits its
            // text in any language instead of assuming fixed-width abbreviations.
            FormattedText[] labels = new FormattedText[states.Count];
            double[] cellWidths = new double[states.Count];
            double contentWidth = 0;
            for (int i = 0; i < states.Count; i++)
            {
                labels[i] = new FormattedText(
                    GhostMorphLabel(states[i]),
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    12,
                    Brushes.White);
                cellWidths[i] = labels[i].Width + cellPadding;
                contentWidth += cellWidths[i];
            }

            double separatorsWidth = Math.Max(0, states.Count - 1) * separatorGap;
            double width = contentWidth + separatorsWidth + 12;
            double centerX = v.LevelToScreen(new Vec2(ghost.X, ghost.Y)).X;
            double top = outline.Min(p => p.Y) - height - 12;
            Rect bubble = new(centerX - (width / 2), top, width, height);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(225, 25, 29, 36)), bubble, 6);

            double x = bubble.X + 6;
            for (int i = 0; i < states.Count; i++)
            {
                GhostMorph morph = states[i];
                Rect hit = new(x, top + 3, cellWidths[i], height - 6);
                if (_ghostPreview.Active == morph)
                {
                    context.FillRectangle(new SolidColorBrush(Color.FromArgb(220, 46, 139, 255)), hit, 4);
                }

                context.DrawText(labels[i], new Point(hit.Center.X - (labels[i].Width / 2), hit.Center.Y - (labels[i].Height / 2)));
                _ghostIconHits.Add((hit, morph));
                x += cellWidths[i];

                if (i < states.Count - 1)
                {
                    FormattedText separator = new(
                        "|",
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface(FontFamily.DefaultFontFamilyName),
                        12,
                        Brushes.LightGray);
                    context.DrawText(separator, new Point(x + 2, top + ((height - separator.Height) / 2)));
                    x += separatorGap;
                }
            }
        }

        /// <summary>The localized selector label for a ghost morph, matching its property-panel toggle.</summary>
        private static string GhostMorphLabel(GhostMorph morph)
        {
            return morph switch
            {
                GhostMorph.Grab => Localizer.AttributeName("grab"),
                GhostMorph.Bubble => Localizer.AttributeName("bubble"),
                GhostMorph.Bouncer => Localizer.AttributeName("bouncer"),
                _ => "?",
            };
        }

        /// <summary>Draws the active ghost morph in place of the ordinary ghost sprite.</summary>
        private void DrawGhostMorphPreview(
            DrawingContext context,
            ViewTransform v,
            SpriteCache sprites,
            LevelObject ghost)
        {
            if (_ghostPreview.MorphSpriteKey is not { } spriteKey
                || sprites.GetSprite(spriteKey) is not { } sprite)
            {
                return;
            }

            if (_ghostPreview.Active == GhostMorph.Bouncer)
            {
                Vec2 center = v.LevelToScreen(new Vec2(ghost.X, ghost.Y));
                double degrees = ObjectRotation.StoredAngle(ghost, GhostBouncerRotation);
                Matrix transform = Matrix.CreateTranslation(-center.X, -center.Y)
                    * Matrix.CreateRotation(degrees * Math.PI / 180.0)
                    * Matrix.CreateTranslation(center.X, center.Y);
                using (context.PushTransform(transform))
                {
                    LevelSceneRenderer.DrawSpritePreview(context, v, sprite, spriteKey, ghost.X, ghost.Y);
                }
                return;
            }

            LevelSceneRenderer.DrawSpritePreview(context, v, sprite, spriteKey, ghost.X, ghost.Y);
        }

        /// <summary>
        /// Draws a small labelled hint at the end of a full polyline, explaining why the append nub is gone once
        /// the path has hit its stored-point cap.
        /// </summary>
        private void DrawPolylineLimitHint(DrawingContext context, ViewTransform v, LevelObject obj)
        {
            Vec2 nub = v.LevelToScreen(PolylineNubPoint(obj));
            FormattedText text = new(
                Localizer.Get("Hint.PolylinePointLimit"),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily.DefaultFontFamilyName),
                12.0,
                Brushes.White);

            Point origin = new(nub.X + 12.0, nub.Y - (text.Height / 2.0));
            Rect box = new(origin.X - 6.0, origin.Y - 3.0, text.Width + 12.0, text.Height + 6.0);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(220, 0, 0, 0)), box, 4.0f);
            context.DrawText(text, origin);
        }

        /// <summary>Draws editable handles, segment inserts, and the append nub for the selected polyline path.</summary>
        private void DrawPolylinePointHandles(DrawingContext context, ViewTransform v, LevelObject obj)
        {
            if (!IsEditablePolyline(obj))
            {
                return;
            }

            string path = obj.GetAttr("path")!;
            Vec2[] points = MoverPath.CanonicalPoints(new Vec2(obj.X, obj.Y), path);
            if (points.Length < 2)
            {
                return;
            }

            bool canAddPoint = MoverPath.CanAddCanonicalPoint(new Vec2(obj.X, obj.Y), path);

            // Segment midpoint insert dots use fuller opacity so they read as interactive handles.
            if (canAddPoint)
            {
                using (context.PushOpacity(0.6))
                {
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        Vec2 midpoint = new(
                            (points[i].X + points[i + 1].X) / 2,
                            (points[i].Y + points[i + 1].Y) / 2);
                        Vec2 screen = v.LevelToScreen(midpoint);
                        context.DrawEllipse(Brushes.White, _palette.OrbitPathArrow,
                            new Point(screen.X, screen.Y), 3, 3);
                    }
                }
            }

            // Waypoint handles are solid, with an outer ring when hovered or dragged.
            for (int i = 1; i < points.Length; i++)
            {
                Vec2 screen = v.LevelToScreen(points[i]);
                Point center = new(screen.X, screen.Y);
                context.DrawEllipse(Brushes.White, _palette.OrbitPathArrow, center, 5, 5);
                if (i == _polylineHoverPoint || i == _polylinePointDrag)
                {
                    context.DrawEllipse(null, _palette.OrbitPathArrow, center, 8, 8);
                }
            }

            if (!canAddPoint)
            {
                return;
            }

            // The append nub follows the tip and fills when hovered.
            Vec2 nub = PolylineNubPoint(obj);
            Vec2 nubScreen = v.LevelToScreen(nub);
            Point nubCenter = new(nubScreen.X, nubScreen.Y);
            context.DrawEllipse(
                _polylineNubHot ? Brushes.White : Brushes.Transparent,
                _palette.OrbitPathArrow,
                nubCenter,
                7,
                7);
            context.DrawLine(_palette.OrbitPathArrow,
                new Point(nubScreen.X - 3, nubScreen.Y), new Point(nubScreen.X + 3, nubScreen.Y));
            context.DrawLine(_palette.OrbitPathArrow,
                new Point(nubScreen.X, nubScreen.Y - 3), new Point(nubScreen.X, nubScreen.Y + 3));
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

            // Water's back layer sits under the scene objects, matching GameScene.Draw's split
            // (DrawBack at :93, DrawFront at :329). Under the grid too, so the grid stays readable.
            LevelBounds? waterBand = WaterGeometry.Band(doc.Width, doc.Height, CurrentWaterHeight(doc));
            if (waterBand is { } backBand)
            {
                WaterRenderer.DrawBack(context, v, sprites, backBand, doc.Height);
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

            // DX batches every grab's pollen into one global pass before scene objects. Preserve object-list
            // insertion order so deterministic particle indices match the game's shared pollen drawer.
            int pollenIndex = 0;
            foreach (LevelObject grab in objects.Where(o => o.Type == "grab"))
            {
                pollenIndex = LevelSceneRenderer.DrawGrabPollen(
                    context,
                    v,
                    sprites,
                    grab,
                    useAnimationPreview && IsAnimationPreviewing(grab) ? AnimationPreviewElapsedSeconds : null,
                    pollenIndex);
            }

            // Draw in the game's fixed z-order (GameScene.Draw), a stable sort so same-layer objects keep list order.
            int ropeSeed = 0;
            bool tutorialDark = ActiveBackground == 0 && ActualThemeVariant == ThemeVariant.Dark;
            foreach (LevelObject obj in objects.OrderBy(LevelSceneRenderer.GameDrawLayer))
            {
                if (obj.Type == "grab")
                {
                    RopeVisual? rope = RopeRenderer.BuildRope(obj, objects, doc.TwoParts, ActiveRopeSkin);
                    // The movable hook lights up while the selected grab's hook is hovered or being slid.
                    bool hookHighlighted =
                        (_railDrag == GrabRail.Handle.SlideHook || _hookHovered) && Equals(obj, SelectedObject);
                    LevelSceneRenderer.DrawGrab(
                        context, v, sprites, obj, objects, doc.TwoParts, rope, ropeSeed, opBounds, hookHighlighted,
                        useAnimationPreview && IsAnimationPreviewing(obj) ? AnimationPreviewElapsedSeconds : null);
                    if (rope is not null)
                    {
                        ropeSeed++;
                    }
                }
                else if (useAnimationPreview
                         && obj.Type == "ghost"
                         && Equals(obj, SelectedObject)
                         && _ghostPreview.Active is not null)
                {
                    DrawGhostMorphPreview(context, v, sprites, obj);
                }
                else if (obj.Type == "rotatedCircle")
                {
                    VinylGeometry.Handle activeHandle = Equals(obj, SelectedObject)
                        ? (_vinylHandleDrag != VinylGeometry.Handle.None ? _vinylHandleDrag : _vinylHandleHovered)
                        : VinylGeometry.Handle.None;
                    LevelSceneRenderer.DrawVinyl(context, v, sprites, obj, activeHandle);
                }
                else
                {
                    LevelSceneRenderer.DrawObject(context, v, sprites, obj, ActiveCandySkin, ActiveOmNomSupport, doc.NightLevel,
                        ActiveBackground > 0 ? Brushes.Black : _palette.StarDurationText,
                        objects,
                        useAnimationPreview && IsAnimationPreviewing(obj) ? AnimationPreviewElapsedSeconds : null,
                        opBounds,
                        tutorialDark);
                }
            }

            // GameScene.DrawFront renders the thirteen side puffs after the late bottle/candy pass.
            foreach (LevelObject steamTube in objects.Where(o => o.Type == "steamTube"))
            {
                LevelSceneRenderer.DrawSteamTubeFront(
                    context,
                    v,
                    sprites,
                    steamTube.X,
                    steamTube.Y,
                    ObjectRotation.StoredAngle(steamTube, RotationTable.For("steamTube")!));
            }

            // Water's front layer — surface tile and top shadow — draws over the scene objects.
            if (waterBand is { } frontBand)
            {
                WaterRenderer.DrawFront(context, v, sprites, frontBand);
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
            return AnimationPreviewMode == ViewModels.AnimationPreviewMode.All
                || (AnimationPreviewMode == ViewModels.AnimationPreviewMode.Focused && Equals(AnimationPreviewObject, obj));
        }

        /// <summary>
        /// True when the object is animating during preview — moving along a path/orbit or spinning in place — so
        /// its drawn transform no longer matches its authored one. Such an object can't be picked and its selection
        /// chrome (outline, handles, rotation ring) is hidden until the preview stops.
        /// </summary>
        private bool IsAnimatingInPreview(LevelObject obj)
        {
            return IsAnimationPreviewing(obj) && (MoverPath.HasActiveMovement(obj) || ObjectSpin.IsSpinning(obj));
        }

        private double PreviewSpinDegrees(LevelObject obj)
        {
            return IsAnimationPreviewing(obj) ? ObjectSpin.PreviewDegrees(obj, AnimationPreviewElapsedSeconds) : 0.0;
        }

        private double? PreviewAnimationSeconds(LevelObject obj)
        {
            return IsAnimationPreviewing(obj) ? AnimationPreviewElapsedSeconds : null;
        }

        /// <summary>The water height to render, in level units.</summary>
        private static double CurrentWaterHeight(LevelDocument doc)
        {
            return doc.Water;
        }
    }
}
