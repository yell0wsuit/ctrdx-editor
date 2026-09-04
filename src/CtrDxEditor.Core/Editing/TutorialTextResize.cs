using System;
using System.Globalization;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Right-edge canvas resize geometry for tutorial text wrap width.</summary>
    public static class TutorialTextResize
    {
        /// <summary>Smallest game wrap width the canvas handle can author.</summary>
        public const int MinimumWidth = 16;

        /// <summary>Screen-space travel required before a handle press becomes a resize.</summary>
        public const double DragThreshold = 2;

        /// <summary>Returns the right-edge midpoint used for the resize knob.</summary>
        public static Vec2 HandlePosition(LevelBounds bounds)
        {
            return new Vec2(bounds.X + bounds.W, bounds.Y + (bounds.H / 2));
        }

        /// <summary>Returns whether a level-space point lies within the resize knob tolerance.</summary>
        /// <param name="bounds">The text object's wrap box, in level units.</param>
        /// <param name="point">The position to test, in level units.</param>
        /// <param name="tolerance">The hit radius in level units.</param>
        public static bool HitTest(LevelBounds bounds, Vec2 point, double tolerance)
        {
            Vec2 handle = HandlePosition(bounds);
            double dx = point.X - handle.X;
            double dy = point.Y - handle.Y;
            return (dx * dx) + (dy * dy) <= tolerance * tolerance;
        }

        /// <summary>Returns the pointer's horizontal offset from the handle at gesture start.</summary>
        /// <param name="bounds">The text object's wrap box, in level units.</param>
        /// <param name="pointerX">The pointer's level-space X at gesture start.</param>
        /// <returns>The offset to feed back to <see cref="EdgeFromPointer"/> so the edge does not jump on the first move.</returns>
        public static double GrabOffset(LevelBounds bounds, double pointerX)
        {
            return pointerX - HandlePosition(bounds).X;
        }

        /// <summary>Converts the current pointer position to an edge while preserving its grab offset.</summary>
        /// <param name="pointerX">The pointer's current level-space X.</param>
        /// <param name="grabOffset">The offset captured by <see cref="GrabOffset"/> at gesture start.</param>
        /// <returns>The new right edge, in level units.</returns>
        public static double EdgeFromPointer(double pointerX, double grabOffset)
        {
            return pointerX - grabOffset;
        }

        /// <summary>Returns whether horizontal travel has crossed the screen-space drag threshold.</summary>
        /// <param name="startPointerX">The pointer's level-space X at gesture start.</param>
        /// <param name="pointerX">The pointer's current level-space X.</param>
        /// <param name="zoom">Screen pixels per level unit; the threshold is screen-space, so it holds at any zoom.</param>
        public static bool HasDragged(double startPointerX, double pointerX, double zoom)
        {
            return Math.Abs(pointerX - startPointerX) * zoom >= DragThreshold;
        }

        /// <summary>Returns whether this move should resize, latching once the threshold was crossed.</summary>
        public static bool ShouldApplyDrag(
            bool hasDragged,
            double startPointerX,
            double pointerX,
            double zoom)
        {
            return hasDragged || HasDragged(startPointerX, pointerX, zoom);
        }

        /// <summary>Switches to manual width and sizes the wrap box from its fixed left edge.</summary>
        /// <param name="text">The text object to resize. Non-text objects are ignored.</param>
        /// <param name="pointerX">The pointer's level-space X, which sets the right edge; the left edge stays fixed.</param>
        public static void ApplyDrag(LevelObject text, double pointerX)
        {
            if (!TutorialObject.IsText(text.Type))
            {
                return;
            }

            int width = Math.Max(MinimumWidth, (int)Math.Round(pointerX - text.X));
            TutorialObject.SetAutoWidth(text, false);
            text.SetAttr("width", width.ToString(CultureInfo.InvariantCulture));
        }
    }
}
