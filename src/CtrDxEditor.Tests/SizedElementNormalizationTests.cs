using CtrDxEditor.Core.Document;
using CtrDxEditor.Core.Editing;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Verifies that spike/bouncer tag names are normalized to match a valid <c>size</c>
    /// attribute. The game reads size only from the attribute, so renaming the tag to match is
    /// behavior-preserving (see Spikes.GetSpikeTextureAndQuad / Bouncer).
    /// </summary>
    public class SizedElementNormalizationTests
    {
        private static LevelDocument Load(string objectsXml)
        {
            string xml =
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" /></layer>" +
                "<layer name=\"Objects\">" + objectsXml + "</layer></map>";
            LevelDocument doc = LevelDocument.Parse(xml);
            _ = LevelObjectPolicy.NormalizeSizedElements(doc);
            return doc;
        }

        private static string TypeOfFirst(LevelDocument doc)
        {
            return doc.AllObjects[0].Type;
        }

        /// <summary>A spike tag disagreeing with its <c>size</c> attribute is renamed to match, since the game reads the size only from the attribute.</summary>
        [Fact]
        public void SpikeTagRenamedToMatchSizeAttribute()
        {
            LevelDocument doc = Load("<spike2 x=\"10\" y=\"20\" size=\"3\" />");

            Assert.Equal("spike3", TypeOfFirst(doc));
            Assert.Equal("3", doc.AllObjects[0].GetAttr("size"));
        }

        /// <summary>Bouncers normalize the same way as spikes, so the two sized elements stay consistent.</summary>
        [Fact]
        public void BouncerTagRenamedToMatchSizeAttribute()
        {
            LevelDocument doc = Load("<bouncer1 x=\"10\" y=\"20\" size=\"2\" />");

            Assert.Equal("bouncer2", TypeOfFirst(doc));
            Assert.Equal("2", doc.AllObjects[0].GetAttr("size"));
        }

        /// <summary>A tag that already agrees with its size is left alone, so normalization is idempotent.</summary>
        [Fact]
        public void MatchingSpikeTagLeftUnchanged()
        {
            LevelDocument doc = Load("<spike3 x=\"10\" y=\"20\" size=\"3\" />");

            Assert.Equal("spike3", TypeOfFirst(doc));
        }

        /// <summary>Electro carries a <c>size</c> attribute but has no per-size tag, so it must be exempt from renaming.</summary>
        [Fact]
        public void ElectroTagLeftUnchanged()
        {
            LevelDocument doc = Load("<electro x=\"10\" y=\"20\" size=\"5\" />");

            Assert.Equal("electro", TypeOfFirst(doc));
        }

        /// <summary>With no size attribute there is nothing to match, so the tag and the absent attribute both survive.</summary>
        [Fact]
        public void SpikeWithoutSizeAttributeLeftUnchanged()
        {
            LevelDocument doc = Load("<spike3 x=\"10\" y=\"20\" />");

            Assert.Equal("spike3", TypeOfFirst(doc));
            Assert.Null(doc.AllObjects[0].GetAttr("size"));
        }

        /// <summary>An out-of-range size has no corresponding tag, so the element is left as-is rather than renamed to a nonexistent one.</summary>
        [Fact]
        public void SpikeWithOutOfRangeSizeLeftUnchanged()
        {
            LevelDocument doc = Load("<spike2 x=\"10\" y=\"20\" size=\"9\" />");

            Assert.Equal("spike2", TypeOfFirst(doc));
        }

        /// <summary>Bouncers reject out-of-range sizes the same way spikes do.</summary>
        [Fact]
        public void BouncerWithOutOfRangeSizeLeftUnchanged()
        {
            LevelDocument doc = Load("<bouncer2 x=\"10\" y=\"20\" size=\"5\" />");

            Assert.Equal("bouncer2", TypeOfFirst(doc));
        }

        /// <summary>The method reports true when it rewrites a tag, which is what marks the document modified.</summary>
        [Fact]
        public void ReportsChangedWhenATagIsRenamed()
        {
            LevelDocument doc = ParseWithoutNormalizing("<spike2 x=\"10\" y=\"20\" size=\"3\" />");

            Assert.True(LevelObjectPolicy.NormalizeSizedElements(doc));
        }

        /// <summary>The method reports false when nothing moved, so merely opening a level does not mark it dirty.</summary>
        [Fact]
        public void ReportsUnchangedWhenAllTagsAlreadyMatch()
        {
            LevelDocument doc = ParseWithoutNormalizing("<spike3 x=\"10\" y=\"20\" size=\"3\" /><electro x=\"5\" y=\"5\" size=\"5\" />");

            Assert.False(LevelObjectPolicy.NormalizeSizedElements(doc));
        }

        private static LevelDocument ParseWithoutNormalizing(string objectsXml)
        {
            return LevelDocument.Parse(
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" /></layer>" +
                "<layer name=\"Objects\">" + objectsXml + "</layer></map>");
        }
    }
}
