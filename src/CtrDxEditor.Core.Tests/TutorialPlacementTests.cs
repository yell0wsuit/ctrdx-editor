using CtrDxEditor.Core.Descriptors;
using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Placing a tutorial stamps locale="en" and carries the descriptor defaults.</summary>
    public class TutorialPlacementTests
    {
        private static LevelDocument EmptyDoc()
        {
            return LevelDocument.Parse("<map><layer name=\"settings\"><map gridSize=\"32\" width=\"640\" height=\"480\"/>"
                + "<gameDesign twoParts=\"false\"/></layer><layer name=\"Objects\"/></map>");
        }

        /// <summary>Adds the English locale and angle default to a placed tutorial icon.</summary>
        [Fact]
        public void PlacedIconGetsEnglishLocaleAndAngle()
        {
            LevelDocument doc = EmptyDoc();
            LevelObject o = Placement.CreateObject(DescriptorTable.CtrObjects.For("tutorial01")!, 10, 20);
            LevelObjectPolicy.ApplyDefaults(o, doc);
            Assert.Equal("en", o.GetAttr("locale"));
            Assert.Equal("0", o.GetAttr("angle"));
        }

        /// <summary>Adds English locale, width, and placeholder defaults to placed tutorial text.</summary>
        [Fact]
        public void PlacedTextGetsLocaleWidthAndPlaceholder()
        {
            LevelDocument doc = EmptyDoc();
            LevelObject o = Placement.CreateObject(DescriptorTable.CtrObjects.For("tutorialText")!, 10, 20);
            LevelObjectPolicy.ApplyDefaults(o, doc);
            Assert.Equal("en", o.GetAttr("locale"));
            Assert.Equal("140", o.GetAttr("width"));
            Assert.Equal("Tutorial text", o.GetAttr("text"));
        }
    }
}
