using System.Xml.Linq;

using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tutorial look attributes and the two authored color spellings.</summary>
    public class TutorialLookTests
    {
        private static LevelObject Prompt(string name, params (string Name, string Value)[] attributes)
        {
            XElement element = new(name);
            foreach ((string attribute, string value) in attributes)
            {
                element.SetAttributeValue(attribute, value);
            }

            return new LevelObject(element);
        }

        /// <summary>Hex and triplet spellings parse to the same color and each round-trips as authored.</summary>
        [Fact]
        public void BothColorSpellingsParseAndRoundTrip()
        {
            Assert.True(TutorialColor.TryParse("#462500", out TutorialColor hex));
            Assert.True(TutorialColor.TryParse("70, 37, 0", out TutorialColor triplet));

            Assert.Equal((byte)70, hex.Red);
            Assert.Equal((byte)37, hex.Green);
            Assert.Equal((byte)0, hex.Blue);
            Assert.Equal(hex.Red, triplet.Red);
            Assert.Equal(hex.Green, triplet.Green);
            Assert.Equal(hex.Blue, triplet.Blue);

            Assert.Equal("#462500", hex.Format());
            Assert.Equal("70,37,0", triplet.Format());
        }

        /// <summary>Format() is not a verbatim reproducer within a spelling family: hex digits always emit uppercase.</summary>
        [Fact]
        public void FormatNormalizesHexToUppercase()
        {
            Assert.True(TutorialColor.TryParse("#ff0000", out TutorialColor color));
            Assert.Equal("#FF0000", color.Format());
        }

        [Theory]
        [InlineData("#46250")]
        [InlineData("#4625000")]
        [InlineData("46,37")]
        [InlineData("70,37,256")]
        [InlineData("70,37,-1")]
        [InlineData("banana")]
        public void MalformedColorsFail(string value)
        {
            Assert.False(TutorialColor.TryParse(value, out _));
        }

        /// <summary>Look defaults are fully opaque, unrotated, unscaled, with no color override.</summary>
        [Fact]
        public void LookDefaults()
        {
            TutorialLook look = TutorialLook.For(Prompt("tutorialText"));

            Assert.Equal(1.0, look.Opacity);
            Assert.Null(look.Color);
            Assert.Equal(0.0, look.Angle);
            Assert.Equal(1.0, look.Size);
            Assert.Equal(1.0, look.LineHeight);
        }

        /// <summary>Authored values are read, and an out-of-range opacity falls back rather than throwing.</summary>
        [Fact]
        public void AuthoredValuesAreReadLeniently()
        {
            TutorialLook look = TutorialLook.For(Prompt(
                "tutorialText",
                ("opacity", "0.5"),
                ("angle", "-30"),
                ("size", "1.4"),
                ("lineHeight", "1.2"),
                ("color", "#FF0000")));

            Assert.Equal(0.5, look.Opacity);
            Assert.Equal(-30.0, look.Angle);
            Assert.Equal(1.4, look.Size);
            Assert.Equal(1.2, look.LineHeight);
            Assert.Equal((byte)255, look.Color!.Value.Red);

            Assert.Equal(1.0, TutorialLook.For(Prompt("tutorialText", ("opacity", "5"))).Opacity);
            Assert.Equal(1.0, TutorialLook.For(Prompt("tutorialText", ("size", "0"))).Size);
        }

        /// <summary>With no authored color, the dark canvas still gets white and the light canvas black.</summary>
        [Fact]
        public void EffectiveColorFallsBackToDarkCanvasInvertWhenUnauthored()
        {
            TutorialLook look = TutorialLook.For(Prompt("tutorialText"));

            TutorialColor onDark = look.EffectiveColor(dark: true);
            TutorialColor onLight = look.EffectiveColor(dark: false);

            Assert.Equal((byte)255, onDark.Red);
            Assert.Equal((byte)255, onDark.Green);
            Assert.Equal((byte)255, onDark.Blue);
            Assert.Equal((byte)0, onLight.Red);
            Assert.Equal((byte)0, onLight.Green);
            Assert.Equal((byte)0, onLight.Blue);
        }

        /// <summary>An authored color wins over the dark-canvas invert in both directions.</summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AuthoredColorSupersedesDarkCanvasInvert(bool dark)
        {
            TutorialLook look = TutorialLook.For(Prompt("tutorialText", ("color", "#8B4513")));

            TutorialColor effective = look.EffectiveColor(dark);

            Assert.Equal((byte)0x8B, effective.Red);
            Assert.Equal((byte)0x45, effective.Green);
            Assert.Equal((byte)0x13, effective.Blue);
        }
    }
}
