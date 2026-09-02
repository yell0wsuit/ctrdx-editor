using System.Xml.Linq;

using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.ViewModels;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Tests that a color field reports itself distinctly from a text field, so the panel renders the
    /// swatch-plus-hex row instead of (or in addition to) a plain text box.
    /// </summary>
    public class AttributeFieldColorTests
    {
        private static LevelObject Obj()
        {
            return new(XElement.Parse("""<tutorialPrompt color="#FF0000" />"""));
        }

        /// <summary>A field constructed with <see cref="AttrType.Color"/> reports <c>IsColor</c> and not <c>IsText</c>.</summary>
        [Fact]
        public void ColorFieldIsColorNotText()
        {
            AttributeFieldViewModel field = new(Obj(), "color", AttrType.Color, null, () => { });

            Assert.True(field.IsColor);
            Assert.False(field.IsText);
        }

        /// <summary>Every other attribute-backed field type still reports <c>IsColor</c> false.</summary>
        [Theory]
        [InlineData(AttrType.Whole)]
        [InlineData(AttrType.Number)]
        [InlineData(AttrType.Bool)]
        [InlineData(AttrType.Ref)]
        [InlineData(AttrType.Text)]
        public void NonColorFieldsAreNotColor(AttrType type)
        {
            AttributeFieldViewModel field = new(Obj(), "color", type, null, () => { });

            Assert.False(field.IsColor);
        }

        /// <summary>A field with no fixed options and no bool/numeric/color type still renders as plain text.</summary>
        [Fact]
        public void TextFieldIsStillText()
        {
            AttributeFieldViewModel field = new(Obj(), "color", AttrType.Text, null, () => { });

            Assert.True(field.IsText);
        }

        /// <summary>Bool and numeric fields are still excluded from <c>IsText</c>, unaffected by the color narrowing.</summary>
        [Theory]
        [InlineData(AttrType.Bool)]
        [InlineData(AttrType.Whole)]
        [InlineData(AttrType.Number)]
        public void BoolAndNumericFieldsAreNotText(AttrType type)
        {
            AttributeFieldViewModel field = new(Obj(), "color", type, null, () => { });

            Assert.False(field.IsText);
        }
    }
}
