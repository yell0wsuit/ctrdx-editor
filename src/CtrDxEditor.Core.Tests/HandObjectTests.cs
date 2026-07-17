using System.Linq;
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

        /// <summary>Deleting the last segment only decrements the count, leaving its slot verbatim.</summary>
        [Fact]
        public void DeleteLastSegmentLeavesSlotVerbatim()
        {
            LevelObject hand = Hand(
                ("segmentsCount", "2"),
                ("segment1Angle", "-90"), ("segment1Length", "70"), ("segment1Rotatable", "true"),
                ("segment2Angle", "180"), ("segment2Length", "50"), ("segment2Rotatable", "false"));

            HandObject.DeleteSegment(hand, 2);

            Assert.Equal("1", hand.GetAttr("segmentsCount"));
            Assert.Equal("180", hand.GetAttr("segment2Angle"));
            Assert.Equal("50", hand.GetAttr("segment2Length"));
            Assert.Equal("false", hand.GetAttr("segment2Rotatable"));
        }

        /// <summary>Deleting a middle segment shifts the live slots above it down.</summary>
        [Fact]
        public void DeleteMiddleSegmentShiftsLiveSlotsDown()
        {
            LevelObject hand = Hand(
                ("segmentsCount", "3"),
                ("segment1Angle", "90"), ("segment1Length", "50"), ("segment1Rotatable", "true"),
                ("segment2Angle", "0"), ("segment2Length", "60"), ("segment2Rotatable", "false"),
                ("segment3Angle", "180"), ("segment3Length", "70"), ("segment3Rotatable", "true"));

            HandObject.DeleteSegment(hand, 2);

            Assert.Equal("2", hand.GetAttr("segmentsCount"));
            Assert.Equal("90", hand.GetAttr("segment1Angle"));
            Assert.Equal("180", hand.GetAttr("segment2Angle"));
            Assert.Equal("70", hand.GetAttr("segment2Length"));
            Assert.Equal("true", hand.GetAttr("segment2Rotatable"));
        }

        /// <summary>Inserting shifts live slots up and writes the new segment at the requested index.</summary>
        [Fact]
        public void InsertSegmentShiftsLiveSlotsUp()
        {
            LevelObject hand = Hand(
                ("segmentsCount", "2"),
                ("segment1Angle", "90"), ("segment1Length", "50"), ("segment1Rotatable", "true"),
                ("segment2Angle", "180"), ("segment2Length", "60"), ("segment2Rotatable", "false"));

            HandObject.InsertSegment(hand, 2, angle: -90, length: 25, rotatable: true);

            Assert.Equal("3", hand.GetAttr("segmentsCount"));
            Assert.Equal("90", hand.GetAttr("segment1Angle"));
            Assert.Equal("-90", hand.GetAttr("segment2Angle"));
            Assert.Equal("25", hand.GetAttr("segment2Length"));
            Assert.Equal("true", hand.GetAttr("segment2Rotatable"));
            Assert.Equal("180", hand.GetAttr("segment3Angle"));
            Assert.Equal("60", hand.GetAttr("segment3Length"));
            Assert.Equal("false", hand.GetAttr("segment3Rotatable"));
        }

        /// <summary>Appending past the last live slot is allowed and increments the count.</summary>
        [Fact]
        public void InsertSegmentAppendsAtCountPlusOne()
        {
            LevelObject hand = Hand(
                ("segmentsCount", "1"),
                ("segment1Angle", "90"), ("segment1Length", "50"), ("segment1Rotatable", "true"));

            HandObject.InsertSegment(hand, 2, angle: 0, length: 40, rotatable: false);

            Assert.Equal("2", hand.GetAttr("segmentsCount"));
            Assert.Equal("0", hand.GetAttr("segment2Angle"));
            Assert.Equal("40", hand.GetAttr("segment2Length"));
            Assert.Equal("false", hand.GetAttr("segment2Rotatable"));
        }

        /// <summary>Insert and delete ignore out-of-range indices instead of corrupting the chain.</summary>
        [Fact]
        public void InsertAndDeleteIgnoreOutOfRangeIndices()
        {
            LevelObject hand = Hand(
                ("segmentsCount", "1"),
                ("segment1Angle", "90"), ("segment1Length", "50"), ("segment1Rotatable", "true"));

            HandObject.DeleteSegment(hand, 0);
            HandObject.DeleteSegment(hand, 2);
            HandObject.InsertSegment(hand, 0, 0, 10, true);
            HandObject.InsertSegment(hand, 3, 0, 10, true);

            Assert.Equal("1", hand.GetAttr("segmentsCount"));
            Assert.Equal("90", hand.GetAttr("segment1Angle"));
        }

        /// <summary>Segment counts above the authored maximum of 3 are supported; the game's loop has no cap.</summary>
        [Fact]
        public void InsertSegmentSupportsCountsAboveThree()
        {
            LevelObject hand = Hand(("segmentsCount", "0"));
            HandObject.SetSegmentCount(hand, 3);

            HandObject.InsertSegment(hand, 4, angle: 45, length: 30, rotatable: true);

            Assert.Equal("4", hand.GetAttr("segmentsCount"));
            Assert.Equal("45", hand.GetAttr("segment4Angle"));
            Assert.Equal("30", hand.GetAttr("segment4Length"));
        }

        /// <summary>
        /// A hand whose segmentsCount is lower than its authored slots (the 6_14.xml shape) survives a
        /// load/save cycle unchanged. The game ignores slots past the count; the editor must preserve them
        /// rather than normalize them away.
        /// </summary>
        [Fact]
        public void DeadSegmentSlotsSurviveRoundTrip()
        {
            const string Original =
                "<level width=\"640\" height=\"480\">" +
                "<hand x=\"162\" y=\"254\" segmentsCount=\"2\" " +
                "segment1Angle=\"-90\" segment1Length=\"70\" segment1Rotatable=\"true\" " +
                "segment2Angle=\"-90\" segment2Length=\"70\" segment2Rotatable=\"true\" " +
                "segment3Angle=\"-90\" segment3Length=\"70\" segment3Rotatable=\"true\" />" +
                "</level>";

            LevelDocument doc = LevelDocument.Parse(Original);
            XDocument after = XDocument.Parse(doc.Save());
            XElement hand = after.Root!.Elements("hand").Single();

            Assert.Equal("2", (string?)hand.Attribute("segmentsCount"));
            Assert.Equal("-90", (string?)hand.Attribute("segment3Angle"));
            Assert.Equal("70", (string?)hand.Attribute("segment3Length"));
            Assert.Equal("true", (string?)hand.Attribute("segment3Rotatable"));
        }

        /// <summary>An authored hand's angles and lengths are never rewritten on load.</summary>
        [Fact]
        public void AuthoredHandValuesAreNotNormalizedOnLoad()
        {
            const string Original =
                "<level width=\"640\" height=\"480\">" +
                "<hand x=\"160\" y=\"303\" segmentsCount=\"1\" " +
                "segment1Angle=\"270\" segment1Length=\"100\" segment1Rotatable=\"true\" />" +
                "</level>";

            LevelDocument doc = LevelDocument.Parse(Original);
            XDocument after = XDocument.Parse(doc.Save());
            XElement hand = after.Root!.Elements("hand").Single();

            // 270 is equivalent to -90, but the editor must not rewrite what it did not edit.
            Assert.Equal("270", (string?)hand.Attribute("segment1Angle"));
        }
    }
}
