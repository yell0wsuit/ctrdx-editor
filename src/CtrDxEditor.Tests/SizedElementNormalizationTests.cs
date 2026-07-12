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
            return doc.Objects[0].Type;
        }

        /// <inheritdoc/>
        [Fact]
        public void SpikeTagRenamedToMatchSizeAttribute()
        {
            LevelDocument doc = Load("<spike2 x=\"10\" y=\"20\" size=\"3\" />");

            Assert.Equal("spike3", TypeOfFirst(doc));
            Assert.Equal("3", doc.Objects[0].GetAttr("size"));
        }

        /// <inheritdoc/>
        [Fact]
        public void BouncerTagRenamedToMatchSizeAttribute()
        {
            LevelDocument doc = Load("<bouncer1 x=\"10\" y=\"20\" size=\"2\" />");

            Assert.Equal("bouncer2", TypeOfFirst(doc));
            Assert.Equal("2", doc.Objects[0].GetAttr("size"));
        }

        /// <inheritdoc/>
        [Fact]
        public void MatchingSpikeTagLeftUnchanged()
        {
            LevelDocument doc = Load("<spike3 x=\"10\" y=\"20\" size=\"3\" />");

            Assert.Equal("spike3", TypeOfFirst(doc));
        }

        /// <inheritdoc/>
        [Fact]
        public void ElectroTagLeftUnchanged()
        {
            LevelDocument doc = Load("<electro x=\"10\" y=\"20\" size=\"5\" />");

            Assert.Equal("electro", TypeOfFirst(doc));
        }

        /// <inheritdoc/>
        [Fact]
        public void SpikeWithoutSizeAttributeLeftUnchanged()
        {
            LevelDocument doc = Load("<spike3 x=\"10\" y=\"20\" />");

            Assert.Equal("spike3", TypeOfFirst(doc));
            Assert.Null(doc.Objects[0].GetAttr("size"));
        }

        /// <inheritdoc/>
        [Fact]
        public void SpikeWithOutOfRangeSizeLeftUnchanged()
        {
            LevelDocument doc = Load("<spike2 x=\"10\" y=\"20\" size=\"9\" />");

            Assert.Equal("spike2", TypeOfFirst(doc));
        }

        /// <inheritdoc/>
        [Fact]
        public void BouncerWithOutOfRangeSizeLeftUnchanged()
        {
            LevelDocument doc = Load("<bouncer2 x=\"10\" y=\"20\" size=\"5\" />");

            Assert.Equal("bouncer2", TypeOfFirst(doc));
        }

        /// <inheritdoc/>
        [Fact]
        public void ReportsChangedWhenATagIsRenamed()
        {
            LevelDocument doc = ParseWithoutNormalizing("<spike2 x=\"10\" y=\"20\" size=\"3\" />");

            Assert.True(LevelObjectPolicy.NormalizeSizedElements(doc));
        }

        /// <inheritdoc/>
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
