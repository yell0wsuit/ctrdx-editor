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
        /// <returns>True when the point is on the ring's edge and a grab with a radius is selected and not animating.</returns>
        private bool OnRadiusEdge(Vec2 levelPt)
        {
            // A grab animating in preview draws its ring at the moving position, but the edge hit-test and drag
            // math below use the authored center — so editing is disabled until the preview stops, matching how
            // an animating object is unpickable.
            if (SelectedObject is not { } selected || IsAnimatingInPreview(selected) || View.Zoom <= 0)
            {
                return false;
            }

            double? radius = selected.Type == "ghost" && _ghostPreview.ShowsRadiusRing(selected)
                ? GrabRadius.Of(selected)
                : RadiusRing.Of(selected)?.Radius;
            return radius is double r
                && GrabRadius.OnEdge(new Vec2(selected.X, selected.Y), r, levelPt, 6 / View.Zoom);
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

        /// <summary>What part of the selected spike/bouncer resize affordance a level point is over.</summary>
        /// <param name="levelPt">The point to test, in level coordinates.</param>
        /// <returns>The strip resize handle under the point, or <see cref="SpikeResize.Handle.None"/>.</returns>
        private SpikeResize.Handle HitStripResize(Vec2 levelPt)
        {
            if (SelectedObject is not { } sel || View.Zoom <= 0)
            {
                return SpikeResize.Handle.None;
            }

            double tol = 9 / View.Zoom;
            double thickness = 12 / View.Zoom;
            return SpikeObject.IsSpike(sel.Type)
                ? SpikeResize.HitTest(sel, levelPt, StripSpriteScale(sel), tol, thickness)
                : BouncerObject.IsBouncer(sel.Type)
                    ? BouncerResize.HitTest(sel, levelPt, StripSpriteScale(sel), tol, thickness)
                    : SpikeResize.Handle.None;
        }

        /// <summary>What part of the selected conveyor a level point is over, or <see cref="ConveyorGeometry.Handle.None"/>.</summary>
        /// <param name="levelPt">The point to test, in level coordinates.</param>
        /// <returns>The conveyor handle under the point, or <see cref="ConveyorGeometry.Handle.None"/>.</returns>
        private ConveyorGeometry.Handle HitConveyor(Vec2 levelPt)
        {
            return SelectedObject is { } sel && View.Zoom > 0 && ConveyorGeometry.Of(sel) is { } s
                ? ConveyorGeometry.HitTest(s, levelPt, endTolerance: 9 / View.Zoom, widthTolerance: 9 / View.Zoom)
                : ConveyorGeometry.Handle.None;
        }

        /// <summary>Applies the active conveyor drag: far-end rewrites length+angle, width rewrites thickness.</summary>
        /// <param name="belt">The conveyor object being edited.</param>
        /// <param name="levelPt">The pointer position in level coordinates.</param>
        private void ApplyConveyorDrag(LevelObject belt, Vec2 levelPt)
        {
            switch (_conveyorDrag)
            {
                case ConveyorGeometry.Handle.FarEnd:
                    ConveyorGeometry.ApplyFarEndDrag(belt, levelPt);
                    break;
                case ConveyorGeometry.Handle.Width:
                    ConveyorGeometry.ApplyWidthDrag(belt, levelPt);
                    break;
                case ConveyorGeometry.Handle.None:
                default:
                    break;
            }
        }

        /// <summary>Which vinyl handle a level point is over, or <see cref="VinylGeometry.Handle.None"/>.</summary>
        private VinylGeometry.Handle HitVinylHandle(Vec2 levelPt)
        {
            return SelectedObject is { Type: "rotatedCircle" } vinyl && View.Zoom > 0
                ? VinylGeometry.HitTest(vinyl, levelPt, 18 / View.Zoom)
                : VinylGeometry.Handle.None;
        }

        /// <summary>Applies a strip resize drag to the object, dispatching to the spike or bouncer helper.</summary>
        private void ApplyStripResize(LevelObject obj, Vec2 levelPt)
        {
            if (SpikeObject.IsSpike(obj.Type))
            {
                SpikeResize.ApplyDrag(obj, levelPt, StripSpriteScale(obj));
            }
            else if (BouncerObject.IsBouncer(obj.Type))
            {
                BouncerResize.ApplyDrag(obj, levelPt, StripSpriteScale(obj));
            }
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
            if (SelectedObject is not { } obj || View.Zoom <= 0 || EditableRotationSpec(obj) is not { } spec)
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

        /// <summary>Resolves an object's ordinary rotation spec or the ghost's preview-only bouncer spec.</summary>
        private RotationSpec? EditableRotationSpec(LevelObject obj)
        {
            return obj.Type == "ghost" && _ghostPreview.ShowsRotationDial(obj)
                ? GhostBouncerRotation
                : RotationTable.EditableFor(obj.Type);
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

        /// <summary>Cursor for a strip resize handle based on the object's current rotation.</summary>
        private Cursor CursorForStripResize()
        {
            if (SelectedObject is not { } obj || RotationTable.For(obj.Type) is not { } spec)
            {
                return ResizeCursor;
            }

            double deg = Math.Abs(ObjectRotation.Normalize(ObjectRotation.DisplayDegrees(obj, spec)));
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

        /// <summary>Updates which vinyl handle is hovered, repainting on a change so its active glow/ring swaps in/out.</summary>
        private void SetVinylHandleHovered(VinylGeometry.Handle hovered)
        {
            if (_vinylHandleHovered != hovered)
            {
                _vinylHandleHovered = hovered;
                InvalidateVisual();
            }
        }

        /// <summary>Whether an object has real polyline movement that supports direct node editing.</summary>
        private static bool IsEditablePolyline(LevelObject obj)
        {
            return MoverPath.IsPolylineMovement(obj.GetAttr("path"));
        }

        /// <summary>Returns the selected canonical waypoint under a level point, or -1.</summary>
        private int HitPolylinePoint(Vec2 levelPt)
        {
            return SelectedObject is { } obj && View.Zoom > 0 && IsEditablePolyline(obj)
                ? MoverPath.HitCanonicalPoint(
                    new Vec2(obj.X, obj.Y), obj.GetAttr("path"), levelPt, tolerance: 9 / View.Zoom)
                : -1;
        }

        /// <summary>Returns the segment whose midpoint insert handle is under a level point, or -1.</summary>
        private int HitPolylineSegment(Vec2 levelPt)
        {
            if (SelectedObject is not { } obj || View.Zoom <= 0 || !IsEditablePolyline(obj)
                || !MoverPath.CanAddCanonicalPoint(new Vec2(obj.X, obj.Y), obj.GetAttr("path")))
            {
                return -1;
            }

            Vec2[] points = MoverPath.CanonicalPoints(new Vec2(obj.X, obj.Y), obj.GetAttr("path"));
            double tolerance = 7 / View.Zoom;
            double toleranceSquared = tolerance * tolerance;
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vec2 midpoint = new(
                    (points[i].X + points[i + 1].X) / 2,
                    (points[i].Y + points[i + 1].Y) / 2);
                double dx = midpoint.X - levelPt.X;
                double dy = midpoint.Y - levelPt.Y;
                if ((dx * dx) + (dy * dy) <= toleranceSquared)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Level-space position of the append "+" nub: ~24 px past the last waypoint along the last segment.</summary>
        private Vec2 PolylineNubPoint(LevelObject obj)
        {
            Vec2[] points = MoverPath.CanonicalPoints(new Vec2(obj.X, obj.Y), obj.GetAttr("path"));
            Vec2 tip = points[^1];
            Vec2 direction = points.Length >= 2
                ? new Vec2(tip.X - points[^2].X, tip.Y - points[^2].Y)
                : new Vec2(1, 0);
            double length = Math.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
            if (length < 0.0001)
            {
                direction = new Vec2(1, 0);
                length = 1;
            }

            double offset = 24 / View.Zoom;
            return new Vec2(
                tip.X + (direction.X / length * offset),
                tip.Y + (direction.Y / length * offset));
        }

        /// <summary>True when the pointer is over the append nub for the selected editable polyline.</summary>
        private bool HitPolylineNub(Vec2 levelPt)
        {
            if (SelectedObject is not { } obj || View.Zoom <= 0 || !IsEditablePolyline(obj)
                || !MoverPath.CanAddCanonicalPoint(new Vec2(obj.X, obj.Y), obj.GetAttr("path")))
            {
                return false;
            }

            Vec2 nub = PolylineNubPoint(obj);
            double tolerance = 9 / View.Zoom;
            double dx = nub.X - levelPt.X;
            double dy = nub.Y - levelPt.Y;
            return (dx * dx) + (dy * dy) <= tolerance * tolerance;
        }

        /// <summary>
        /// True when hovering the end of a selected editable polyline that has hit its point cap, so the missing
        /// append nub gets an explanatory hint instead of just silently vanishing.
        /// </summary>
        private bool HoveringPolylineLimit(Vec2 levelPt)
        {
            if (SelectedObject is not { } obj || View.Zoom <= 0 || !IsEditablePolyline(obj)
                || IsAnimatingInPreview(obj)
                || MoverPath.CanAddCanonicalPoint(new Vec2(obj.X, obj.Y), obj.GetAttr("path")))
            {
                return false;
            }

            Vec2 nub = PolylineNubPoint(obj);
            double tolerance = 22 / View.Zoom;
            double dx = nub.X - levelPt.X;
            double dy = nub.Y - levelPt.Y;
            return (dx * dx) + (dy * dy) <= tolerance * tolerance;
        }

        /// <summary>Rounds a point to whole units after snapping its direction from an anchor to 45-degree increments.</summary>
        private static (int X, int Y) SnapAngle(Vec2 anchor, Vec2 point)
        {
            double dx = point.X - anchor.X;
            double dy = point.Y - anchor.Y;
            double length = Math.Sqrt((dx * dx) + (dy * dy));
            if (length < 0.0001)
            {
                return ((int)Math.Round(point.X), (int)Math.Round(point.Y));
            }

            double angle = Math.Round(Math.Atan2(dy, dx) / (Math.PI / 4)) * (Math.PI / 4);
            return (
                (int)Math.Round(anchor.X + (Math.Cos(angle) * length)),
                (int)Math.Round(anchor.Y + (Math.Sin(angle) * length)));
        }

        /// <summary>Deletes a single canonical waypoint from the selected polyline, reconnecting neighbors.</summary>
        private void DeleteSelectedPolylineVertex(int index)
        {
            if (index <= 0 || SelectedObject is not { } obj)
            {
                return;
            }

            BeginDocumentEdit?.Invoke();
            obj.SetAttr("path", MoverPath.DeleteCanonicalPoint(
                new Vec2(obj.X, obj.Y), obj.GetAttr("path"), index));
            _polylineHoverPoint = -1;
            SelectedObjectMoved?.Invoke();
            CompleteDocumentEdit?.Invoke();
            InvalidateVisual();
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
            // An animating object (moving or spinning) is a moving target whose hit box no longer matches where it's
            // drawn, so it can't be picked until the preview stops. Static objects stay editable.
            return !IsAnimatingInPreview(obj)
                && LevelSceneRenderer.SelectionContains(obj, bounds, point, PreviewSpinDegrees(obj), PreviewAnimationSeconds(obj));
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

            Point p = e.GetPosition(this);
            Vec2 levelPt = View.ScreenToLevel(new Vec2(p.X, p.Y));
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                int rightHit = HitPolylinePoint(levelPt);
                if (rightHit > 0)
                {
                    DeleteSelectedPolylineVertex(rightHit);
                    e.Handled = true;
                }
                return;
            }
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (SelectedObject is { Type: "ghost" } selectedGhost)
            {
                foreach ((Rect iconRect, GhostMorph morph) in _ghostIconHits)
                {
                    if (iconRect.Contains(p))
                    {
                        _ghostPreview.Set(selectedGhost, morph);
                        InvalidateVisual();
                        e.Handled = true;
                        return;
                    }
                }
            }

            // A vinyl handle drag rotates the disc; it takes priority over the size ring since both sit on
            // the disc edge (the ring wins everywhere except the two handle spots).
            VinylGeometry.Handle vinylHandle = HitVinylHandle(levelPt);
            if (vinylHandle != VinylGeometry.Handle.None && SelectedObject is { } vinylObj)
            {
                BeginDocumentEdit?.Invoke();
                _vinylHandleDrag = vinylHandle;
                vinylObj.SetAttr("handleAngle", Whole(VinylGeometry.AngleFor(vinylObj, vinylHandle, levelPt)));
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                e.Pointer.Capture(this);
                return;
            }

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

            SpikeResize.Handle stripHandle = HitStripResize(levelPt);
            if (stripHandle != SpikeResize.Handle.None && SelectedObject is { } stripResizeObj)
            {
                BeginDocumentEdit?.Invoke();
                _stripResizeDrag = stripHandle;
                ApplyStripResize(stripResizeObj, levelPt);
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                e.Pointer.Capture(this);
                return;
            }

            // Grabbing the selected conveyor's far-end (length+angle) or width handle. Rotation lives here,
            // not on the generic dial, because the belt's angle is CCW while a rotation-dial offset is CW-only.
            ConveyorGeometry.Handle conveyorHandle = HitConveyor(levelPt);
            if (conveyorHandle != ConveyorGeometry.Handle.None && SelectedObject is { } conveyorObj)
            {
                BeginDocumentEdit?.Invoke();
                _conveyorDrag = conveyorHandle;
                ApplyConveyorDrag(conveyorObj, levelPt);
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                e.Pointer.Capture(this);
                return;
            }

            // Grabbing the selected object's rotation dial (knob or ring) rotates it; takes priority over
            // object hit-testing so the dial wins over anything beneath it.
            if (HitRotationDial(levelPt) != ObjectRotation.Handle.None
                && SelectedObject is { } rotObj && EditableRotationSpec(rotObj) is { } rotSpec)
            {
                BeginDocumentEdit?.Invoke();
                _rotating = true;
                ApplyRotation(rotObj, rotSpec, levelPt, e.KeyModifiers);
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                e.Pointer.Capture(this);
                return;
            }

            int pointHit = HitPolylinePoint(levelPt);
            if (pointHit > 0 && SelectedObject is not null)
            {
                BeginDocumentEdit?.Invoke();
                _polylinePointDrag = pointHit;
                e.Handled = true;
                e.Pointer.Capture(this);
                return;
            }

            int segmentHit = HitPolylineSegment(levelPt);
            if (segmentHit >= 0 && SelectedObject is { } segmentObj)
            {
                Vec2 start = new(segmentObj.X, segmentObj.Y);
                Vec2[] points = MoverPath.CanonicalPoints(start, segmentObj.GetAttr("path"));
                (int x, int y) = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                    ? SnapAngle(points[segmentHit], levelPt)
                    : Snap(levelPt);
                BeginDocumentEdit?.Invoke();
                segmentObj.SetAttr("path", MoverPath.InsertCanonicalPoint(
                    start, segmentObj.GetAttr("path"), segmentHit, new Vec2(x, y)));
                SelectedObjectMoved?.Invoke();
                _polylinePointDrag = segmentHit + 1;
                InvalidateVisual();
                e.Handled = true;
                e.Pointer.Capture(this);
                return;
            }

            if (HitPolylineNub(levelPt) && SelectedObject is { } nubObj)
            {
                Vec2 start = new(nubObj.X, nubObj.Y);
                Vec2[] points = MoverPath.CanonicalPoints(start, nubObj.GetAttr("path"));
                (int x, int y) = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                    ? SnapAngle(points[^1], levelPt)
                    : Snap(levelPt);
                BeginDocumentEdit?.Invoke();
                nubObj.SetAttr("path", MoverPath.AppendCanonicalPoint(
                    start, nubObj.GetAttr("path"), new Vec2(x, y)));
                SelectedObjectMoved?.Invoke();
                _polylinePointDrag = MoverPath.CanonicalPoints(start, nubObj.GetAttr("path")).Length - 1;
                InvalidateVisual();
                e.Handled = true;
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

            if (_vinylHandleDrag != VinylGeometry.Handle.None && SelectedObject is { } vinylDrag)
            {
                vinylDrag.SetAttr("handleAngle", Whole(VinylGeometry.AngleFor(vinylDrag, _vinylHandleDrag, levelPt)));
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                return;
            }

            if (_resizingRadius && SelectedObject is { } g)
            {
                string? attr = g.Type == "ghost" && _ghostPreview.ShowsRadiusRing(g)
                    ? "radius"
                    : RadiusRing.Of(g)?.Attr;
                if (attr is null)
                {
                    return;
                }
                double r = GrabRadius.FromDrag(new Vec2(g.X, g.Y), levelPt);
                g.SetAttr(attr, ((int)Math.Round(r)).ToString(CultureInfo.InvariantCulture));
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

            if (_stripResizeDrag != SpikeResize.Handle.None && SelectedObject is { } stripObj)
            {
                ApplyStripResize(stripObj, levelPt);
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                return;
            }

            if (_conveyorDrag != ConveyorGeometry.Handle.None && SelectedObject is { } conveyorDragObj)
            {
                ApplyConveyorDrag(conveyorDragObj, levelPt);
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                return;
            }

            if (_rotating && SelectedObject is { } rotObj && EditableRotationSpec(rotObj) is { } rotSpec)
            {
                ApplyRotation(rotObj, rotSpec, levelPt, e.KeyModifiers);
                SelectedObjectMoved?.Invoke();
                InvalidateVisual();
                return;
            }

            if (_polylinePointDrag > 0 && SelectedObject is { } pathObj)
            {
                Vec2 start = new(pathObj.X, pathObj.Y);
                Vec2[] points = MoverPath.CanonicalPoints(start, pathObj.GetAttr("path"));
                if (_polylinePointDrag < points.Length)
                {
                    (int x, int y) = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                        ? SnapAngle(points[_polylinePointDrag - 1], levelPt)
                        : Snap(levelPt);
                    pathObj.SetAttr("path", MoverPath.MoveCanonicalPoint(
                        start, pathObj.GetAttr("path"), _polylinePointDrag, new Vec2(x, y)));
                    SelectedObjectMoved?.Invoke();
                    InvalidateVisual();
                }
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
                SpikeResize.Handle stripHandle = HitStripResize(levelPt);
                ConveyorGeometry.Handle conveyorHover = HitConveyor(levelPt);
                VinylGeometry.Handle vinylHover = HitVinylHandle(levelPt);
                SetVinylHandleHovered(vinylHover);
                int oldHoverPoint = _polylineHoverPoint;
                bool oldNubHot = _polylineNubHot;
                bool oldLimitHint = _polylineAtLimitHint;
                _polylineHoverPoint = HitPolylinePoint(levelPt);
                _polylineNubHot = HitPolylineNub(levelPt);
                _polylineAtLimitHint = HoveringPolylineLimit(levelPt);
                bool overPolylineInsert = HitPolylineSegment(levelPt) >= 0;
                if (oldHoverPoint != _polylineHoverPoint || oldNubHot != _polylineNubHot || oldLimitHint != _polylineAtLimitHint)
                {
                    InvalidateVisual();
                }
                Cursor = vinylHover != VinylGeometry.Handle.None ? new Cursor(StandardCursorType.Hand)
                    : _polylineNubHot || _polylineHoverPoint > 0 || overPolylineInsert
                    ? new Cursor(StandardCursorType.Hand)
                    : dial != ObjectRotation.Handle.None ? new Cursor(StandardCursorType.Hand)
                    : stripHandle != SpikeResize.Handle.None ? CursorForStripResize()
                    : conveyorHover != ConveyorGeometry.Handle.None ? ResizeCursor
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
            bool gestureActive = _dragging || _panning || _resizingRadius || _polylinePointDrag > 0
                || _railDrag != GrabRail.Handle.None || _stripResizeDrag != SpikeResize.Handle.None
                || _conveyorDrag != ConveyorGeometry.Handle.None
                || _vinylHandleDrag != VinylGeometry.Handle.None || _rotating || _hookHovered;
            if (!gestureActive)
            {
                return;
            }

            bool editedDocument = _dragging || _resizingRadius || _polylinePointDrag > 0
                || _railDrag != GrabRail.Handle.None || _stripResizeDrag != SpikeResize.Handle.None
                || _conveyorDrag != ConveyorGeometry.Handle.None
                || _vinylHandleDrag != VinylGeometry.Handle.None || _rotating;
            _dragging = false;
            _panning = false;
            _resizingRadius = false;
            _railDrag = GrabRail.Handle.None;
            _stripResizeDrag = SpikeResize.Handle.None;
            _conveyorDrag = ConveyorGeometry.Handle.None;
            _vinylHandleDrag = VinylGeometry.Handle.None;
            _rotating = false;
            _polylinePointDrag = -1;
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
            SetVinylHandleHovered(VinylGeometry.Handle.None); // nor the vinyl handle glow
            ResetPolylineHover();
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if ((e.Key == Key.Delete || e.Key == Key.Back) && _polylineHoverPoint > 0)
            {
                DeleteSelectedPolylineVertex(_polylineHoverPoint);
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
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

        private double StripSpriteScale(LevelObject obj)
        {
            return Document is { } doc
                && Sprites?.GetSprite(LevelSceneRenderer.CanvasSpriteKey(obj, doc.NightLevel), ActiveCandySkin, ActiveOmNomSupport) is { } sprite
                ? sprite.Scale
                : 1.0;
        }
    }
}
