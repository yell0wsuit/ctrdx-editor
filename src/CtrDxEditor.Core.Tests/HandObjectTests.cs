using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests mechanical hand attribute access and segment slot seeding.</summary>
    public class HandObjectTests
    {
        private static LevelObject Hand(params (string Name, string Value)[] attrs)
        {
            XElement e = new("hand");
            e.SetAttributeValue("x", "162");
            e.SetAttributeValue("y", "254");
            foreach ((string name, string value) in attrs)
            {
                e.SetAttributeValue(name, value);
            }
            return new LevelObject(e);
        }

        /// <summary>IsHand recognizes only the game's hand tag.</summary>
        [Fact]
        public void IsHandRecognizesOnlyHandTag()
        {
            Assert.True(HandObject.IsHand("hand"));
            Assert.False(HandObject.IsHand("grab"));
        }

        /// <summary>Slot attribute names are 1-based and match the game's naming.</summary>
        [Fact]
        public void SlotAttributeNamesMatchGame()
        {
            Assert.Equal("segment1Angle", HandObject.AngleAttr(1));
            Assert.Equal("segment2Length", HandObject.LengthAttr(2));
            Assert.Equal("segment3Rotatable", HandObject.RotatableAttr(3));
        }

        /// <summary>SegmentCount reads the attribute; missing/unparseable/negative all yield 0 like ParseIntOrZero.</summary>
        [Theory]
        [InlineData("2", 2)]
        [InlineData("0", 0)]
        [InlineData("-1", 0)]
        [InlineData("abc", 0)]
        [InlineData(null, 0)]
        public void SegmentCountMatchesParseIntOrZero(string? stored, int expected)
        {
            LevelObject hand = stored is null ? Hand() : Hand(("segmentsCount", stored));
            Assert.Equal(expected, HandObject.SegmentCount(hand));
        }

        /// <summary>Angle and Length fall back to 0 on missing or unparseable values, like ParseFloatOrZero.</summary>
        [Fact]
        public void AngleAndLengthFallBackToZero()
        {
            LevelObject hand = Hand(("segment1Angle", "-90"), ("segment1Length", "70"), ("segment2Angle", "junk"));
            Assert.Equal(-90, HandObject.Angle(hand, 1));
            Assert.Equal(70, HandObject.Length(hand, 1));
            Assert.Equal(0, HandObject.Angle(hand, 2));
            Assert.Equal(0, HandObject.Length(hand, 9));
        }

        /// <summary>Rotatable is false for anything bool.TryParse rejects, matching the game.</summary>
        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData("True", true)]
        [InlineData("1", false)]
        [InlineData("yes", false)]
        [InlineData(null, false)]
        public void RotatableMatchesBoolTryParse(string? stored, bool expected)
        {
            LevelObject hand = stored is null ? Hand() : Hand(("segment1Rotatable", stored));
            Assert.Equal(expected, HandObject.Rotatable(hand, 1));
        }

        /// <summary>Rotatable is written strictly as lowercase true/false.</summary>
        [Fact]
        public void SetRotatableWritesStrictLowercase()
        {
            LevelObject hand = Hand();
            HandObject.SetRotatable(hand, 1, true);
            Assert.Equal("true", hand.GetAttr("segment1Rotatable"));
            HandObject.SetRotatable(hand, 1, false);
            Assert.Equal("false", hand.GetAttr("segment1Rotatable"));
        }

        /// <summary>Angle is written as invariant whole degrees; length is written whole with a floor of 1.</summary>
        [Fact]
        public void SettersWriteWholeInvariantValues()
        {
            LevelObject hand = Hand();
            HandObject.SetAngle(hand, 1, -89.6);
            HandObject.SetLength(hand, 1, 70.4);
            Assert.Equal("-90", hand.GetAttr("segment1Angle"));
            Assert.Equal("70", hand.GetAttr("segment1Length"));

            HandObject.SetLength(hand, 1, 0);
            Assert.Equal("1", hand.GetAttr("segment1Length"));
        }

        /// <summary>Growing the count seeds only absent slots with the authored inactive defaults.</summary>
        [Fact]
        public void SetSegmentCountSeedsAbsentSlots()
        {
            LevelObject hand = Hand(("segmentsCount", "0"));
            HandObject.SetSegmentCount(hand, 2);

            Assert.Equal("2", hand.GetAttr("segmentsCount"));
            Assert.Equal("0", hand.GetAttr("segment1Angle"));
            Assert.Equal("10", hand.GetAttr("segment1Length"));
            Assert.Equal("true", hand.GetAttr("segment1Rotatable"));
            Assert.Equal("10", hand.GetAttr("segment2Length"));
        }

        /// <summary>Growing the count restores an existing orphan slot rather than overwriting it.</summary>
        [Fact]
        public void SetSegmentCountPreservesExistingOrphanSlot()
        {
            LevelObject hand = Hand(
                ("segmentsCount", "2"),
                ("segment3Angle", "180"),
                ("segment3Length", "64"),
                ("segment3Rotatable", "false"));

            HandObject.SetSegmentCount(hand, 3);

            Assert.Equal("180", hand.GetAttr("segment3Angle"));
            Assert.Equal("64", hand.GetAttr("segment3Length"));
            Assert.Equal("false", hand.GetAttr("segment3Rotatable"));
        }

        /// <summary>Shrinking the count leaves the now-dead slot untouched.</summary>
        [Fact]
        public void SetSegmentCountLeavesOrphanSlotsVerbatim()
        {
            LevelObject hand = Hand(
                ("segmentsCount", "3"),
                ("segment3Angle", "180"),
                ("segment3Length", "64"),
                ("segment3Rotatable", "false"));

            HandObject.SetSegmentCount(hand, 1);

            Assert.Equal("1", hand.GetAttr("segmentsCount"));
            Assert.Equal("180", hand.GetAttr("segment3Angle"));
            Assert.Equal("64", hand.GetAttr("segment3Length"));
        }
    }
}
