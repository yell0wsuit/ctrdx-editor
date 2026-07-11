using System;

using Avalonia;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Palette placement: drag preview and dropping / adding objects.</summary>
    public sealed partial class LevelCanvas
    {
        /// <summary>Shows a translucent preview of <paramref name="element"/> at the snapped drop position.</summary>
        /// <param name="element">The palette element id to preview.</param>
        /// <param name="screenPoint">The pointer position in screen pixels.</param>
        public void ShowGhost(string element, Point screenPoint)
        {
            Vec2 levelPt = View.ScreenToLevel(new Vec2(screenPoint.X, screenPoint.Y));
            (int gx, int gy) = Snap(levelPt);
            _dragPreviewElement = element;
            _dragPreviewLevel = new Vec2(gx, gy);
            _dragPreviewActive = true;
            InvalidateVisual();
        }

        /// <summary>Clears the drag preview, if any.</summary>
        public void HideGhost()
        {
            if (_dragPreviewActive)
            {
                _dragPreviewActive = false;
                _dragPreviewElement = null;
                InvalidateVisual();
            }
        }

        /// <summary>Adds an object at the level's center (for single-click placement from the palette).</summary>
        /// <param name="element">The palette element id to add.</param>
        /// <returns>True when an object was placed.</returns>
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
        /// <param name="element">The palette element id to place.</param>
        /// <param name="screenPoint">The drop position in screen pixels.</param>
        /// <returns>True when an object was placed.</returns>
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

        /// <summary>Rounds a level-space point to whole units, snapping to the grid when snapping is enabled.</summary>
        /// <param name="levelPt">The point to snap, in level coordinates.</param>
        /// <returns>The snapped integer level coordinates.</returns>
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
