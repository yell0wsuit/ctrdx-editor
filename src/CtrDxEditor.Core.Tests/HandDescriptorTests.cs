using System.Linq;

using CtrDxEditor.Core.Descriptors;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests the mechanical hand object descriptor.</summary>
    public class HandDescriptorTests
    {
        /// <summary>The hand is registered as an unbounded Experiments object.</summary>
        [Fact]
        public void HandIsRegistered()
        {
            ObjectDescriptor? d = DescriptorTable.CtrObjects.For("hand");

            Assert.NotNull(d);
            Assert.Equal("Mechanical hand", d.DisplayName);
            Assert.Equal(int.MaxValue, d.MaxCount);
            Assert.Equal("Cut the Rope: Experiments", d.Game);
        }

        /// <summary>The hand exposes segmentsCount defaulting to one live segment.</summary>
        [Fact]
        public void HandExposesSegmentsCount()
        {
            ObjectDescriptor d = DescriptorTable.CtrObjects.For("hand")!;
            AttributeSpec spec = d.Attributes.Single(a => a.Name == "segmentsCount");

            Assert.Equal(AttrType.Whole, spec.Type);
            Assert.Equal("1", spec.Default);
        }

        /// <summary>
        /// Segment slots are index-dependent, so they are built dynamically rather than declared as static
        /// attribute specs.
        /// </summary>
        [Fact]
        public void HandDeclaresNoStaticSegmentSlots()
        {
            ObjectDescriptor d = DescriptorTable.CtrObjects.For("hand")!;
            Assert.DoesNotContain(d.Attributes, a => a.Name.StartsWith("segment1", System.StringComparison.Ordinal));
        }
    }
}
