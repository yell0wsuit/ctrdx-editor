using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests bouncer width helpers for the game's bouncer1/bouncer2 XML family.</summary>
    public class BouncerObjectTests
    {
        /// <summary>Only the two concrete DX bouncer element names belong to the family.</summary>
        [Theory]
        [InlineData("bouncer1", true)]
        [InlineData("bouncer2", true)]
        [InlineData("bouncer", false)]
        [InlineData("spike1", false)]
        public void IdentifiesBouncerElements(string element, bool expected)
        {
            Assert.Equal(expected, BouncerObject.IsBouncer(element));
        }

        /// <summary>A valid size attribute wins; otherwise the element suffix supplies the width.</summary>
        [Theory]
        [InlineData("bouncer1", "2", "2")]
        [InlineData("bouncer2", null, "2")]
        [InlineData("bouncer1", "invalid", "1")]
        public void SizeUsesValidAttributeThenElementSuffix(string element, string? attribute, string expected)
        {
            XElement xml = new(element);
            xml.SetAttributeValue("size", attribute);

            Assert.Equal(expected, BouncerObject.Size(new LevelObject(xml)));
        }

        /// <summary>Changing width keeps the size attribute and dispatch element name synchronized.</summary>
        [Fact]
        public void SetSizeRenamesBackingElement()
        {
            LevelObject bouncer = new(XElement.Parse("""<bouncer1 size="1" />"""));

            BouncerObject.SetSize(bouncer, "2");

            Assert.Equal("bouncer2", bouncer.Type);
            Assert.Equal("2", bouncer.GetAttr("size"));
        }

        /// <summary>Unsupported size values leave authored XML unchanged.</summary>
        [Fact]
        public void SetSizeIgnoresUnsupportedValues()
        {
            LevelObject bouncer = new(XElement.Parse("""<bouncer1 size="1" />"""));

            BouncerObject.SetSize(bouncer, "3");

            Assert.Equal("bouncer1", bouncer.Type);
            Assert.Equal("1", bouncer.GetAttr("size"));
        }
    }
}
