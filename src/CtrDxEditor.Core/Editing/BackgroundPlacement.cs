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
    public readonly record struct BackgroundLayout(double Left, double Width, double TileHeight, LevelBounds? P2);

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
        public static BackgroundLayout Compute(
            double levelWidth, double levelHeight, double p1Aspect,
            double p2Aspect = 0.0, int p2Y = 0)
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

            return new BackgroundLayout(left, width, tileHeight, p2);
        }
    }
}
