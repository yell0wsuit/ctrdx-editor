using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the lantern object descriptor.</summary>
    public class LanternDescriptorTests
    {
        /// <summary>The lantern descriptor exposes one boolean capture flag that defaults to false.</summary>
        [Fact]
        public void LanternHasCandyCapturedBoolDefaultFalse()
        {
            ObjectDescriptor? lantern = DescriptorTable.CtrObjects.For("lantern");
            Assert.NotNull(lantern);

            AttributeSpec attr = Assert.Single(lantern.Attributes);
            Assert.Equal("candyCaptured", attr.Name);
            Assert.Equal(AttrType.Bool, attr.Type);
            Assert.Equal("false", attr.Default);
            Assert.Equal(int.MaxValue, lantern.MaxCount);
        }
    }
}
