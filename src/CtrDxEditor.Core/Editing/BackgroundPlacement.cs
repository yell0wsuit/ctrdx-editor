using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Geometry;

namespace CtrDxEditor.Core.Editing
{
    /// <summary>
    /// Placement of a level-decoration background in level space: an infinite p1 tile grid plus the
    /// secondary p2 layers and earth decorations drawn over it.
    /// </summary>
    /// <param name="Left">Level-space X of the p1 grid's origin column; tiles repeat both ways from here.</param>
    /// <param name="Top">Level-space Y of the p1 grid's origin row; tiles repeat both ways from here.</param>
    /// <param name="Width">Level-space width of one p1 tile.</param>
    /// <param name="TileHeight">Level-space height of one p1 tile.</param>
    /// <param name="Scale">
    /// The game's background cover scale (internal pixels drawn per texture pixel), needed to place and
    /// size the overlays the game draws inside the background's own scaled matrix.
    /// </param>
    /// <param name="P2">
    /// The secondary background's bounds, one per seam between p1 sections, empty when the map is short
    /// or the background has no p2.
    /// </param>
    /// <param name="EarthCenters">
    /// Level-space centers of the earth decoration (cosmic box only), empty when there is no earth. The
    /// game draws a base earth plus a horizontal copy for maps wider than one screen and a vertical copy
    /// for maps taller than one screen (up to three), so this holds 0..3 positions.
    /// </param>
    public readonly record struct BackgroundLayout(
        double Left, double Top, double Width, double TileHeight, double Scale,
        IReadOnlyList<LevelBounds> P2, IReadOnlyList<Vec2> EarthCenters);

    /// <summary>
    /// Pure background placement math. The game (GameScene.Draw / GameScene.Init / GameScene.cs) draws the
    /// box background as a <c>TileMap</c> repeating on <em>both</em> axes (<c>Repeat.ALL</c> horizontally and
    /// vertically), scaled by <c>GetBackgroundCoverScale</c> - a cover fit of the p1 texture against the
    /// internal screen, so the axis with room to spare crops. The grid is anchored on the design screen's
    /// centre (<c>UpdateBackgroundScale</c> sets <c>back.x = SCREEN_WIDTH / 2 / scale - texWidth / 2</c> and
    /// the same for y), and the map is itself centered in that screen (offsetX = (SCREEN_WIDTH - mapWidth) / 2),
    /// so the grid comes out centered on the map horizontally.
    /// A p2 texture is drawn once per seam between p1 sections (<c>BackgroundTiling.GetP2Count</c> /
    /// <c>ResolveP2Y</c>), and the earth decorations sit at <c>back.x/back.y</c> plus their authored offset -
    /// all inside the background's scaled matrix, hence the <see cref="BackgroundLayout.Scale"/> factor.
    /// The world is <see cref="SpritePlacement.MapScale"/> times level space, so all screen-space
    /// constants below are divided by it.
    /// </summary>
    public static class BackgroundPlacement
    {
        /// <summary>Game internal screen width (Application sets PORTRAIT_SCREEN_WIDTH to 2560).</summary>
        public const double ScreenWidth = 2560.0;

        /// <summary>Game internal screen height (Application sets PORTRAIT_SCREEN_HEIGHT to 1440).</summary>
        public const double ScreenHeight = 1440.0;

        /// <summary>The internal screen width in level space (the p1 tile width for 16:9 art).</summary>
        public static double LevelScreenWidth => ScreenWidth / SpritePlacement.MapScale;

        /// <summary>The internal screen height in level space (one p1 section of the map).</summary>
        public static double LevelScreenHeight => ScreenHeight / SpritePlacement.MapScale;

        /// <summary>
        /// The game's slack when counting p1 sections, so a map that is an exact multiple of one screen
        /// is not credited with a further section (<c>BackgroundTiling.Epsilon</c>).
        /// </summary>
        private const double SectionEpsilon = 0.001;

