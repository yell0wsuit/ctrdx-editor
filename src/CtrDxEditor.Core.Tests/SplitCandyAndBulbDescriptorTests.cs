using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests descriptors for the two-part candy halves and the night-level light bulb.</summary>
    public class SplitCandyAndBulbDescriptorTests
    {
        /// <summary>Split candy halves remain singleton objects with no additional editable attributes.</summary>
        [Fact]
        public void SplitCandyHalvesAreSingletonWithNoAttributes()
        {
            DescriptorTable table = DescriptorTable.Default;

            ObjectDescriptor? left = table.For("candyL");
            ObjectDescriptor? right = table.For("candyR");
            Assert.NotNull(left);
            Assert.NotNull(right);

            Assert.Equal(1, left.MaxCount);
            Assert.Equal(1, right.MaxCount);
            Assert.Empty(left.Attributes);
            Assert.Empty(right.Attributes);
        }

        /// <summary>Light bulbs expose litRadius only; bulb ids are assigned internally.</summary>
        [Fact]
        public void LightBulbHasOnlyLitRadius()
        {
            ObjectDescriptor? bulb = DescriptorTable.Default.For("lightBulb");
            Assert.NotNull(bulb);

            AttributeSpec litRadius = Assert.Single(bulb.Attributes);
            Assert.Equal("litRadius", litRadius.Name);
            Assert.Equal(AttrType.Whole, litRadius.Type);
            Assert.Equal("50", litRadius.Default);
        }

        /// <summary>Plain candy ids are assigned internally and are not editable descriptor fields.</summary>
        [Fact]
        public void PlainCandyHasNoEditableAttributes()
        {
            ObjectDescriptor? candy = DescriptorTable.Default.For("candy");
            Assert.NotNull(candy);

            Assert.Empty(candy.Attributes);
        }
    }
}
