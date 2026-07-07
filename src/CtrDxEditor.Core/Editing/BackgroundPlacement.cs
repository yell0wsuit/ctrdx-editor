using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Placement of a level-decoration background in level space: a single vertically-repeating p1
    /// column plus an optional secondary p2 layer.
    /// </summary>
    /// <param name="Left">Level-space X of the p1 column's left edge (also the p2 layer's left edge).</param>
    /// <param name="Width">Level-space width of the p1 column (the internal screen width).</param>
    /// <param name="TileHeight">Level-space height of one p1 tile; repeat this down from y=0.</param>
    /// <param name="P2">The secondary background's bounds, or null when the map is short or has no p2.</param>
    /// <param name="EarthCenters">
    /// Level-space centers of the earth decoration (cosmic box only), empty when there is no earth. The
    /// game draws a base earth plus a horizontal copy for maps wider than one screen and a vertical copy
    /// for maps taller than one screen (up to three), so this holds 0..3 positions.
    /// </param>
    public readonly record struct BackgroundLayout(
        double Left, double Width, double TileHeight, LevelBounds? P2, IReadOnlyList<Vec2> EarthCenters);

    /// <summary>
    /// Pure background placement math. The game (GameScene.Draw / GameScene.Init) draws the box
    /// background as a <c>TileMap</c> scaled so the p1 texture spans the internal screen width
    /// (<c>GetBackgroundWidthScale</c> = SCREEN_WIDTH / textureWidth), repeated
    /// vertically (<c>Repeat.ALL</c>) but never horizontally (<c>Repeat.NONE</c>). The map is centered
    /// in that screen (offsetX = (SCREEN_WIDTH - mapWidth) / 2), so the column is centered on the map.
    /// A secondary p2 texture is drawn once at (0, p2Y) only when the map is taller than one screen.
    /// The world is <see cref="SpritePlacement.MapScale"/> times level space, so all screen-space
    /// constants below are divided by it.
    /// </summary>
    public static class BackgroundPlacement
    {
        /// <summary>Game internal screen width (Application sets PORTRAIT_SCREEN_WIDTH to 2560).</summary>
        public const double ScreenWidth = 2560.0;

        /// <summary>Game internal screen height (Application sets PORTRAIT_SCREEN_HEIGHT to 1440).</summary>
        public const double ScreenHeight = 1440.0;

        /// <summary>The p1 column's drawn width in level space (internal screen width ÷ MapScale).</summary>
        public static double LevelScreenWidth => ScreenWidth / SpritePlacement.MapScale;

        /// <summary>
        /// Computes background placement for a level of the given size.
        /// </summary>
        /// <param name="levelWidth">Level width in level (XML) units.</param>
        /// <param name="levelHeight">Level height in level (XML) units.</param>
        /// <param name="p1Aspect">The decoded p1 bitmap's height ÷ width (aspect is preserved on decode).</param>
        /// <param name="p2Aspect">The decoded p2 bitmap's height ÷ width, or 0 when there is no p2.</param>
        /// <param name="p2Y">The pack's <c>boxBackgroundP2Y</c> (internal px), or 0 when there is no p2.</param>
        /// <param name="earthPosition">
        /// The earth decoration's center in internal pixels (the game's <c>earthBgPosition</c>, measured
        /// from the p1 column's top-left), or null when the background has no earth layer.
        /// </param>
        public static BackgroundLayout Compute(
            double levelWidth, double levelHeight, double p1Aspect,
            double p2Aspect = 0.0, int p2Y = 0, Vec2? earthPosition = null)
        {
            double width = LevelScreenWidth;
            double left = (levelWidth - width) / 2.0;
            double tileHeight = width * p1Aspect;

            LevelBounds? p2 = null;
            // The game only draws p2 when mapHeight (world) exceeds SCREEN_HEIGHT (GameScene.Draw).
            bool tall = levelHeight > ScreenHeight / SpritePlacement.MapScale;
            if (tall && p2Y > 0 && p2Aspect > 0.0)
            {
                // p2 ships the same pixel width as p1, so it spans the same screen-wide column.
                p2 = new LevelBounds(left, p2Y / SpritePlacement.MapScale, width, width * p2Aspect);
            }

            // The earth is center-anchored at earthBgPosition within the p1 column's space (GameScene
            // CreateEarthImageWithOffsetXY), so it maps to level space by ÷MapScale, offset by the column.
            // The game wraps it with the tiled background when the map exceeds one screen: an extra copy
            // one column to the right for wide maps (mapWidth > SCREEN_WIDTH) and one tile down for tall
            // maps (mapHeight > SCREEN_HEIGHT). Both offsets scale to the p1 column width / tile height.
            List<Vec2> earthCenters = [];
            if (earthPosition is { } e)
            {
                Vec2 baseEarth = new(left + (e.X / SpritePlacement.MapScale), e.Y / SpritePlacement.MapScale);
                earthCenters.Add(baseEarth);
                if (levelWidth > width)
                {
                    earthCenters.Add(new Vec2(baseEarth.X + width, baseEarth.Y));
                }
                if (tall)
                {
                    earthCenters.Add(new Vec2(baseEarth.X, baseEarth.Y + tileHeight));
                }
            }

            return new BackgroundLayout(left, width, tileHeight, p2, earthCenters);
        }
    }
}
