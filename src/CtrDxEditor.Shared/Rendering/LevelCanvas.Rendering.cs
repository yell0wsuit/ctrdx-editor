using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Descriptors;
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

        /// <summary>
        /// Screen-pixel slack added around the viewport when culling objects, absorbing art that overhangs its
        /// bounds box (duration text, tutorial overhang, glow halos) so nothing pops at the edge.
        /// </summary>
        private const double CullMargin = 256;

        /// <summary>
        /// Whether handles should draw from selection alone rather than waiting for hover. Touch screens
        /// report no hover, so a hover-gated handle is unreachable: it never appears, so it is never grabbed.
        /// </summary>
        private bool ShowHandlesWithoutHover => _lastPointerWasTouch;

        /// <summary>Cross-frame memo for the background layout; see <see cref="BackgroundLayoutCache"/>.</summary>
        private readonly BackgroundLayoutCache _backgroundLayout = new();

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

            IReadOnlyList<LevelObject> objects = [.. doc.AllObjects.Where(obj => !IsHidden(obj))];

            GrabRenderer.DrawRadiusRings(context, v, objects, _palette.GrabRadius, _palette.BulbRadius, PreviewAnimationSeconds);

            IntSize viewport = new((int)Math.Ceiling(Bounds.Width), (int)Math.Ceiling(Bounds.Height));
            foreach (LevelObject obj in objects)
            {
                if (ShowMovementPaths)
                {
                    // A Timed tutorial prompt draws its own path: TutorialMotion.Timed falls back to the
                    // game's moveSpeed default (100) when none is authored, so the shared mover's "moveSpeed
                    // must be authored and positive" active-movement gate would otherwise hide the line (and
                    // leave the ease markers below floating with nothing to sit on) for a prompt that really
                    // does move in the game.
                    bool timedTutorial = (TutorialObject.IsText(obj.Type) || TutorialObject.IsImage(obj.Type))
                        && TutorialMotion.ModeOf(obj) == TutorialMotionMode.Timed;
                    if (timedTutorial)
                    {
                        LevelSceneRenderer.DrawTutorialMotionPath(context, v, obj, _palette.OrbitPath, _palette.OrbitPathArrow, viewport);
                    }
                    else
                    {
                        LevelSceneRenderer.DrawMovementPath(context, v, obj, _palette.OrbitPath, _palette.OrbitPathArrow, viewport);
                    }
                    LevelSceneRenderer.DrawTutorialEaseMarkers(context, v, obj, _palette.OrbitPathArrow);
                }
                LevelSceneRenderer.DrawTutorialArea(context, v, obj, _palette.TutorialArea);
                if (!IsAnimationPreviewing(obj))
                {
                    LevelSceneRenderer.DrawSpinArrow(context, v, obj, _palette.SpinArrow);
                }
                DrawTutorialBadge(context, v, sprites, obj);
            }

            if (ShowHitboxes)
            {
                HitboxModel hitboxModel = HitboxTable.ModelFor(doc.UseMobilePhysics);
                HashSet<LevelObject> hazardCandies = [.. HazardOverlap.CandiesInHazards(doc)];
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
                        PreviewAnimationSeconds(obj),
                        doc.UseTimeTravelRocketPhysics);
                    // The bamboo tube has no rectangular bb; its collision is the two circular capture
                    // holes candy contacts, so draw those in place of a box hitbox.
                    if (obj.Type == "pipe")
                    {
                        LevelSceneRenderer.DrawBambooHitbox(
                            context, v, obj, _palette.HitboxDesktop, PreviewSpinDegrees(obj));
                    }
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

            // Every selected object gets an outline, with the primary drawn last. The fallback preserves the
            // SelectedObject compatibility surface for isolated canvas tests and callers that have not supplied
            // the full set yet.
            IEnumerable<LevelObject> outlined = SelectedObjects.Count > 0
                ? SelectedObjects
                : PrimaryObject is { } fallback ? [fallback] : [];
            foreach (LevelObject outlinedObject in outlined
                .Where(o => !IsHidden(o) && !IsAnimatingInPreview(o))
                .OrderBy(o => Equals(o, PrimaryObject) ? 1 : 0))
            {
                LevelBounds outlineBounds = LevelSceneRenderer.SelectionBounds(
                    sprites, outlinedObject, ActiveCandySkin, ActiveOmNomSupport, doc.NightLevel);
                Pen outlinePen = Equals(LockedObject, outlinedObject)
                    ? _palette.ObjectLocked
                    : _palette.ObjectSelected;
                Point[] outlinePoints = LevelSceneRenderer.SelectionOutlinePointsWithPreview(
                    v,
                    outlinedObject,
                    outlineBounds,
                    PreviewSpinDegrees(outlinedObject),
                    PreviewAnimationSeconds(outlinedObject));
                for (int i = 0; i < outlinePoints.Length; i++)
                {
                    context.DrawLine(outlinePen, outlinePoints[i], outlinePoints[(i + 1) % outlinePoints.Length]);
                }
            }

            // Specialized edit chrome is only available for one selected object and is hidden while that object
            // is a moving preview target — it isn't pickable, and stale handles would be misleading.
            LevelObject? selected = IsSingleSelection
                && PrimaryObject is { } s
                && !IsHidden(s)
                && !IsAnimatingInPreview(s)
                    ? s
                    : null;
            if (selected is not null)
            {
                LevelBounds sb = LevelSceneRenderer.SelectionBounds(sprites, selected, ActiveCandySkin, ActiveOmNomSupport, doc.NightLevel);
                Point[] points = LevelSceneRenderer.SelectionOutlinePointsWithPreview(v, selected, sb, PreviewSpinDegrees(selected), PreviewAnimationSeconds(selected));
                DrawTutorialTextResizeHandle(context, v, sprites, selected);
                DrawPolylinePointHandles(context, v, selected);
                DrawTutorialAreaCornerHandles(context, v, selected);
                DrawRopeLengthHandle(context, v);

                // Tint the active hand segment so it is clear which one the dial and fields act on, and give
                // the segment under the cursor a fainter hover tint as a "click to select" cue.
                if (HandObject.IsHand(selected.Type))
                {
                    if (_handHoverSegment > 0 && _handHoverSegment != _handActiveSegment)
                    {
                        HandRenderer.DrawSegmentHighlight(
                            context, v, sprites, selected, _handHoverSegment,
                            _palette.HandSegmentHoverTint, _palette.HandSegmentHoverTint);
                    }
                    if (_handActiveSegment > 0)
                    {
                        HandRenderer.DrawSegmentHighlight(
                            context, v, sprites, selected, _handActiveSegment,
                            _palette.HandSegmentTint, _palette.HandSegmentMark);
                    }
                }

                // Ghost segment button showing where an Alt-click would split the hovered bone.
                if (_handSplitPreview is { } splitPreview && HandObject.IsHand(selected.Type))
                {
                    HandRenderer.DrawSplitPreview(
                        context, v, sprites, splitPreview.Position, splitPreview.Rotatable, 0.6);
                }

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
                && (_waterHandleHovered || _waterDrag || ShowHandlesWithoutHover))
            {
                Vec2 left = v.LevelToScreen(new Vec2(handleBand.X, handleBand.Y));
                Vec2 right = v.LevelToScreen(new Vec2(handleBand.X + handleBand.W, handleBand.Y));
                context.DrawLine(_palette.OrbitPathArrow, new Point(left.X, left.Y), new Point(right.X, right.Y));
            }

            if (selected is not null && EditableRotationTarget(selected) is { } rotTarget)
            {
                RotationDialRenderer.Draw(
                    context,
                    v,
                    rotTarget.Center,
                    rotTarget.StoredAngle,
                    rotTarget.Spec,
                    _rotating || _dialKnobHovered);
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
                else if (HandObject.IsHand(dragPreviewElement)
                    && DescriptorTable.CtrObjects.For(dragPreviewElement) is { } handDescriptor)
                {
                    // The hand is custom-rendered from hand_parts, so preview the real articulated arm built
                    // exactly as the drop builds it (same placement defaults) rather than a composited sprite.
                    // DrawGhost composites the whole arm as one layer so its pieces fade uniformly.
                    LevelObject previewHand = Placement.CreateObject(
                        handDescriptor,
                        (int)Math.Round(_dragPreviewLevel.X),
                        (int)Math.Round(_dragPreviewLevel.Y));
                    HandRenderer.DrawGhost(
                        context, v, sprites, previewHand, 0.7,
                        TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
                }
                else if (AntPath.IsAnts(dragPreviewElement)
                    && DescriptorTable.CtrObjects.For(dragPreviewElement) is { } antDescriptor)
                {
                    LevelObject previewAnts = Placement.CreateObject(
                        antDescriptor,
                        (int)Math.Round(_dragPreviewLevel.X),
                        (int)Math.Round(_dragPreviewLevel.Y));
                    using (context.PushOpacity(0.7))
                    {
                        AntRenderer.Draw(context, v, sprites, previewAnts, elapsedSeconds: null);
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

            // Last, so no handle or outline paints over the number the drag is reporting.
            DrawDragReadout(context, v);
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
            if (EditablePath.For(obj) is not { } path)
            {
                return;
            }

            Vec2[] points = path.Points;
            if (points.Length < 2)
            {
                return;
            }

            bool canAddPoint = path.CanAdd;

            // Segment midpoint insert dots use fuller opacity so they read as interactive handles.
            if (canAddPoint)
            {
                using (context.PushOpacity(0.6))
                {
                    for (int i = 0; i < path.SegmentCount; i++)
                    {
                        Vec2 next = points[(i + 1) % points.Length];
                        Vec2 midpoint = new(
                            (points[i].X + next.X) / 2,
                            (points[i].Y + next.Y) / 2);
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
        /// Draws the four corner handles of the selected prompt's <c>inArea</c> trigger region. Only these
        /// dots are hit-testable (see <see cref="HitTutorialAreaCorner"/>); the rectangle's interior, drawn
        /// faintly for every prompt in <see cref="LevelSceneRenderer.DrawTutorialArea"/>, never intercepts a
        /// click so an object sitting inside a <c>candyMoved</c> region stays selectable.
        /// </summary>
        private void DrawTutorialAreaCornerHandles(DrawingContext context, ViewTransform v, LevelObject obj)
        {
            if ((!TutorialObject.IsText(obj.Type) && !TutorialObject.IsImage(obj.Type))
                || !TutorialArea.TryParseRuntime(obj.GetAttr("inArea"), out TutorialArea area))
            {
                return;
            }

            Vec2[] corners = TutorialAreaResize.Corners(area);
            for (int i = 0; i < corners.Length; i++)
            {
                Vec2 screen = v.LevelToScreen(corners[i]);
                Point center = new(screen.X, screen.Y);
                context.DrawEllipse(Brushes.White, _palette.TutorialArea, center, 5, 5);
                if (i == _tutorialAreaCornerHover || i == _tutorialAreaCornerDrag)
                {
                    context.DrawEllipse(null, _palette.TutorialArea, center, 8, 8);
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

            // Decoration is clipped to the level's vertical span but not its width: the background grid and
            // the water band both legitimately overhang a narrow level's sides, while nothing may spill past
            // its top or bottom edge.
            Rect levelClip = new(0, tl.Y, renderSize.Width, br.Y - tl.Y);

            Bitmap? bg = sprites.GetBackground(ActiveBackground);
            if (bg is not null && bg.Size is { Width: > 0, Height: > 0 } bgSize)
            {
                Bitmap? p2 = sprites.GetBackgroundP2(ActiveBackground);
                double p2Aspect = p2 is { Size: { Width: > 0 } p2s } ? p2s.Height / p2s.Width : 0.0;
                double p1Aspect = bgSize.Height / bgSize.Width;
                BackgroundLayout layout = _backgroundLayout.Get(
                    doc.Width, doc.Height, ActiveBackground, p1Aspect, p2Aspect,
                    () => BackgroundPlacement.Compute(
                        doc.Width, doc.Height, p1Aspect,
                        SpriteCache.GetBackgroundTextureWidth(ActiveBackground),
                        p2Aspect, SpriteCache.GetBackgroundP2Y(ActiveBackground),
                        SpriteCache.GetEarthBgPosition(ActiveBackground)));

                using (context.PushClip(levelClip))
                {
                    // The game's tile map repeats on both axes (Repeat.ALL), so this is a grid rather than a
                    // column: a level wider than one screen is backed the whole way across, not down its
                    // middle only. Both loops start at the last grid line at or before the level's own edge,
                    // since the grid is anchored on the design screen and need not begin on it.
                    if (layout.TileHeight > 0.5 && layout.Width > 0.5)
                    {
                        double firstX = BackgroundPlacement.GridStart(layout.Left, layout.Width);
                        double firstY = BackgroundPlacement.GridStart(layout.Top, layout.TileHeight);

                        // One pass per layer rather than one per column, so the layers stack in the game's
                        // order (GameScene.Draw: the whole tile map, then every p2, then every earth) and no
                        // column's art can be painted over by the next column's tiles.
                        Rect bgSrc = new(bgSize);
                        for (double tx = firstX; tx < doc.Width; tx += layout.Width)
                        {
                            for (double ty = firstY; ty < doc.Height; ty += layout.TileHeight)
                            {
                                context.DrawImage(bg, bgSrc, LevelSceneRenderer.LevelRectToScreen(v, tx, ty, layout.Width, layout.TileHeight));
                            }
                        }

                        // p2 dresses the seam on every column, but its rows stay the map's business rather
                        // than the grid's - a level of a single section has no seam to dress at all.
                        if (p2 is not null && layout.P2.Count > 0)
                        {
                            Rect p2Src = new(p2.Size);
                            for (double tx = firstX; tx < doc.Width; tx += layout.Width)
                            {
                                foreach (LevelBounds p2b in layout.P2)
                                {
                                    context.DrawImage(p2, p2Src, LevelSceneRenderer.LevelRectToScreen(v, tx, p2b.Y, p2b.W, p2b.H));
                                }
                            }
                        }

                        // The earth belongs to the p1 tile rather than to the map, so it rides the grid on
                        // both axes. Its art is drawn inside the background's own scaled matrix, so it takes
                        // the background's cover scale rather than being sized off the atlas alone.
                        if (layout.EarthOffset is { } eo && sprites.GetEarthArt() is { } earthArt)
                        {
                            IntRect ef = earthArt.Frame.Frame;
                            double ew = ef.W * layout.Scale / SpritePlacement.MapScale;
                            double eh = ef.H * layout.Scale / SpritePlacement.MapScale;
                            Rect earthSrc = new(ef.X, ef.Y, ef.W, ef.H);
                            for (double tx = firstX; tx < doc.Width; tx += layout.Width)
                            {
                                for (double ty = firstY; ty < doc.Height; ty += layout.TileHeight)
                                {
                                    context.DrawImage(
                                        earthArt.Bitmap,
                                        earthSrc,
                                        LevelSceneRenderer.LevelRectToScreen(
                                            v, tx + eo.X - (ew / 2.0), ty + eo.Y - (eh / 2.0), ew, eh));
                                }
                            }
                        }
                    }
                }
            }

            // Water's back layer sits under the scene objects, matching GameScene.Draw's split
            // (DrawBack at :93, DrawFront at :329). Under the grid too, so the grid stays readable.
            //
            // Clipped to the level because the game's bottom shadow is anchored to the screen's bottom edge
            // and drawn 323px tall from a 115px tile, so it tiles ~2.8x down and spills below the map. In
            // game those repeats fall off-screen; the editor has no screen, so the clip is what hides them.
            LevelBounds? waterBand = WaterGeometry.Band(doc.Width, doc.Height, CurrentWaterHeight(doc));
            if (waterBand is { } backBand)
            {
                using (context.PushClip(levelClip))
                {
                    WaterRenderer.DrawBack(context, v, sprites, backBand, doc.Height);
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

            IReadOnlyList<LevelObject> objects = [.. doc.AllObjects.Where(obj => !IsHidden(obj))];
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
                    RopeVisual? rope = BuildRopeForVisibleGrab(obj, doc);
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
                    // Offscreen objects are skipped: at high zoom most of the level lies outside the viewport, and
                    // drawing it costs a full sprite pass per object for no pixels. Only this branch is culled —
                    // a grab's rope reaches an arbitrary target, and a vinyl's handles extend past its disc, so
                    // neither object's own bounds predict where it draws.
                    //
                    // The object's drawn position is resolved once here and handed to both the cull and the
                    // draw. A mover is drawn where preview has carried it, so a cull that re-derived that
                    // position could disagree and drop the object mid-flight.
                    double? previewSeconds =
                        useAnimationPreview && IsAnimationPreviewing(obj) ? AnimationPreviewElapsedSeconds : null;
                    Vec2 drawOffset = LevelSceneRenderer.DrawOffset(obj, previewSeconds);
                    LevelBounds cullBounds = LevelSceneRenderer.CullBounds(
                        sprites, obj, ActiveCandySkin, ActiveOmNomSupport, doc.NightLevel, drawOffset);
                    if (!LevelSceneRenderer.IsWithinViewport(cullBounds, v, renderSize, CullMargin))
                    {
                        continue;
                    }

                    LevelSceneRenderer.DrawObject(context, v, sprites, obj, ActiveCandySkin, ActiveOmNomSupport, doc.NightLevel,
                        ActiveBackground > 0 ? Brushes.Black : _palette.StarDurationText,
                        objects,
                        drawOffset,
                        previewSeconds,
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

            // Water's front layer — surface tile and top shadow — draws over the scene objects. Clipped to
            // the level for the same reason as the back layer: the 323px top shadow runs past the map's
            // bottom edge whenever the pool is shallower than it is tall.
            if (waterBand is { } frontBand)
            {
                using (context.PushClip(levelClip))
                {
                    WaterRenderer.DrawFront(context, v, sprites, frontBand);
                }
            }

            if (grabRadiusPen is not null)
            {
                GrabRenderer.DrawGrabRadiusRings(context, v, objects, grabRadiusPen);
            }
        }

        /// <summary>
        /// Draws the selected grab's rope-length knob on its cord. A taut rope's knob is hollow: every length
        /// at or below the straight-line gap draws the same cord, so past that point the shared drag readout
        /// is the only feedback there is.
        /// </summary>
        /// <param name="context">Target drawing context.</param>
        /// <param name="v">Current level-to-screen transform.</param>
        private void DrawRopeLengthHandle(DrawingContext context, ViewTransform v)
        {
            if (SelectedRopeGeometry() is not { } g)
            {
                return;
            }

            bool dragging = _ropeDrag != RopeLength.Handle.None;
            Vec2 screen = v.LevelToScreen(g.Knob);
            Point center = new(screen.X, screen.Y);
            context.DrawEllipse(
                g.Taut ? Brushes.Transparent : Brushes.White, _palette.OrbitPathArrow, center, 5, 5);
            if (_ropeHovered || dragging)
            {
                context.DrawEllipse(null, _palette.OrbitPathArrow, center, 8, 8);
            }
        }

        private RopeVisual? BuildRopeForVisibleGrab(LevelObject grab, LevelDocument doc)
        {
            RopeTarget target = RopeResolver.Resolve(grab, doc.AllObjects, doc.TwoParts);
            return target.Target is { } boundObject && IsHidden(boundObject)
                ? null
                : RopeRenderer.BuildRope(grab, target, RopePhysics.For(doc.UseMobilePhysics), ActiveRopeSkin);
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

        /// <summary>
        /// The water height to render, in level units. Under a full animation preview the pool drains over
        /// the elapsed time as GameScene.Update does — the water falls, it never rises. Water is level-wide
        /// with no object to focus, so it only previews in All mode.
        /// </summary>
        private double CurrentWaterHeight(LevelDocument doc)
        {
            return AnimationPreviewMode == ViewModels.AnimationPreviewMode.All
                ? WaterGeometry.DrainedWater(doc.Water, doc.WaterSpeed, AnimationPreviewElapsedSeconds)
                : doc.Water;
        }
    }
}
