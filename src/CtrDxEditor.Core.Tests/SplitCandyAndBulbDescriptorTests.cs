using System.Linq;

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

        /// <summary>Light bulbs expose litRadius and an editable bulbNumber with no load-time default.</summary>
        [Fact]
        public void LightBulbHasLitRadiusAndBulbNumber()
        {
            ObjectDescriptor? bulb = DescriptorTable.Default.For("lightBulb");
            Assert.NotNull(bulb);

            AttributeSpec litRadius = bulb.Attributes.Single(a => a.Name == "litRadius");
            Assert.Equal(AttrType.Whole, litRadius.Type);
            Assert.Equal("50", litRadius.Default);

            AttributeSpec bulbNumber = bulb.Attributes.Single(a => a.Name == "bulbNumber");
            Assert.Equal(AttrType.Text, bulbNumber.Type);
            Assert.Null(bulbNumber.Default);
        }
    }
}
