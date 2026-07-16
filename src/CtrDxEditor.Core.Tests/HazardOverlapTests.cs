using System.Collections.Generic;
using System.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the candy-starts-inside-a-breaking-hazard geometric check.</summary>
    public class HazardOverlapTests
    {
        // spike1 desktop band = 212x10 world, +15 tolerance => 242x40 world, /3 => level half-extents
        // X in [-40.33, 40.33], Y in [-6.67, 6.67] about the spike center.
        private static LevelDocument Doc(string objects, string flags = "")
        {
            return LevelDocument.Parse($"""
            <map>
                <layer name="settings">
                    <map gridSize="32" width="320" height="480" />
                    <gameDesign {flags} />
                </layer>
                <layer name="Objects">{objects}</layer>
            </map>
            """);
        }

        /// <summary>A candy sitting on a spike's center is inside the band and gets flagged.</summary>
        [Fact]
        public void CandyCenteredOnSpikeIsFlagged()
        {
            LevelDocument doc = Doc("<candy x=\"0\" y=\"0\" /><spike1 x=\"0\" y=\"0\" />");
            _ = Assert.Single(HazardOverlap.CandiesInHazards(doc));
        }

        /// <summary>A candy past the band's half-height clears the spike and is not flagged.</summary>
        [Fact]
        public void CandyOutsideBandIsNotFlagged()
        {
            // y=8 is beyond the band's 6.67 half-height, so no overlap.
            LevelDocument doc = Doc("<candy x=\"0\" y=\"8\" /><spike1 x=\"0\" y=\"0\" />");
            Assert.Empty(HazardOverlap.CandiesInHazards(doc));
        }

        /// <summary>Rotating the spike's band 90 deg brings a candy the axis-aligned band missed into range.</summary>
        [Fact]
        public void RotatedSpikeCatchesCandyTheAxisAlignedBandMisses()
        {
            // Candy at (0,8): outside the unrotated band, inside once the band is rotated 90 deg.
            LevelDocument doc = Doc("<candy x=\"0\" y=\"8\" /><spike1 x=\"0\" y=\"0\" angle=\"90\" />");
            _ = Assert.Single(HazardOverlap.CandiesInHazards(doc));
        }

        /// <summary>Rotating the spike's band 90 deg moves a candy the axis-aligned band caught out of range.</summary>
        [Fact]
        public void RotatedSpikeMissesCandyTheAxisAlignedBandWouldCatch()
        {
            // Candy at (40,0): inside the unrotated band, outside once rotated 90 deg.
            LevelDocument doc = Doc("<candy x=\"40\" y=\"0\" /><spike1 x=\"0\" y=\"0\" angle=\"90\" />");
            Assert.Empty(HazardOverlap.CandiesInHazards(doc));
        }

        /// <summary>Electro is a breaking hazard, so a candy on it is flagged like a spike.</summary>
        [Fact]
        public void ElectroCountsAsBreakingHazard()
        {
            LevelDocument doc = Doc("<candy x=\"0\" y=\"0\" /><electro x=\"0\" y=\"0\" />");
            _ = Assert.Single(HazardOverlap.CandiesInHazards(doc));
        }

        /// <summary>Bouncer and bubble do not break candy, so overlapping them is not flagged.</summary>
        [Fact]
        public void BouncerAndBubbleAreNotBreakingHazards()
        {
            LevelDocument doc = Doc("<candy x=\"0\" y=\"0\" /><bouncer1 x=\"0\" y=\"0\" /><bubble x=\"0\" y=\"0\" />");
            Assert.Empty(HazardOverlap.CandiesInHazards(doc));
        }

        /// <summary>Split-candy halves (candyL/candyR) are checked, and only the overlapping half is returned.</summary>
        [Fact]
        public void SplitCandyHalvesAreChecked()
        {
            LevelDocument doc = Doc(
                "<candyL x=\"0\" y=\"0\" /><candyR x=\"200\" y=\"200\" /><spike1 x=\"0\" y=\"0\" />",
                "twoParts=\"true\"");
            IReadOnlyList<LevelObject> hit = HazardOverlap.CandiesInHazards(doc);
            _ = Assert.Single(hit);
            Assert.Equal("candyL", hit[0].Type);
        }

        /// <summary>The mobile physics model resolves its own band, catching a candy the desktop band would miss.</summary>
        [Fact]
        public void MobilePhysicsModelResolves()
        {
            // Mobile spike1 band = 204x30 world, +15 => 234x60, /3 => Y half-extent 10. Candy at y=8 is inside.
            LevelDocument doc = Doc("<candy x=\"0\" y=\"8\" /><spike1 x=\"0\" y=\"0\" />", "useMobilePhysics=\"true\"");
            _ = Assert.Single(HazardOverlap.CandiesInHazards(doc));
        }
    }
}
