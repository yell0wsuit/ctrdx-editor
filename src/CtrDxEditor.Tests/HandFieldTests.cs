using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Localization;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests mechanical hand property panel fields.</summary>
    public class HandFieldTests
    {
        private static LevelObject Hand(string xml)
        {
            return new LevelObject(XElement.Parse(xml));
        }

        private static ObservableCollection<AttributeFieldViewModel> Build(LevelObject hand)
        {
            ObservableCollection<AttributeFieldViewModel> fields = [];
            HandFieldBuilder.Build(fields, hand, () => { }, () => { }, () => { });
            return fields;
        }

        /// <summary>One section is rendered per live segment, and dead slots get none.</summary>
        [Fact]
        public void RendersOneSectionPerLiveSegment()
        {
            LevelObject hand = Hand("""
                <hand x='162' y='254' segmentsCount='2'
                      segment1Angle='-90' segment1Length='70' segment1Rotatable='true'
                      segment2Angle='-90' segment2Length='70' segment2Rotatable='true'
                      segment3Angle='-90' segment3Length='70' segment3Rotatable='true' />
                """);

            ObservableCollection<AttributeFieldViewModel> fields = Build(hand);

            Assert.Contains(fields, f => f.Name == "segmentsCount");
            Assert.Equal([1, 2], [.. fields.Where(f => f.GroupIndex > 0).Select(f => f.GroupIndex).Distinct()]);
            Assert.DoesNotContain(fields, f => f.Name == "segment3Angle");
        }

        /// <summary>Each segment section exposes angle, length, and rotatable, bound to its own slot.</summary>
        [Fact]
        public void SectionExposesSlotAttributes()
        {
            LevelObject hand = Hand("""
                <hand x='162' y='254' segmentsCount='1'
                      segment1Angle='-90' segment1Length='70' segment1Rotatable='true' />
                """);

            ObservableCollection<AttributeFieldViewModel> fields = Build(hand);

            Assert.Contains(fields, f => f.Name == "segment1Angle" && f.GroupIndex == 1);
            Assert.Contains(fields, f => f.Name == "segment1Length" && f.GroupIndex == 1);
            Assert.Contains(fields, f => f.Name == "segment1Rotatable" && f.GroupIndex == 1);
        }

        /// <summary>Segment fields reuse the shared attribute labels rather than per-index strings.</summary>
        [Fact]
        public void SegmentFieldsUseSharedLabels()
        {
            LevelObject hand = Hand("<hand x='0' y='0' segmentsCount='1' segment1Angle='0' segment1Length='10' segment1Rotatable='true' />");
            ObservableCollection<AttributeFieldViewModel> fields = Build(hand);

            Assert.Equal(Localizer.AttributeName("angle"), fields.Single(f => f.Name == "segment1Angle").Label);
            Assert.Equal(Localizer.AttributeName("length"), fields.Single(f => f.Name == "segment1Length").Label);
            Assert.Equal(Localizer.AttributeName("isRotatable"), fields.Single(f => f.Name == "segment1Rotatable").Label);
        }

        /// <summary>Editing a segment field writes back to that slot.</summary>
        [Fact]
        public void EditingASegmentFieldWritesItsSlot()
        {
            LevelObject hand = Hand("<hand x='0' y='0' segmentsCount='1' segment1Angle='0' segment1Length='10' segment1Rotatable='true' />");
            ObservableCollection<AttributeFieldViewModel> fields = Build(hand);

            fields.Single(f => f.Name == "segment1Length").Value = "85";
            Assert.Equal("85", hand.GetAttr("segment1Length"));
        }

        /// <summary>Angle edits use the hand writer, which stores invariant whole degrees.</summary>
        [Fact]
        public void EditingAngleWritesWholeDegrees()
        {
            LevelObject hand = Hand("<hand x='0' y='0' segmentsCount='1' segment1Angle='0' segment1Length='10' segment1Rotatable='true' />");
            ObservableCollection<AttributeFieldViewModel> fields = Build(hand);

            fields.Single(f => f.Name == "segment1Angle").Value = "12.5";

            Assert.Equal("12", hand.GetAttr("segment1Angle"));
        }

        /// <summary>Growing the count seeds the new slot and triggers a rebuild.</summary>
        [Fact]
        public void GrowingCountSeedsSlotAndRebuilds()
        {
            LevelObject hand = Hand("<hand x='0' y='0' segmentsCount='1' segment1Angle='0' segment1Length='10' segment1Rotatable='true' />");
            int rebuilds = 0;
            ObservableCollection<AttributeFieldViewModel> fields = [];
            HandFieldBuilder.Build(fields, hand, () => { }, () => { }, () => rebuilds++);

            fields.Single(f => f.Name == "segmentsCount").Value = "2";

            Assert.Equal("2", hand.GetAttr("segmentsCount"));
            Assert.Equal("10", hand.GetAttr("segment2Length"));
            Assert.Equal("true", hand.GetAttr("segment2Rotatable"));
            Assert.Equal(1, rebuilds);
        }

        /// <summary>The segment count renders as an up/down stepper, and stepping its number grows the arm.</summary>
        [Fact]
        public void SegmentCountIsAStepper()
        {
            LevelObject hand = Hand("<hand x='0' y='0' segmentsCount='1' segment1Angle='0' segment1Length='10' segment1Rotatable='true' />");
            ObservableCollection<AttributeFieldViewModel> fields = Build(hand);
            AttributeFieldViewModel count = fields.Single(f => f.Name == "segmentsCount");

            Assert.True(count.IsStepper);
            Assert.False(count.IsPlainNumeric);
            Assert.Equal(1m, count.NumericValue);

            count.NumericValue = 2m;
            Assert.Equal("2", hand.GetAttr("segmentsCount"));
        }

        /// <summary>Growing past the authored maximum of three is allowed; the game's loop is uncapped.</summary>
        [Fact]
        public void CountIsNotCappedAtThree()
        {
            LevelObject hand = Hand("<hand x='0' y='0' segmentsCount='3' segment1Angle='0' segment1Length='10' segment1Rotatable='true' />");
            ObservableCollection<AttributeFieldViewModel> fields = Build(hand);

            fields.Single(f => f.Name == "segmentsCount").Value = "5";

            Assert.Equal("5", hand.GetAttr("segmentsCount"));
            Assert.Equal("10", hand.GetAttr("segment5Length"));
        }

        /// <summary>Shrinking the count hides a slot without destroying it, so re-growing restores it.</summary>
        [Fact]
        public void ShrinkingHidesSlotWithoutDestroyingIt()
        {
            LevelObject hand = Hand("""
                <hand x='0' y='0' segmentsCount='2'
                      segment1Angle='0' segment1Length='10' segment1Rotatable='true'
                      segment2Angle='180' segment2Length='64' segment2Rotatable='false' />
                """);
            ObservableCollection<AttributeFieldViewModel> fields = Build(hand);

            fields.Single(f => f.Name == "segmentsCount").Value = "1";
            Assert.Equal("180", hand.GetAttr("segment2Angle"));
            Assert.Equal("64", hand.GetAttr("segment2Length"));

            ObservableCollection<AttributeFieldViewModel> regrown = [];
            HandFieldBuilder.Build(regrown, hand, () => { }, () => { }, () => { });
            regrown.Single(f => f.Name == "segmentsCount").Value = "2";

            Assert.Equal("180", hand.GetAttr("segment2Angle"));
            Assert.Equal("64", hand.GetAttr("segment2Length"));
        }
    }
}
