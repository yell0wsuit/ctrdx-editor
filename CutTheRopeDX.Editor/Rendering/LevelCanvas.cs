using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using CutTheRopeDX.Editor.Content;
using CutTheRopeDX.Editor.Core.Document;
using CutTheRopeDX.Editor.Core.Editing;
using CutTheRopeDX.Editor.Core.Geometry;

namespace CutTheRopeDX.Editor.Rendering
{
    public sealed class LevelCanvas : Control
    {
        public static readonly StyledProperty<LevelDocument?> DocumentProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelDocument?>(nameof(Document));

        public static readonly StyledProperty<SpriteCache?> SpritesProperty =
            AvaloniaProperty.Register<LevelCanvas, SpriteCache?>(nameof(Sprites));

        public static readonly StyledProperty<ViewTransform> ViewProperty =
            AvaloniaProperty.Register<LevelCanvas, ViewTransform>(nameof(View), ViewTransform.Identity);

        public static readonly StyledProperty<bool> SnapEnabledProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(SnapEnabled));

        public static readonly StyledProperty<LevelObject?> SelectedObjectProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelObject?>(
                nameof(SelectedObject), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<LevelObject?> LockedObjectProperty =
            AvaloniaProperty.Register<LevelCanvas, LevelObject?>(
                nameof(LockedObject), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<bool> ShowHitboxesProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(ShowHitboxes), defaultValue: true);

        public static readonly StyledProperty<bool> ShowMobileHitboxesProperty =
            AvaloniaProperty.Register<LevelCanvas, bool>(nameof(ShowMobileHitboxes));

        static LevelCanvas()
        {
            AffectsRender<LevelCanvas>(
                DocumentProperty, SpritesProperty, ViewProperty, SnapEnabledProperty,
                SelectedObjectProperty, LockedObjectProperty,
                ShowHitboxesProperty, ShowMobileHitboxesProperty);
        }

        public LevelDocument? Document { get => GetValue(DocumentProperty); set => SetValue(DocumentProperty, value); }
        public SpriteCache? Sprites { get => GetValue(SpritesProperty); set => SetValue(SpritesProperty, value); }
        public ViewTransform View { get => GetValue(ViewProperty); set => SetValue(ViewProperty, value); }
        public bool SnapEnabled { get => GetValue(SnapEnabledProperty); set => SetValue(SnapEnabledProperty, value); }
        public LevelObject? SelectedObject { get => GetValue(SelectedObjectProperty); set => SetValue(SelectedObjectProperty, value); }
        public LevelObject? LockedObject { get => GetValue(LockedObjectProperty); set => SetValue(LockedObjectProperty, value); }
        public bool ShowHitboxes { get => GetValue(ShowHitboxesProperty); set => SetValue(ShowHitboxesProperty, value); }
        public bool ShowMobileHitboxes { get => GetValue(ShowMobileHitboxesProperty); set => SetValue(ShowMobileHitboxesProperty, value); }

        public Func<string, int, int, LevelObject?>? PlaceAt { get; set; }
        public Action<LevelObject?>? ToggleLock { get; set; }

        private bool _dragging;
        private Vec2 _dragOffset;
        private int _lastHitIndex = -1;
        private bool _panning;
        private Point _panLast;
        private bool _pendingFit;
        private bool _ghostActive;
        private string? _ghostElement;
        private Vec2 _ghostLevel;

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
        }

        /// <summary>Zooms about the viewport center (for menu/keyboard zoom).</summary>
        public void ZoomBy(double factor)
        {
            ViewTransform v = View;
            double newZoom = Math.Clamp(v.Zoom * factor, 0.1, 10.0);
            double cx = Bounds.Width / 2, cy = Bounds.Height / 2;
            double panX = cx - ((cx - v.PanX) * (newZoom / v.Zoom));
            double panY = cy - ((cy - v.PanY) * (newZoom / v.Zoom));
            View = new ViewTransform(newZoom, panX, panY);
        }

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
            foreach (LevelObject obj in objects)
            {
                if (obj.Type != "grab")
                {
                    continue;
                }

                RopeTarget rope = RopeResolver.Resolve(obj, objects, doc.TwoParts);
                if (rope.Target is null)
                {
                    continue;
                }

                Vec2 a = v.LevelToScreen(new Vec2(obj.X, obj.Y));
                Vec2 b = v.LevelToScreen(new Vec2(rope.Target.X, rope.Target.Y));
                IBrush brush = rope.Kind == RopeTargetKind.Bulb ? Brushes.Khaki : Brushes.IndianRed;
                Pen pen = new(brush, 3) { DashStyle = new DashStyle([4, 3], 0) };
                context.DrawLine(pen, new Point(a.X, a.Y), new Point(b.X, b.Y));
            }

            foreach (LevelObject obj in objects)
            {
                DrawObject(context, v, sprites, obj);
            }

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
            ObjectSprite? sprite = sprites.GetSprite(obj.Type);
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
            ObjectSprite? sprite = sprites.GetSprite(obj.Type);
            if (sprite is not null)
            {
                DrawSprite(ctx, v, sprite, obj.X, obj.Y);
            }
        }

        private static void DrawSprite(DrawingContext ctx, ViewTransform v, ObjectSprite sprite, double x, double y)
        {
            foreach (SpriteLayerDraw layer in sprite.Layers)
            {
                SpriteLayout layout = SpritePlacement.Compute(layer.Frame, x, y, sprite.Scale);
                Rect source = new(layout.Source.X, layout.Source.Y, layout.Source.W, layout.Source.H);
                Vec2 dtl = v.LevelToScreen(new Vec2(layout.Dest.X, layout.Dest.Y));
                Vec2 dbr = v.LevelToScreen(new Vec2(layout.Dest.X + layout.Dest.W, layout.Dest.Y + layout.Dest.H));
                ctx.DrawImage(layer.Bitmap, source, new Rect(dtl.X, dtl.Y, dbr.X - dtl.X, dbr.Y - dtl.Y));
            }
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

            Point p = e.GetPosition(this);
            Vec2 levelPt = View.ScreenToLevel(new Vec2(p.X, p.Y));
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
                return;
            }

            LevelObject obj = doc.Objects[hit];
            SelectedObject = obj;
            _dragOffset = levelPt - new Vec2(obj.X, obj.Y);
            _dragging = true;
            e.Pointer.Capture(this);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_panning)
            {
                Point now = e.GetPosition(this);
                View = new ViewTransform(View.Zoom, View.PanX + (now.X - _panLast.X), View.PanY + (now.Y - _panLast.Y));
                _panLast = now;
                return;
            }

            if (!_dragging || SelectedObject is not { } selected)
            {
                return;
            }

            Point p = e.GetPosition(this);
            Vec2 levelPt = View.ScreenToLevel(new Vec2(p.X, p.Y));
            (int gx, int gy) = Snap(levelPt - _dragOffset);
            selected.X = gx;
            selected.Y = gy;
            InvalidateVisual();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _dragging = false;
            _panning = false;
            e.Pointer.Capture(null);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            Point p = e.GetPosition(this);
            ViewTransform v = View;
            double factor = e.Delta.Y > 0 ? 1.1 : 1 / 1.1;
            double newZoom = Math.Clamp(v.Zoom * factor, 0.1, 10.0);
            double panX = p.X - ((p.X - v.PanX) * (newZoom / v.Zoom));
            double panY = p.Y - ((p.Y - v.PanY) * (newZoom / v.Zoom));
            View = new ViewTransform(newZoom, panX, panY);
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
