using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the bamboo-tube descriptor, which the game loads from the <c>pipe</c> element.</summary>
    public class BambooTubeDescriptorTests
    {
        /// <summary>The bamboo tube is an Experiments-era object, so it groups with the rocket in the palette.</summary>
        [Fact]
        public void BambooTubeIsRegisteredInTheExperimentsGroup()
        {
            ObjectDescriptor? pipe = DescriptorTable.CtrObjects.For("pipe");
            Assert.NotNull(pipe);
            Assert.Equal("Bamboo tube", pipe.DisplayName);
            Assert.Equal(int.MaxValue, pipe.MaxCount);
            Assert.Equal("Cut the Rope: Experiments", pipe.Game);
        }

        /// <summary>LoadBambooTube reads x/y/angle only, so the tube exposes a single angle field.</summary>
        [Fact]
        public void BambooTubeExposesOnlyAnAngleAttribute()
        {
            ObjectDescriptor pipe = DescriptorTable.CtrObjects.For("pipe")!;
            AttributeSpec attr = Assert.Single(pipe.Attributes);
            Assert.Equal("angle", attr.Name);
            Assert.Equal(AttrType.Number, attr.Type);
            Assert.Equal("0", attr.Default);
        }
    }
}
