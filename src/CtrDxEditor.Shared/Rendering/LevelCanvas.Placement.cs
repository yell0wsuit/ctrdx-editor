using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;

using CtrDxEditor.Content;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Rendering
{
    /// <summary>Palette placement: drag-ghost preview and dropping / adding objects.</summary>
    public sealed partial class LevelCanvas
    {
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
