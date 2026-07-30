using System.Collections.Generic;

using Avalonia;
using Avalonia.Media;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>
    /// Maps live drag state to the badge that reports it. A pull model: this reads the drag flags at render
    /// time rather than having each drag branch push a readout, which keeps <c>LevelCanvas.Input.cs</c>
    /// untouched and the mapping in one readable place.
    /// </summary>
    public sealed partial class LevelCanvas
    {
        /// <summary>
        /// Draws the readout badge for whatever drag is in flight. Called last in the overlay pass so no
        /// handle paints over it. Draws nothing when no drag is active, when the press has not yet cleared
        /// the tap threshold, or when the resolver finds nothing to report.
        /// </summary>
        /// <param name="context">Target drawing context.</param>
        /// <param name="v">Current level-to-screen transform.</param>
        private void DrawDragReadout(DrawingContext context, ViewTransform v)
        {
            if (!AnyDragActive || !_readoutArmed)
            {
                return;
            }

            (DragKind kind, int index, Vec2 point) = CurrentDrag();
            if (kind == DragKind.None)
            {
                return;
            }

            LevelObject? obj = kind == DragKind.Water ? null : PrimaryObject;
            IReadOnlyList<DragReadout.Entry> entries = DragReadout.For(kind, obj, index, point);
            if (entries.Count == 0)
            {
                return;
            }

            BadgeRenderer.DrawReadout(context, entries, ReadoutAnchor(v, kind, obj), Bounds.Size);
        }

        /// <summary>
        /// Which drag is in flight, plus the extra state its readout needs: the 1-based hand segment for
        /// <see cref="DragKind.HandJoint"/>, and the level-space value for the two kinds whose number is
        /// not stored on the object (<see cref="DragKind.PolylinePoint"/> and <see cref="DragKind.Water"/>).
        /// </summary>
        /// <remarks>
        /// Ordered to match <c>OnPointerMoved</c>'s branches, so the badge always reports the drag that is
        /// actually being applied when two flags could overlap.
        /// </remarks>
        private (DragKind Kind, int Index, Vec2 Point) CurrentDrag()
        {
            if (_handJointDrag > 0)
            {
                return (DragKind.HandJoint, _handJointDrag, default);
            }

            if (_handBaseDrag)
            {
                return (DragKind.HandBase, 0, default);
            }

            if (_resizingTutorialText)
            {
                return (DragKind.TutorialWidth, 0, default);
            }

            if (_vinylHandleDrag != VinylGeometry.Handle.None)
            {
                return (DragKind.VinylAngle, 0, default);
            }

            if (_resizingRadius)
            {
                return (DragKind.Radius, 0, default);
            }

            if (_railDrag == GrabRail.Handle.SlideHook)
            {
                return (DragKind.RailOffset, 0, default);
            }

            if (_railDrag != GrabRail.Handle.None)
            {
                return (DragKind.RailResize, 0, default);
            }

            if (_ropeDrag != RopeLength.Handle.None)
            {
                return (DragKind.RopeLength, 0, default);
            }

            if (_stripResizeDrag != SpikeResize.Handle.None)
            {
                return (DragKind.StripSize, 0, default);
            }

            if (_conveyorDrag == ConveyorGeometry.Handle.FarEnd)
            {
                return (DragKind.ConveyorLength, 0, default);
            }

            if (_conveyorDrag != ConveyorGeometry.Handle.None)
            {
                return (DragKind.ConveyorWidth, 0, default);
            }

            if (_rotating)
            {
                return (DragKind.Rotate, _handActiveSegment, default);
            }

            if (_polylinePointDrag > 0)
            {
                return (DragKind.PolylinePoint, _polylinePointDrag, PolylineDragPoint());
            }

            if (_waterDrag && Document is { } doc)
            {
                return (DragKind.Water, 0, new Vec2(0, doc.Water));
            }

            return _dragging ? (DragKind.Move, 0, default) : (DragKind.None, 0, default);
        }

        /// <summary>The dragged polyline vertex in level space, or the origin when it cannot be resolved.</summary>
        private Vec2 PolylineDragPoint()
        {
            if (PrimaryObject is not { } obj || EditablePath.For(obj) is not { } path)
            {
                return default;
            }

            Vec2[] points = path.Points;
            return _polylinePointDrag < points.Length ? points[_polylinePointDrag] : default;
        }

        /// <summary>
        /// Where the badge sits: the dragged handle's own screen point when there is one, so the number
        /// tracks the thing under the cursor; otherwise the top edge of the selection outline.
        /// </summary>
        /// <param name="v">Current level-to-screen transform.</param>
        /// <param name="kind">The drag in flight.</param>
        /// <param name="obj">The dragged object, or null for water.</param>
        /// <returns>The screen point the badge sits above.</returns>
        private Point ReadoutAnchor(ViewTransform v, DragKind kind, LevelObject? obj)
        {
            if (kind == DragKind.Water)
            {
                // The water attribute is a depth measured up from the level's bottom edge, not a Y
                // coordinate, so the surface has to come from the shared band geometry — the same source
                // HitsWaterHandle uses. Deriving it here is how the badge ended up inverted.
                if (Document is not { } waterDoc
                    || WaterGeometry.Band(waterDoc.Width, waterDoc.Height, waterDoc.Water) is not { } band)
                {
                    return default;
                }

                // The waterline spans the level, so there is no "dragged point" to track horizontally.
                // Centering on the viewport keeps the badge stable instead of skating with the cursor.
                Vec2 surface = v.LevelToScreen(new Vec2(band.X, band.Y));
                return new Point(Bounds.Width / 2, surface.Y);
            }

            if (kind == DragKind.RopeLength && SelectedRopeGeometry() is { } rope)
            {
                Vec2 knob = v.LevelToScreen(rope.Knob);
                return new Point(knob.X, knob.Y);
            }

            if (kind == DragKind.PolylinePoint)
            {
                Vec2 vertex = v.LevelToScreen(PolylineDragPoint());
                return new Point(vertex.X, vertex.Y);
            }

            if (obj is null)
            {
                return default;
            }

            if (kind is DragKind.RailOffset or DragKind.RailResize
                && GrabRail.Of(obj) is { } rail)
            {
                Vec2 handle = _railDrag switch
                {
                    GrabRail.Handle.SlideHook => rail.Hook,
                    GrabRail.Handle.ResizeStart => rail.Start,
                    GrabRail.Handle.ResizeEnd => rail.End,
                    GrabRail.Handle.None or GrabRail.Handle.MoveBar => rail.Hook,
                    _ => rail.Hook,
                };
                Vec2 screen = v.LevelToScreen(handle);
                return new Point(screen.X, screen.Y);
            }

            if (kind == DragKind.Rotate && EditableRotationTarget(obj) is { } target)
            {
                double radius = v.Zoom > 0 ? RotationDialRenderer.RadiusPx / v.Zoom : 0;
                Vec2 knob = ObjectRotation.KnobPosition(
                    target.Center, target.StoredAngle, target.Spec, radius);
                Vec2 screen = v.LevelToScreen(knob);
                return new Point(screen.X, screen.Y);
            }

            // Everything else anchors to the top of the selection outline: the handle sits on or inside
            // the object's bounds, so the outline's top edge clears both.
            Vec2 center = v.LevelToScreen(new Vec2(obj.X, obj.Y));
            return SelectionTop(v, obj) ?? new Point(center.X, center.Y);
        }

        /// <summary>
        /// The top-center of an object's selection outline in screen space, or null when its bounds cannot
        /// be measured (no sprite cache yet).
        /// </summary>
        private Point? SelectionTop(ViewTransform v, LevelObject obj)
        {
            if (Document is not { } doc || Sprites is not { } sprites)
            {
                return null;
            }

            LevelBounds bounds = LevelSceneRenderer.SelectionBounds(
                sprites, obj, ActiveCandySkin, ActiveOmNomSupport, doc.NightLevel);
            Point[] outline = LevelSceneRenderer.SelectionOutlinePointsWithPreview(
                v, obj, bounds, PreviewSpinDegrees(obj), PreviewAnimationSeconds(obj));

            if (outline.Length == 0)
            {
                return null;
            }

            double top = outline[0].Y;
            double left = outline[0].X;
            double right = outline[0].X;
            foreach (Point p in outline)
            {
                top = System.Math.Min(top, p.Y);
                left = System.Math.Min(left, p.X);
                right = System.Math.Max(right, p.X);
            }

            return new Point((left + right) / 2, top);
        }
    }
}
