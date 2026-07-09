using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Input;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Pointer, wheel, and touch input: hit-testing, dragging, and gesture handling.</summary>
    public sealed partial class LevelCanvas
    {
        /// <summary>
        /// Horizontal-resize cursor (col-resize) shown over the auto-catch radius ring or a horizontal rail end/hook.
        /// Created lazily rather than in the static constructor: eager creation would touch Avalonia's cursor factory
        /// at type load, which throws in the headless test host. <see cref="Lazy{T}.Value"/> still allocates the cursor
        /// only once, so pointer-move hit-testing doesn't churn instances.
        /// </summary>
        private static readonly Lazy<Cursor> LazyResizeCursor = new(() => new Cursor(StandardCursorType.SizeWestEast));

        /// <summary>Vertical-resize cursor shown over a vertical rail end/hook; lazily created for the same reason as <see cref="LazyResizeCursor"/>.</summary>
        private static readonly Lazy<Cursor> LazyVResizeCursor = new(() => new Cursor(StandardCursorType.SizeNorthSouth));

        /// <summary>The shared horizontal-resize cursor instance.</summary>
        private static Cursor ResizeCursor => LazyResizeCursor.Value;

        /// <summary>The shared vertical-resize cursor instance.</summary>
        private static Cursor VResizeCursor => LazyVResizeCursor.Value;

        /// <summary>
        /// Whether a level-space point sits on the selected grab's auto-catch radius ring, within a ~6 px screen
        /// tolerance (converted to level units by the current zoom).
        /// </summary>
        /// <param name="levelPt">The point to test, in level coordinates.</param>
        /// <returns>True when the point is on the ring's edge and a grab with a radius is selected.</returns>
        private bool OnRadiusEdge(Vec2 levelPt)
        {
            return SelectedObject is { } g && View.Zoom > 0 && RadiusRing.Of(g) is { } ring
                && GrabRadius.OnEdge(new Vec2(g.X, g.Y), ring.Radius, levelPt, 6 / View.Zoom);
        }

        /// <summary>What part of the selected movable grab's rail a level point is over, or <see cref="GrabRail.Handle.None"/>.</summary>
        /// <remarks>
        /// The hit-testing itself lives in <see cref="GrabRail"/>; here we only supply the selected grab's geometry and
        /// the screen-derived tolerances: ~9 px for the end caps, the hook's own footprint, and the bar's half thickness.
        /// </remarks>
        /// <param name="levelPt">The point to test, in level coordinates.</param>
        /// <returns>The rail handle under the point, or <see cref="GrabRail.Handle.None"/>.</returns>
        private GrabRail.Handle HitRail(Vec2 levelPt)
        {
            return SelectedObject is { Type: "grab" } sel
                && View.Zoom > 0
                && GrabRenderer.DrawsMovableRail(sel)
                && GrabRail.Of(sel) is { } g
                ? GrabRail.HitTest(g, levelPt, endTolerance: 9 / View.Zoom, hookTolerance: 24, barThickness: 20)
                : GrabRail.Handle.None;
        }

        /// <summary>What part of the selected spike resize affordance a level point is over.</summary>
        /// <param name="levelPt">The point to test, in level coordinates.</param>
        /// <returns>The spike resize handle under the point, or <see cref="SpikeResize.Handle.None"/>.</returns>
        private SpikeResize.Handle HitSpikeResize(Vec2 levelPt)
        {
            return SelectedObject is { } sel
                && View.Zoom > 0
                && SpikeObject.IsSpike(sel.Type)
                ? SpikeResize.HitTest(sel, levelPt, SpikeSpriteScale(sel), tolerance: 9 / View.Zoom, thickness: 12 / View.Zoom)
                : SpikeResize.Handle.None;
        }

        /// <summary>What part of the selected rotatable object's dial a level point is over, or <see cref="ObjectRotation.Handle.None"/>.</summary>
        /// <remarks>
        /// The geometry lives in <see cref="ObjectRotation"/>; here we supply the object's spec and the screen-derived
        /// tolerances (converted to level units by the current zoom), matching the rail/radius handles.
        /// </remarks>
        /// <param name="levelPt">The point to test, in level coordinates.</param>
        /// <returns>The dial handle under the point, or <see cref="ObjectRotation.Handle.None"/>.</returns>
        private ObjectRotation.Handle HitRotationDial(Vec2 levelPt)
        {
            if (SelectedObject is not { } obj || View.Zoom <= 0 || RotationTable.For(obj.Type) is not { } spec)
            {
                return ObjectRotation.Handle.None;
            }
            Vec2 c = new(obj.X, obj.Y);
            double radius = RotationDialRenderer.RadiusPx / View.Zoom;
            return ObjectRotation.HitTest(
                c, ObjectRotation.StoredAngle(obj, spec), spec, radius, levelPt,
                ringTolerance: RotationDialRenderer.RingTolerancePx / View.Zoom,
                knobTolerance: RotationDialRenderer.KnobTolerancePx / View.Zoom);
        }

        /// <summary>
        /// Writes the object's new angle from a dial drag: free (whole degrees) unless <see cref="KeyModifiers.Alt"/>
        /// is held, which snaps to the spec's step (15°).
        /// </summary>
        /// <param name="obj">The rotatable object being edited.</param>
        /// <param name="spec">Rotation spec describing the object's angle attribute and snap step.</param>
        /// <param name="levelPt">The pointer position in level coordinates.</param>
        /// <param name="mods">Active keyboard modifiers; Alt enables snapping.</param>
        private static void ApplyRotation(LevelObject obj, RotationSpec spec, Vec2 levelPt, KeyModifiers mods)
        {
            bool snap = mods.HasFlag(KeyModifiers.Alt);
            double angle = ObjectRotation.AngleFromPoint(new Vec2(obj.X, obj.Y), levelPt, spec, snap);
            obj.SetAttr(spec.AttributeName, ObjectRotation.Format(angle));
        }

        /// <summary>
        /// Applies the active rail drag to the grab: sliding moves the hook (object x/y) and its offset together so the
        /// rail stays put; resizing an end rewrites <c>moveLength</c> (and <c>moveOffset</c> for the near end). All
        /// constrained by <see cref="GrabRail"/> so the hook never leaves the rail.
        /// </summary>
        /// <param name="grab">The grab object being edited.</param>
        /// <param name="g">The grab's current rail geometry.</param>
        /// <param name="levelPt">The pointer position in level coordinates.</param>
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

        /// <summary>Rounds a level-space value to a whole number and formats it with the invariant culture for an attribute.</summary>
        /// <param name="value">The value to round and format.</param>
        /// <returns>The rounded integer as an invariant-culture string.</returns>
        private static string Whole(double value)
        {
            return ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The cursor for a rail handle: a horizontal rail end/hook reads as a horizontal resize, a vertical one as a
        /// vertical resize (the hook slides along the same axis). The bar keeps the default arrow — it is still
        /// draggable to move the whole grab, but a move cursor over the whole rail is noisy.
        /// </summary>
        /// <param name="handle">The rail handle under the cursor.</param>
        /// <returns>The cursor to display for that handle.</returns>
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

        /// <summary>Cursor for a spike resize handle based on the spike's current rotation.</summary>
        private Cursor CursorForSpikeResize()
        {
            if (SelectedObject is not { } spike || RotationTable.For(spike.Type) is not { } spec)
            {
                return ResizeCursor;
            }

            double deg = Math.Abs(ObjectRotation.Normalize(ObjectRotation.DisplayDegrees(spike, spec)));
            return deg is > 45 and < 135 ? VResizeCursor : ResizeCursor;
        }

        /// <summary>Updates the hook hover state, repainting only on a change so the highlight art swaps in/out.</summary>
        /// <param name="hovered">True when the pointer is over the selected grab's hook.</param>
        private void SetHookHovered(bool hovered)
        {
            if (_hookHovered != hovered)
            {
                _hookHovered = hovered;
                InvalidateVisual();
            }
        }

        /// <summary>Updates the rotation-knob hover state, repainting only on a change so the knob lights up/down.</summary>
        /// <param name="hovered">True when the pointer is over the selected object's rotation knob.</param>
        private void SetDialKnobHovered(bool hovered)
        {
            if (_dialKnobHovered != hovered)
            {
                _dialKnobHovered = hovered;
                InvalidateVisual();
            }
        }

        /// <summary>Finds the index of an object within the list by reference/value equality.</summary>
        /// <param name="objects">The object list to search.</param>
        /// <param name="target">The object to locate.</param>
        /// <returns>The zero-based index of <paramref name="target"/>, or -1 when it is not present.</returns>
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

        /// <summary>Builds the per-object click-test bounds, one entry per object in document order.</summary>
        /// <remarks>Clicking uses the same box that the selection marquee draws (trimmed art + 25%).</remarks>
        /// <param name="doc">The level document whose objects are measured.</param>
        /// <returns>Level-space hit-test bounds parallel to <see cref="LevelDocument.Objects"/>.</returns>
        private List<LevelBounds> BuildHitBounds(LevelDocument doc)
        {
            List<LevelBounds> list = [];
            foreach (LevelObject o in doc.Objects)
            {
                list.Add(Sprites is null
                    ? new LevelBounds(o.X - 8, o.Y - 8, 16, 16)
                    : LevelSceneRenderer.SelectionBounds(Sprites, o, ActiveCandySkin, ActiveOmNomSupport, doc.NightLevel));
            }
            return list;
        }

        private bool HitBoundContains(LevelObject obj, LevelBounds bounds, Vec2 point)
        {
            return LevelSceneRenderer.SelectionContains(obj, bounds, point, PreviewSpinDegrees(obj), PreviewAnimationSeconds(obj));
        }

        private int TopmostHit(IReadOnlyList<LevelObject> objects, List<LevelBounds> bounds, Vec2 point, int afterIndex = -1)
        {
            int n = bounds.Count;
            if (n == 0)
            {
                return -1;
            }

            int start = afterIndex >= 0 ? afterIndex - 1 + n : n - 1;
            for (int step = 0; step < n; step++)
            {
                int i = (start - step) % n;
                if (HitBoundContains(objects[i], bounds[i], point))
                {
                    return i;
                }
            }

            return -1;
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

            SpikeResize.Handle spikeHandle = HitSpikeResize(levelPt);
            if (spikeHandle != SpikeResize.Handle.None && SelectedObject is { } spikeResizeObj)
            {
                BeginDocumentEdit?.Invoke();
                _spikeResizeDrag = spikeHandle;
                SpikeResize.ApplyDrag(spikeResizeObj, levelPt, SpikeSpriteScale(spikeResizeObj));
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                e.Pointer.Capture(this);
                return;
            }

            // Grabbing the selected object's rotation dial (knob or ring) rotates it; takes priority over
            // object hit-testing so the dial wins over anything beneath it.
            if (HitRotationDial(levelPt) != ObjectRotation.Handle.None
                && SelectedObject is { } rotObj && RotationTable.For(rotObj.Type) is { } rotSpec)
            {
                BeginDocumentEdit?.Invoke();
                _rotating = true;
                ApplyRotation(rotObj, rotSpec, levelPt, e.KeyModifiers);
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                e.Pointer.Capture(this);
                return;
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
                int dh = TopmostHit(doc.Objects, bounds, levelPt);
                ToggleLock?.Invoke(dh >= 0 ? doc.Objects[dh] : null);
                return;
            }

            // While an object is locked, only it is interactive — clicks never fall through to other objects.
            if (LockedObject is { } locked)
            {
                int li = IndexOf(doc.Objects, locked);
                if (li >= 0 && HitBoundContains(locked, bounds[li], levelPt))
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
                        && HitBoundContains(doc.Objects[_lastHitIndex], bounds[_lastHitIndex], levelPt) ? _lastHitIndex : -1;
            int hit = TopmostHit(doc.Objects, bounds, levelPt, after);
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

            if (_spikeResizeDrag != SpikeResize.Handle.None && SelectedObject is { } spikeObj)
            {
                SpikeResize.ApplyDrag(spikeObj, levelPt, SpikeSpriteScale(spikeObj));
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                return;
            }

            if (_rotating && SelectedObject is { } rotObj && RotationTable.For(rotObj.Type) is { } rotSpec)
            {
                ApplyRotation(rotObj, rotSpec, levelPt, e.KeyModifiers);
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
                ObjectRotation.Handle dial = HitRotationDial(levelPt);
                SetDialKnobHovered(dial == ObjectRotation.Handle.Knob);
                SpikeResize.Handle spikeHandle = HitSpikeResize(levelPt);
                Cursor = dial != ObjectRotation.Handle.None ? new Cursor(StandardCursorType.Hand)
                    : spikeHandle != SpikeResize.Handle.None ? CursorForSpikeResize()
                    : OnRadiusEdge(levelPt) ? ResizeCursor : CursorForHandle(handle);
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

        /// <summary>
        /// Ends any active pointer gesture (drag, pan, radius resize, rail drag, rotation, hook hover), resets its
        /// state, and fires <see cref="CompleteDocumentEdit"/> once when the gesture edited the document.
        /// </summary>
        private void EndPointerGesture()
        {
            // Capture loss (including the release path's own Capture(null)) can fire with nothing in
            // progress; skip the resets and completion callback unless a gesture is actually active.
            bool gestureActive = _dragging || _panning || _resizingRadius
                || _railDrag != GrabRail.Handle.None || _spikeResizeDrag != SpikeResize.Handle.None
                || _rotating || _hookHovered;
            if (!gestureActive)
            {
                return;
            }

            bool editedDocument = _dragging || _resizingRadius
                || _railDrag != GrabRail.Handle.None || _spikeResizeDrag != SpikeResize.Handle.None || _rotating;
            _dragging = false;
            _panning = false;
            _resizingRadius = false;
            _railDrag = GrabRail.Handle.None;
            _spikeResizeDrag = SpikeResize.Handle.None;
            _rotating = false;
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
            SetDialKnobHovered(false); // nor the rotation knob
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

        /// <summary>Handles a touch pinch gesture, converting its cumulative scale into an incremental zoom about the pinch origin.</summary>
        /// <param name="sender">The gesture source (unused).</param>
        /// <param name="e">Pinch event data supplying the scale and origin.</param>
        private void Canvas_Pinch(object? sender, PinchEventArgs e)
        {
            double factor = ViewNavigation.PinchScaleToZoomFactor(_lastPinchScale, e.Scale);
            _lastPinchScale = e.Scale;
            ZoomBy(factor, e.ScaleOrigin);
            e.Handled = true;
        }

        /// <summary>Resets the pinch scale baseline when a touch pinch gesture ends.</summary>
        /// <param name="sender">The gesture source (unused).</param>
        /// <param name="e">Pinch-ended event data (unused).</param>
        private void Canvas_PinchEnded(object? sender, PinchEndedEventArgs e)
        {
            _lastPinchScale = 1;
            e.Handled = true;
        }

        /// <summary>Handles a trackpad magnify gesture, converting its delta into a zoom about the pointer position.</summary>
        /// <param name="sender">The gesture source (unused).</param>
        /// <param name="e">Magnify event data supplying the delta and pointer position.</param>
        private void Canvas_TouchPadMagnify(object? sender, PointerDeltaEventArgs e)
        {
            double delta = Math.Abs(e.Delta.Y) > double.Epsilon ? e.Delta.Y : e.Delta.X;
            ZoomBy(ViewNavigation.MagnifyDeltaToZoomFactor(delta), e.GetPosition(this));
            e.Handled = true;
        }

        private double SpikeSpriteScale(LevelObject spike)
        {
            return Document is { } doc
                && Sprites?.GetSprite(LevelSceneRenderer.CanvasSpriteKey(spike, doc.NightLevel), ActiveCandySkin, ActiveOmNomSupport) is { } sprite
                ? sprite.Scale
                : 1.0;
        }
    }
}
