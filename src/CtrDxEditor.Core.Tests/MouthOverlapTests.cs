using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the candy-starts-on-Om-Nom's-mouth geometric check.</summary>
    public class MouthOverlapTests
    {
        // Desktop mouth band (target: -56,30,108,2 world / mapScale 3) about the target center:
        // x in [-18.67, 17.33], y in [10, 10.67].
        // Desktop candy band (candy: -54,-52,112,104 world / 3) about the candy center:
        // x in [-18, 19.33], y in [-17.33, 17.33].
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

        /// <summary>A candy centered on the target overlaps the mouth line and gets flagged.</summary>
        [Fact]
        public void CandyCenteredOnTargetIsFlagged()
        {
            LevelDocument doc = Doc("<candy x=\"0\" y=\"0\" /><target x=\"0\" y=\"0\" />");
            _ = Assert.Single(MouthOverlap.CandiesOnMouth(doc));
        }

        /// <summary>A candy sitting well above the mouth line clears it and is not flagged.</summary>
        [Fact]
        public void CandyAboveMouthIsNotFlagged()
        {
            // Candy bottom edge (y-17.33 .. y+17.33) must stay above the mouth top (y+10).
            // At y=-30 the candy band is y in [-47.33, -12.67], clear of the mouth at [10, 10.67].
            LevelDocument doc = Doc("<candy x=\"0\" y=\"-30\" /><target x=\"0\" y=\"0\" />");
            Assert.Empty(MouthOverlap.CandiesOnMouth(doc));
        }

        /// <summary>A candy horizontally clear of the narrow mouth band is not flagged.</summary>
        [Fact]
        public void CandyBesideMouthIsNotFlagged()
        {
            // Candy at x=60: band x in [42, 79.33], mouth x in [-18.67, 17.33]. No x overlap.
            LevelDocument doc = Doc("<candy x=\"60\" y=\"10\" /><target x=\"0\" y=\"0\" />");
            Assert.Empty(MouthOverlap.CandiesOnMouth(doc));
        }

        /// <summary>Two-part halves are checked too: a candyL on the mouth is flagged.</summary>
        [Fact]
        public void CandyLeftOnMouthIsFlagged()
        {
            LevelDocument doc = Doc(
                "<candyL x=\"0\" y=\"0\" /><target x=\"0\" y=\"0\" />",
                flags: "twoParts=\"1\"");
            _ = Assert.Single(MouthOverlap.CandiesOnMouth(doc));
        }

        /// <summary>Every Om Nom is checked: a candy on a second target is flagged.</summary>
        [Fact]
        public void CandyOnSecondTargetIsFlagged()
        {
            LevelDocument doc = Doc(
                "<candy x=\"200\" y=\"200\" /><target x=\"0\" y=\"0\" /><target x=\"200\" y=\"200\" />");
            _ = Assert.Single(MouthOverlap.CandiesOnMouth(doc));
        }

        /// <summary>A level with no target produces no mouth-overlap flags.</summary>
        [Fact]
        public void NoTargetProducesNoFlags()
        {
            LevelDocument doc = Doc("<candy x=\"0\" y=\"0\" />");
            Assert.Empty(MouthOverlap.CandiesOnMouth(doc));
        }
    }
}
