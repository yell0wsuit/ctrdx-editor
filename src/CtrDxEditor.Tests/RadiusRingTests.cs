using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>Tests the object→radius-ring resolver shared by grabs and light bulbs.</summary>
    public class RadiusRingTests
    {
        private static LevelObject Obj(string type, string attr, string? value)
        {
            XElement e = new(type, new XAttribute("x", "0"), new XAttribute("y", "0"));
            if (value is not null)
            {
                e.SetAttributeValue(attr, value);
            }
            return new LevelObject(e);
        }

        [Theory]
        [InlineData("100", 100.0)]
        [InlineData(null, null)]
        [InlineData("-1", null)]
        [InlineData("0", null)]
        public void GrabResolvesToRadiusAttr(string? value, double? expected)
        {
            (double Radius, string Attr)? ring = RadiusRing.Of(Obj("grab", "radius", value));
            Assert.Equal(expected, ring?.Radius);
            if (expected is not null)
            {
                Assert.Equal("radius", ring!.Value.Attr);
            }
        }

        [Theory]
        [InlineData("50", 50.0)]
        [InlineData(null, null)]
        [InlineData("-1", null)]
        [InlineData("0", null)]
        public void LightBulbResolvesToLitRadiusAttr(string? value, double? expected)
        {
            (double Radius, string Attr)? ring = RadiusRing.Of(Obj("lightBulb", "litRadius", value));
            Assert.Equal(expected, ring?.Radius);
            if (expected is not null)
            {
                Assert.Equal("litRadius", ring!.Value.Attr);
            }
        }

        [Fact]
        public void OtherTypesHaveNoRing()
        {
            Assert.Null(RadiusRing.Of(Obj("candy", "radius", "100")));
        }
    }
}