        /// <summary>
        /// Computes background placement for a level of the given size.
        /// </summary>
        /// <param name="levelWidth">Level width in level (XML) units.</param>
        /// <param name="levelHeight">Level height in level (XML) units.</param>
        /// <param name="p1Aspect">The decoded p1 bitmap's height ÷ width (aspect is preserved on decode).</param>
        /// <param name="p1TextureWidth">
        /// The p1 art's authored pixel width, which the cover scale is measured against. Defaults to the
        /// design width, where the scale is the identity.
        /// </param>
        /// <param name="p2Aspect">The decoded p2 bitmap's height ÷ width, or 0 when there is no p2.</param>
        /// <param name="p2Y">The pack's <c>boxBackgroundP2Y</c> (p1 texture px), or 0 when there is no p2.</param>
        /// <param name="earthPosition">
        /// The earth decoration's center in p1 texture pixels (the game's <c>earthBgPosition</c>, measured
        /// from the p1 texture's top-left), or null when the background has no earth layer.
        /// </param>
        public static BackgroundLayout Compute(
            double levelWidth, double levelHeight, double p1Aspect, double p1TextureWidth = ScreenWidth,
            double p2Aspect = 0.0, int p2Y = 0, Vec2? earthPosition = null)
        {
            // GetBackgroundCoverScale takes the larger of the two fits, so the drawn tile is at least a
            // screen on both axes. Expressed against the texture's aspect alone, its drawn width is
            // whichever of the screen width and the width its screen-tall form would have is larger.
            double drawnWidth = p1Aspect > 0.0
                ? Math.Max(ScreenWidth, ScreenHeight / p1Aspect)
                : ScreenWidth;
            double width = drawnWidth / SpritePlacement.MapScale;
            double tileHeight = width * p1Aspect;

            // The scale the game applies to everything drawn inside the background's matrix: the p2 offset,
            // and the earth positions and art. Those are all authored in the p1 texture's own pixels, which
            // the cover fit spreads across drawnWidth internal ones.
            double scale = p1TextureWidth > 0.0 ? drawnWidth / p1TextureWidth : 1.0;

            // The grid is anchored on the design screen's centre. Horizontally that lands on the map's
            // centre once the map's own centring offset is taken back out; vertically it is a straight
            // centring of one tile on one screen, which is exactly 0 for art the screen's own shape.
            double left = (levelWidth - width) / 2.0;
            double top = (LevelScreenHeight - tileHeight) / 2.0;

            List<LevelBounds> p2 = [];
            // One p2 per seam between p1 sections (BackgroundTiling.GetP2Count / ResolveP2Y). p2 ships the
            // same pixel width as p1, so it spans one tile and only its height follows its own aspect.
            int p2Count = SeamCount(levelHeight);
            if (p2Y > 0 && p2Aspect > 0.0)
            {
                double firstY = top + (p2Y * scale / SpritePlacement.MapScale);
                for (int seam = 0; seam < p2Count; seam++)
                {
                    p2.Add(new LevelBounds(left, firstY + (seam * tileHeight), width, width * p2Aspect));
                }
            }

            // The earth is center-anchored at earthBgPosition within the p1 texture's space (GameScene
            // CreateEarthImageWithOffsetXY + GravityState.RelayoutEarthAnimations), so it maps to level
            // space by ×Scale ÷MapScale, offset by the grid origin. The game wraps it with the tiled
            // background when the map exceeds one screen: an extra copy one tile to the right for wide
            // maps (mapWidth > SCREEN_WIDTH) and one tile down for tall maps (mapHeight > SCREEN_HEIGHT).
            // Only ever one copy per axis, however far the map runs on - the p1 grid repeats, the earth
            // does not (GameScene.LoadMetadata's map case).
            List<Vec2> earthCenters = [];
            if (earthPosition is { } e)
            {
                Vec2 baseEarth = new(
                    left + (e.X * scale / SpritePlacement.MapScale),
                    top + (e.Y * scale / SpritePlacement.MapScale));
                earthCenters.Add(baseEarth);
                if (levelWidth > LevelScreenWidth)
                {
                    earthCenters.Add(new Vec2(baseEarth.X + width, baseEarth.Y));
                }
                if (levelHeight > LevelScreenHeight)
                {
                    earthCenters.Add(new Vec2(baseEarth.X, baseEarth.Y + tileHeight));
                }
            }

            return new BackgroundLayout(left, top, width, tileHeight, scale, p2, earthCenters);
        }

        /// <summary>
        /// The first grid line at or before the level's own edge, from which tiles can be laid down to the
        /// far edge to cover it.
        /// </summary>
        /// <remarks>
        /// The p1 grid is anchored on the design screen rather than on the level, and repeats both ways
        /// from there (<c>Repeat.ALL</c>), so the tile covering the level's top-left corner generally
        /// starts outside the level. Walking forward from the anchor alone would leave that corner bare.
        /// </remarks>
        /// <param name="origin">The grid's anchor on this axis, in level units.</param>
        /// <param name="tileSize">The tile's extent on this axis, in level units.</param>
        /// <returns>The anchor shifted back by whole tiles until it is at or before zero.</returns>
        public static double GridStart(double origin, double tileSize)
        {
            return tileSize > 0.0
                ? origin - (Math.Ceiling(origin / tileSize) * tileSize)
                : origin;
        }

        /// <summary>
        /// The number of seams between the p1 sections a map of this height uses, which is the number of
        /// p2 overlays the game draws (<c>BackgroundTiling.GetP2Count</c>).
        /// </summary>
        /// <param name="levelHeight">Level height in level (XML) units.</param>
        /// <returns>One fewer than the map's p1 section count, and never negative.</returns>
        public static int SeamCount(double levelHeight)
        {
            int sections = Math.Max(
                1,
                (int)Math.Ceiling((levelHeight / LevelScreenHeight) - SectionEpsilon));
            return Math.Max(0, sections - 1);
        }
    }
}
