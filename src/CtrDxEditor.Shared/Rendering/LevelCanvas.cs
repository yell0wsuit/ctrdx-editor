using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Interactive editor canvas for rendering, selecting, dragging, zooming, and placing level objects.</summary>
    public sealed class LevelCanvas : Control
    {
        /// <summary>Avalonia property backing <see cref="Document"/>.</summary>
        public static readonly StyledProperty<LevelDocument?> DocumentProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelDocument?>(nameof(Document));

        /// <summary>Avalonia property backing <see cref="Sprites"/>.</summary>
        public static readonly StyledProperty<SpriteCache?> SpritesProperty =
            AvaloniaProperty.Register<LevelCanvas, SpriteCache?>(nameof(Sprites));

        /// <summary>Avalonia property backing <see cref="View"/>.</summary>
        public static readonly StyledProperty<ViewTransform> ViewProperty =
            AvaloniaProperty.Register<LevelCanvas, ViewTransform>(nameof(View), ViewTransform.Identity);

        /// <summary>Avalonia property backing <see cref="SnapEnabled"/>.</summary>
        public static readonly StyledProperty<bool> SnapEnabledProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(SnapEnabled));

        /// <summary>Avalonia property backing <see cref="SelectedObject"/>.</summary>
        public static readonly StyledProperty<LevelObject?> SelectedObjectProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelObject?>(
                nameof(SelectedObject), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        /// <summary>Avalonia property backing <see cref="LockedObject"/>.</summary>
        public static readonly StyledProperty<LevelObject?> LockedObjectProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelObject?>(
                nameof(LockedObject), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        /// <summary>Avalonia property backing <see cref="ShowHitboxes"/>.</summary>
        public static readonly StyledProperty<bool> ShowHitboxesProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(ShowHitboxes), defaultValue: true);

        /// <summary>Avalonia property backing <see cref="ShowMobileHitboxes"/>.</summary>
        public static readonly StyledProperty<bool> ShowMobileHitboxesProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(ShowMobileHitboxes));

        /// <summary>Editor-decoration rope skin index applied to every rope (0 = default brown).</summary>
        public static readonly StyledProperty<int> ActiveRopeSkinProperty =
            AvaloniaProperty.Register<LevelCanvas, int>(nameof(ActiveRopeSkin));

        /// <summary>Editor-decoration background id (0 = none, 1..7 = bgr_01..bgr_07).</summary>
        public static readonly StyledProperty<int> ActiveBackgroundProperty =
            AvaloniaProperty.Register<LevelCanvas, int>(nameof(ActiveBackground));

        /// <summary>Editor-decoration candy skin index applied to candy sprites (0 = default candy).</summary>
        public static readonly StyledProperty<int> ActiveCandySkinProperty =
            AvaloniaProperty.Register<LevelCanvas, int>(nameof(ActiveCandySkin));

        /// <summary>Editor-decoration Om Nom sitting platform index applied to the target (0 = default).</summary>
        public static readonly StyledProperty<int> ActiveOmNomSupportProperty =
            AvaloniaProperty.Register<LevelCanvas, int>(nameof(ActiveOmNomSupport));

        /// <summary>Avalonia property backing <see cref="HorizontalScrollMaximum"/>.</summary>
        public static readonly StyledProperty<double> HorizontalScrollMaximumProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(nameof(HorizontalScrollMaximum));

        /// <summary>Avalonia property backing <see cref="VerticalScrollMaximum"/>.</summary>
        public static readonly StyledProperty<double> VerticalScrollMaximumProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(nameof(VerticalScrollMaximum));

        /// <summary>Avalonia property backing <see cref="HorizontalScrollViewport"/>.</summary>
        public static readonly StyledProperty<double> HorizontalScrollViewportProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(nameof(HorizontalScrollViewport));

        /// <summary>Avalonia property backing <see cref="VerticalScrollViewport"/>.</summary>
        public static readonly StyledProperty<double> VerticalScrollViewportProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(nameof(VerticalScrollViewport));

        /// <summary>Avalonia property backing <see cref="HorizontalScrollValue"/>.</summary>
        public static readonly StyledProperty<double> HorizontalScrollValueProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(
                nameof(HorizontalScrollValue), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        /// <summary>Avalonia property backing <see cref="VerticalScrollValue"/>.</summary>
        public static readonly StyledProperty<double> VerticalScrollValueProperty =
            AvaloniaProperty.Register<LevelCanvas, double>(
                nameof(VerticalScrollValue), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        static LevelCanvas()
        {
            AffectsRender<LevelCanvas>(
                DocumentProperty, SpritesProperty, ViewProperty, SnapEnabledProperty,
                SelectedObjectProperty, LockedObjectProperty,
                ShowHitboxesProperty, ShowMobileHitboxesProperty,
                ActiveRopeSkinProperty, ActiveBackgroundProperty, ActiveCandySkinProperty,
                ActiveOmNomSupportProperty);
        }

        /// <summary>The loaded level document to render and edit.</summary>
        public LevelDocument? Document { get => GetValue(DocumentProperty); set => SetValue(DocumentProperty, value); }

        /// <summary>Sprite cache used to render object art.</summary>
        public SpriteCache? Sprites { get => GetValue(SpritesProperty); set => SetValue(SpritesProperty, value); }

        /// <summary>Current zoom and pan transform.</summary>
        public ViewTransform View { get => GetValue(ViewProperty); set => SetValue(ViewProperty, value); }

        /// <summary>Whether object moves and placements snap to the level grid.</summary>
        public bool SnapEnabled { get => GetValue(SnapEnabledProperty); set => SetValue(SnapEnabledProperty, value); }

        /// <summary>The currently selected object, if any.</summary>
        public LevelObject? SelectedObject { get => GetValue(SelectedObjectProperty); set => SetValue(SelectedObjectProperty, value); }

        /// <summary>The object locked for exclusive interaction, if any.</summary>
        public LevelObject? LockedObject { get => GetValue(LockedObjectProperty); set => SetValue(LockedObjectProperty, value); }

        /// <summary>Whether desktop hitboxes are drawn over objects.</summary>
        public bool ShowHitboxes { get => GetValue(ShowHitboxesProperty); set => SetValue(ShowHitboxesProperty, value); }

        /// <summary>Whether phone hitboxes are drawn over objects.</summary>
        public bool ShowMobileHitboxes { get => GetValue(ShowMobileHitboxesProperty); set => SetValue(ShowMobileHitboxesProperty, value); }

        /// <summary>Editor-decoration rope skin index applied to every rope (0 = default brown).</summary>
        public int ActiveRopeSkin { get => GetValue(ActiveRopeSkinProperty); set => SetValue(ActiveRopeSkinProperty, value); }

        /// <summary>Editor-decoration background id (0 = none, 1..7 = bgr_01..bgr_07).</summary>
        public int ActiveBackground { get => GetValue(ActiveBackgroundProperty); set => SetValue(ActiveBackgroundProperty, value); }

        /// <summary>Editor-decoration candy skin index applied to candy sprites (0 = default candy).</summary>
        public int ActiveCandySkin { get => GetValue(ActiveCandySkinProperty); set => SetValue(ActiveCandySkinProperty, value); }

        /// <summary>Editor-decoration Om Nom sitting platform index applied to the target (0 = default).</summary>
        public int ActiveOmNomSupport { get => GetValue(ActiveOmNomSupportProperty); set => SetValue(ActiveOmNomSupportProperty, value); }

        /// <summary>Largest horizontal scroll offset in screen pixels.</summary>
        public double HorizontalScrollMaximum { get => GetValue(HorizontalScrollMaximumProperty); private set => SetValue(HorizontalScrollMaximumProperty, value); }

        /// <summary>Largest vertical scroll offset in screen pixels.</summary>
        public double VerticalScrollMaximum { get => GetValue(VerticalScrollMaximumProperty); private set => SetValue(VerticalScrollMaximumProperty, value); }

        /// <summary>Visible canvas width used by the horizontal scrollbar thumb.</summary>
        public double HorizontalScrollViewport { get => GetValue(HorizontalScrollViewportProperty); private set => SetValue(HorizontalScrollViewportProperty, value); }

        /// <summary>Visible canvas height used by the vertical scrollbar thumb.</summary>
        public double VerticalScrollViewport { get => GetValue(VerticalScrollViewportProperty); private set => SetValue(VerticalScrollViewportProperty, value); }

        /// <summary>Current horizontal scroll offset in screen pixels.</summary>
        public double HorizontalScrollValue { get => GetValue(HorizontalScrollValueProperty); set => SetValue(HorizontalScrollValueProperty, value); }

        /// <summary>Current vertical scroll offset in screen pixels.</summary>
        public double VerticalScrollValue { get => GetValue(VerticalScrollValueProperty); set => SetValue(VerticalScrollValueProperty, value); }

        /// <summary>Callback used to place a new object at level coordinates.</summary>
        public Func<string, int, int, LevelObject?>? PlaceAt { get; set; }

        /// <summary>Callback used to toggle the locked object from canvas gestures.</summary>
        public Action<LevelObject?>? ToggleLock { get; set; }

        /// <summary>Callback raised when a canvas drag moves the selected object, so bound views can refresh.</summary>
        public Action? SelectedObjectMoved { get; set; }

        /// <summary>Callback raised before a direct canvas edit begins, so the view model can capture undo state.</summary>
        public Action? BeginDocumentEdit { get; set; }

        /// <summary>Callback raised after a direct canvas edit ends, so the view model can commit undo state.</summary>
        public Action? CompleteDocumentEdit { get; set; }

        // Hovering / dragging the auto-catch radius ring, or a horizontal rail end/hook, uses a horizontal-
        // resize cursor (col-resize); a vertical rail end/hook uses the vertical one.
        // Cached, but created lazily rather than in the static constructor: eager creation would touch
        // Avalonia's cursor factory at type load, which throws in the headless test host. Lazy .Value
        // still allocates each cursor only once, so pointer-move hit-testing doesn't churn instances.
        private static readonly Lazy<Cursor> LazyResizeCursor = new(() => new Cursor(StandardCursorType.SizeWestEast));
        private static readonly Lazy<Cursor> LazyVResizeCursor = new(() => new Cursor(StandardCursorType.SizeNorthSouth));
        private static Cursor ResizeCursor => LazyResizeCursor.Value;
        private static Cursor VResizeCursor => LazyVResizeCursor.Value;

        // Game-accurate grab auto-catch ring for screenshots: a dashed blue circle matching the game's
        // Grab.DrawGrabCircle (RGBA 0.2/0.5/0.9, drawn as alternating segments). The on-canvas ring keeps
        // the themed orange editor guide; this fixed color is only baked into the exported image.
        private static readonly Pen ScreenshotGrabRadiusPen =
            new(new SolidColorBrush(Color.FromArgb(255, 51, 128, 230)), 3.0)
            {
                DashStyle = new DashStyle([4, 3], 0),
            };

        private bool _dragging;
        private bool _resizingRadius;
        // Which movable-rail handle the current drag is manipulating (slide the hook or resize an end);
        // None when no rail drag is in progress. A MoveBar drag routes through _dragging instead.
        private GrabRail.Handle _railDrag;
        // Whether the pointer is hovering the selected grab's hook, so it shows the highlight art even
        // before a drag begins (the game highlights the mover on interaction).
        private bool _hookHovered;
        private Vec2 _dragOffset;
        private int _lastHitIndex = -1;
        private bool _panning;
        private Point _panLast;
        private double _lastPinchScale = 1;
        private bool _syncingScroll;
        private bool _pendingFit;
        private bool _ghostActive;
        private string? _ghostElement;
        private Vec2 _ghostLevel;

        // Editor-chrome brushes/pens resolved from the theme once per theme change, not per Render.
        private readonly CanvasPalette _palette = new();

        /// <summary>Creates the canvas and enables native touch gestures.</summary>
        public LevelCanvas()
        {
            GestureRecognizers.Add(new PinchGestureRecognizer());
            AddHandler(PinchEvent, Canvas_Pinch, RoutingStrategies.Bubble);
            AddHandler(PinchEndedEvent, Canvas_PinchEnded, RoutingStrategies.Bubble);
            AddHandler(PointerTouchPadGestureMagnifyEvent, Canvas_TouchPadMagnify, RoutingStrategies.Bubble);
        }

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _palette.Refresh(this);
            ActualThemeVariantChanged += OnActualThemeVariantChanged;
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            ActualThemeVariantChanged -= OnActualThemeVariantChanged;
            base.OnDetachedFromVisualTree(e);
        }

        private void OnActualThemeVariantChanged(object? sender, EventArgs e)
        {
            _palette.Refresh(this);
            InvalidateVisual();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DocumentProperty)
            {
                // Auto-fit only when the level's dimensions change (a fresh load, a new level, or a
                // resolution change). An undo/redo restore swaps in a re-parsed same-sized document,
                // and refitting it would throw away the user's current zoom and pan.
                LevelDocument? oldDoc = change.GetOldValue<LevelDocument?>();
                LevelDocument? newDoc = change.GetNewValue<LevelDocument?>();
                if (newDoc is not null
                    && (oldDoc is null || oldDoc.Width != newDoc.Width || oldDoc.Height != newDoc.Height))
                {
                    _pendingFit = true;
                    TryFit(); // fits immediately if already laid out (later loads); else waits for Bounds.
                }
                UpdateScrollState();
            }
            else if (change.Property == BoundsProperty && _pendingFit)
            {
                TryFit();
            }
            else if (change.Property == BoundsProperty || change.Property == ViewProperty)
            {
                UpdateScrollState();
            }
            else if ((change.Property == HorizontalScrollValueProperty || change.Property == VerticalScrollValueProperty)
                     && !_syncingScroll)
            {
                ScrollTo(HorizontalScrollValue, VerticalScrollValue);
            }
        }

        private void TryFit()
        {
            if (_pendingFit && Bounds is { Width: > 0, Height: > 0 })
            {
                FitToView();
                _pendingFit = false;
            }
        }

        /// <summary>Scales and centers the level to fit the current viewport with a small margin.</summary>
        public void FitToView()
        {
            LevelDocument? doc = Document;
            if (doc is null || doc.Width <= 0 || doc.Height <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
            {
                return;
            }
            const double margin = 24;
            double zoom = Math.Min(
                (Bounds.Width - (2 * margin)) / doc.Width,
                (Bounds.Height - (2 * margin)) / doc.Height);
            if (zoom <= 0)
            {
                zoom = 1;
            }
            double panX = (Bounds.Width - (doc.Width * zoom)) / 2;
            double panY = (Bounds.Height - (doc.Height * zoom)) / 2;
            View = new ViewTransform(zoom, panX, panY);
            UpdateScrollState();
        }

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

        /// <summary>Zooms about the viewport center (for menu/keyboard zoom).</summary>
        public void ZoomBy(double factor)
        {
            ZoomBy(factor, new Point(Bounds.Width / 2, Bounds.Height / 2));
        }

        /// <summary>Zooms about a screen-space point in this canvas.</summary>
        public void ZoomBy(double factor, Point anchor)
        {
            View = ViewNavigation.ZoomBy(View, factor, new Vec2(anchor.X, anchor.Y), 0.1, 10.0);
            UpdateScrollState();
        }

        /// <summary>Scrolls the level viewport by screen-space pixels.</summary>
        public void ScrollBy(double deltaX, double deltaY)
        {
            ScrollTo(HorizontalScrollValue + deltaX, VerticalScrollValue + deltaY);
        }

        private void ScrollTo(double offsetX, double offsetY)
        {
            if (Document is not { } doc)
            {
                return;
            }

            View = ViewNavigation.ScrollTo(View, doc.Width, doc.Height, Bounds.Width, Bounds.Height, offsetX, offsetY);
            UpdateScrollState();
        }

        private void UpdateScrollState()
        {
            LevelDocument? doc = Document;
            double viewportWidth = Math.Max(0, Bounds.Width);
            double viewportHeight = Math.Max(0, Bounds.Height);
            double maxX = 0;
            double maxY = 0;
            double valueX = 0;
            double valueY = 0;

            if (doc is not null)
            {
                double contentWidth = Math.Max(0, doc.Width * View.Zoom);
                double contentHeight = Math.Max(0, doc.Height * View.Zoom);
                maxX = Math.Max(0, contentWidth - viewportWidth);
                maxY = Math.Max(0, contentHeight - viewportHeight);
                valueX = Math.Clamp(-View.PanX, 0, maxX);
                valueY = Math.Clamp(-View.PanY, 0, maxY);
            }

            _syncingScroll = true;
            try
            {
                HorizontalScrollViewport = viewportWidth;
                VerticalScrollViewport = viewportHeight;
                HorizontalScrollMaximum = maxX;
                VerticalScrollMaximum = maxY;
                HorizontalScrollValue = valueX;
                VerticalScrollValue = valueY;
            }
            finally
            {
                _syncingScroll = false;
            }
        }

        /// <summary>Maps a level-space rectangle (x, y, w, h) to its axis-aligned screen rectangle.</summary>
        private static Rect LevelRectToScreen(ViewTransform v, double x, double y, double w, double h)
        {
            Vec2 tl = v.LevelToScreen(new Vec2(x, y));
            Vec2 br = v.LevelToScreen(new Vec2(x + w, y + h));
            return new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y);
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
            DrawLevelContent(context, v, Bounds.Size, doc, sprites, drawGrid: true, grabRadiusPen: null);

            IReadOnlyList<LevelObject> objects = doc.Objects;

            GrabRenderer.DrawRadiusRings(context, v, objects, _palette.GrabRadius, _palette.BulbRadius);

            if (ShowHitboxes || ShowMobileHitboxes)
            {
                foreach (LevelObject obj in objects)
                {
                    if (sprites.GetSprite(obj.Type) is not { } sprite)
                    {
                        continue;
                    }
                    if (ShowHitboxes)
                    {
                        DrawHitbox(context, v, obj, sprite.Scale, HitboxModel.Desktop, _palette.HitboxDesktop);
                    }
                    if (ShowMobileHitboxes)
                    {
                        DrawHitbox(context, v, obj, sprite.Scale, HitboxModel.Phone, _palette.HitboxPhone);
                    }
                }
            }

            LevelObject? selected = SelectedObject;
            if (selected is not null)
            {
                LevelBounds sb = SelectionBounds(sprites, selected, ActiveCandySkin, ActiveOmNomSupport);
                Vec2 stl = v.LevelToScreen(new Vec2(sb.X, sb.Y));
                Vec2 sbr = v.LevelToScreen(new Vec2(sb.X + sb.W, sb.Y + sb.H));
                // Both boxes are dashed; a locked object is red, an unlocked one blue.
                Pen pen = Equals(LockedObject, selected) ? _palette.ObjectLocked : _palette.ObjectSelected;
                context.DrawRectangle(null, pen, new Rect(stl.X, stl.Y, sbr.X - stl.X, sbr.Y - stl.Y));
            }

            // Translucent ghost of the object being dragged from the palette, at its snapped drop spot.
            if (_ghostActive && _ghostElement is { } ghostElement
                && sprites.GetSprite(ghostElement, ActiveCandySkin, ActiveOmNomSupport) is { } ghostSprite)
            {
                using (context.PushOpacity(0.7))
                {
                    DrawSprite(context, v, ghostSprite, _ghostLevel.X, _ghostLevel.Y);
                }
            }
        }

        // Draws the level itself - background decoration, optional border+grid, light-bulb glow, and all
        // objects/grabs in the game's z-order - into the given surface. Interactive chrome (selection,
        // hitboxes, ghost) is NOT drawn here; Render layers that on top. drawGrid gates the editor-only
        // border and grid so a clean screenshot can omit them. grabRadiusPen, when set, bakes the grab
        // auto-catch rings into the image with that pen (the screenshot's game-blue ring); Render passes
        // null and draws its own themed rings in the chrome pass instead.
        private void DrawLevelContent(
            DrawingContext context,
            ViewTransform v,
            Size renderSize,
            LevelDocument doc,
            SpriteCache sprites,
            bool drawGrid,
            Pen? grabRadiusPen)
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
                            context.DrawImage(bg, bgSrc, LevelRectToScreen(v, layout.Left, ty, layout.Width, layout.TileHeight));
                        }
                    }

                    if (layout.P2 is { } p2b && p2 is not null)
                    {
                        context.DrawImage(p2, new Rect(p2.Size), LevelRectToScreen(v, p2b.X, p2b.Y, p2b.W, p2b.H));
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
                                LevelRectToScreen(v, ec.X - (ew / 2.0), ec.Y - (eh / 2.0), ew, eh));
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
            foreach (LevelObject obj in objects.OrderBy(GameDrawLayer))
            {
                if (obj.Type == "grab")
                {
                    RopeVisual? rope = RopeRenderer.BuildRope(obj, objects, doc.TwoParts, ActiveRopeSkin);
                    DrawGrab(context, v, sprites, obj, objects, doc.TwoParts, rope, ropeSeed, opBounds);
                    if (rope is not null)
                    {
                        ropeSeed++;
                    }
                }
                else
                {
                    DrawObject(context, v, sprites, obj, ActiveCandySkin, ActiveOmNomSupport);
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
                DrawLevelContent(ctx, frame.View, renderSize, doc, sprites, drawGrid: false, grabRadiusPen: ScreenshotGrabRadiusPen);
            }
            return rtb;
        }

        // Selection marquee: the trimmed (visible) sprite bounds — the union of every layer's drawn
        // region — grown 25% so the dashed box sits a little outside the art rather than hugging the
        // untrimmed sourceSize box (which is much larger than what the player sees).
        private static LevelBounds SelectionBounds(SpriteCache sprites, LevelObject obj, int candySkin, int omNomSupport)
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
            ObjectSprite? sprite = sprites.GetSprite(GrabRenderer.RenderSpriteKey(obj), candySkin, omNomSupport);
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
        private static int GameDrawLayer(LevelObject obj)
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
        private static void DrawObject(
            DrawingContext ctx, ViewTransform v, SpriteCache sprites, LevelObject obj, int candySkin, int omNomSupport)
        {
            ObjectSprite? sprite = sprites.GetSprite(GrabRenderer.SpriteKey(obj), candySkin, omNomSupport);
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

        // A pale grab is one the game hides outright (invisible="true"); the editor keeps it visible at
        // this opacity so it stays selectable and editable rather than vanishing.
        private const double InvisibleGrabOpacity = 0.3;

        // Draws a grab with its rope threaded between the hook's back and front art, matching the game's
        // Grab.DrawBack (back art) then Grab.Draw (rope, then front art) order. An invisible grab (hidden
        // entirely in-game) is drawn pale so it can still be selected. A movable grab splits into its rail
        // bar (back) and movable hook (front); every other grab splits its sprite layers by
        // GrabRenderer.BackLayerCount. rope is null when the grab has nothing to hang from.
        private void DrawGrab(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            LevelObject obj,
            IReadOnlyList<LevelObject> objects,
            bool twoParts,
            RopeVisual? rope,
            int ropeSeed,
            Rect opBounds)
        {
            // The hook art and Christmas lights are DrawImage calls that PushOpacity fades; the rope is a
            // Skia custom draw op that PushOpacity does not reach, so its alpha is passed through explicitly.
            double opacity = IsInvisible(obj) ? InvisibleGrabOpacity : 1.0;
            if (opacity < 1.0)
            {
                using (ctx.PushOpacity(opacity))
                {
                    DrawGrabContent(ctx, v, sprites, obj, objects, twoParts, rope, ropeSeed, opBounds, opacity);
                }
            }
            else
            {
                DrawGrabContent(ctx, v, sprites, obj, objects, twoParts, rope, ropeSeed, opBounds, opacity);
            }
        }

        private static bool IsInvisible(LevelObject obj)
        {
            return bool.TryParse(obj.GetAttr("invisible"), out bool b) && b;
        }

        private void DrawGrabContent(
            DrawingContext ctx,
            ViewTransform v,
            SpriteCache sprites,
            LevelObject obj,
            IReadOnlyList<LevelObject> objects,
            bool twoParts,
            RopeVisual? rope,
            int ropeSeed,
            Rect opBounds,
            double ropeOpacity)
        {
            if (GrabRenderer.DrawsMovableRail(obj) && GrabRail.Of(obj) is { } rail)
            {
                // Highlight the hook while it's hovered or being slid, matching the game's mover art.
                bool active = (_railDrag == GrabRail.Handle.SlideHook || _hookHovered) && Equals(obj, SelectedObject);
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

        private static void DrawSprite(DrawingContext ctx, ViewTransform v, ObjectSprite sprite, double x, double y)
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

        private static void DrawHitbox(
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

        // Whether a level-space point sits on the selected grab's auto-catch radius ring, within a
        // ~6px screen tolerance (converted to level units by the current zoom).
        private bool OnRadiusEdge(Vec2 levelPt)
        {
            return SelectedObject is { } g && View.Zoom > 0 && RadiusRing.Of(g) is { } ring
                && GrabRadius.OnEdge(new Vec2(g.X, g.Y), ring.Radius, levelPt, 6 / View.Zoom);
        }

        // What part of the selected movable grab's rail a level point is over, or None. The hit-testing
        // itself lives in GrabRail; here we only supply the selected grab's geometry and the screen-derived
        // tolerances: ~9 px for the end caps, the hook's own footprint, and the bar's half thickness.
        private GrabRail.Handle HitRail(Vec2 levelPt)
        {
            return SelectedObject is { Type: "grab" } sel
                && View.Zoom > 0
                && GrabRenderer.DrawsMovableRail(sel)
                && GrabRail.Of(sel) is { } g
                ? GrabRail.HitTest(g, levelPt, endTolerance: 9 / View.Zoom, hookTolerance: 24, barThickness: 20)
                : GrabRail.Handle.None;
        }

        // Applies the active rail drag to the grab: sliding moves the hook (object x/y) and its offset
        // together so the rail stays put; resizing an end rewrites moveLength (and moveOffset for the near
        // end). All constrained by GrabRail so the hook never leaves the rail.
        private void ApplyRailDrag(LevelObject grab, GrabRail.Geometry g, Vec2 levelPt)
        {
            switch (_railDrag)
            {
                case GrabRail.Handle.SlideHook:
                    (double hookAxis, double offset) = GrabRail.SlideHook(g, levelPt);
                    if (g.Vertical)
                    {
                        grab.Y = (int)Math.Round(hookAxis);
                    }
                    else
                    {
                        grab.X = (int)Math.Round(hookAxis);
                    }
                    grab.SetAttr("moveOffset", Whole(offset));
                    break;
                case GrabRail.Handle.ResizeEnd:
                    grab.SetAttr("moveLength", Whole(GrabRail.ResizeEnd(g, levelPt)));
                    break;
                case GrabRail.Handle.ResizeStart:
                    (double offA, double length) = GrabRail.ResizeStart(g, levelPt);
                    grab.SetAttr("moveOffset", Whole(offA));
                    grab.SetAttr("moveLength", Whole(length));
                    break;
                case GrabRail.Handle.MoveBar:
                case GrabRail.Handle.None:
                default:
                    break;
            }
        }

        private static string Whole(double value)
        {
            return ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
        }

        // The cursor for a rail handle: a horizontal rail end/hook reads as a horizontal resize, a vertical
        // one as a vertical resize (the hook slides along the same axis). The bar keeps the default arrow -
        // it is still draggable to move the whole grab, but a move cursor over the whole rail is noisy.
        private Cursor CursorForHandle(GrabRail.Handle handle)
        {
            return handle switch
            {
                GrabRail.Handle.ResizeStart or GrabRail.Handle.ResizeEnd or GrabRail.Handle.SlideHook =>
                    SelectedObject is { } s && GrabRail.Vertical(s) ? VResizeCursor : ResizeCursor,
                GrabRail.Handle.MoveBar => Cursor.Default,
                GrabRail.Handle.None => Cursor.Default,
                _ => Cursor.Default,
            };
        }

        // Updates the hook hover state, repainting only on a change so the highlight art swaps in/out.
        private void SetHookHovered(bool hovered)
        {
            if (_hookHovered != hovered)
            {
                _hookHovered = hovered;
                InvalidateVisual();
            }
        }

        private static int IndexOf(IReadOnlyList<LevelObject> objects, LevelObject target)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (Equals(objects[i], target))
                {
                    return i;
                }
            }
            return -1;
        }

        private List<LevelBounds> BuildHitBounds(LevelDocument doc)
        {
            // Clicking uses the same box that the selection marquee draws (trimmed art + 25%).
            List<LevelBounds> list = [];
            foreach (LevelObject o in doc.Objects)
            {
                list.Add(Sprites is null
                    ? new LevelBounds(o.X - 8, o.Y - 8, 16, 16)
                    : SelectionBounds(Sprites, o, ActiveCandySkin, ActiveOmNomSupport));
            }
            return list;
        }

        /// <inheritdoc />
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            LevelDocument? doc = Document;
            if (doc is null)
            {
                return;
            }

            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
            {
                _panning = true;
                _panLast = e.GetPosition(this);
                e.Pointer.Capture(this);
                return;
            }
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            Point p = e.GetPosition(this);
            Vec2 levelPt = View.ScreenToLevel(new Vec2(p.X, p.Y));

            // Grabbing the auto-catch ring resizes the radius; it takes priority over object hit-testing
            // (the ring can sit over other objects) but not over a middle-button pan.
            if (OnRadiusEdge(levelPt))
            {
                BeginDocumentEdit?.Invoke();
                _resizingRadius = true;
                e.Pointer.Capture(this);
                return;
            }

            // Grabbing the selected movable grab's rail: an end cap resizes, the hook slides, the bar moves
            // the whole grab. Takes priority over hit-testing so the rail wins over anything beneath it.
            GrabRail.Handle handle = HitRail(levelPt);
            switch (handle)
            {
                case GrabRail.Handle.ResizeStart:
                case GrabRail.Handle.ResizeEnd:
                case GrabRail.Handle.SlideHook:
                    BeginDocumentEdit?.Invoke();
                    _railDrag = handle;
                    e.Pointer.Capture(this);
                    return;
                case GrabRail.Handle.MoveBar:
                    BeginDocumentEdit?.Invoke();
                    _dragOffset = levelPt - new Vec2(SelectedObject!.X, SelectedObject.Y);
                    _dragging = true;
                    e.Pointer.Capture(this);
                    return;
                case GrabRail.Handle.None:
                default:
                    break;
            }

            List<LevelBounds> bounds = BuildHitBounds(doc);

            // Double-click toggles the lock. ClickCount keeps climbing (3, 4, ...) while clicking in the
            // same spot rather than resetting, so every even count counts as a double-click — otherwise a
            // second double-click at the same position would be missed unless the cursor moved.
            if (e.ClickCount % 2 == 0)
            {
                if (LockedObject is { } current)
                {
                    ToggleLock?.Invoke(current);
                    return;
                }
                int dh = HitTester.Topmost(bounds, levelPt, -1);
                ToggleLock?.Invoke(dh >= 0 ? doc.Objects[dh] : null);
                return;
            }

            // While an object is locked, only it is interactive — clicks never fall through to other objects.
            if (LockedObject is { } locked)
            {
                int li = IndexOf(doc.Objects, locked);
                if (li >= 0 && bounds[li].Contains(levelPt))
                {
                    SelectedObject = locked;
                    BeginDocumentEdit?.Invoke();
                    _dragOffset = levelPt - new Vec2(locked.X, locked.Y);
                    _dragging = true;
                    e.Pointer.Capture(this);
                }
                return;
            }

            int after = _lastHitIndex >= 0 && _lastHitIndex < bounds.Count
                        && bounds[_lastHitIndex].Contains(levelPt) ? _lastHitIndex : -1;
            int hit = HitTester.Topmost(bounds, levelPt, after);
            _lastHitIndex = hit;

            if (hit < 0)
            {
                SelectedObject = null;
                _panning = true;
                _panLast = p;
                e.Pointer.Capture(this);
                return;
            }

            LevelObject obj = doc.Objects[hit];
            SelectedObject = obj;
            BeginDocumentEdit?.Invoke();
            _dragOffset = levelPt - new Vec2(obj.X, obj.Y);
            _dragging = true;
            e.Pointer.Capture(this);
        }

        /// <inheritdoc />
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            Point p = e.GetPosition(this);
            Vec2 levelPt = View.ScreenToLevel(new Vec2(p.X, p.Y));

            if (_resizingRadius && SelectedObject is { } g && RadiusRing.Of(g) is { } ring)
            {
                double r = GrabRadius.FromDrag(new Vec2(g.X, g.Y), levelPt);
                g.SetAttr(ring.Attr, ((int)Math.Round(r)).ToString(CultureInfo.InvariantCulture));
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                return;
            }

            if (_railDrag != GrabRail.Handle.None && SelectedObject is { } rg && GrabRail.Of(rg) is { } rail)
            {
                ApplyRailDrag(rg, rail, levelPt);
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                return;
            }

            if (_panning)
            {
                ScrollBy(_panLast.X - p.X, _panLast.Y - p.Y);
                _panLast = p;
                return;
            }

            if (!_dragging || SelectedObject is not { } selected)
            {
                // Reflect the affordance under the cursor so ring resize / rail edit are discoverable, and
                // light up the hook when it's hovered.
                GrabRail.Handle handle = HitRail(levelPt);
                SetHookHovered(handle == GrabRail.Handle.SlideHook);
                Cursor = OnRadiusEdge(levelPt) ? ResizeCursor : CursorForHandle(handle);
                return;
            }

            (int gx, int gy) = Snap(levelPt - _dragOffset);
            selected.X = gx;
            selected.Y = gy;
            SelectedObjectMoved?.Invoke();
            InvalidateVisual();
        }

        /// <inheritdoc />
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            EndPointerGesture();
            e.Pointer.Capture(null);
        }

        /// <inheritdoc />
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            EndPointerGesture();
        }

        private void EndPointerGesture()
        {
            // Capture loss (including the release path's own Capture(null)) can fire with nothing in
            // progress; skip the resets and completion callback unless a gesture is actually active.
            bool gestureActive = _dragging || _panning || _resizingRadius
                || _railDrag != GrabRail.Handle.None || _hookHovered;
            if (!gestureActive)
            {
                return;
            }

            bool editedDocument = _dragging || _resizingRadius || _railDrag != GrabRail.Handle.None;
            _dragging = false;
            _panning = false;
            _resizingRadius = false;
            _railDrag = GrabRail.Handle.None;
            if (editedDocument)
            {
                CompleteDocumentEdit?.Invoke();
            }
            // Letting go ends the "grabbed" look; a fresh hover re-lights it if the cursor is on the hook.
            SetHookHovered(false);
        }

        /// <inheritdoc />
        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            SetHookHovered(false); // don't leave the hook lit when the cursor leaves the canvas
        }

        /// <inheritdoc />
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            const double wheelPixels = 48;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                double factor = e.Delta.Y > 0 ? 1.1 : 1 / 1.1;
                ZoomBy(factor, e.GetPosition(this));
            }
            else
            {
                double deltaX = e.Delta.X * -wheelPixels;
                double deltaY = e.Delta.Y * -wheelPixels;
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift) && Math.Abs(deltaX) < double.Epsilon)
                {
                    deltaX = deltaY;
                    deltaY = 0;
                }
                ScrollBy(deltaX, deltaY);
            }
            e.Handled = true;
        }

        private void Canvas_Pinch(object? sender, PinchEventArgs e)
        {
            double factor = ViewNavigation.PinchScaleToZoomFactor(_lastPinchScale, e.Scale);
            _lastPinchScale = e.Scale;
            ZoomBy(factor, e.ScaleOrigin);
            e.Handled = true;
        }

        private void Canvas_PinchEnded(object? sender, PinchEndedEventArgs e)
        {
            _lastPinchScale = 1;
            e.Handled = true;
        }

        private void Canvas_TouchPadMagnify(object? sender, PointerDeltaEventArgs e)
        {
            double delta = Math.Abs(e.Delta.Y) > double.Epsilon ? e.Delta.Y : e.Delta.X;
            ZoomBy(ViewNavigation.MagnifyDeltaToZoomFactor(delta), e.GetPosition(this));
            e.Handled = true;
        }

        /// <summary>Shows a translucent preview of <paramref name="element"/> at the snapped drop position.</summary>
        public void ShowGhost(string element, Point screenPoint)
        {
            Vec2 levelPt = View.ScreenToLevel(new Vec2(screenPoint.X, screenPoint.Y));
            (int gx, int gy) = Snap(levelPt);
            _ghostElement = element;
            _ghostLevel = new Vec2(gx, gy);
            _ghostActive = true;
            InvalidateVisual();
        }

        /// <summary>Clears the drag preview, if any.</summary>
        public void HideGhost()
        {
            if (_ghostActive)
            {
                _ghostActive = false;
                _ghostElement = null;
                InvalidateVisual();
            }
        }

        /// <summary>Adds an object at the level's center (for single-click placement from the palette).</summary>
        public bool AddAtCenter(string element)
        {
            if (Document is not { } doc)
            {
                return false;
            }

            LevelObject? placed = PlaceAt?.Invoke(element, doc.Width / 2, doc.Height / 2);
            if (placed is not null)
            {
                SelectedObject = placed;
            }
            InvalidateVisual();
            return placed is not null;
        }

        /// <summary>Drops an object at a screen-space point, snapping according to the current settings.</summary>
        public bool DropElement(string element, Point screenPoint)
        {
            Vec2 levelPt = View.ScreenToLevel(new Vec2(screenPoint.X, screenPoint.Y));
            (int gx, int gy) = Snap(levelPt);
            LevelObject? placed = PlaceAt?.Invoke(element, gx, gy);
            if (placed is not null)
            {
                SelectedObject = placed;
            }
            InvalidateVisual();
            return placed is not null;
        }

        private (int X, int Y) Snap(Vec2 levelPt)
        {
            int x = (int)Math.Round(levelPt.X);
            int y = (int)Math.Round(levelPt.Y);
            if (SnapEnabled && Document is { } d && d.GridSize > 0)
            {
                x = (int)Math.Round(levelPt.X / d.GridSize) * d.GridSize;
                y = (int)Math.Round(levelPt.Y / d.GridSize) * d.GridSize;
            }
            return (x, y);
        }
    }
}
