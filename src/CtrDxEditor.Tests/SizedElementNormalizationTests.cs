using System.Linq;

using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Tests
{
    /// <summary>
    /// Verifies that on load, spike/bouncer tag names are normalized to match a valid
    /// <c>size</c> attribute. The game reads size only from the attribute, so renaming the
    /// tag to match is behavior-preserving (see Spikes.GetSpikeTextureAndQuad / Bouncer).
    /// </summary>
    public class SizedElementNormalizationTests
    {
        private static LevelDocument Load(string objectsXml)
        {
            string xml =
                "<map><layer name=\"settings\"><map gridSize=\"32\" width=\"100\" height=\"80\" /></layer>" +
                "<layer name=\"Objects\">" + objectsXml + "</layer></map>";
            return LevelDocument.Parse(xml);
        }

        private static string TypeOfFirst(LevelDocument doc)
        {
            return doc.Objects.First().Type;
        }

        [Fact]
        public void SpikeTagRenamedToMatchSizeAttribute()
        {
            LevelDocument doc = Load("<spike2 x=\"10\" y=\"20\" size=\"3\" />");

            Assert.Equal("spike3", TypeOfFirst(doc));
            Assert.Equal("3", doc.Objects.First().GetAttr("size"));
        }

        [Fact]
        public void BouncerTagRenamedToMatchSizeAttribute()
        {
            LevelDocument doc = Load("<bouncer1 x=\"10\" y=\"20\" size=\"2\" />");

            Assert.Equal("bouncer2", TypeOfFirst(doc));
            Assert.Equal("2", doc.Objects.First().GetAttr("size"));
        }

        [Fact]
        public void MatchingSpikeTagLeftUnchanged()
        {
            LevelDocument doc = Load("<spike3 x=\"10\" y=\"20\" size=\"3\" />");

            Assert.Equal("spike3", TypeOfFirst(doc));
        }

        [Fact]
        public void ElectroTagLeftUnchanged()
        {
            LevelDocument doc = Load("<electro x=\"10\" y=\"20\" size=\"5\" />");

            Assert.Equal("electro", TypeOfFirst(doc));
        }

        [Fact]
        public void SpikeWithoutSizeAttributeLeftUnchanged()
        {
            LevelDocument doc = Load("<spike3 x=\"10\" y=\"20\" />");

            Assert.Equal("spike3", TypeOfFirst(doc));
            Assert.Null(doc.Objects.First().GetAttr("size"));
        }

        [Fact]
        public void SpikeWithOutOfRangeSizeLeftUnchanged()
        {
            LevelDocument doc = Load("<spike2 x=\"10\" y=\"20\" size=\"9\" />");

            Assert.Equal("spike2", TypeOfFirst(doc));
        }

        [Fact]
        public void BouncerWithOutOfRangeSizeLeftUnchanged()
        {
            LevelDocument doc = Load("<bouncer2 x=\"10\" y=\"20\" size=\"5\" />");

            Assert.Equal("bouncer2", TypeOfFirst(doc));
        }
    }
}
