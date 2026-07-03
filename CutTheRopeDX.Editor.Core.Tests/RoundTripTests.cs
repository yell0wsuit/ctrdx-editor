using System.IO;
using System.Linq;
using System.Xml.Linq;

using CutTheRopeDX.Editor.Core.Document;

using Xunit;

namespace CutTheRopeDX.Editor.Core.Tests
{
    public class RoundTripTests
    {
        [Theory]
        [InlineData("TestData/2_21.xml")]
        [InlineData("TestData/5_1.xml")]
        public void Load_then_save_is_semantically_identical(string path)
        {
            string original = File.ReadAllText(path);
            LevelDocument doc = LevelDocument.Parse(original);

            string saved = doc.Save();

            // Semantic identity: same element/attribute tree, ignoring insignificant whitespace.
            XDocument before = XDocument.Parse(original);
            XDocument after = XDocument.Parse(saved);
            Assert.True(XNode.DeepEquals(Normalize(before), Normalize(after)),
                $"Round-trip changed the document for {path}.");
        }

        [Fact]
        public void Locale_layers_survive_round_trip()
        {
            LevelDocument doc = LevelDocument.Parse(File.ReadAllText("TestData/5_1.xml"));
            XDocument after = XDocument.Parse(doc.Save());

            string[] localeLayers = [.. after.Root!.Elements("layer")
                .Select(l => (string?)l.Attribute("name") ?? string.Empty)];
            Assert.Contains("Ru", localeLayers);
            Assert.Contains("Ja", localeLayers);
        }

        // Re-serialize with normalized whitespace so DeepEquals compares structure, not formatting.
        private static XDocument Normalize(XDocument d)
        {
            return XDocument.Parse(d.ToString(SaveOptions.None));
        }
    }
}
