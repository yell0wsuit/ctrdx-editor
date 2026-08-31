using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests for placing the level-decoration background the way the game does: the p1 texture is given a
    /// cover fit against the internal screen (2560x1440, i.e. 2560/3 x 1440/3 in level space) and repeated
    /// on both axes from a grid anchored on that screen's centre; a p2 layer dresses each seam between p1
    /// sections; and the p2 band and the cosmic box's earth ride that repeat rather than being anchored
    /// to one section.
    /// </summary>
    public class BackgroundPlacementTests
    {
        // A 16:9 p1 background (2560x1440 -> aspect 0.5625) fills one screen exactly, at scale 1.
        private const double P1Aspect16By9 = 9.0 / 16.0;

        // bgr_11 is authored at 2048x1153: a quarter under the design width, so its cover scale is 1.25.
        private const double Bgr11Width = 2048.0;
        private const double Bgr11Aspect = 1153.0 / 2048.0;

        /// <summary>A design-width p1 tile is the internal screen width in level units, centered on the map.</summary>
        [Fact]
        public void P1TileIsScreenWidthCenteredOnMap()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9);

            double screenW = 2560.0 / 3.0;
            Assert.Equal(screenW, layout.Width, precision: 9);
            Assert.Equal((320 - screenW) / 2.0, layout.Left, precision: 9);
        }

        /// <summary>A 16:9 p1 tile is exactly one screen (480 level units) tall, and needs no vertical offset.</summary>
        [Fact]
        public void P1TileHeightPreservesAspect()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9);

            Assert.Equal(480, layout.TileHeight, precision: 9);
            Assert.Equal(0, layout.Top, precision: 9);
            Assert.Equal(1.0, layout.Scale, precision: 9);
        }

        /// <summary>
        /// Art authored below the design width still draws one screen wide - the cover fit scales it up -
        /// and reports that scale for the overlays drawn in the background's matrix.
        /// </summary>
        [Fact]
        public void UnderWidthArtIsCoverScaledToTheScreen()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: Bgr11Aspect, p1TextureWidth: Bgr11Width);

            Assert.Equal(2560.0 / 3.0, layout.Width, precision: 9);
            Assert.Equal(1.25, layout.Scale, precision: 9);
            Assert.Equal(1153.0 * 1.25 / 3.0, layout.TileHeight, precision: 9);
        }

        /// <summary>
        /// The tile grid is centered on the design screen vertically, so art slightly off 16:9 starts just
        /// above the map's top edge rather than flush with it.
        /// </summary>
        [Fact]
        public void GridIsCenteredOnTheDesignScreenVertically()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: Bgr11Aspect, p1TextureWidth: Bgr11Width);

            Assert.Equal((480.0 - layout.TileHeight) / 2.0, layout.Top, precision: 9);
            Assert.True(layout.Top < 0);
        }

        /// <summary>Single-screen maps (height 480 -> internal 1440) have no seam, so no p2.</summary>
        [Fact]
        public void NoP2ForSingleScreenMaps()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9,
                p2Aspect: 884.0 / 2559.0, p2Y: 1120);

            Assert.Empty(layout.P2);
        }

        /// <summary>Tall maps draw p2 at the seam, full tile width, aspect-preserved.</summary>
        [Fact]
        public void P2ForTallMapsPlacedAtP2Y()
        {
            double p2Aspect = 884.0 / 2559.0;
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 960, p1Aspect: P1Aspect16By9,
                p2Aspect: p2Aspect, p2Y: 1120);

            LevelBounds p2 = Assert.Single(layout.P2);
            double screenW = 2560.0 / 3.0;
            Assert.Equal(layout.Left, p2.X, precision: 9);
            Assert.Equal(1120.0 / 3.0, p2.Y, precision: 9);
            Assert.Equal(screenW, p2.W, precision: 9);
            Assert.Equal(screenW * p2Aspect, p2.H, precision: 9);
        }

        /// <summary>
        /// p2Y is authored in p1 texture pixels, which the cover scale stretches along with the art: on
        /// bgr_11 that is the difference between sitting on the seam and a sixth of a screen above it.
        /// </summary>
        [Fact]
        public void P2YTakesTheCoverScale()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 960, p1Aspect: Bgr11Aspect, p1TextureWidth: Bgr11Width,
                p2Aspect: 980.0 / 2048.0, p2Y: 802);

            LevelBounds p2 = Assert.Single(layout.P2);
            Assert.Equal(layout.Top + (802.0 * 1.25 / 3.0), p2.Y, precision: 9);
        }

        /// <summary>
        /// A map spanning three p1 sections has two seams, so p2 is drawn twice, one p1 tile apart
        /// (BackgroundTiling.GetP2Count / ResolveP2Y).
        /// </summary>
        [Fact]
        public void P2RepeatsAtEverySeam()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 1440, p1Aspect: P1Aspect16By9,
                p2Aspect: 884.0 / 2559.0, p2Y: 1120);

            Assert.Equal(2, layout.P2.Count);
            Assert.Equal(layout.P2[0].Y + layout.TileHeight, layout.P2[1].Y, precision: 9);
        }

        /// <summary>
        /// The grid starts at or before the level's left edge, so a level narrower than one tile keeps the
        /// single overhanging tile it has always had.
        /// </summary>
        [Fact]
        public void NarrowLevelStartsAtTheOverhangingTile()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9);

            Assert.Equal(layout.Left, BackgroundPlacement.GridStart(layout.Left, layout.Width), precision: 9);
            Assert.True(layout.Left < 0);
        }

        /// <summary>
        /// A level wider than one tile is backed the whole way across: the grid steps back a tile so the
        /// left edge is covered, and enough tiles follow to reach the right one.
        /// </summary>
        [Fact]
        public void WideLevelIsTiledAcross()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 1600, levelHeight: 480, p1Aspect: P1Aspect16By9);

            double start = BackgroundPlacement.GridStart(layout.Left, layout.Width);
            Assert.Equal(layout.Left - layout.Width, start, precision: 9);
            Assert.True(start <= 0);
            Assert.True(start + layout.Width > 0);

            int tiles = 0;
            for (double tx = start; tx < 1600; tx += layout.Width)
            {
                tiles++;
            }
            Assert.Equal(3, tiles);
            Assert.True(start + (tiles * layout.Width) >= 1600);
        }

        /// <summary>A grid already flush with the level's edge is left where it is.</summary>
        [Fact]
        public void FlushGridIsNotShiftedBack()
        {
            Assert.Equal(0, BackgroundPlacement.GridStart(0, 480), precision: 9);
        }

        /// <summary>
        /// The section count carries the game's epsilon, so a map that is an exact multiple of one screen
        /// is not credited with a further section: 960 spans two sections and one seam, not three and two.
        /// </summary>
        [Theory]
        [InlineData(480, 0)]
        [InlineData(700, 1)]
        [InlineData(960, 1)]
        [InlineData(1440, 2)]
        [InlineData(1441, 3)]
        public void SeamCountMatchesTheGamesSectionCount(double levelHeight, int expected)
        {
            Assert.Equal(expected, BackgroundPlacement.SeamCount(levelHeight));
        }

        /// <summary>No earth layer by default; the cosmic box supplies its position explicitly.</summary>
        [Fact]
        public void NoEarthWithoutPosition()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9);

            Assert.Null(layout.EarthOffset);
        }

        /// <summary>
        /// The earth is placed within its p1 tile, not within the map: the offset is earthBgPosition
        /// scaled into level units, which the renderer adds to each tile's own corner.
        /// </summary>
        [Fact]
        public void EarthIsOffsetWithinItsTile()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9,
                earthPosition: new Vec2(1284, 724));

            Vec2 earth = Assert.NotNull(layout.EarthOffset);
            Assert.Equal(1284.0 / 3.0, earth.X, precision: 9);
            Assert.Equal(724.0 / 3.0, earth.Y, precision: 9);
        }

        /// <summary>The earth's authored position is in texture pixels too, so it takes the cover scale.</summary>
        [Fact]
        public void EarthOffsetTakesTheCoverScale()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: Bgr11Aspect, p1TextureWidth: Bgr11Width,
                earthPosition: new Vec2(1284, 724));

            Vec2 earth = Assert.NotNull(layout.EarthOffset);
            Assert.Equal(1284.0 * 1.25 / 3.0, earth.X, precision: 9);
            Assert.Equal(724.0 * 1.25 / 3.0, earth.Y, precision: 9);
        }

        /// <summary>
        /// The offset does not depend on the map: a wide or tall level repeats the same tile-relative
        /// earth over its extra sections rather than being given a different placement.
        /// </summary>
        [Fact]
        public void EarthOffsetIsIndependentOfMapSize()
        {
            BackgroundLayout small = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9,
                earthPosition: new Vec2(1284, 724));
            BackgroundLayout large = BackgroundPlacement.Compute(
                levelWidth: 4000, levelHeight: 4000, p1Aspect: P1Aspect16By9,
                earthPosition: new Vec2(1284, 724));

            Assert.Equal(small.EarthOffset, large.EarthOffset);
        }

        /// <summary>A missing p2 (no p2Y) yields no p2 even for tall maps.</summary>
        [Fact]
        public void NoP2WhenP2YIsZero()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 960, p1Aspect: P1Aspect16By9,
                p2Aspect: 0, p2Y: 0);

            Assert.Empty(layout.P2);
        }
    }
}
