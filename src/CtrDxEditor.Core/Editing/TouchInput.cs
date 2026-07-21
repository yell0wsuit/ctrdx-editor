using System;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Pointer-type-dependent input thresholds. A fingertip is far less precise than a cursor and covers
    /// what it is aiming at, so touch needs larger hit targets and a movement threshold before a press
    /// becomes a drag.
    /// </summary>
    public static class TouchInput
    {
        /// <summary>
        /// Multiplier applied to screen-space hit tolerances for touch, sized so the common 9px handle
        /// tolerance yields a target around the 44pt platform touch guideline.
        /// </summary>
        public const double ToleranceScale = 2.5;

        /// <summary>
        /// Screen pixels a touch must travel before a press counts as a drag. Fingers wobble on contact;
        /// without this every tap-to-select nudges the object a pixel and marks the document modified.
        /// </summary>
        public const double DragSlopPx = 10;

        /// <summary>Scales a screen-space hit tolerance for the pointer doing the hitting.</summary>
        /// <param name="basePx">The mouse tolerance in screen pixels.</param>
        /// <param name="isTouch">Whether the pointer is a touch contact.</param>
        /// <returns>The tolerance to use, unchanged for mouse and scaled up for touch.</returns>
        public static double Tolerance(double basePx, bool isTouch)
        {
            return isTouch ? basePx * ToleranceScale : basePx;
        }

        /// <summary>Whether a press has moved far enough to be treated as a drag rather than a tap.</summary>
        /// <param name="startPx">Screen-space position where the press began.</param>
        /// <param name="currentPx">Current screen-space position.</param>
        /// <param name="isTouch">Whether the pointer is a touch contact.</param>
        /// <returns>
        /// For touch, true once movement exceeds <see cref="DragSlopPx"/>. For mouse, true on any movement,
        /// preserving the existing pixel-precise desktop drag.
        /// </returns>
        public static bool ExceedsDragSlop(Vec2 startPx, Vec2 currentPx, bool isTouch)
        {
            double dx = currentPx.X - startPx.X;
            double dy = currentPx.Y - startPx.Y;
            return !isTouch
                ? Math.Abs(dx) > double.Epsilon || Math.Abs(dy) > double.Epsilon
                : (dx * dx) + (dy * dy) > DragSlopPx * DragSlopPx;
        }
    }
}
