using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the snail object descriptor, which the game loads from the <c>load</c> element.</summary>
    public class SnailDescriptorTests
    {
        /// <summary>The snail is an Experiments-era object, so it groups with the rocket in the palette.</summary>
        [Fact]
        public void SnailIsRegisteredInTheExperimentsGroup()
        {
            ObjectDescriptor? snail = DescriptorTable.CtrObjects.For("load");
            Assert.NotNull(snail);
            Assert.Equal("Snail", snail.DisplayName);
            Assert.Equal(int.MaxValue, snail.MaxCount);
            Assert.Equal("Cut the Rope: Experiments", snail.Game);
        }

        /// <summary>LoadSnails.cs reads x/y only, so the snail exposes no attribute fields.</summary>
        [Fact]
        public void SnailHasNoAttributes()
        {
            Assert.Empty(DescriptorTable.CtrObjects.For("load")!.Attributes);
        }

        /// <summary>The XML element stays <c>load</c>, but the UI localizes it under the readable name.</summary>
        [Fact]
        public void SnailLocalizesUnderItsDisplayNameNotItsElementName()
        {
            Assert.Equal("snail", DescriptorTable.CtrObjects.For("load")!.LocalizationName);
        }
    }
}
