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
        public static bool HitTest(LevelBounds bounds, Vec2 point, double tolerance)
        {
            Vec2 handle = HandlePosition(bounds);
            double dx = point.X - handle.X;
            double dy = point.Y - handle.Y;
            return (dx * dx) + (dy * dy) <= tolerance * tolerance;
        }

        /// <summary>Returns the pointer's horizontal offset from the handle at gesture start.</summary>
        public static double GrabOffset(LevelBounds bounds, double pointerX)
        {
            return pointerX - HandlePosition(bounds).X;
        }

        /// <summary>Converts the current pointer position to an edge while preserving its grab offset.</summary>
        public static double EdgeFromPointer(double pointerX, double grabOffset)
        {
            return pointerX - grabOffset;
        }

        /// <summary>Returns whether horizontal travel has crossed the screen-space drag threshold.</summary>
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
