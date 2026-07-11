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

        /// <summary>An object without a ring returns null.</summary>
        [Fact]
        public void BubbleHasNoRing()
        {
            LevelObject b = new(new XElement("bubble", new XAttribute("x", 0), new XAttribute("y", 0)));
            Assert.Null(RadiusRing.Of(b));
        }
    }
}
