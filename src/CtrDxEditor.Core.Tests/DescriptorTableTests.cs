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
            Assert.True(t.Knows("pump"));
            Assert.True(t.Knows("spike1"));
            Assert.True(t.Knows("spike2"));
            Assert.True(t.Knows("spike3"));
            Assert.True(t.Knows("spike4"));
            Assert.True(t.Knows("electro"));
            Assert.True(t.Knows("sock"));
        }

        /// <summary>Verifies magic hats expose the integer transporter pairing group used by DX.</summary>
        [Fact]
        public void SockHasTeleportGroupDefaultingToZero()
        {
            ObjectDescriptor sock = DescriptorTable.Default.For("sock")!;

            Assert.Equal("Magic Hat", sock.DisplayName);
            Assert.Equal(int.MaxValue, sock.MaxCount);
            AttributeSpec group = Assert.Single(sock.Attributes);
            Assert.Equal("group", group.Name);
            Assert.Equal(AttrType.Whole, group.Type);
            Assert.Equal("0", group.Default);
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

        /// <summary>Verifies the pump exposes a single float angle attribute defaulting to 0.</summary>
        [Fact]
        public void PumpHasAngleAttributeDefaultingToZero()
        {
            ObjectDescriptor pump = DescriptorTable.Default.For("pump")!;
            AttributeSpec angle = Assert.Single(pump.Attributes);
            Assert.Equal("angle", angle.Name);
            Assert.Equal(AttrType.Number, angle.Type);
            Assert.Equal("0", angle.Default);
            Assert.Equal(int.MaxValue, pump.MaxCount);
        }

        /// <summary>Verifies spike descriptors mirror the game's spike1-4 XML elements.</summary>
        [Theory]
        [InlineData("spike1", "1")]
        [InlineData("spike2", "2")]
        [InlineData("spike3", "3")]
        [InlineData("spike4", "4")]
        public void SpikesCarryAngleSizeAndToggledAttributes(string element, string size)
        {
            ObjectDescriptor spike = DescriptorTable.Default.For(element)!;

            Assert.Equal("Spike", spike.DisplayName);
            Assert.Equal(int.MaxValue, spike.MaxCount);
            Assert.Contains(spike.Attributes, a => a.Name == "angle" && a.Type == AttrType.Number && a.Default == "0");
            Assert.Contains(spike.Attributes, a => a.Name == "size" && a.Type == AttrType.Enum && a.Default == size);
            Assert.Contains(spike.Attributes, a => a.Name == "toggled" && a.Type == AttrType.Bool && a.Default == "false");
        }

        /// <summary>Verifies electro descriptors expose only the editable timed electrode fields.</summary>
        [Fact]
        public void ElectroCarriesTimingAndAngleAttributes()
        {
            ObjectDescriptor electro = DescriptorTable.Default.For("electro")!;

            Assert.Equal("Electro", electro.DisplayName);
            Assert.Equal(int.MaxValue, electro.MaxCount);
            Assert.Contains(electro.Attributes, a => a.Name == "initialDelay" && a.Type == AttrType.Number && a.Default == "0.0");
            Assert.Contains(electro.Attributes, a => a.Name == "offTime" && a.Type == AttrType.Number && a.Default == "2.0");
            Assert.Contains(electro.Attributes, a => a.Name == "onTime" && a.Type == AttrType.Number && a.Default == "2.0");
            Assert.Contains(electro.Attributes, a => a.Name == "angle" && a.Type == AttrType.Number && a.Default == "0");
            Assert.DoesNotContain(electro.Attributes, a => a.Name == "size");
        }
    }
}
