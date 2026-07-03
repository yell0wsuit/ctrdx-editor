using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    public class DescriptorTableTests
    {
        [Fact]
        public void DefaultKnowsTheFourV1Objects()
        {
            DescriptorTable t = DescriptorTable.Default;
            Assert.True(t.Knows("target"));
            Assert.True(t.Knows("candy"));
            Assert.True(t.Knows("star"));
            Assert.True(t.Knows("grab"));
            Assert.False(t.Knows("bubble"));
        }

        [Fact]
        public void TargetAndCandyAreSingletons()
        {
            Assert.Equal(1, DescriptorTable.Default.For("target")!.MaxCount);
            Assert.Equal(1, DescriptorTable.Default.For("candy")!.MaxCount);
            Assert.Equal(int.MaxValue, DescriptorTable.Default.For("star")!.MaxCount);
        }

        [Fact]
        public void StarDefaultTimeoutIsMinusOne()
        {
            AttributeSpec timeout = Assert.Single(DescriptorTable.Default.For("star")!.Attributes);
            Assert.Equal("timeout", timeout.Name);
            Assert.Equal("-1", timeout.Default);
        }
    }
}
