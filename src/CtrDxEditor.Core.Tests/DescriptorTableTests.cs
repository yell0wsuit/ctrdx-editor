using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for built-in object descriptor metadata.</summary>
    public class DescriptorTableTests
    {
        /// <summary>Verifies that the default descriptor table contains the supported objects.</summary>
        [Fact]
        public void DefaultKnowsTheSupportedObjects()
        {
            DescriptorTable t = DescriptorTable.Default;
            Assert.True(t.Knows("target"));
            Assert.True(t.Knows("candy"));
            Assert.True(t.Knows("star"));
            Assert.True(t.Knows("grab"));
            Assert.True(t.Knows("bubble"));
            Assert.True(t.Knows("gravitySwitch"));
            Assert.False(t.Knows("pump"));
        }

        /// <summary>Verifies that bubble is unbounded and has no editable attributes (game reads only x/y).</summary>
        [Fact]
        public void BubbleIsUnboundedAndAttributeFree()
        {
            ObjectDescriptor bubble = DescriptorTable.Default.For("bubble")!;
            Assert.Empty(bubble.Attributes);
            Assert.Equal(int.MaxValue, bubble.MaxCount);
        }

        /// <summary>Verifies that gravity switches are plain placeable buttons with no extra attributes.</summary>
        [Fact]
        public void GravitySwitchIsAttributeFree()
        {
            ObjectDescriptor gravitySwitch = DescriptorTable.Default.For("gravitySwitch")!;
            Assert.Empty(gravitySwitch.Attributes);
            Assert.Equal(int.MaxValue, gravitySwitch.MaxCount);
        }

        /// <summary>Verifies unbounded limits for multi-target and multi-candy descriptors.</summary>
        [Fact]
        public void TargetAndCandyAreUnbounded()
        {
            Assert.Equal(int.MaxValue, DescriptorTable.Default.For("target")!.MaxCount);
            Assert.Equal(int.MaxValue, DescriptorTable.Default.For("candy")!.MaxCount);
            Assert.Equal(int.MaxValue, DescriptorTable.Default.For("star")!.MaxCount);
        }

        /// <summary>Verifies the timeout metadata for star descriptors.</summary>
        [Fact]
        public void StarTimeoutIsDecimalWithMinusOneDefault()
        {
            AttributeSpec timeout = Assert.Single(DescriptorTable.Default.For("star")!.Attributes);
            Assert.Equal("timeout", timeout.Name);
            Assert.Equal(AttrType.Number, timeout.Type);
            Assert.Equal("-1", timeout.Default);
        }

        /// <summary>Verifies that grab exposes suction cup state attributes.</summary>
        [Fact]
        public void GrabHasSuctionCupAttributes()
        {
            ObjectDescriptor grab = DescriptorTable.Default.For("grab")!;
            Assert.Contains(grab.Attributes, a => a.Name == "kickable" && a.Type == AttrType.Bool);
            Assert.Contains(grab.Attributes, a => a.Name == "kicked" && a.Type == AttrType.Bool);
        }
    }
}
