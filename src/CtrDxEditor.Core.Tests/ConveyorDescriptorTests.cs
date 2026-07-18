using System.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Editing;

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
            Assert.Equal("Conveyor", d.DisplayName);
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

        /// <summary>Freshly placed conveyors default to the game's manual type.</summary>
        [Fact]
        public void TransporterTypeAttributeDefaultsToManual()
        {
            ObjectDescriptor d = DescriptorTable.CtrObjects.For("transporter")!;
            AttributeSpec type = d.Attributes.Single(a => a.Name == "type");
            Assert.Equal("manual", type.Default);
        }

        /// <summary>The conveyor uses the shared dial with counter-clockwise stored game angles.</summary>
        [Fact]
        public void TransporterRotationDialUsesCounterClockwiseStorage()
        {
            RotationSpec spec = RotationTable.EditableFor("transporter")!;
            Assert.NotNull(spec);
            Assert.Equal(-1, spec.StoredAngleSign);
            Assert.Equal("angle", spec.AttributeName);
            Assert.Equal(RotationCenterKind.ConveyorMidpoint, spec.CenterKind);
        }
    }
}
