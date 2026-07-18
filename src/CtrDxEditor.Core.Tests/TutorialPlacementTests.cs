using System.Xml.Linq;

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
            Assert.Equal("Text", o.GetAttr("text"));
            Assert.True(TutorialObject.IsAutoWidth(o));
        }

        /// <summary>Keeps the editor's auto-width mode out of game level XML.</summary>
        [Fact]
        public void PlacedAutoWidthTextDoesNotSerializeEditorState()
        {
            LevelDocument doc = EmptyDoc();
            LevelObject o = Placement.CreateObject(DescriptorTable.CtrObjects.For("tutorialText")!, 10, 20);
            LevelObjectPolicy.ApplyDefaults(o, doc);
            doc.Add(o, doc.Layers[0]);

            Assert.True(TutorialObject.IsAutoWidth(o));
            Assert.Null(o.GetAttr("autoWidth"));
            Assert.DoesNotContain("autoWidth", doc.Save());
        }

        /// <summary>Strips editor-only state emitted by pre-release editor builds during export.</summary>
        [Fact]
        public void LegacyAutoWidthAttributeIsNotSerialized()
        {
            const string xml = "<map><layer name=\"Objects\">"
                + "<tutorialText x=\"10\" y=\"20\" text=\"Text\" width=\"40\" autoWidth=\"true\"/>"
                + "</layer></map>";

            LevelDocument doc = LevelDocument.Parse(xml);

            Assert.DoesNotContain("autoWidth", doc.Save());
        }

        /// <summary>Preserves tutorial attributes and non-English sibling layers without mutation.</summary>
        [Fact]
        public void MultiLocaleTutorialMapRoundTripsUnchanged()
        {
            const string xml =
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"640\" height=\"480\"/>"
                + "<gameDesign twoParts=\"false\"/></layer>"
                + "<layer name=\"Objects\">"
                + "<tutorialText x=\"215\" y=\"331\" locale=\"en\" text=\"TUTORIAL_LVL_8_1_01\" width=\"140\"/>"
                + "<tutorial04 x=\"222\" y=\"429\" locale=\"en\" angle=\"35\" moveSpeed=\"100\" rotateSpeed=\"100\"/>"
                + "</layer>"
                + "<layer name=\"Ru\">"
                + "<tutorialText x=\"229\" y=\"328\" locale=\"ru\" text=\"TUTORIAL_LVL_8_1_01\" width=\"140\"/>"
                + "</layer></map>";

            LevelDocument doc = LevelDocument.Parse(xml);
            XDocument before = XDocument.Parse(xml);
            XDocument after = XDocument.Parse(doc.Save());

            Assert.True(XNode.DeepEquals(before, after));
        }
    }
}
