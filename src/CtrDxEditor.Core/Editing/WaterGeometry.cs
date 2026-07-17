using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>A sub-rect of an atlas image, in texture pixels.</summary>
    /// <param name="X">The left edge in texture pixels, measured from the atlas's left.</param>
    /// <param name="Y">The top edge in texture pixels, measured from the atlas's top.</param>
    /// <param name="W">The width in texture pixels, extending right from <paramref name="X"/>.</param>
    /// <param name="H">The height in texture pixels, extending down from <paramref name="Y"/>.</param>
    public readonly record struct AtlasRect(double X, double Y, double W, double H);

    /// <summary>One tile of a tiled quad: the atlas pixels to sample and where they land in level space.</summary>
    public readonly record struct WaterTile(AtlasRect Source, LevelBounds Dest);

    /// <summary>
    /// Placement and tiling for the level's water band, matching the game exactly.
    /// <para>
    /// Water is not an object: the game reads <c>water</c> and <c>waterSpeed</c> off <c>&lt;gameDesign&gt;</c>
    /// (<c>GameScene.LoadMetadata.cs</c>) and builds one bottom-pinned band per level.
    /// </para>
    /// </summary>
    public static class WaterGeometry
    {
        /// <summary>
        /// The water band in level space, or null when the level has no water.
        /// <para>
        /// The game uses the map width, or snaps to <c>x = 0, width = SCREEN_WIDTH</c> when the map is
        /// narrower than the screen (<c>GameScene.LoadMetadata.cs:84-88</c>). Since the screen column is
        /// only ever wider than the playfield in the branch that selects it, both collapse to a single
        /// max(). The band is pinned to the map's bottom edge.
        /// </para>
        /// </summary>
        /// <param name="levelWidth">Level width in level (XML) units.</param>
        /// <param name="levelHeight">Level height in level (XML) units.</param>
        /// <param name="water">The <c>water</c> attribute, in level units.</param>
        public static LevelBounds? Band(int levelWidth, int levelHeight, double water)
        {
            if (water <= 0.0)
            {
                return null;
            }

            double width = Math.Max(levelWidth, BackgroundPlacement.LevelScreenWidth);
            return new LevelBounds((levelWidth - width) / 2.0, levelHeight - water, width, water);
        }

        /// <summary>
        /// The water height after draining for <paramref name="elapsedSeconds"/>.
        /// <para>
        /// <c>GameScene.Update.cs:1520-1524</c> moves the level toward <c>-SCREEN_HEIGHT</c> at
        /// <c>waterSpeed</c>, and <c>Mover.MoveVariableToTarget</c> walks a variable toward its target — so
        /// a positive speed <em>drains</em> the water. It never rises. Only engages when speed is positive.
        /// </para>
        /// </summary>
        public static double DrainedWater(double water, double waterSpeed, double elapsedSeconds)
        {
            return waterSpeed > 0.0 ? Math.Max(0.0, water - (waterSpeed * elapsedSeconds)) : water;
        }

        /// <summary>
        /// The tiles filling <paramref name="dest"/> with <paramref name="quad"/>, matching the game's
        /// <c>DrawHelper.DrawImageTiled</c>: row-major from the origin, with the last row and column
        /// <em>cropped</em> from the tile's top-left rather than scaled.
        /// </summary>
        /// <param name="quad">The atlas frame rect to tile, in texture pixels.</param>
        /// <param name="dest">The region to fill, in level units.</param>
        /// <param name="mapScale">Level units to game pixels.</param>
        public static IReadOnlyList<WaterTile> Tiles(
            IntRect quad, LevelBounds dest, double mapScale = SpritePlacement.MapScale)
        {
            List<WaterTile> tiles = [];
            double tileW = quad.W / mapScale;
            double tileH = quad.H / mapScale;
            if (tileW <= 0.0 || tileH <= 0.0 || dest.W <= 0.0 || dest.H <= 0.0)
            {
                return tiles;
            }

            int columns = (int)Math.Ceiling(dest.W / tileW);
            int rows = (int)Math.Ceiling(dest.H / tileH);
            for (int row = 0; row < rows; row++)
            {
                double offsetY = row * tileH;
                double drawH = Math.Min(tileH, dest.H - offsetY);
                for (int column = 0; column < columns; column++)
                {
                    double offsetX = column * tileW;
                    double drawW = Math.Min(tileW, dest.W - offsetX);
                    tiles.Add(new WaterTile(
                        new AtlasRect(quad.X, quad.Y, drawW * mapScale, drawH * mapScale),
                        new LevelBounds(dest.X + offsetX, dest.Y + offsetY, drawW, drawH)));
                }
            }

            return tiles;
        }
    }
}
