using System.Linq;

using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the conveyor (transporter) object descriptor.</summary>
    public class ConveyorDescriptorTests
    {
        [Fact]
        public void TransporterIsRegistered()
        {
            ObjectDescriptor? d = DescriptorTable.Default.For("transporter");
            Assert.NotNull(d);
            Assert.Equal("Conveyor", d!.DisplayName);
            Assert.Equal(int.MaxValue, d.MaxCount);
        }

        [Fact]
        public void TransporterExposesGameAttributesWithDefaults()
        {
            ObjectDescriptor d = DescriptorTable.Default.For("transporter")!;

            AttributeSpec velocity = d.Attributes.Single(a => a.Name == "velocity");
            Assert.Equal(AttrType.Number, velocity.Type);
            Assert.Equal("10", velocity.Default);

            AttributeSpec direction = d.Attributes.Single(a => a.Name == "direction");
            Assert.Equal(AttrType.Enum, direction.Type);
            Assert.Equal("forward", direction.Default);
            Assert.Equal(["forward", "backward"], direction.EnumValues);

            Assert.Equal("250", d.Attributes.Single(a => a.Name == "length").Default);
            Assert.Equal("50", d.Attributes.Single(a => a.Name == "width").Default);
            Assert.Equal("0", d.Attributes.Single(a => a.Name == "angle").Default);
        }

        [Fact]
        public void TransporterTypeAttributeHasNoDefaultSoNewBeltsAreAutomatic()
        {
            ObjectDescriptor d = DescriptorTable.Default.For("transporter")!;
            AttributeSpec type = d.Attributes.Single(a => a.Name == "type");
            Assert.Null(type.Default);
        }
    }
}
