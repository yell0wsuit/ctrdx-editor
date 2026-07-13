using System.Linq;

using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the mouse/gap object descriptor.</summary>
    public class MouseDescriptorTests
    {
        /// <summary>The mouse is authored as the <c>gap</c> element (the tag every shipped level uses).</summary>
        [Fact]
        public void MouseIsRegisteredUnderGapElement()
        {
            ObjectDescriptor? gap = DescriptorTable.CtrObjects.For("gap");
            Assert.NotNull(gap);
            Assert.Equal("Mouse", gap.DisplayName);
            Assert.Equal(int.MaxValue, gap.MaxCount);
        }

        /// <summary>The <c>mouse</c> alias is not a separate descriptor; it normalizes to <c>gap</c> on load.</summary>
        [Fact]
        public void MouseAliasIsNotASeparateDescriptor()
        {
            Assert.False(DescriptorTable.CtrObjects.Knows("mouse"));
        }

        /// <summary>
        /// The gap exposes angle, radius, activeTime and index. Placement defaults match the shipped
        /// levels (radius 50, activeTime 1.0); index carries no static default because it is
        /// auto-numbered on placement.
        /// </summary>
        [Fact]
        public void GapExposesMouseAttributesWithShippedDefaults()
        {
            ObjectDescriptor gap = DescriptorTable.CtrObjects.For("gap")!;

            AttributeSpec angle = gap.Attributes.Single(a => a.Name == "angle");
            Assert.Equal(AttrType.Number, angle.Type);
            Assert.Equal("0", angle.Default);

            AttributeSpec radius = gap.Attributes.Single(a => a.Name == "radius");
            Assert.Equal(AttrType.Number, radius.Type);
            Assert.Equal("50", radius.Default);

            AttributeSpec activeTime = gap.Attributes.Single(a => a.Name == "activeTime");
            Assert.Equal(AttrType.Number, activeTime.Type);
            Assert.Equal("1.0", activeTime.Default);

            AttributeSpec index = gap.Attributes.Single(a => a.Name == "index");
            Assert.Equal(AttrType.Whole, index.Type);
            Assert.Null(index.Default);
        }
    }
}
