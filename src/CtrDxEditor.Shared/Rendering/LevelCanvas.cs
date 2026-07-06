using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

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
                ShowHitboxesProperty, ShowMobileHitboxesProperty);
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

        // Hovering / dragging the auto-catch radius ring, or a horizontal rail end/hook, uses a horizontal-
        // resize cursor (col-resize); a vertical rail end/hook uses the vertical one.
        private static readonly Cursor ResizeCursor = new(StandardCursorType.SizeWestEast);
        private static readonly Cursor VResizeCursor = new(StandardCursorType.SizeNorthSouth);

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

        /// <summary>Creates the canvas and enables native touch gestures.</summary>
        public LevelCanvas()
        {
            GestureRecognizers.Add(new PinchGestureRecognizer());
            AddHandler(PinchEvent, Canvas_Pinch, RoutingStrategies.Bubble);
            AddHandler(PinchEndedEvent, Canvas_PinchEnded, RoutingStrategies.Bubble);
            AddHandler(PointerTouchPadGestureMagnifyEvent, Canvas_TouchPadMagnify, RoutingStrategies.Bubble);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DocumentProperty)
            {
                _pendingFit = true;
                TryFit(); // fits immediately if already laid out (later loads); else waits for Bounds.
            }
            else if (change.Property == BoundsProperty && _pendingFit)
            {
                TryFit();
            }
            else if (change.Property == BoundsProperty || change.Property == ViewProperty || change.Property == DocumentProperty)
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

        /// <inheritdoc />
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            context.FillRectangle(new SolidColorBrush(Color.FromRgb(40, 44, 52)), new Rect(Bounds.Size));

            LevelDocument? doc = Document;
            SpriteCache? sprites = Sprites;
            if (doc is null || sprites is null)
            {
                return;
            }

            ViewTransform v = View;
            Vec2 tl = v.LevelToScreen(new Vec2(0, 0));
            Vec2 br = v.LevelToScreen(new Vec2(doc.Width, doc.Height));
            context.DrawRectangle(null, new Pen(Brushes.DimGray, 1),
                new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y));

            int grid = doc.GridSize > 0 ? doc.GridSize : 32;
            Pen gridPen = new(new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), 1);
            for (int gx = 0; gx <= doc.Width; gx += grid)
            {
                Vec2 a = v.LevelToScreen(new Vec2(gx, 0));
                Vec2 b = v.LevelToScreen(new Vec2(gx, doc.Height));
                context.DrawLine(gridPen, new Point(a.X, a.Y), new Point(b.X, b.Y));
            }
            for (int gy = 0; gy <= doc.Height; gy += grid)
            {
                Vec2 a = v.LevelToScreen(new Vec2(0, gy));
                Vec2 b = v.LevelToScreen(new Vec2(doc.Width, gy));
                context.DrawLine(gridPen, new Point(a.X, a.Y), new Point(b.X, b.Y));
            }

            IReadOnlyList<LevelObject> objects = doc.Objects;
            RopeRenderer.Draw(context, v, sprites, doc, new Rect(Bounds.Size));

            foreach (LevelObject obj in objects)
            {
                if (GrabRenderer.DrawsMovableRail(obj) && GrabRail.Of(obj) is { } rail)
                {
                    // Highlight the hook while it's hovered or being slid, matching the game's mover art.
                    bool active = (_railDrag == GrabRail.Handle.SlideHook || _hookHovered) && Equals(obj, SelectedObject);
                    GrabRenderer.DrawMovableGrab(context, v, sprites, rail, active);
                }
                else
                {
                    DrawObject(context, v, sprites, obj);
                }
            }

            GrabRenderer.DrawRadiusRings(context, v, objects);

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
                        DrawHitbox(context, v, obj, sprite.Scale, HitboxModel.Desktop, Brushes.LimeGreen);
                    }
                    if (ShowMobileHitboxes)
                    {
                        DrawHitbox(context, v, obj, sprite.Scale, HitboxModel.Phone, Brushes.Magenta);
                    }
                }
            }

            LevelObject? selected = SelectedObject;
            if (selected is not null)
            {
                LevelBounds sb = SelectionBounds(sprites, selected);
                Vec2 stl = v.LevelToScreen(new Vec2(sb.X, sb.Y));
                Vec2 sbr = v.LevelToScreen(new Vec2(sb.X + sb.W, sb.Y + sb.H));
                // Both boxes are dashed; a locked object is red, an unlocked one blue.
                Pen pen = Equals(LockedObject, selected)
                    ? new Pen(Brushes.Red, 2) { DashStyle = new DashStyle([4, 3], 0) }
                    : new Pen(Brushes.DeepSkyBlue, 1.5) { DashStyle = new DashStyle([4, 3], 0) };
                context.DrawRectangle(null, pen, new Rect(stl.X, stl.Y, sbr.X - stl.X, sbr.Y - stl.Y));
            }

            // Translucent ghost of the object being dragged from the palette, at its snapped drop spot.
            if (_ghostActive && _ghostElement is { } ghostElement
                && sprites.GetSprite(ghostElement) is { } ghostSprite)
            {
                using (context.PushOpacity(0.7))
                {
                    DrawSprite(context, v, ghostSprite, _ghostLevel.X, _ghostLevel.Y);
                }
            }
        }

        // Selection marquee: the trimmed (visible) sprite bounds — the union of every layer's drawn
        // region — grown 25% so the dashed box sits a little outside the art rather than hugging the
        // untrimmed sourceSize box (which is much larger than what the player sees).
        private static LevelBounds SelectionBounds(SpriteCache sprites, LevelObject obj)
        {
            // A movable grab's marquee / click target wraps the whole rail, not just the hook, so it can
            // be selected by clicking anywhere along the bar.
            if (GrabRenderer.DrawsMovableRail(obj) && GrabRail.Of(obj) is { } rail)
            {
                return GrabRenderer.RailBounds(rail);
            }

            ObjectSprite? sprite = sprites.GetSprite(GrabRenderer.SpriteKey(obj));
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

        private static void DrawObject(DrawingContext ctx, ViewTransform v, SpriteCache sprites, LevelObject obj)
        {
            ObjectSprite? sprite = sprites.GetSprite(GrabRenderer.SpriteKey(obj));
            if (sprite is not null)
            {
                if (sprite.Variants.Count > 0)
                {
                    DrawLayer(ctx, v, sprite.Variants[SpriteVariantPicker.Pick(obj.Element, sprite.Variants.Count)], obj.X, obj.Y, sprite.Scale);
                }
                DrawSprite(ctx, v, sprite, obj.X, obj.Y);
            }
            foreach (string overlayKey in GrabRenderer.OverlaySpriteKeys(obj))
            {
                if (sprites.GetSprite(overlayKey) is { } overlay)
                {
                    DrawSprite(ctx, v, overlay, obj.X, obj.Y);
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
            double scale)
        {
            SpriteLayout layout = SpritePlacement.Compute(layer.Frame, x, y, scale);
            Rect source = new(layout.Source.X, layout.Source.Y, layout.Source.W, layout.Source.H);
            Vec2 dtl = v.LevelToScreen(new Vec2(layout.Dest.X, layout.Dest.Y));
            Vec2 dbr = v.LevelToScreen(new Vec2(layout.Dest.X + layout.Dest.W, layout.Dest.Y + layout.Dest.H));
            ctx.DrawImage(layer.Bitmap, source, new Rect(dtl.X, dtl.Y, dbr.X - dtl.X, dbr.Y - dtl.Y));
        }

        private static void DrawHitbox(
            DrawingContext ctx,
            ViewTransform v,
            LevelObject obj,
            double scale,
            HitboxModel model,
            IBrush brush)
        {
            if (HitboxTable.Compute(obj.Type, obj.X, obj.Y, scale, model) is not { } b)
            {
                return;
            }
            Vec2 tl = v.LevelToScreen(new Vec2(b.X, b.Y));
            Vec2 br = v.LevelToScreen(new Vec2(b.X + b.W, b.Y + b.H));
            Pen pen = new(brush, 1.5) { DashStyle = new DashStyle([4, 3], 0) };
            ctx.DrawRectangle(null, pen, new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y));
        }

        // Whether a level-space point sits on the selected grab's auto-catch radius ring, within a
        // ~6px screen tolerance (converted to level units by the current zoom).
        private bool OnRadiusEdge(Vec2 levelPt)
        {
            return SelectedObject is { Type: "grab" } g && View.Zoom > 0 && GrabRadius.Of(g) is double r && GrabRadius.OnEdge(new Vec2(g.X, g.Y), r, levelPt, 6 / View.Zoom);
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
                    : SelectionBounds(Sprites, o));
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
                    _railDrag = handle;
                    e.Pointer.Capture(this);
                    return;
                case GrabRail.Handle.MoveBar:
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

            if (_resizingRadius && SelectedObject is { } g)
            {
                double r = GrabRadius.FromDrag(new Vec2(g.X, g.Y), levelPt);
                g.SetAttr("radius", ((int)Math.Round(r)).ToString(CultureInfo.InvariantCulture));
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
            _dragging = false;
            _panning = false;
            _resizingRadius = false;
            _railDrag = GrabRail.Handle.None;
            // Letting go ends the "grabbed" look; a fresh hover re-lights it if the cursor is on the hook.
            SetHookHovered(false);
            e.Pointer.Capture(null);
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
        public void AddAtCenter(string element)
        {
            if (Document is not { } doc)
            {
                return;
            }

            LevelObject? placed = PlaceAt?.Invoke(element, doc.Width / 2, doc.Height / 2);
            if (placed is not null)
            {
                SelectedObject = placed;
            }
            InvalidateVisual();
        }

        /// <summary>Drops an object at a screen-space point, snapping according to the current settings.</summary>
        public void DropElement(string element, Point screenPoint)
        {
            Vec2 levelPt = View.ScreenToLevel(new Vec2(screenPoint.X, screenPoint.Y));
            (int gx, int gy) = Snap(levelPt);
            LevelObject? placed = PlaceAt?.Invoke(element, gx, gy);
            if (placed is not null)
            {
                SelectedObject = placed;
            }
            InvalidateVisual();
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
