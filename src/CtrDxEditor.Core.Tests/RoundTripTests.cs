using System.IO;
using System.Linq;
using System.Xml.Linq;

using CtrDxEditor.Core.Document;

using Xunit;

namespace CtrDxEditor.Core.Tests
{
    /// <summary>Tests for preserving level XML during load/save round trips.</summary>
    public class RoundTripTests
    {
        /// <summary>Verifies that loading and saving fixture levels preserves XML semantics.</summary>
        [Theory]
        [InlineData("TestData/2_21.xml")]
        [InlineData("TestData/5_1.xml")]
        [InlineData("TestData/16_5.xml")] // official level with spinner (path="0,0"+rotateSpeed) and plain bouncers
        public void LoadThenSaveIsSemanticallyIdentical(string path)
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

        /// <summary>Verifies that localized layer data survives round-trip serialization.</summary>
        [Fact]
        public void LocaleLayersSurviveRoundTrip()
        {
            LevelDocument doc = LevelDocument.Parse(File.ReadAllText("TestData/5_1.xml"));
            XDocument after = XDocument.Parse(doc.Save());

            string[] localeLayers = [.. after.Root!.Elements("layer")
                .Select(l => (string?)l.Attribute("name") ?? string.Empty)];
            Assert.Contains("Ru", localeLayers);
            Assert.Contains("Ja", localeLayers);
        }

        /// <summary>Star timeout values survive round-trip as authored, including decimals and zero.</summary>
        [Fact]
        public void StarTimeoutsSurviveRoundTrip()
        {
            const string original =
                "<level width=\"640\" height=\"480\">" +
                "<star x=\"100\" y=\"120\" timeout=\"4.5\" />" +
                "<star x=\"200\" y=\"220\" timeout=\"0\" />" +
                "</level>";

            LevelDocument doc = LevelDocument.Parse(original);
            XDocument after = XDocument.Parse(doc.Save());

            string[] timeouts = [.. after.Root!.Elements("star").Select(s => (string?)s.Attribute("timeout") ?? string.Empty)];
            Assert.Equal(["4.5", "0"], timeouts);
        }

        /// <summary>Optional pollen visibility remains absent unless authored and preserves an authored value.</summary>
        [Fact]
        public void GrabHidePathRoundTripsWithoutInjectingDefault()
        {
            const string original =
                "<level width='640' height='480'>" +
                "<grab x='100' y='120' path='100,0' moveSpeed='50' />" +
                "<grab x='200' y='220' path='RC40' moveSpeed='50' hidePath='true' />" +
                "</level>";

            XDocument after = XDocument.Parse(LevelDocument.Parse(original).Save());
            XElement[] grabs = [.. after.Root!.Elements("grab")];

            Assert.Null(grabs[0].Attribute("hidePath"));
            Assert.Equal("true", (string?)grabs[1].Attribute("hidePath"));
        }

        // Re-serialize with normalized whitespace so DeepEquals compares structure, not formatting.
        private static XDocument Normalize(XDocument d)
        {
            return XDocument.Parse(d.ToString(SaveOptions.None));
        }
    }
}
