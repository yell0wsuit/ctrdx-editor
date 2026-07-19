using System;
using System.Collections.Generic;

using CtrDxEditor.Core.Editing;
using CtrDxEditor.Core.Geometry;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the water band's placement, drain, and game-accurate tile cropping.</summary>
    public class WaterGeometryTests
    {
        /// <summary>Water at or below zero means the level has no water band at all.</summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(-5.0)]
        public void NoBandWhenWaterNotPositive(double water)
        {
            Assert.Null(WaterGeometry.Band(640, 480, water));
        }

        /// <summary>A level at least as wide as the screen column gets a band spanning its own width.</summary>
        [Fact]
        public void WideLevelBandSpansPlayfield()
        {
            LevelBounds band = WaterGeometry.Band(1000, 480, 120)
                ?? throw new Xunit.Sdk.XunitException("Expected a water band.");

            Assert.Equal(0.0, band.X, 3);
            Assert.Equal(1000.0, band.W, 3);
        }

        /// <summary>A level narrower than the screen column widens to that column, centered, overhanging both edges.</summary>
        [Fact]
        public void NarrowLevelBandWidensToCenteredScreenColumn()
        {
            LevelBounds band = WaterGeometry.Band(640, 480, 120)
                ?? throw new Xunit.Sdk.XunitException("Expected a water band.");

            Assert.Equal(BackgroundPlacement.LevelScreenWidth, band.W, 3);
            Assert.Equal((640 - BackgroundPlacement.LevelScreenWidth) / 2.0, band.X, 3);
            Assert.True(band.X < 0, "a narrow level's band overhangs the playfield");
        }

        /// <summary>Exactly at the screen-column width, the playfield branch applies and the band does not overhang.</summary>
        [Fact]
        public void BandAtScreenColumnWidthDoesNotOverhang()
        {
            int width = (int)Math.Ceiling(BackgroundPlacement.LevelScreenWidth);
            LevelBounds band = WaterGeometry.Band(width, 480, 120)
                ?? throw new Xunit.Sdk.XunitException("Expected a water band.");

            Assert.Equal(0.0, band.X, 3);
            Assert.Equal(width, band.W, 3);
        }

        /// <summary>The band is pinned to the bottom of the map and is exactly as tall as the water value.</summary>
        [Fact]
        public void BandIsPinnedToMapBottom()
        {
            LevelBounds band = WaterGeometry.Band(1000, 480, 120)
                ?? throw new Xunit.Sdk.XunitException("Expected a water band.");

            Assert.Equal(360.0, band.Y, 3);
            Assert.Equal(120.0, band.H, 3);
        }

        /// <summary>waterSpeed drains the water: the level falls, it never rises.</summary>
        [Fact]
        public void DrainLowersWaterOverTime()
        {
            Assert.Equal(180.0, WaterGeometry.DrainedWater(240, 12, 5), 3);
        }

        /// <summary>Draining clamps at empty rather than going negative.</summary>
        [Fact]
        public void DrainClampsAtZero()
        {
            Assert.Equal(0.0, WaterGeometry.DrainedWater(240, 12, 999), 3);
        }

        /// <summary>A non-positive waterSpeed is a static pool, matching the game's `waterSpeed > 0` guard.</summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(-3.0)]
        public void NonPositiveSpeedHoldsWaterStatic(double speed)
        {
            Assert.Equal(240.0, WaterGeometry.DrainedWater(240, speed, 50), 3);
        }

        /// <summary>An exact multiple of the tile size produces whole tiles with no cropping.</summary>
        [Fact]
        public void ExactMultipleTilesAreUncropped()
        {
            // A 30x30 atlas quad is 10x10 level units at MapScale 3.
            IntRect quad = new(100, 200, 30, 30);
            IReadOnlyList<WaterTile> tiles = WaterGeometry.Tiles(quad, new LevelBounds(0, 0, 20, 10));

            Assert.Equal(2, tiles.Count);
            Assert.All(tiles, t => Assert.Equal(30.0, t.Source.W, 3));
            Assert.All(tiles, t => Assert.Equal(10.0, t.Dest.W, 3));
            Assert.Equal(0.0, tiles[0].Dest.X, 3);
            Assert.Equal(10.0, tiles[1].Dest.X, 3);
        }

        /// <summary>The final column is cropped from the tile's left edge, never scaled down.</summary>
        [Fact]
        public void FinalColumnIsCroppedNotScaled()
        {
            IntRect quad = new(100, 200, 30, 30);
            IReadOnlyList<WaterTile> tiles = WaterGeometry.Tiles(quad, new LevelBounds(0, 0, 14, 10));

            Assert.Equal(2, tiles.Count);
            // Second tile covers only the remaining 4 level units = 12 atlas px, sampled from the quad's left.
            Assert.Equal(4.0, tiles[1].Dest.W, 3);
            Assert.Equal(12.0, tiles[1].Source.W, 3);
            Assert.Equal(100.0, tiles[1].Source.X, 3);
        }

        /// <summary>The final row is cropped from the tile's top edge, matching the column behavior.</summary>
        [Fact]
        public void FinalRowIsCroppedNotScaled()
        {
            IntRect quad = new(100, 200, 30, 30);
            IReadOnlyList<WaterTile> tiles = WaterGeometry.Tiles(quad, new LevelBounds(0, 0, 10, 13));

            Assert.Equal(2, tiles.Count);
            Assert.Equal(3.0, tiles[1].Dest.H, 3);
            Assert.Equal(9.0, tiles[1].Source.H, 3);
            Assert.Equal(200.0, tiles[1].Source.Y, 3);
        }

        /// <summary>Tiles are emitted in the game's row-major order so overlap resolves identically.</summary>
        [Fact]
        public void TilesAreRowMajor()
        {
            IntRect quad = new(0, 0, 30, 30);
            IReadOnlyList<WaterTile> tiles = WaterGeometry.Tiles(quad, new LevelBounds(0, 0, 20, 20));

            Assert.Equal(4, tiles.Count);
            Assert.Equal(new LevelBounds(0, 0, 10, 10), tiles[0].Dest);
            Assert.Equal(new LevelBounds(10, 0, 10, 10), tiles[1].Dest);
            Assert.Equal(new LevelBounds(0, 10, 10, 10), tiles[2].Dest);
            Assert.Equal(new LevelBounds(10, 10, 10, 10), tiles[3].Dest);
        }

        /// <summary>A degenerate destination or quad yields no tiles rather than dividing by zero.</summary>
        [Fact]
        public void DegenerateInputYieldsNoTiles()
        {
            Assert.Empty(WaterGeometry.Tiles(new IntRect(0, 0, 30, 30), new LevelBounds(0, 0, 0, 10)));
            Assert.Empty(WaterGeometry.Tiles(new IntRect(0, 0, 0, 30), new LevelBounds(0, 0, 10, 10)));
        }
    }
}
