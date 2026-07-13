using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>Role of one node in the game's static conveyor visual composition.</summary>
    public enum ConveyorVisualPieceKind
    {
        /// <summary>Scaled background filling the transporter body.</summary>
        Middle,
        /// <summary>One of the two end caps.</summary>
        End,
        /// <summary>One of the two longitudinal side rails.</summary>
        Side,
        /// <summary>One of the four end/side corners.</summary>
        Corner,
        /// <summary>Tiled moving plate surface.</summary>
        PlateSurface,
        /// <summary>Direction arrow repeated as a child of each plate tile.</summary>
        Arrow,
        /// <summary>One of the two end highlights.</summary>
        Highlight,
    }

    /// <summary>One game-authored conveyor node in unrotated visual-root coordinates.</summary>
    /// <param name="Kind">The node's visual role.</param>
    /// <param name="Quad">Index in <c>obj_conveyor</c>.</param>
    /// <param name="Bounds">Destination bounds in level units before the visual root's 90-degree rotation.</param>
    /// <param name="FlipX">Whether the game mirrors the node horizontally.</param>
    /// <param name="FlipY">Whether the game mirrors the node vertically.</param>
    /// <param name="OffsetY">Authored Y offset in level units, useful for preserving cap placement.</param>
    /// <param name="Direction">Arrow direction (-1, 0, or 1); zero for non-arrow nodes.</param>
    public readonly record struct ConveyorVisualPiece(
        ConveyorVisualPieceKind Kind,
        int Quad,
        LevelBounds Bounds,
        bool FlipX = false,
        bool FlipY = false,
        double OffsetY = 0,
        int Direction = 0);

    /// <summary>
    /// Pure static port of <c>ConveyorBelt.BuildVisuals</c>. Coordinates are retained in the game's
    /// width-by-length visual root; the renderer applies its authored 90-degree rotation afterwards.
    /// </summary>
    public sealed record ConveyorVisualLayout(
        double RootWidth,
        double RootHeight,
        double RootTranslationX,
        double RootTranslationY,
        double ParentRotationPivotX,
        IReadOnlyList<ConveyorVisualPiece> Pieces)
    {
        private const double EndScale = 0.6;
        private const double PlateScale = 0.8;
        private const double CapOffset = 18;

        /// <summary>
        /// Returns the visible union after the game's 90-degree visual-root rotation, in belt-local
        /// level coordinates. Used to crop the complete palette thumbnail without clipping its frame.
        /// </summary>
        /// <returns>Axis-aligned bounds containing every visible transporter piece.</returns>
        public LevelBounds BeltLocalBounds()
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (ConveyorVisualPiece piece in Pieces)
            {
                // Arrow is a child contained by PlateSurface, so it does not expand the visible union.
                if (piece.Kind == ConveyorVisualPieceKind.Arrow)
                {
                    continue;
                }

                // root rotation: (u,v) -> (RootTranslationX-v, RootTranslationY+u).
                double left = RootTranslationX - (piece.Bounds.Y + piece.Bounds.H);
                double right = RootTranslationX - piece.Bounds.Y;
                double top = RootTranslationY + piece.Bounds.X;
                double bottom = RootTranslationY + piece.Bounds.X + piece.Bounds.W;
                minX = Math.Min(minX, left);
                minY = Math.Min(minY, top);
                maxX = Math.Max(maxX, right);
                maxY = Math.Max(maxY, bottom);
            }

            return minX == double.MaxValue
                ? new LevelBounds(0, 0, 0, 0)
                : new LevelBounds(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>Builds the complete static transporter composition in editor level units.</summary>
        /// <param name="length">Transporter length from XML.</param>
        /// <param name="width">Transporter thickness from XML.</param>
        /// <param name="arrowSign">Automatic direction (-1 or 1), or zero for a manual belt.</param>
        /// <returns>The game-authored visual nodes and their local layout.</returns>
        public static ConveyorVisualLayout Build(double length, double width, int arrowSign)
        {
            double mapScale = SpritePlacement.MapScale;
            double scaledWidth = width * mapScale;
            double scaledLength = length * mapScale;
            double rootW = Math.Ceiling(scaledWidth);
            double rootH = Math.Ceiling(scaledLength);
            List<ConveyorVisualPiece> pieces = [];

            // Atlas frame sizes are the Image width/height values used by the game (trim is not restored).
            pieces.Add(Anchored(ConveyorVisualPieceKind.Middle, 2, 144, 83, 18, 18,
                rootW, rootH, scaleX: (rootW - 10) / 144, scaleY: rootH / 83));

            pieces.Add(Anchored(ConveyorVisualPieceKind.End, 0, 144, 79, 34, 34,
                rootW, rootH, y: CapOffset, scaleX: rootW * EndScale / 144, offsetY: CapOffset));
            pieces.Add(Anchored(ConveyorVisualPieceKind.End, 0, 144, 79, 10, 10,
                rootW, rootH, y: -CapOffset, scaleX: rootW * EndScale / 144, offsetY: -CapOffset));

            double sideScaleY = (rootH - (2 * CapOffset)) / 83;
            pieces.Add(Anchored(ConveyorVisualPieceKind.Side, 3, 67, 83, 17, 17,
                rootW, rootH, scaleX: -1, scaleY: sideScaleY));
            pieces.Add(Anchored(ConveyorVisualPieceKind.Side, 3, 67, 83, 20, 20,
                rootW, rootH, scaleY: sideScaleY));

            pieces.Add(Anchored(ConveyorVisualPieceKind.Corner, 1, 67, 79, 36, 36,
                rootW, rootH, y: CapOffset, offsetY: CapOffset));
            pieces.Add(Anchored(ConveyorVisualPieceKind.Corner, 1, 67, 79, 33, 33,
                rootW, rootH, y: CapOffset, scaleX: -1, offsetY: CapOffset));
            pieces.Add(Anchored(ConveyorVisualPieceKind.Corner, 1, 67, 79, 9, 9,
                rootW, rootH, y: -CapOffset, scaleX: -1, scaleY: -1, offsetY: -CapOffset));
            pieces.Add(Anchored(ConveyorVisualPieceKind.Corner, 1, 67, 79, 12, 12,
                rootW, rootH, y: -CapOffset, scaleY: -1, offsetY: -CapOffset));

            // beltVisual is separately ceiled and center-anchored. Its integer half-width cancels between
            // its own anchor and plateSection's parent anchor, leaving the plate scaled around rootW >> 1.
            double plateWidth = rootW * PlateScale;
            double plateScaleX = plateWidth / 235;
            double platePivotX = Math.Floor(rootW / 2);
            LevelBounds plateBounds = new((platePivotX - (117 * plateScaleX)) / mapScale, 0,
                plateWidth / mapScale, rootH / mapScale);
            pieces.Add(new ConveyorVisualPiece(ConveyorVisualPieceKind.PlateSurface, 4, plateBounds));
            if (arrowSign != 0)
            {
                pieces.Add(new ConveyorVisualPiece(ConveyorVisualPieceKind.Arrow, 5, plateBounds,
                    Direction: Math.Sign(arrowSign)));
            }

            pieces.Add(Anchored(ConveyorVisualPieceKind.Highlight, 6, 235, 24, 34, 34,
                rootW, rootH, scaleX: rootW * PlateScale / 235));
            pieces.Add(Anchored(ConveyorVisualPieceKind.Highlight, 6, 235, 24, 10, 10,
                rootW, rootH, scaleX: rootW * PlateScale / 235, scaleY: -1));

            double halfRootW = Math.Floor(rootW / 2);
            double halfRootH = Math.Floor(rootH / 2);
            return new ConveyorVisualLayout(
                rootW / mapScale,
                rootH / mapScale,
                (halfRootH + halfRootH) / mapScale,
                -halfRootW / mapScale,
                (halfRootH - (scaledLength / 2)) / mapScale,
                pieces);
        }

        private static ConveyorVisualPiece Anchored(
            ConveyorVisualPieceKind kind,
            int quad,
            double frameW,
            double frameH,
            int anchor,
            int parentAnchor,
            double parentW,
            double parentH,
            double x = 0,
            double y = 0,
            double scaleX = 1,
            double scaleY = 1,
            double offsetY = 0)
        {
            double left = ParentAxis(parentAnchor, parentW, horizontal: true) + x
                - AnchorOffset(anchor, frameW, horizontal: true);
            double top = ParentAxis(parentAnchor, parentH, horizontal: false) + y
                - AnchorOffset(anchor, frameH, horizontal: false);
            double pivotX = left + Math.Floor(frameW / 2);
            double pivotY = top + Math.Floor(frameH / 2);
            double x1 = pivotX + (scaleX * (left - pivotX));
            double x2 = pivotX + (scaleX * (left + frameW - pivotX));
            double y1 = pivotY + (scaleY * (top - pivotY));
            double y2 = pivotY + (scaleY * (top + frameH - pivotY));
            double scaledW = frameW * Math.Abs(scaleX);
            double scaledH = frameH * Math.Abs(scaleY);
            double s = SpritePlacement.MapScale;
            return new ConveyorVisualPiece(
                kind,
                quad,
                new LevelBounds(Math.Min(x1, x2) / s, Math.Min(y1, y2) / s, scaledW / s, scaledH / s),
                scaleX < 0,
                scaleY < 0,
                offsetY / s);
        }

        private static double ParentAxis(int flags, double size, bool horizontal)
        {
            int start = horizontal ? 1 : 8;
            int center = horizontal ? 2 : 16;
            return (flags & start) != 0 ? 0 : (flags & center) != 0 ? Math.Floor(size / 2) : size;
        }

        private static double AnchorOffset(int flags, double size, bool horizontal)
        {
            int start = horizontal ? 1 : 8;
            int center = horizontal ? 2 : 16;
            return (flags & start) != 0 ? 0 : (flags & center) != 0 ? Math.Floor(size / 2) : size;
        }
    }
}
