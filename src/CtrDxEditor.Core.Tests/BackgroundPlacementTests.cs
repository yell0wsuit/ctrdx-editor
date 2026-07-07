using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>
    /// Tests for placing the level-decoration background the way the game does: the p1 texture is
    /// scaled to the internal screen width (2560, i.e. 2560/3 in level space), centered on the map,
    /// and repeated vertically; a p2 layer is drawn once for maps taller than one screen.
    /// </summary>
    public class BackgroundPlacementTests
    {
        // A 16:9 p1 background (2560x1440 -> aspect 0.5625) fills one screen height exactly.
        private const double P1Aspect16By9 = 9.0 / 16.0;

        /// <summary>The p1 column is the internal screen width in level units, centered on the map.</summary>
        [Fact]
        public void P1ColumnIsScreenWidthCenteredOnMap()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9);

            double screenW = 2560.0 / 3.0;
            Assert.Equal(screenW, layout.Width, precision: 9);
            Assert.Equal((320 - screenW) / 2.0, layout.Left, precision: 9);
        }

        /// <summary>A 16:9 p1 tile is exactly one screen (480 level units) tall.</summary>
        [Fact]
        public void P1TileHeightPreservesAspect()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9);

            Assert.Equal(480, layout.TileHeight, precision: 9);
        }

        /// <summary>Single-screen maps (height 480 -> internal 1440, not &gt; SCREEN_HEIGHT) get no p2.</summary>
        [Fact]
        public void NoP2ForSingleScreenMaps()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9,
                p2Aspect: 884.0 / 2559.0, p2Y: 1120);

            Assert.Null(layout.P2);
        }

        /// <summary>Tall maps draw p2 once, full screen width, at p2Y/MapScale, aspect-preserved.</summary>
        [Fact]
        public void P2ForTallMapsPlacedAtP2Y()
        {
            double p2Aspect = 884.0 / 2559.0;
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 960, p1Aspect: P1Aspect16By9,
                p2Aspect: p2Aspect, p2Y: 1120);

            LevelBounds p2 = Assert.NotNull(layout.P2);
            double screenW = 2560.0 / 3.0;
            Assert.Equal(layout.Left, p2.X, precision: 9);
            Assert.Equal(1120.0 / 3.0, p2.Y, precision: 9);
            Assert.Equal(screenW, p2.W, precision: 9);
            Assert.Equal(screenW * p2Aspect, p2.H, precision: 9);
        }

        /// <summary>No earth layer by default; the cosmic box supplies its position explicitly.</summary>
        [Fact]
        public void NoEarthWithoutPosition()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9);

            Assert.Empty(layout.EarthCenters);
        }

        /// <summary>A single-screen cosmic box draws one earth at earthBgPosition ÷ MapScale, column-offset.</summary>
        [Fact]
        public void EarthCenteredAtScaledPosition()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 480, p1Aspect: P1Aspect16By9,
                earthPosition: new Vec2(1284, 724));

            Vec2 earth = Assert.Single(layout.EarthCenters);
            Assert.Equal(layout.Left + (1284.0 / 3.0), earth.X, precision: 9);
            Assert.Equal(724.0 / 3.0, earth.Y, precision: 9);
        }

        /// <summary>Maps wider than one screen (mapWidth &gt; 2560) get a second earth one column right.</summary>
        [Fact]
        public void WideMapDuplicatesEarthHorizontally()
        {
            // levelWidth 1000 -> mapWidth 3000 > 2560, so a horizontal copy is added; height stays 1 screen.
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 1000, levelHeight: 480, p1Aspect: P1Aspect16By9,
                earthPosition: new Vec2(1284, 724));

            Assert.Equal(2, layout.EarthCenters.Count);
            Vec2 baseEarth = layout.EarthCenters[0];
            Vec2 copy = layout.EarthCenters[1];
            Assert.Equal(baseEarth.X + layout.Width, copy.X, precision: 9);
            Assert.Equal(baseEarth.Y, copy.Y, precision: 9);
        }

        /// <summary>Maps taller than one screen (mapHeight &gt; 1440) get a second earth one tile down.</summary>
        [Fact]
        public void TallMapDuplicatesEarthVertically()
        {
            // levelHeight 960 -> mapHeight 2880 > 1440, so a vertical copy is added; width stays 1 screen.
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 960, p1Aspect: P1Aspect16By9,
                earthPosition: new Vec2(1284, 724));

            Assert.Equal(2, layout.EarthCenters.Count);
            Vec2 baseEarth = layout.EarthCenters[0];
            Vec2 copy = layout.EarthCenters[1];
            Assert.Equal(baseEarth.X, copy.X, precision: 9);
            Assert.Equal(baseEarth.Y + layout.TileHeight, copy.Y, precision: 9);
        }

        /// <summary>A map larger than one screen in both axes gets all three earths (base + two copies).</summary>
        [Fact]
        public void LargeMapDrawsThreeEarths()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 1000, levelHeight: 960, p1Aspect: P1Aspect16By9,
                earthPosition: new Vec2(1284, 724));

            Assert.Equal(3, layout.EarthCenters.Count);
        }

        /// <summary>A missing p2 (no p2Y) yields no p2 even for tall maps.</summary>
        [Fact]
        public void NoP2WhenP2YIsZero()
        {
            BackgroundLayout layout = BackgroundPlacement.Compute(
                levelWidth: 320, levelHeight: 960, p1Aspect: P1Aspect16By9,
                p2Aspect: 0, p2Y: 0);

            Assert.Null(layout.P2);
        }
    }
}
