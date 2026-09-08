using System.Collections.Generic;

using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests which art a magic hat draws for its group, against DX's SockArt.</summary>
    public class SockArtTests
    {
        /// <summary>The shipped art bakes a band into two frames; everything past them generates one.</summary>
        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(50, true)]
        [InlineData(-1, false)]
        public void OnlyGroupsPastTheAuthoredArtWearAGeneratedBand(int group, bool expected)
        {
            Assert.Equal(expected, SockArt.WearsGeneratedBand(group));
        }

        /// <summary>
        /// The base frame alternates, so a generated-band group draws frame 0 or 1 rather than always
        /// the "grouped" one. This is the distinction the editor used to get wrong for group 2.
        /// </summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 0)]
        [InlineData(3, 1)]
        [InlineData(4, 0)]
        [InlineData(-3, 0)]
        public void PatternAlternatesBetweenTheTwoAuthoredFrames(int group, int expected)
        {
            Assert.Equal(expected, SockArt.PatternFor(group));
        }

        /// <summary>Groups below the authored count address their own color, not a generated slot.</summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(-9, 0)]
        public void AuthoredGroupsMapToThemselves(int group, int expected)
        {
            Assert.Equal(expected, SockArt.BandSlot(group));
        }

        /// <summary>
        /// Generated slots run out and repeat rather than reaching for a color a player would have to
        /// compare side by side, so group 6 wears group 2's band.
        /// </summary>
        [Theory]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 4)]
        [InlineData(5, 5)]
        [InlineData(6, 2)]
        [InlineData(9, 5)]
        [InlineData(1000, 4)]
        public void GeneratedSlotsWrapOnceTheColorsRunOut(int group, int expected)
        {
            Assert.Equal(expected, SockArt.BandSlot(group));
        }

        /// <summary>
        /// A slot fixes the base frame as well as the color, which is what lets one sprite key per slot
        /// cover every group that maps to it. Only holds while the generated count is a whole number of
        /// patterns; a fifth generated color would break it, and this is what would say so.
        /// </summary>
        [Fact]
        public void ASlotDeterminesTheBaseFrameAsWellAsTheColor()
        {
            for (int group = SockArt.AuthoredCount; group < SockArt.AuthoredCount + (SockArt.GeneratedCount * 4); group++)
            {
                Assert.Equal(SockArt.PatternFor(SockArt.BandSlot(group)), SockArt.PatternFor(group));
            }
        }

        /// <summary>Every generated slot wears a different color, or the pairs would be unreadable.</summary>
        [Fact]
        public void EveryGeneratedSlotHasItsOwnColor()
        {
            HashSet<RopeRgba> colors = [];

            for (int group = 0; group < SockArt.AuthoredCount + SockArt.GeneratedCount; group++)
            {
                Assert.True(colors.Add(SockBandPalette.ColorForGroup(group)), $"group {group} repeats a color");
            }
        }

        /// <summary>The palette wraps with the slots, so a hat's color always matches its pair's.</summary>
        [Theory]
        [InlineData(2, 6)]
        [InlineData(3, 7)]
        [InlineData(5, 9)]
        public void ColorsWrapWithTheSlots(int group, int wrapped)
        {
            Assert.Equal(SockBandPalette.ColorForGroup(group), SockBandPalette.ColorForGroup(wrapped));
        }

        /// <summary>
        /// The two authored colors are what the shipped frames already show. The editor never tints with
        /// them, but a palette that misreported them would be measuring the generated colors' separation
        /// against colors nobody sees.
        /// </summary>
        [Fact]
        public void TheAuthoredGroupsReportTheColorsTheArtAlreadyShows()
        {
            AssertColor(255, 48, 20, SockBandPalette.ColorForGroup(0));
            AssertColor(135, 255, 0, SockBandPalette.ColorForGroup(1));
        }

        /// <summary>
        /// The generated colors as DX's own SockBandPalette produces them at
        /// <see cref="SockBandPalette.Seed"/>. Pinned because nothing here re-derives them: if the game's
        /// generator moves, this is the only thing that will notice.
        /// </summary>
        [Theory]
        [InlineData(2, 39, 252, 254)]
        [InlineData(3, 219, 6, 234)]
        [InlineData(4, 226, 179, 57)]
        [InlineData(5, 40, 153, 87)]
        public void GeneratedColorsMatchTheGameAtTheFallbackSeed(int group, int r, int g, int b)
        {
            AssertColor(r, g, b, SockBandPalette.ColorForGroup(group));
        }

        /// <summary>Bands are fully opaque; a translucent tint would wash the mask into the hat body.</summary>
        [Fact]
        public void EveryBandIsFullyOpaque()
        {
            for (int group = 0; group < SockArt.AuthoredCount + SockArt.GeneratedCount; group++)
            {
                Assert.Equal(1.0, SockBandPalette.ColorForGroup(group).A);
            }
        }

        private static void AssertColor(int r, int g, int b, RopeRgba actual)
        {
            Assert.Equal(r, (int)System.Math.Round(actual.R * 255.0));
            Assert.Equal(g, (int)System.Math.Round(actual.G * 255.0));
            Assert.Equal(b, (int)System.Math.Round(actual.B * 255.0));
        }
    }
}
