using System;

using Avalonia.Input;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Canvas handles specific to a selected grab: its movable rail and its rope.</summary>
    public sealed partial class LevelCanvas
    {
        /// <summary>What part of the selected movable grab's rail a level point is over, or <see cref="GrabRail.Handle.None"/>.</summary>
        /// <remarks>
        /// The hit-testing itself lives in <see cref="GrabRail"/>; here we only supply the selected grab's geometry and
        /// the screen-derived tolerances: ~9 px for the end caps, the hook's own footprint, and the bar's half thickness.
        /// </remarks>
        /// <param name="levelPt">The point to test, in level coordinates.</param>
        /// <returns>The rail handle under the point, or <see cref="GrabRail.Handle.None"/>.</returns>
        private GrabRail.Handle HitRail(Vec2 levelPt)
        {
            return IsSingleSelection
                && SelectedObject is { Type: "grab" } sel
                && View.Zoom > 0
                && GrabRenderer.DrawsMovableRail(sel)
                && GrabRail.Of(sel) is { } g
                ? GrabRail.HitTest(g, levelPt, endTolerance: HitTolerance(9), hookTolerance: 24, barThickness: 20)
                : GrabRail.Handle.None;
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
    }
}
