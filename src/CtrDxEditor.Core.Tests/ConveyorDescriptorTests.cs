using System.Linq;

using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the conveyor (transporter) object descriptor.</summary>
    public class ConveyorDescriptorTests
    {
        /// <summary>The transporter descriptor is registered as an unbounded "Conveyor".</summary>
        [Fact]
        public void TransporterIsRegistered()
        {
            ObjectDescriptor? d = DescriptorTable.CtrObjects.For("transporter");
            Assert.NotNull(d);
            Assert.Equal("Conveyor", d!.DisplayName);
            Assert.Equal(int.MaxValue, d.MaxCount);
        }

        /// <summary>The transporter exposes the game's velocity/direction/length/width/angle with defaults.</summary>
        [Fact]
        public void TransporterExposesGameAttributesWithDefaults()
        {
            ObjectDescriptor d = DescriptorTable.CtrObjects.For("transporter")!;

            AttributeSpec velocity = d.Attributes.Single(a => a.Name == "velocity");
            Assert.Equal(AttrType.Number, velocity.Type);
            Assert.Equal("10", velocity.Default);

            AttributeSpec direction = d.Attributes.Single(a => a.Name == "direction");
            Assert.Equal(AttrType.Enum, direction.Type);
            Assert.Equal("forward", direction.Default);
            Assert.Equal(["forward", "backward"], direction.EnumValues!);

            Assert.Equal("250", d.Attributes.Single(a => a.Name == "length").Default);
            Assert.Equal("50", d.Attributes.Single(a => a.Name == "width").Default);
            Assert.Equal("0", d.Attributes.Single(a => a.Name == "angle").Default);
        }

        /// <summary>The type attribute has no default so freshly placed belts are automatic.</summary>
        [Fact]
        public void TransporterTypeAttributeHasNoDefaultSoNewBeltsAreAutomatic()
        {
            ObjectDescriptor d = DescriptorTable.CtrObjects.For("transporter")!;
            AttributeSpec type = d.Attributes.Single(a => a.Name == "type");
            Assert.Null(type.Default);
        }
    }
}
