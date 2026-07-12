using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for the resizable radius-ring resolver.</summary>
    public class RadiusRingTests
    {
        /// <summary>A vinyl disc exposes its size as a resizable ring.</summary>
        [Fact]
        public void VinylExposesSizeAsRing()
        {
            LevelObject v = new(new XElement("rotatedCircle",
                new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("size", 120)));
            (double Radius, string Attr)? ring = RadiusRing.Of(v);
            Assert.True(ring.HasValue);
            Assert.Equal(120, ring!.Value.Radius);
            Assert.Equal("size", ring.Value.Attr);
        }

        /// <summary>A mouse exposes its grab radius as a resizable ring stored in the "radius" attribute.</summary>
        [Fact]
        public void MouseExposesRadiusAsRing()
        {
            LevelObject gap = new(new XElement("gap",
                new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("radius", 50)));
            (double Radius, string Attr)? ring = RadiusRing.Of(gap);
            Assert.True(ring.HasValue);
            Assert.Equal(50, ring!.Value.Radius);
            Assert.Equal("radius", ring.Value.Attr);
        }

        /// <summary>A mouse with a missing or non-positive radius exposes no ring.</summary>
        [Theory]
        [InlineData(null)]
        [InlineData("0")]
        [InlineData("-1")]
        public void MouseWithoutPositiveRadiusHasNoRing(string? radius)
        {
            XElement e = new("gap", new XAttribute("x", 0), new XAttribute("y", 0));
            if (radius is not null)
            {
                e.SetAttributeValue("radius", radius);
            }
            Assert.Null(RadiusRing.Of(new LevelObject(e)));
        }

        /// <summary>An object without a ring returns null.</summary>
        [Fact]
        public void BubbleHasNoRing()
        {
            LevelObject b = new(new XElement("bubble", new XAttribute("x", 0), new XAttribute("y", 0)));
            Assert.Null(RadiusRing.Of(b));
        }
    }
}
